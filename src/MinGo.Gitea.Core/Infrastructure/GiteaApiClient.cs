using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;
using Newtonsoft.Json;

namespace MinGo.Gitea.Infrastructure;

public class GiteaApiClient : IGiteaApiClient
{
    private readonly HttpClient _httpClient;
    private readonly GiteaConfig _config;

    public GiteaApiClient(HttpClient httpClient, GiteaConfig config)
    {
        _httpClient = httpClient;
        _config = config ?? throw new ArgumentNullException(nameof(config));
        
        if (string.IsNullOrEmpty(config.ServerUrl))
        {
            throw new ArgumentNullException(nameof(config.ServerUrl), "ServerUrl cannot be null or empty");
        }
        
        _httpClient.BaseAddress = new Uri(_config.ServerUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {_config.Token}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/v1/user");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Repository>> GetRepositoriesAsync()
    {
        var response = await _httpClient.GetAsync($"/api/v1/user/repos");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var repos = JsonConvert.DeserializeObject<List<GiteaRepository>>(content);
        
        return repos?.Select(r => new Repository
        {
            Id = (int)r.Id,
            Name = r.Name,
            FullName = r.FullName,
            Owner = r.Owner.Login,
            HtmlUrl = r.HtmlUrl
        }) ?? Enumerable.Empty<Repository>();
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/branches");
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

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/actions/workflows");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GiteaWorkflowListResponse>(content);
        
        return result?.Workflows.Select(w => new Workflow
        {
            Id = w.Id,
            Name = w.Name,
            Path = w.Path,
            State = w.State
        }) ?? Enumerable.Empty<Workflow>();
    }

    public async Task<WorkflowRun?> TriggerWorkflowAsync(string owner, string repo, string workflowId, string branch, Dictionary<string, string>? inputs = null)
    {
        var requestBody = new GiteaWorkflowDispatchRequest
        {
            Ref = branch,
            Inputs = inputs
        };
        
        var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"/api/v1/repos/{owner}/{repo}/actions/workflows/{workflowId}/dispatches", content);
        response.EnsureSuccessStatusCode();
        
        // Gitea returns 204 No Content on success, so we need to get the latest run
        var runs = await GetWorkflowRunsAsync(owner, repo);
        return runs.FirstOrDefault();
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string owner, string repo, int runId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/actions/runs/{runId}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var run = JsonConvert.DeserializeObject<GiteaWorkflowRun>(content);
        
        if (run == null)
        {
            return null;
        }
        
        return new WorkflowRun
        {
            Id = (int)run.Id,
            Name = run.Name,
            Status = run.Status,
            Conclusion = run.Conclusion,
            CreatedAt = DateTime.Parse(run.CreatedAt),
            UpdatedAt = DateTime.Parse(run.UpdatedAt),
            Branch = run.HeadBranch
        };
    }

    public async Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/actions/runs");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<GiteaWorkflowRunListResponse>(content);
        
        return result?.WorkflowRuns.Select(r => new WorkflowRun
        {
            Id = (int)r.Id,
            Name = r.Name,
            Status = r.Status,
            Conclusion = r.Conclusion,
            CreatedAt = DateTime.Parse(r.CreatedAt),
            UpdatedAt = DateTime.Parse(r.UpdatedAt),
            Branch = r.HeadBranch
        }) ?? Enumerable.Empty<WorkflowRun>();
    }
}