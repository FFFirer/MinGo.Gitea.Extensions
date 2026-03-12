using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

    /// <summary>
    /// 获取工作流的 inputs 定义
    /// </summary>
    public async Task<List<WorkflowInput>> GetWorkflowInputsAsync(string owner, string repo, string workflowPath)
    {
        try
        {
            var yamlContent = await _apiClient.GetWorkflowFileContentAsync(owner, repo, workflowPath);
            if (string.IsNullOrEmpty(yamlContent))
            {
                return new List<WorkflowInput>();
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            var workflow = deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
            if (workflow == null)
            {
                return new List<WorkflowInput>();
            }

            if (!workflow.TryGetValue("on", out var onValue))
            {
                return new List<WorkflowInput>();
            }

            Dictionary<string, object>? workflowDispatch = null;

            if (onValue is Dictionary<string, object> onDict)
            {
                if (onDict.TryGetValue("workflowDispatch", out var wd))
                {
                    workflowDispatch = wd as Dictionary<string, object>;
                }
            }
            else if (onValue is string onStr && onStr == "workflow_dispatch")
            {
                return new List<WorkflowInput>();
            }

            if (workflowDispatch == null || !workflowDispatch.TryGetValue("inputs", out var inputsObj) || inputsObj is not Dictionary<object, object> inputs)
            {
                return new List<WorkflowInput>();
            }

            var result = new List<WorkflowInput>();
            foreach (var (key, value) in inputs)
            {
                var inputName = key?.ToString() ?? "";
                var inputDict = value as Dictionary<object, object>;

                var input = new WorkflowInput
                {
                    Name = inputName,
                    Type = inputDict?.TryGetValue("type", out var type) == true ? type?.ToString() ?? "string" : "string",
                    Description = inputDict?.TryGetValue("description", out var desc) == true ? desc?.ToString() : null,
                    Required = inputDict?.TryGetValue("required", out var req) == true && bool.TryParse(req?.ToString(), out var reqVal) && reqVal,
                    DefaultValue = inputDict?.TryGetValue("default", out var def) == true ? def?.ToString() : null
                };

                if (input.Type == "choice" && inputDict?.TryGetValue("options", out var opts) == true)
                {
                    input.Options = (opts as List<object>)?.Select(o => o?.ToString() ?? "").ToList();
                }

                result.Add(input);
            }

            return result
                .OrderByDescending(i => i.Required)
                .ThenBy(i => i.Name)
                .ToList();
        }
        catch
        {
            return new List<WorkflowInput>();
        }
    }

    public async Task<Repository?> GetRepositoryByNameAsync(string fullName)
    {
        var repositories = await GetRepositoriesAsync();
        return repositories.FirstOrDefault(r => r.FullName.Equals(fullName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Ref?> GetRefAsync(string owner, string repo, string @ref)
    {
        return await _apiClient.GetRefAsync(owner, repo, @ref);
    }

    public async Task<WorkflowRunHistory?> TriggerWorkflowAsync(string owner, string repo, Workflow workflow, Ref @ref, string triggerType, Dictionary<string, string>? inputs = null)
    {
        await _apiClient.TriggerWorkflowAsync(owner, repo, workflow.Id, @ref.Name, inputs);

        await Task.Delay(TimeSpan.FromSeconds(5));

        var runs = await _apiClient.GetWorkflowRunsAsync(owner, repo, headSha: @ref.CommitSha, @event: "workflow_dispatch", page: 1, limit: 1);
        var run = runs.FirstOrDefault();

        if (_dbContext != null && run != null)
        {
            string? inputsJson = null;
            if (inputs != null && inputs.Count > 0)
            {
                inputsJson = JsonConvert.SerializeObject(inputs);
            }

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
                StartedAt = run.CreatedAt,
                EndedAt = run.Status == "completed" ? run.UpdatedAt : null,
                TriggerType = triggerType,
                Inputs = inputsJson,
            };

            _dbContext.WorkflowRunHistories.Add(history);
            await _dbContext.SaveChangesAsync();

            return history;
        }

        return null;
    }

    public virtual async Task<WorkflowRun?> GetWorkflowRunAsync(string owner, string repo, int runId)
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
            .Where(h => h.Status == "in_progress")
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

    /// <summary>
    /// 获取所有工作流触发模板
    /// </summary>
    public async Task<IEnumerable<WorkflowTriggerTemplate>> GetTemplatesAsync()
    {
        if (_dbContext == null)
        {
            return Enumerable.Empty<WorkflowTriggerTemplate>();
        }

        return await _dbContext.WorkflowTriggerTemplates
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// 保存工作流触发模板
    /// </summary>
    public async Task<WorkflowTriggerTemplate?> SaveTemplateAsync(string name, WorkflowRunHistory history)
    {
        if (_dbContext == null)
        {
            return null;
        }

        var template = new WorkflowTriggerTemplate
        {
            Name = name,
            Owner = history.Owner,
            Repo = history.Repo,
            WorkflowId = history.WorkflowId,
            WorkflowName = history.WorkflowName,
            Ref = history.Ref,
            CreatedAt = DateTime.Now
        };

        _dbContext.WorkflowTriggerTemplates.Add(template);
        await _dbContext.SaveChangesAsync();

        return template;
    }

    /// <summary>
    /// 删除工作流触发模板
    /// </summary>
    public async Task<bool> DeleteTemplateAsync(int id)
    {
        if (_dbContext == null)
        {
            return false;
        }

        var template = await _dbContext.WorkflowTriggerTemplates.FindAsync(id);
        if (template == null)
        {
            return false;
        }

        _dbContext.WorkflowTriggerTemplates.Remove(template);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// 更新模板的最后触发时间
    /// </summary>
    public async Task<bool> UpdateTemplateTriggerTimeAsync(int templateId)
    {
        if (_dbContext == null)
        {
            return false;
        }

        var template = await _dbContext.WorkflowTriggerTemplates.FindAsync(templateId);
        if (template == null)
        {
            return false;
        }

        template.LastTriggeredAt = DateTime.Now;
        await _dbContext.SaveChangesAsync();

        return true;
    }
}