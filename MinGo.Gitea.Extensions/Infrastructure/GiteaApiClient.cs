using MinGo.GiteaExtensions.Core.Interfaces;
using MinGo.GiteaExtensions.Core.Models;
using Newtonsoft.Json;

namespace MinGo.GiteaExtensions.Infrastructure;

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
        var repos = JsonConvert.DeserializeObject<List<dynamic>>(content);
        
        return repos.Select(r => new Repository
        {
            Id = r.id,
            Name = r.name,
            FullName = r.full_name,
            Owner = r.owner.login,
            HtmlUrl = r.html_url
        });
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/branches");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var branches = JsonConvert.DeserializeObject<List<dynamic>>(content);
        
        return branches.Select(b => new Branch
        {
            Name = b.name,
            Commit = new Commit
            {
                Sha = b.commit.sha,
                Url = b.commit.url
            }
        });
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/actions/workflows");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(content);
        var workflows = result?.workflows as IEnumerable<dynamic>;
        
        return workflows?.Select(w => new Workflow
        {
            Id = w.id,
            Name = w.name,
            Path = w.path,
            State = w.state
        }) ?? Enumerable.Empty<Workflow>();
    }

    public async Task<WorkflowRun> TriggerWorkflowAsync(string owner, string repo, string workflowId, string branch, Dictionary<string, string> inputs = null)
    {
        var requestBody = new
        {
            @ref = branch,
            inputs = inputs
        };
        
        var content = new StringContent(JsonConvert.SerializeObject(requestBody), System.Text.Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync($"/api/v1/repos/{owner}/{repo}/actions/workflows/{workflowId}/dispatches", content);
        response.EnsureSuccessStatusCode();
        
        // Gitea returns 204 No Content on success, so we need to get the latest run
        var runs = await GetWorkflowRunsAsync(owner, repo);
        return runs.FirstOrDefault();
    }

    public async Task<WorkflowRun> GetWorkflowRunAsync(string owner, string repo, int runId)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/actions/runs/{runId}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var run = JsonConvert.DeserializeObject<dynamic>(content);
        
        return new WorkflowRun
        {
            Id = run.id,
            Name = run.name,
            Status = run.status,
            Conclusion = run.conclusion,
            CreatedAt = DateTime.Parse(run.created_at.ToString()),
            UpdatedAt = DateTime.Parse(run.updated_at.ToString()),
            Branch = run.head_branch
        };
    }

    public async Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo)
    {
        var response = await _httpClient.GetAsync($"/api/v1/repos/{owner}/{repo}/actions/runs");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<dynamic>(content);
        var runs = result?.workflow_runs as IEnumerable<dynamic>;
        
        return runs?.Select(r => new WorkflowRun
        {
            Id = r.id,
            Name = r.name,
            Status = r.status,
            Conclusion = r.conclusion,
            CreatedAt = DateTime.Parse(r.created_at.ToString()),
            UpdatedAt = DateTime.Parse(r.updated_at.ToString()),
            Branch = r.head_branch
        }) ?? Enumerable.Empty<WorkflowRun>();
    }
}