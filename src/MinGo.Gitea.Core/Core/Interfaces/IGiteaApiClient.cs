using MinGo.Gitea.Core.Models;

namespace MinGo.Gitea.Core.Interfaces;

public interface IGiteaApiClient
{
    Task<bool> TestConnectionAsync();
    Task<IEnumerable<Repository>> GetRepositoriesAsync();
    Task<IEnumerable<Branch>> GetBranchesAsync(string owner, string repo);
    Task<IEnumerable<Ref>> GetRefsAsync(string owner, string repo);
    Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo);
    Task TriggerWorkflowAsync(string owner, string repo, string workflowId, string @ref, Dictionary<string, string>? inputs = null);
    Task<WorkflowRun?> GetWorkflowRunAsync(string owner, string repo, int runId);
    Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo, int? page = null, int? limit = null, string? status = null, string? branch = null, string? @event = null, int? workflowId = null, int? actorId = null, string? created = null, string? headSha = null);
    Task<Ref?> GetRefAsync(string owner, string repo, string @ref);
}