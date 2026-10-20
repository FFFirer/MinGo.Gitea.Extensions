namespace MinGo.Gitea.Core.Models;

/// <summary>一个 Gitea / GitHub 连接配置</summary>
public class ProviderConnection
{
    public int Id { get; set; }
    public string Name { get; set; } = "default";
    public ProviderType ProviderType { get; set; } = ProviderType.Gitea;
    /// <summary>Gitea 实例地址，或 GitHub API 地址（默认 https://api.github.com）</summary>
    public required string ServerUrl { get; set; }
    public required string Token { get; set; }
    public required string Username { get; set; }
    public bool IsEnabled { get; set; } = true;
}
