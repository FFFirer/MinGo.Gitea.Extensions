namespace MinGo.Gitea.Core.Models;

// 仓库模型
public class GiteaRepository
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string FullName { get; set; }
    public required GiteaOwner Owner { get; set; }
    public required string HtmlUrl { get; set; }
}

// 所有者模型
public class GiteaOwner
{
    public long Id { get; set; }
    public required string Login { get; set; }
    public required string HtmlUrl { get; set; }
}

// 分支模型
public class GiteaBranch
{
    public required string Name { get; set; }
    public required GiteaCommit Commit { get; set; }
}

// 提交模型
public class GiteaCommit
{
    public required string Sha { get; set; }
    public required string Url { get; set; }
}

// 工作流模型
public class GiteaWorkflow
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Path { get; set; }
    public required string State { get; set; }
    public required string CreatedAt { get; set; }
    public required string UpdatedAt { get; set; }
}

// 工作流列表响应
public class GiteaWorkflowListResponse
{
    public required List<GiteaWorkflow> Workflows { get; set; }
}

// 工作流运行模型
public class GiteaWorkflowRun
{
    public long Id { get; set; }
    public required string Name { get; set; }
    public required string Status { get; set; }
    public required string Conclusion { get; set; }
    public required string CreatedAt { get; set; }
    public required string UpdatedAt { get; set; }
    public required string HeadBranch { get; set; }
    public required string HeadSha { get; set; }
}

// 工作流运行列表响应
public class GiteaWorkflowRunListResponse
{
    public required List<GiteaWorkflowRun> WorkflowRuns { get; set; }
}

// 触发工作流请求
public class GiteaWorkflowDispatchRequest
{
    public required string Ref { get; set; }
    public Dictionary<string, string>? Inputs { get; set; }
}

// 用户模型
public class GiteaUser
{
    public long Id { get; set; }
    public required string Login { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string AvatarUrl { get; set; }
}
