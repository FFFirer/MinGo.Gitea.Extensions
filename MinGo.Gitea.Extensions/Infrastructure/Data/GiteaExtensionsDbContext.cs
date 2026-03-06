using Microsoft.EntityFrameworkCore;
using MinGo.Gitea.Extensions.Core.Models;

namespace MinGo.Gitea.Extensions.Infrastructure.Data;

public class GiteaExtensionsDbContext : DbContext
{
    public DbSet<ScheduledTrigger> ScheduledTriggers { get; set; }
    public DbSet<WorkflowRunHistory> WorkflowRunHistories { get; set; }
    public DbSet<GiteaConfig> GiteaConfigs { get; set; }

    public GiteaExtensionsDbContext(DbContextOptions<GiteaExtensionsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置ScheduledTrigger模型
        modelBuilder.Entity<ScheduledTrigger>()
            .HasKey(t => t.Id);
        
        // 配置WorkflowRunHistory模型
        modelBuilder.Entity<WorkflowRunHistory>()
            .HasKey(h => h.Id);
        
        // 配置GiteaConfig模型
        modelBuilder.Entity<GiteaConfig>()
            .HasKey(c => c.Id);
    }
}