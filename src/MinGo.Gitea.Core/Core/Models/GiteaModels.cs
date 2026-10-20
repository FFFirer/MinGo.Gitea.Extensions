namespace MinGo.Gitea.Core.Models;

public class Repository
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string FullName { get; set; }
    public required string Owner { get; set; }
    public required string HtmlUrl { get; set; }
    public int ConnectionId { get; set; }
    public ProviderType Provider { get; set; } = ProviderType.Gitea;
}

public class Branch
{
    public required string Name { get; set; }
    public required Commit Commit { get; set; }
}

public class Commit
{
    public required string Sha { get; set; }
    public required string Url { get; set; }
}

public class Workflow
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string State { get; set; }
    public int ConnectionId { get; set; }
    public ProviderType Provider { get; set; } = ProviderType.Gitea;
}

public class WorkflowRun
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Status { get; set; }
    public required string Conclusion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public required string Ref { get; set; }
    public required string CommitSha { get; set; }
    public int ConnectionId { get; set; }
    public ProviderType Provider { get; set; } = ProviderType.Gitea;
}

