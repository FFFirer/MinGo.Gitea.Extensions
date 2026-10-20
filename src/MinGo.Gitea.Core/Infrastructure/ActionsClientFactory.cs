using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;

namespace MinGo.Gitea.Infrastructure;

/// <summary>按连接创建对应的 Actions 客户端</summary>
public class ActionsClientFactory
{
    private readonly IHttpClientFactory _factory;

    public ActionsClientFactory(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public IActionsApiClient Create(ProviderConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.ProviderType switch
        {
            ProviderType.GitHub => new GitHubApiClient(_factory, connection),
            _ => new GiteaApiClient(_factory, connection),
        };
    }

    public IGiteaApiClient CreateGitea(ProviderConnection connection)
    {
        var client = Create(connection);
        return (IGiteaApiClient)client;
    }
}
