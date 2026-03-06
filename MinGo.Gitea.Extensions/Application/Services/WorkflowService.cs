using MinGo.GiteaExtensions.Core.Interfaces;
using MinGo.GiteaExtensions.Core.Models;
using MinGo.GiteaExtensions.Infrastructure.Data;

namespace MinGo.GiteaExtensions.Application.Services;

public class WorkflowService
{
    private readonly IGiteaApiClient _apiClient;
    private readonly GiteaExtensionsDbContext _dbContext;

    public WorkflowService(IGiteaApiClient apiClient, GiteaExtensionsDbContext dbContext = null)
    {
        _apiClient = apiClient;
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<Repository>> GetRepositoriesAsync()
    {
        return await _apiClient.GetRepositoriesAsync();
    }

    public async Task<IEnumerable<Branch>> GetBranchesAsync(string owner, string repo)
    {
        return await _apiClient.GetBranchesAsync(owner, repo);
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo)
    {
        return await _apiClient.GetWorkflowsAsync(owner, repo);
    }

    public async Task<Repository> GetRepositoryByNameAsync(string fullName)
    {
        var repositories = await GetRepositoriesAsync();
        return repositories.FirstOrDefault(r => r.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<WorkflowRun> TriggerWorkflowAsync(string owner, string repo, string workflowId, string branch, Dictionary<string, string> inputs = null)
    {
        var run = await _apiClient.TriggerWorkflowAsync(owner, repo, workflowId, branch, inputs);
        
        // 记录执行历史
        if (_dbContext != null && run != null)
        {
            var history = new WorkflowRunHistory
            {
                Owner = owner,
                Repo = repo,
                WorkflowId = workflowId,
                WorkflowName = run.Name,
                Branch = branch,
                RunId = run.Id,
                Status = run.Status,
                Conclusion = run.Conclusion,
                CreatedAt = run.CreatedAt,
                UpdatedAt = run.UpdatedAt,
                TriggerType = "手动触发"
            };
            
            _dbContext.WorkflowRunHistories.Add(history);
            await _dbContext.SaveChangesAsync();
        }
        
        return run;
    }

    public async Task<WorkflowRun> GetWorkflowRunAsync(string owner, string repo, int runId)
    {
        return await _apiClient.GetWorkflowRunAsync(owner, repo, runId);
    }

    public async Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo)
    {
        return await _apiClient.GetWorkflowRunsAsync(owner, repo);
    }
}