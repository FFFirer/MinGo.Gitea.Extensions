namespace MinGo.Gitea.Core.Models;

public class WorkflowRunHistory
{
    public int Id { get; set; }
    public required string Owner { get; set; }
    public required string Repo { get; set; }
    public required string WorkflowId { get; set; }
    public required string WorkflowName { get; set; }
    public required string Ref { get; set; }
    public required string CommitSha { get; set; }
    public int RunId { get; set; }
    public required string Status { get; set; }
    public required string Conclusion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? StartedAt { get; set; } // 工作流开始执行时间
    public DateTime? EndedAt { get; set; } // 工作流结束执行时间
    public required string TriggerType { get; set; } // 手动触发或定时触发
    public string? Inputs { get; set; }
}