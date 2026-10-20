using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure;

namespace MinGo.Gitea.Application.Services;

/// <summary>兼容旧 API：绑定单个 Gitea 客户端</summary>
public class GiteaApiClientFactory
{
    private readonly IHttpClientFactory _factory;
    private readonly ProviderConnectionService _configService;

    public GiteaApiClientFactory(IHttpClientFactory httpClient, ProviderConnectionService configService)
    {
        _factory = httpClient;
        _configService = configService;
    }

    public async Task<IGiteaApiClient> CreateAsync()
    {
        var config = await _configService.GetConfigAsync();
        if (config == null)
        {
            throw new Exception("Gitea configuration not found. Please set up the configuration first.");
        }

        return (IGiteaApiClient)new ActionsClientFactory(_factory).Create(config);
    }

    public IGiteaApiClient Create()
    {
        var config = _configService.GetConfigAsync().GetAwaiter().GetResult();
        if (config == null)
        {
            throw new Exception("Gitea configuration not found. Please set up the configuration first.");
        }

        return (IGiteaApiClient)new ActionsClientFactory(_factory).Create(config);
    }

    public IGiteaApiClient Create(ProviderConnection config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return (IGiteaApiClient)new ActionsClientFactory(_factory).Create(config);
    }
}
