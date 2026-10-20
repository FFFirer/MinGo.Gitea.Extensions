using Microsoft.EntityFrameworkCore;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;

namespace MinGo.Gitea.Application.Services;

/// <summary>管理多个 Gitea / GitHub 连接配置</summary>
public class ProviderConnectionService
{
    private readonly GiteaExtensionsDbContext _dbContext;

    public ProviderConnectionService(GiteaExtensionsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<ProviderConnection>> GetAllAsync(bool enabledOnly = false)
    {
        var query = _dbContext.ProviderConnections.AsQueryable();
        if (enabledOnly)
        {
            query = query.Where(c => c.IsEnabled);
        }
        return await query.OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<ProviderConnection?> GetByIdAsync(int id)
    {
        return await _dbContext.ProviderConnections.FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>兼容旧单配置接口：返回第一个启用的连接</summary>
    public async Task<ProviderConnection?> GetConfigAsync()
    {
        return await _dbContext.ProviderConnections
            .Where(c => c.IsEnabled)
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<ProviderConnection?> SaveAsync(ProviderConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (connection.Id > 0)
        {
            var existing = await _dbContext.ProviderConnections.FirstOrDefaultAsync(c => c.Id == connection.Id);
            if (existing == null) return null;

            existing.Name = connection.Name;
            existing.ProviderType = connection.ProviderType;
            existing.ServerUrl = connection.ServerUrl;
            existing.Token = connection.Token;
            existing.Username = connection.Username;
            existing.IsEnabled = connection.IsEnabled;
            _dbContext.ProviderConnections.Update(existing);
            await _dbContext.SaveChangesAsync();
            return existing;
        }

        await _dbContext.ProviderConnections.AddAsync(connection);
        await _dbContext.SaveChangesAsync();
        return connection;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var existing = await _dbContext.ProviderConnections.FirstOrDefaultAsync(c => c.Id == id);
        if (existing == null) return false;
        _dbContext.ProviderConnections.Remove(existing);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    /// <summary>兼容旧接口</summary>
    public Task<bool> SaveConfigAsync(ProviderConnection config) =>
        SaveAsync(config).ContinueWith(t => t.Result != null);

    public async Task<int> EnsureDefaultSchemaAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE TABLE IF NOT EXISTS ProviderConnections (
                Id INTEGER NOT NULL CONSTRAINT PK_ProviderConnections PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL DEFAULT 'default',
                ProviderType INTEGER NOT NULL DEFAULT 0,
                ServerUrl TEXT NOT NULL,
                Token TEXT NOT NULL,
                Username TEXT NOT NULL,
                IsEnabled INTEGER NOT NULL DEFAULT 1
            );
            """);

        // 从旧表 GiteaConfigs 迁移单配置（若新表为空）
        var count = await _dbContext.ProviderConnections.CountAsync();
        if (count == 0)
        {
            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO ProviderConnections (Name, ProviderType, ServerUrl, Token, Username, IsEnabled)
                    SELECT 'default', 0, ServerUrl, Token, Username, 1 FROM GiteaConfigs;
                    """);
                count = await _dbContext.ProviderConnections.CountAsync();
            }
            catch
            {
                // 旧表不存在则跳过
            }
        }
        return count;
    }
}
