using Microsoft.EntityFrameworkCore;
using MinGo.Gitea.Extensions.Core.Models;
using MinGo.Gitea.Extensions.Infrastructure.Data;

namespace MinGo.Gitea.Extensions.Application.Services;

/// <summary>
/// Gitea配置服务，用于管理Gitea API配置的保存和读取
/// </summary>
public class GiteaConfigService
{
    private readonly GiteaExtensionsDbContext _dbContext;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    public GiteaConfigService(GiteaExtensionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 保存Gitea配置
    /// </summary>
    /// <param name="config">Gitea配置</param>
    /// <returns>保存是否成功</returns>
    public async Task<bool> SaveConfigAsync(GiteaConfig config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        try
        {
            // 检查是否已有配置
            var existingConfig = await _dbContext.GiteaConfigs.FirstOrDefaultAsync();
            if (existingConfig != null)
            {
                // 更新现有配置
                existingConfig.ServerUrl = config.ServerUrl;
                existingConfig.Token = config.Token;
                existingConfig.Username = config.Username;
                _dbContext.GiteaConfigs.Update(existingConfig);
            }
            else
            {
                // 添加新配置
                await _dbContext.GiteaConfigs.AddAsync(config);
            }

            await _dbContext.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取Gitea配置
    /// </summary>
    /// <returns>Gitea配置</returns>
    public async Task<GiteaConfig> GetConfigAsync()
    {
        return await _dbContext.GiteaConfigs.FirstOrDefaultAsync();
    }
}
