using Microsoft.EntityFrameworkCore;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;

namespace MinGo.Gitea.Application.Services;

/// <summary>兼容包装：委托给 <see cref="ProviderConnectionService"/></summary>
public class GiteaConfigService
{
    private readonly ProviderConnectionService _inner;

    public GiteaConfigService(ProviderConnectionService inner)
    {
        _inner = inner;
    }

    public Task<bool> SaveConfigAsync(GiteaConfig config) => _inner.SaveConfigAsync(config);

    public Task<ProviderConnection?> GetConfigAsync() => _inner.GetConfigAsync();
}
