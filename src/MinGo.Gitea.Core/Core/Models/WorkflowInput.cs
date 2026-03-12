namespace MinGo.Gitea.Core.Models;

public class WorkflowInput
{
    public required string Name { get; set; }
    public required string Type { get; set; }
    public string? Description { get; set; }
    public bool Required { get; set; }
    public string? DefaultValue { get; set; }
    public List<string>? Options { get; set; }
}

public class WorkflowInputsCollection
{
    public List<WorkflowInput> Inputs { get; set; } = new List<WorkflowInput>();
}
