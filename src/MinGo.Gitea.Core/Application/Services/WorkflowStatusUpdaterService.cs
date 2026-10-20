using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MinGo.Gitea.Application.Services;

public class WorkflowStatusUpdaterService
{
    private readonly WorkflowService _workflowService;
    private readonly GiteaExtensionsDbContext? _dbContext;
    private readonly ILogger<WorkflowStatusUpdaterService>? _logger;
    private const int MaxConcurrentUpdates = 5;

    public WorkflowStatusUpdaterService(WorkflowService workflowService, GiteaExtensionsDbContext? dbContext = null, ILogger<WorkflowStatusUpdaterService>? logger = null)
    {
        _workflowService = workflowService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task UpdateWorkflowStatusesAsync()
    {
        if (_dbContext == null)
        {
            _logger?.LogWarning("Database context is not available, skipping status update");
            return;
        }

        try
        {
            var runningWorkflows = await _dbContext.WorkflowRunHistories
                .Where(h => h.Status != "completed")
                .ToListAsync();

            _logger?.LogInformation("Found {Count} workflows to update status", runningWorkflows.Count);

            if (runningWorkflows.Count == 0)
            {
                _logger?.LogDebug("No workflows need status update");
                return;
            }

            var updatedCount = 0;
            var batches = runningWorkflows.Chunk(MaxConcurrentUpdates);

            foreach (var batch in batches)
            {
                var batchTasks = new List<Task<bool>>();

                foreach (var history in batch)
                {
                    batchTasks.Add(UpdateWorkflowStatusAsync(history));
                }

                var results = await Task.WhenAll(batchTasks);
                updatedCount += results.Count(r => r);
            }

            if (updatedCount > 0)
            {
                await _dbContext.SaveChangesAsync();
                _logger?.LogInformation("Updated {Count} workflows status", updatedCount);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during workflow status update");
        }
    }

    private async Task<bool> UpdateWorkflowStatusAsync(WorkflowRunHistory history)
    {
        try
        {
            // 按 history.ConnectionId 路由到对应 Gitea / GitHub 连接
            var run = await _workflowService.GetWorkflowRunAsync(history.Owner, history.Repo, history.RunId, history.ConnectionId);

            if (run != null)
            {
                history.Status = run.Status;
                history.Conclusion = run.Conclusion;
                history.UpdatedAt = DateTime.Now;

                if (run.Status == "completed")
                {
                    history.EndedAt = run.UpdatedAt;
                }

                _logger?.LogDebug("Updated workflow {RunId} status to {Status}", history.RunId, run.Status);
                return true;
            }
            else
            {
                _logger?.LogWarning("No workflow run found for RunId {RunId}", history.RunId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error updating workflow {RunId} status: {Message}", history.RunId, ex.Message);
            return false;
        }
    }
}
