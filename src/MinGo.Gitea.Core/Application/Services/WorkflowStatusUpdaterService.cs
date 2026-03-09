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
    private const int MaxConcurrentUpdates = 5; // 最大并发更新数

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
            // 获取所有状态不是 completed 的工作流执行记录
            var runningWorkflows = await _dbContext.WorkflowRunHistories
                .Where(h => h.Status != "completed")
                .ToListAsync();

            _logger?.LogInformation("Found {Count} workflows to update status", runningWorkflows.Count);

            if (runningWorkflows.Count == 0)
            {
                _logger?.LogDebug("No workflows need status update");
                return;
            }

            // 分批并发处理
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

            // 保存所有更新
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
            // 调用 Gitea API 获取最新状态
            var run = await _workflowService.GetWorkflowRunAsync(history.Owner, history.Repo, history.RunId);

            if (run != null)
            {
                // 更新状态和相关信息
                history.Status = run.Status;
                history.Conclusion = run.Conclusion;
                history.UpdatedAt = DateTime.Now;

                // 如果工作流已完成，更新结束时间
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
            // 记录错误但继续处理其他记录
            _logger?.LogError(ex, "Error updating workflow {RunId} status: {Message}", history.RunId, ex.Message);
            return false;
        }
    }
}