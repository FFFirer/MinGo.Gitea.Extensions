namespace MinGo.Gitea.Core.Models;

public class Repository
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string FullName { get; set; }
    public string Owner { get; set; }
    public string HtmlUrl { get; set; }
}

public class Branch
{
    public string Name { get; set; }
    public Commit Commit { get; set; }
}

public class Commit
{
    public string Sha { get; set; }
    public string Url { get; set; }
}

public class Workflow
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public string State { get; set; }
}

public class WorkflowRun
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Status { get; set; }
    public string Conclusion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Branch { get; set; }
}