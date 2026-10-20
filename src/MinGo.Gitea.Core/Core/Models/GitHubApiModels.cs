using Newtonsoft.Json;

namespace MinGo.Gitea.Core.Models;

// GitHub REST API DTO（snake_case 字段）

public class GitHubOwner
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("login")] public required string Login { get; set; }
    [JsonProperty("html_url")] public string? HtmlUrl { get; set; }
}

public class GitHubRepository
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("name")] public required string Name { get; set; }
    [JsonProperty("full_name")] public required string FullName { get; set; }
    [JsonProperty("owner")] public required GitHubOwner Owner { get; set; }
    [JsonProperty("html_url")] public required string HtmlUrl { get; set; }
    [JsonProperty("default_branch")] public string? DefaultBranch { get; set; }
}

public class GitHubBranchCommit
{
    [JsonProperty("sha")] public required string Sha { get; set; }
    [JsonProperty("url")] public string? Url { get; set; }
}

public class GitHubBranch
{
    [JsonProperty("name")] public required string Name { get; set; }
    [JsonProperty("commit")] public required GitHubBranchCommit Commit { get; set; }
}

public class GitHubRefObject
{
    [JsonProperty("type")] public string? Type { get; set; }
    [JsonProperty("sha")] public required string Sha { get; set; }
    [JsonProperty("url")] public string? Url { get; set; }
}

public class GitHubRef
{
    [JsonProperty("ref")] public required string Ref { get; set; }
    [JsonProperty("url")] public string? Url { get; set; }
    [JsonProperty("object")] public required GitHubRefObject Object { get; set; }
}

public class GitHubWorkflow
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("path")] public string? Path { get; set; }
    [JsonProperty("state")] public string? State { get; set; }
}

public class GitHubWorkflowListResponse
{
    [JsonProperty("total_count")] public long TotalCount { get; set; }
    [JsonProperty("workflows")] public List<GitHubWorkflow>? Workflows { get; set; }
}

public class GitHubWorkflowRun
{
    [JsonProperty("id")] public long Id { get; set; }
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("display_title")] public string? DisplayTitle { get; set; }
    [JsonProperty("status")] public string? Status { get; set; }
    [JsonProperty("conclusion")] public string? Conclusion { get; set; }
    [JsonProperty("created_at")] public string? CreatedAt { get; set; }
    [JsonProperty("updated_at")] public string? UpdatedAt { get; set; }
    [JsonProperty("run_started_at")] public string? RunStartedAt { get; set; }
    [JsonProperty("head_branch")] public string? HeadBranch { get; set; }
    [JsonProperty("head_sha")] public string? HeadSha { get; set; }
    [JsonProperty("html_url")] public string? HtmlUrl { get; set; }
    [JsonProperty("event")] public string? Event { get; set; }
}

public class GitHubWorkflowRunListResponse
{
    [JsonProperty("total_count")] public long TotalCount { get; set; }
    [JsonProperty("workflow_runs")] public List<GitHubWorkflowRun>? WorkflowRuns { get; set; }
}

public class GitHubFileContent
{
    [JsonProperty("name")] public string? Name { get; set; }
    [JsonProperty("path")] public string? Path { get; set; }
    [JsonProperty("content")] public string? Content { get; set; }
    [JsonProperty("encoding")] public string? Encoding { get; set; }
}

public class GitHubWorkflowDispatchRequest
{
    [JsonProperty("ref")] public required string Ref { get; set; }
    [JsonProperty("inputs")] public Dictionary<string, string>? Inputs { get; set; }
}
