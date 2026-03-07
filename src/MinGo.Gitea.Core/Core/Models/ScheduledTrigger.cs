namespace MinGo.Gitea.Core.Models;

public class ScheduledTrigger
{
    public int Id { get; set; }
    public required string Owner { get; set; }
    public required string Repo { get; set; }
    public required string CronExpression { get; set; }
    public required string Name { get; set; }
    public Dictionary<string, string> Inputs { get; set; } = new Dictionary<string, string>();
    public DateTime CreatedAt { get; set; }
    public DateTime? LastRunAt { get; set; }
    public bool IsEnabled { get; set; }
    public required Workflow Workflow { get; set; }
    public required Ref Ref { get; set; }
}