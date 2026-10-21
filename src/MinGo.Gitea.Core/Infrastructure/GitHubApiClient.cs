using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;

namespace MinGo.Gitea.Infrastructure;

/// <summary>GitHub Actions REST API 客户端</summary>
public class GitHubApiClient : IActionsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ProviderConnection _config;

    public ProviderType ProviderType => ProviderType.GitHub;
    public int ConnectionId => _config.Id;

    public GitHubApiClient(IHttpClientFactory factory, ProviderConnection config)
    {
        _httpClient = factory.CreateClient("GitHubApiClient");
        _config = config;
        if (string.IsNullOrWhiteSpace(_config.ServerUrl))
        {
            _config.ServerUrl = "https://api.github.com";
        }
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint, string? accept = null)
    {
        ArgumentNullException.ThrowIfNull(_config.ServerUrl);
        ArgumentNullException.ThrowIfNull(_config.Token);

        var baseUrl = _config.ServerUrl.TrimEnd('/');
        if (!baseUrl.Contains("github.com") && !baseUrl.Contains("api."))
        {
            // GitHub Enterprise: 用户可能填 https://github.example.com，补 API 前缀
            if (!baseUrl.EndsWith("/api/v3", StringComparison.OrdinalIgnoreCase))
            {
                baseUrl += "/api/v3";
            }
        }

        var uri = new Uri(baseUrl + endpoint);
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Authorization", $"Bearer {_config.Token}");
        request.Headers.Add("Accept", accept ?? "application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Headers.Add("User-Agent", "MinGo-Gitea-Extensions");
        return request;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "/user");
            var response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Repository>> GetRepositoriesAsync()
    {
        var request = CreateRequest(HttpMethod.Get, "/user/repos?per_page=100&affiliation=owner,collaborator,organization_member");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var repos = JsonConvert.DeserializeObject<List<GitHubRepository>>(content);

        return repos?.Select(r => new Repository
        {
            Id = (int)r.Id,
            Name = r.Name,
            FullName = r.FullName,
            Owner = r.Owner.Login,
            HtmlUrl = r.HtmlUrl,
            ConnectionId = _config.Id,
            Provider = ProviderType.GitHub
        }) ?? Enumerable.Empty<Repository>();
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(string owner, string repo)
    {
        var request = CreateRequest(HttpMethod.Get, $"/repos/{owner}/{repo}/branches?per_page=100");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var branches = JsonConvert.DeserializeObject<List<GitHubBranch>>(content);

        return branches?.Select(b => new Branch
        {
            Name = b.Name,
            Commit = new Commit { Sha = b.Commit.Sha, Url = b.Commit.Url ?? "" }
        }) ?? Enumerable.Empty<Branch>();
    }

    public async Task<IEnumerable<Ref>> GetRefsAsync(string owner, string repo)
    {
        var result = new List<Ref>();

        foreach (var (prefix, type) in new[] { ("heads", "branch"), ("tags", "tag") })
        {
            var request = CreateRequest(HttpMethod.Get, $"/repos/{owner}/{repo}/git/matching-refs/{prefix}/");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) continue;
            var content = await response.Content.ReadAsStringAsync();
            var refs = JsonConvert.DeserializeObject<List<GitHubRef>>(content);
            if (refs == null) continue;

            result.AddRange(refs.Select(r => new Ref
            {
                Name = r.Ref,
                Type = type,
                Commit = new() { Sha = r.Object.Sha, Url = r.Object.Url ?? "" }
            }));
        }

        return result;
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo)
    {
        var request = CreateRequest(HttpMethod.Get, $"/repos/{owner}/{repo}/actions/workflows?per_page=100");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GitHubWorkflowListResponse>(content);

        return result?.Workflows?.Select(w => new Workflow
        {
            Id = w.Id.ToString(),
            Name = w.Name ?? w.Path ?? w.Id.ToString(),
            Path = w.Path ?? "",
            State = w.State ?? "active",
            ConnectionId = _config.Id,
            Provider = ProviderType.GitHub
        }) ?? Enumerable.Empty<Workflow>();
    }

    public async Task<string> GetWorkflowFileContentAsync(string owner, string repo, string path)
    {
        // path 形如 .github/workflows/foo.yml
        var normalized = path.Replace("\\", "/").TrimStart('/');
        var request = CreateRequest(HttpMethod.Get, $"/repos/{owner}/{repo}/contents/{normalized}", "application/vnd.github.raw");
        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return string.Empty;
        }
        return await response.Content.ReadAsStringAsync();
    }

    public async Task TriggerWorkflowAsync(string owner, string repo, string workflowId, string @ref, Dictionary<string, string>? inputs = null)
    {
        var body = new GitHubWorkflowDispatchRequest { Ref = @ref, Inputs = inputs };
        var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
        var request = CreateRequest(HttpMethod.Post, $"/repos/{owner}/{repo}/actions/workflows/{workflowId}/dispatches");
        request.Content = content;
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string owner, string repo, long runId)
    {
        var request = CreateRequest(HttpMethod.Get, $"/repos/{owner}/{repo}/actions/runs/{runId}");
        var response = await _httpClient.SendAsync(request);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var run = JsonConvert.DeserializeObject<GitHubWorkflowRun>(content);
        if (run == null) return null;

        return MapRun(run);
    }

    public async Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo, int? page = null, int? limit = null, string? status = null, string? branch = null, string? @event = null, int? workflowId = null, int? actorId = null, string? created = null, string? headSha = null)
    {
        var queryParams = new List<string>
        {
            $"per_page={limit ?? 30}"
        };
        if (page.HasValue) queryParams.Add($"page={page}");
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrEmpty(branch)) queryParams.Add($"branch={Uri.EscapeDataString(branch)}");
        if (!string.IsNullOrEmpty(@event)) queryParams.Add($"event={Uri.EscapeDataString(@event)}");
        if (!string.IsNullOrEmpty(created)) queryParams.Add($"created={Uri.EscapeDataString(created)}");
        if (!string.IsNullOrEmpty(headSha)) queryParams.Add($"head_sha={Uri.EscapeDataString(headSha)}");

        // 优先走 workflow 专属 runs 接口，避免混入其它 workflow 的 run
        string endpoint;
        if (workflowId.HasValue)
        {
            endpoint = $"/repos/{owner}/{repo}/actions/workflows/{workflowId}/runs?{string.Join("&", queryParams)}";
        }
        else
        {
            endpoint = $"/repos/{owner}/{repo}/actions/runs?{string.Join("&", queryParams)}";
        }
        var request = CreateRequest(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GitHubWorkflowRunListResponse>(content);

        IEnumerable<WorkflowRun> runs = result?.WorkflowRuns?.Select(MapRun) ?? [];
        return runs;
    }

    public async Task<Ref?> GetRefAsync(string owner, string repo, string @ref)
    {
        var normalized = @ref.StartsWith("refs/") ? @ref["refs/".Length..] : @ref;
        // 优先按 branch 查
        foreach (var prefix in new[] { "heads", "tags" })
        {
            var request = CreateRequest(HttpMethod.Get, $"/repos/{owner}/{repo}/git/ref/{prefix}/{normalized}");
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) continue;
            var content = await response.Content.ReadAsStringAsync();
            var gitHubRef = JsonConvert.DeserializeObject<GitHubRef>(content);
            if (gitHubRef == null) continue;

            return new Ref
            {
                Type = prefix == "heads" ? "branch" : "tag",
                Name = @ref.StartsWith("refs/") ? @ref : $"refs/{prefix}/{normalized}",
                Commit = new() { Sha = gitHubRef.Object.Sha, Url = gitHubRef.Object.Url ?? "" }
            };
        }
        return null;
    }

    private WorkflowRun MapRun(GitHubWorkflowRun r)
    {
        DateTime Parse(string? s) => s != null && DateTime.TryParse(s, out var dt) ? dt : DateTime.MinValue;

        return new WorkflowRun
        {
            Id = r.Id,
            Name = r.DisplayTitle ?? r.Name ?? string.Empty,
            Status = r.Status ?? string.Empty,
            Conclusion = r.Conclusion ?? string.Empty,
            CreatedAt = Parse(r.RunStartedAt ?? r.CreatedAt),
            UpdatedAt = Parse(r.UpdatedAt),
            Ref = r.HeadBranch ?? string.Empty,
            CommitSha = r.HeadSha ?? string.Empty,
            ConnectionId = _config.Id,
            Provider = ProviderType.GitHub,
            HtmlUrl = r.HtmlUrl
        };
    }
}
