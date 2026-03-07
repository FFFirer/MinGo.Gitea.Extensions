using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;

namespace MinGo.Gitea.Application.Services;

public class WorkflowService
{
    public readonly IGiteaApiClient _apiClient;
    private readonly GiteaExtensionsDbContext? _dbContext;

    public WorkflowService(IGiteaApiClient apiClient, GiteaExtensionsDbContext? dbContext = null)
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

    public async Task<IEnumerable<Ref>> GetRefsAsync(string owner, string repo)
    {
        return await _apiClient.GetRefsAsync(owner, repo);
    }

    public async Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo)
    {
        return await _apiClient.GetWorkflowsAsync(owner, repo);
    }

    public async Task<Repository?> GetRepositoryByNameAsync(string fullName)
    {
        var repositories = await GetRepositoriesAsync();
        return repositories.FirstOrDefault(r => r.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<WorkflowRun?> TriggerWorkflowAsync(string owner, string repo, Workflow workflow, Ref @ref, string triggerType, Dictionary<string, string>? inputs = null)
    {
        await _apiClient.TriggerWorkflowAsync(owner, repo, workflow.Id, @ref.Name, inputs);

        await Task.Delay(TimeSpan.FromSeconds(5));

        // 获取最新的工作流运行记录
        var runs = await _apiClient.GetWorkflowRunsAsync(owner, repo, headSha: @ref.CommitSha, @event: "workflow_dispatch", page: 1, limit: 1);
        var run = runs.FirstOrDefault();

        // 记录执行历史
        if (_dbContext != null && run != null)
        {
            var history = new WorkflowRunHistory
            {
                Owner = owner,
                Repo = repo,
                WorkflowId = workflow.Id,
                WorkflowName = workflow.Name,
                Ref = @ref.Name,
                CommitSha = @ref.CommitSha,
                RunId = run.Id,
                Status = run.Status,
                Conclusion = run.Conclusion,
                CreatedAt = run.CreatedAt,
                UpdatedAt = run.UpdatedAt,
                TriggerType = triggerType,
            };

            _dbContext.WorkflowRunHistories.Add(history);
            await _dbContext.SaveChangesAsync();
        }

        return run;
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string owner, string repo, int runId)
    {
        return await _apiClient.GetWorkflowRunAsync(owner, repo, runId);
    }

    public async Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo)
    {
        return await _apiClient.GetWorkflowRunsAsync(owner, repo);
    }

    internal async Task<string?> TriggerWorkflowAsync(string owner, string repo, object workflow, object @ref, Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }
}