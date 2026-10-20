using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;

using Newtonsoft.Json;

using System.Net.Http;

namespace MinGo.Gitea.Infrastructure;

public class GiteaApiClient : IGiteaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ProviderConnection _config;

    public ProviderType ProviderType => ProviderType.Gitea;
    public int ConnectionId => _config.Id;

    public GiteaApiClient(IHttpClientFactory factory, ProviderConnection config)
    {
        _httpClient = factory.CreateClient("GiteaApiClient");
        _config = config;
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string endpoint)
    {
        ArgumentNullException.ThrowIfNull(_config.ServerUrl, $"{nameof(ProviderConnection)}:{nameof(ProviderConnection.ServerUrl)}");
        ArgumentNullException.ThrowIfNull(_config.Token, $"{nameof(ProviderConnection)}:{nameof(ProviderConnection.Token)}");

        var uri = new Uri(new Uri(_config.ServerUrl), endpoint);
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("Authorization", $"token {_config.Token}");
        request.Headers.Add("Accept", "application/json");
        return request;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var request = CreateRequest(HttpMethod.Get, "/api/v1/user");
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
        var request = CreateRequest(HttpMethod.Get, "/api/v1/user/repos");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var repos = JsonConvert.DeserializeObject<List<GiteaRepository>>(content);

        return repos?.Select(r => new Repository
        {
            Id = (int)r.Id,
            Name = r.Name,
            FullName = r.FullName,
            Owner = r.Owner.Login,
            HtmlUrl = r.HtmlUrl,
            ConnectionId = _config.Id,
            Provider = ProviderType.Gitea
        }) ?? Enumerable.Empty<Repository>();
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(string owner, string repo)
    {
        var request = CreateRequest(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/branches");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var branches = JsonConvert.DeserializeObject<List<GiteaBranch>>(content);

        return branches?.Select(b => new Branch
        {
            Name = b.Name,
            Commit = new Commit
            {
                Sha = b.Commit.Sha,
                Url = b.Commit.Url
            }
        }) ?? Enumerable.Empty<Branch>();
    }

    public async Task<IEnumerable<Ref>> GetRefsAsync(string owner, string repo)
    {
        var request = CreateRequest(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/git/refs");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var refs = JsonConvert.DeserializeObject<List<GiteaRef>>(content);

        return refs?.Select(r => new Ref
        {
            Name = r.Ref,
            Type = r.Ref.StartsWith("refs/heads/") ? "branch" : "tag",
            Commit = new() { Sha = r.Object.Sha, Url = r.Object.Url }
        }) ?? Enumerable.Empty<Ref>();
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo)
    {
        var request = CreateRequest(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/actions/workflows");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GiteaWorkflowListResponse>(content);

        return result?.workflows.Select(w => new Workflow
        {
            Id = w.id,
            Name = w.name,
            Path = w.path,
            State = w.state,
            ConnectionId = _config.Id,
            Provider = ProviderType.Gitea
        }) ?? Enumerable.Empty<Workflow>();
    }

    public async Task<string> GetWorkflowFileContentAsync(string owner, string repo, string path)
    {
        var request = CreateRequest(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/contents/{path}");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var fileResult = JsonConvert.DeserializeObject<GiteaFileContent>(content);

        if (fileResult == null || string.IsNullOrEmpty(fileResult.Content))
        {
            return string.Empty;
        }

        var decodedContent = System.Text.Encoding.UTF8.GetString(
            Convert.FromBase64String(fileResult.Content.Replace("\n", ""))
        );

        return decodedContent;
    }

    public async Task TriggerWorkflowAsync(string owner, string repo, string workflowId, string @ref, Dictionary<string, string>? inputs = null)
    {
        var requestBody = new GiteaWorkflowDispatchRequest
        {
            Ref = @ref,
            Inputs = inputs
        };

        var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");
        var request = CreateRequest(HttpMethod.Post, $"/api/v1/repos/{owner}/{repo}/actions/workflows/{workflowId}/dispatches");
        request.Content = content;
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string owner, string repo, int runId)
    {
        var request = CreateRequest(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/actions/runs/{runId}");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var run = JsonConvert.DeserializeObject<GiteaWorkflowRun>(content);

        if (run == null)
        {
            return null;
        }

        return MapRun(run);
    }

    public async Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo, int? page = null, int? limit = null, string? status = null, string? branch = null, string? @event = null, int? workflowId = null, int? actorId = null, string? created = null, string? headSha = null)
    {
        var queryParams = new List<string>();
        if (page.HasValue) queryParams.Add($"page={page}");
        if (limit.HasValue) queryParams.Add($"limit={limit}");
        if (!string.IsNullOrEmpty(status)) queryParams.Add($"status={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrEmpty(branch)) queryParams.Add($"branch={Uri.EscapeDataString(branch)}");
        if (!string.IsNullOrEmpty(@event)) queryParams.Add($"event={Uri.EscapeDataString(@event)}");
        if (workflowId.HasValue) queryParams.Add($"workflow_id={workflowId}");
        if (actorId.HasValue) queryParams.Add($"actor_id={actorId}");
        if (!string.IsNullOrEmpty(created)) queryParams.Add($"created={Uri.EscapeDataString(created)}");
        if (!string.IsNullOrEmpty(headSha)) queryParams.Add($"head_sha={Uri.EscapeDataString(headSha)}");

        var endpoint = $"/api/v1/repos/{owner}/{repo}/actions/runs";
        if (queryParams.Count > 0)
        {
            endpoint += "?" + string.Join("&", queryParams);
        }

        var request = CreateRequest(HttpMethod.Get, endpoint);
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GiteaWorkflowRunListResponse>(content);

        return result?.workflow_runs?.Select(MapRun) ?? [];
    }

    public async Task<Ref?> GetRefAsync(string owner, string repo, string @ref)
    {
        if (@ref.StartsWith("refs/"))
        {
            @ref = @ref["refs/".Length..];
        }
        var request = CreateRequest(HttpMethod.Get, $"/api/v1/repos/{owner}/{repo}/git/refs/{@ref}");
        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var giteaRefs = JsonConvert.DeserializeObject<GiteaRef[]>(content);

        if (giteaRefs == null || giteaRefs.Length == 0)
        {
            return null;
        }

        var giteaRef = giteaRefs.First();

        return new Ref
        {
            Type = giteaRef.Ref.StartsWith("/refs/heads") ? "branch" : "tag",
            Name = giteaRef.Ref,
            Commit = new() { Sha = giteaRef.Object.Sha, Url = giteaRef.Object.Url }
        };
    }

    private WorkflowRun MapRun(GiteaWorkflowRun r) => new()
    {
        Id = (int)r.id,
        Name = r.display_title ?? string.Empty,
        Status = r.status ?? string.Empty,
        Conclusion = r.conclusion ?? string.Empty,
        CreatedAt = r.started_at != null ? DateTime.Parse(r.started_at) : DateTime.MinValue,
        UpdatedAt = r.completed_at != null ? DateTime.Parse(r.completed_at) : DateTime.MinValue,
        Ref = r.head_branch ?? string.Empty,
        CommitSha = r.head_sha ?? string.Empty,
        ConnectionId = _config.Id,
        Provider = ProviderType.Gitea
    };
}
