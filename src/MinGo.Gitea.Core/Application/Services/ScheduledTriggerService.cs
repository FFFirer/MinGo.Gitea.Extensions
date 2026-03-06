using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace MinGo.Gitea.Application.Services;

public class ScheduledTriggerService
{
    private readonly WorkflowService _workflowService;
    private readonly GiteaExtensionsDbContext _dbContext;

    public ScheduledTriggerService(WorkflowService workflowService, GiteaExtensionsDbContext dbContext = null)
    {
        _workflowService = workflowService;
        _dbContext = dbContext;
    }

    public async Task<ScheduledTrigger> CreateTriggerAsync(ScheduledTrigger trigger)
    {
        trigger.CreatedAt = DateTime.UtcNow;
        if (_dbContext != null)
        {
            _dbContext.ScheduledTriggers.Add(trigger);
            await _dbContext.SaveChangesAsync();
        }
        return trigger;
    }

    public async Task<ScheduledTrigger> UpdateTriggerAsync(ScheduledTrigger trigger)
    {
        if (_dbContext != null)
        {
            var existing = await _dbContext.ScheduledTriggers.FindAsync(trigger.Id);
            if (existing != null)
            {
                existing.Owner = trigger.Owner;
                existing.Repo = trigger.Repo;
                existing.WorkflowId = trigger.WorkflowId;
                existing.Branch = trigger.Branch;
                existing.CronExpression = trigger.CronExpression;
                existing.Name = trigger.Name;
                existing.Inputs = trigger.Inputs;
                existing.IsEnabled = trigger.IsEnabled;
                existing.LastRunAt = trigger.LastRunAt;
                await _dbContext.SaveChangesAsync();
                return existing;
            }
        }
        return null;
    }

    public async Task DeleteTriggerAsync(int id)
    {
        if (_dbContext != null)
        {
            var trigger = await _dbContext.ScheduledTriggers.FindAsync(id);
            if (trigger != null)
            {
                _dbContext.ScheduledTriggers.Remove(trigger);
                await _dbContext.SaveChangesAsync();
            }
        }
    }

    public async Task<IEnumerable<ScheduledTrigger>> GetTriggersAsync()
    {
        if (_dbContext != null)
        {
            return await _dbContext.ScheduledTriggers.ToListAsync();
        }
        return Enumerable.Empty<ScheduledTrigger>();
    }

    public async Task<ScheduledTrigger> GetTriggerAsync(int id)
    {
        if (_dbContext != null)
        {
            return await _dbContext.ScheduledTriggers.FindAsync(id);
        }
        return null;
    }

    public async Task CheckAndExecuteTriggersAsync()
    {
        if (_dbContext != null)
        {
            var now = DateTime.UtcNow;
            var triggers = await _dbContext.ScheduledTriggers.Where(t => t.IsEnabled).ToListAsync();
            
            foreach (var trigger in triggers)
            {
                // 简化的Cron表达式检查（实际项目中应使用专门的Cron库）
                if (ShouldExecute(trigger, now))
                {
                    await ExecuteTriggerAsync(trigger);
                    trigger.LastRunAt = now;
                    await _dbContext.SaveChangesAsync();
                }
            }
        }
    }

    private bool ShouldExecute(ScheduledTrigger trigger, DateTime now)
    {
        // 简化实现：每分钟检查一次
        // 实际项目中应使用Cron表达式解析库
        if (!trigger.LastRunAt.HasValue)
        {
            return true;
        }
        return now.Subtract(trigger.LastRunAt.Value).TotalMinutes >= 1;
    }

    private async Task ExecuteTriggerAsync(ScheduledTrigger trigger)
    {
        var run = await _workflowService.TriggerWorkflowAsync(
            trigger.Owner,
            trigger.Repo,
            trigger.WorkflowId,
            trigger.Branch,
            trigger.Inputs
        );
        
        // 记录执行历史
        var history = new WorkflowRunHistory
        {
            Owner = trigger.Owner,
            Repo = trigger.Repo,
            WorkflowId = trigger.WorkflowId,
            WorkflowName = run?.Name,
            Branch = trigger.Branch,
            RunId = run?.Id ?? 0,
            Status = run?.Status,
            Conclusion = run?.Conclusion,
            CreatedAt = run?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = run?.UpdatedAt ?? DateTime.UtcNow,
            TriggerType = "定时触发"
        };
        
        _dbContext.WorkflowRunHistories.Add(history);
        await _dbContext.SaveChangesAsync();
    }
}