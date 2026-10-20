namespace MinGo.Gitea.Core.Models;

/// <summary>兼容旧代码的 Gitea 连接配置（等价 ProviderConnection + Gitea）</summary>
public class GiteaConfig : ProviderConnection
{
    public GiteaConfig()
    {
        ProviderType = ProviderType.Gitea;
    }
}
