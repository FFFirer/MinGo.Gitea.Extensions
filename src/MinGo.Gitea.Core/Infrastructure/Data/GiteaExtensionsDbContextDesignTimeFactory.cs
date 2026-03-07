using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace MinGo.Gitea.Infrastructure.Data;

public class GiteaExtensionsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<GiteaExtensionsDbContext>
{
    public GiteaExtensionsDbContext CreateDbContext(string[] args)
    {
        // 构建配置
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        // 配置 DbContextOptions
        var optionsBuilder = new DbContextOptionsBuilder<GiteaExtensionsDbContext>();
        optionsBuilder.UseSqlite("Data Source=gitea_extensions.db");

        return new GiteaExtensionsDbContext(optionsBuilder.Options);
    }
}