using MinGo.Gitea.Application.Services;
using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;

namespace MinGo.Gitea.Infrastructure;

public class GiteaApiClientFactory
{
    private readonly HttpClient _httpClient;
    private readonly GiteaConfigService _configService;

    public GiteaApiClientFactory(HttpClient httpClient, GiteaConfigService configService)
    {
        _httpClient = httpClient;
        _configService = configService;
    }

    public async Task<IGiteaApiClient> CreateAsync()
    {
        var config = await _configService.GetConfigAsync();
        if (config == null)
        {
            throw new Exception("Gitea configuration not found. Please set up the configuration first.");
        }

        return new GiteaApiClient(_httpClient, config);
    }

    public IGiteaApiClient Create()
    {
        var config = _configService.GetConfigAsync().GetAwaiter().GetResult();
        if (config == null)
        {
            throw new Exception("Gitea configuration not found. Please set up the configuration first.");
        }

        return new GiteaApiClient(_httpClient, config);
    }

    public IGiteaApiClient Create(GiteaConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        return new GiteaApiClient(_httpClient, config);
    }
}