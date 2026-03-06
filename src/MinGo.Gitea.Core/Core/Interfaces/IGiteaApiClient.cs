using MinGo.Gitea.Core.Models;

namespace MinGo.Gitea.Core.Interfaces;

public interface IGiteaApiClient
{
    Task<bool> TestConnectionAsync();
    Task<IEnumerable<Repository>> GetRepositoriesAsync();
    Task<IEnumerable<Branch>> GetBranchesAsync(string owner, string repo);
    Task<IEnumerable<Workflow>> GetWorkflowsAsync(string owner, string repo);
    Task<WorkflowRun> TriggerWorkflowAsync(string owner, string repo, string workflowId, string branch, Dictionary<string, string> inputs = null);
    Task<WorkflowRun> GetWorkflowRunAsync(string owner, string repo, int runId);
    Task<IEnumerable<WorkflowRun>> GetWorkflowRunsAsync(string owner, string repo);
}