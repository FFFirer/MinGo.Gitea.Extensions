namespace MinGo.Gitea.Core.Models;

/// <summary>
/// 工作流触发模板实体
/// </summary>
public class WorkflowTriggerTemplate
{
    public int Id { get; set; }
    /// <summary>所属连接（ProviderConnections.Id）</summary>
    public int ConnectionId { get; set; }
    public ProviderType Provider { get; set; } = ProviderType.Gitea;

    /// <summary>
    /// 模板名称
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// 仓库所有者
    /// </summary>
    public required string Owner { get; set; }

    /// <summary>
    /// 仓库名称
    /// </summary>
    public required string Repo { get; set; }

    /// <summary>
    /// 工作流ID
    /// </summary>
    public required string WorkflowId { get; set; }

    /// <summary>
    /// 工作流名称
    /// </summary>
    public required string WorkflowName { get; set; }

    /// <summary>
    /// Git引用(分支/标签)
    /// </summary>
    public required string Ref { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 最后触发时间
    /// </summary>
    public DateTime? LastTriggeredAt { get; set; }
}
