namespace MinGo.Gitea.Extensions.Core.Models;

public class ScheduledTrigger
{
    public int Id { get; set; }
    public string Owner { get; set; }
    public string Repo { get; set; }
    public string WorkflowId { get; set; }
    public string Branch { get; set; }
    public string CronExpression { get; set; }
    public string Name { get; set; }
    public Dictionary<string, string> Inputs { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public bool IsEnabled { get; set; }
}