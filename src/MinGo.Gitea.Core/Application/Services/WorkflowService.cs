using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task<WorkflowRunHistory?> TriggerWorkflowAsync(string owner, string repo, Workflow workflow, Ref @ref, string triggerType, Dictionary<string, string>? inputs = null)
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
                StartedAt = run.CreatedAt, // 开始时间
                EndedAt = run.Status == "completed" ? run.UpdatedAt : null, // 结束时间
                TriggerType = triggerType,
            };

            _dbContext.WorkflowRunHistories.Add(history);
            await _dbContext.SaveChangesAsync();

            return history;
        }

        return null;
    }

    public async Task<WorkflowRun?> GetWorkflowRunAsync(string owner, string repo, int runId)
    {
        return await _apiClient.GetWorkflowRunAsync(owner, repo, runId);
    }

    public async Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo)
    {
        return await _apiClient.GetWorkflowRunsAsync(owner, repo);
    }

    public async Task<(IEnumerable<WorkflowRunHistory>, int)> GetWorkflowRunHistoriesAsync(int page = 1, int pageSize = 10, string? filter = null)
    {
        if (_dbContext == null)
        {
            return (Enumerable.Empty<WorkflowRunHistory>(), 0);
        }

        var query = _dbContext.WorkflowRunHistories.AsQueryable();

        if (!string.IsNullOrEmpty(filter))
        {
            query = query.Where(h => 
                h.WorkflowName.Contains(filter) || 
                h.Repo.Contains(filter) || 
                h.Ref.Contains(filter) || 
                h.Status.Contains(filter)
            );
        }

        var totalCount = await query.CountAsync();
        var histories = await query
            .OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (histories, totalCount);
    }

    public async Task<WorkflowRunHistory?> GetWorkflowRunHistoryByIdAsync(int id)
    {
        if (_dbContext == null)
        {
            return null;
        }

        return await _dbContext.WorkflowRunHistories.FirstOrDefaultAsync(h => h.Id == id);
    }

    public async Task<int> GetTotalRunCountAsync()
    {
        if (_dbContext == null)
        {
            return 0;
        }

        return await _dbContext.WorkflowRunHistories.CountAsync();
    }

    public async Task<int> GetMonthlyRunCountAsync()
    {
        if (_dbContext == null)
        {
            return 0;
        }

        var now = DateTime.Now;
        var startOfMonth = new DateTime(now.Year, now.Month, 1);

        return await _dbContext.WorkflowRunHistories
            .Where(h => h.CreatedAt >= startOfMonth)
            .CountAsync();
    }

    public async Task<(int, int)> GetSuccessRateAsync()
    {
        if (_dbContext == null)
        {
            return (0, 0);
        }

        var total = await _dbContext.WorkflowRunHistories.CountAsync();
        if (total == 0)
        {
            return (0, 0);
        }

        var successful = await _dbContext.WorkflowRunHistories
            .Where(h => h.Status == "completed" && h.Conclusion == "success")
            .CountAsync();

        return (successful, total);
    }

    public async Task<TimeSpan?> GetAverageExecutionTimeAsync()
    {
        if (_dbContext == null)
        {
            return null;
        }

        var recentRuns = await _dbContext.WorkflowRunHistories
            .Where(h => h.StartedAt != null && h.EndedAt != null)
            .OrderByDescending(h => h.CreatedAt)
            .Take(10)
            .ToListAsync();

        if (recentRuns.Count == 0)
        {
            return null;
        }

        var totalDuration = recentRuns.Sum(run => (run.EndedAt!.Value - run.StartedAt!.Value).TotalSeconds);
        var averageSeconds = totalDuration / recentRuns.Count;

        return TimeSpan.FromSeconds(averageSeconds);
    }

    public async Task<int> GetRunningRunCountAsync()
    {
        if (_dbContext == null)
        {
            return 0;
        }

        return await _dbContext.WorkflowRunHistories
            .Where(h => h.Status == "running")
            .CountAsync();
    }

    public async Task<IEnumerable<WorkflowRunHistory>> GetRecentRunsAsync(int count = 5)
    {
        if (_dbContext == null)
        {
            return Enumerable.Empty<WorkflowRunHistory>();
        }

        return await _dbContext.WorkflowRunHistories
            .OrderByDescending(h => h.CreatedAt)
            .Take(count)
            .ToListAsync();
    }
}