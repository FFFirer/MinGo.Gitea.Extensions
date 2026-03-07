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
    public required string Type { get; set;}
    public required string Sha { get; set; }
    public required string Url { get; set; }
}

// 工作流模型
public class GiteaWorkflow
{
    public required string id { get; set; }
    public required string name { get; set; }
    public required string path { get; set; }
    public required string state { get; set; }
    public string? created_at { get; set; }
    public string? updated_at { get; set; }
    public string? badge_url { get; set; }
    public string? html_url { get; set; }
    public string? url { get; set; }
}

// 工作流列表响应
public class GiteaWorkflowListResponse
{
    public required List<GiteaWorkflow> workflows { get; set; }
    public long total_count { get; set; }
}

// 工作流运行模型
public class GiteaWorkflowRun
{
    public long id { get; set; }
    public string? display_title { get; set; }
    public string? status { get; set; }
    public string? conclusion { get; set; }
    public string? started_at { get; set; }
    public string? completed_at { get; set; }
    public string? head_branch { get; set; }
    public string? head_sha { get; set; }
    public string? html_url { get; set; }
    public string? path { get; set; }
    public long run_attempt { get; set; }
    public long run_number { get; set; }
    public string? url { get; set; }
}

// 工作流运行列表响应
public class GiteaWorkflowRunListResponse
{
    public List<GiteaWorkflowRun>? workflow_runs { get; set; }
    public long total_count { get; set; }
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

// 引用模型
public class GiteaRef
{
    public required string Ref { get; set; }
    public required string Url { get; set; }
    public required GiteaObject Object { get; set; }
}

public class GiteaObject {
    public required string Type { get; set;}
    public required string Sha { get; set; }
    public required string Url { get; set; }
}


