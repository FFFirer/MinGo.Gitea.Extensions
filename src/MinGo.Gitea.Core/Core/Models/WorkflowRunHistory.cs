namespace MinGo.Gitea.Core.Models;

public class WorkflowRunHistory
{
    public int Id { get; set; }
    public string Owner { get; set; }
    public string Repo { get; set; }
    public string WorkflowId { get; set; }
    public string WorkflowName { get; set; }
    public string Branch { get; set; }
    public int RunId { get; set; }
    public string Status { get; set; }
    public string Conclusion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string TriggerType { get; set; } // 手动触发或定时触发
}