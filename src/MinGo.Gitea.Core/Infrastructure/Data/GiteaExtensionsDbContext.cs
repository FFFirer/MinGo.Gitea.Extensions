using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MinGo.Gitea.Core.Models;
using Newtonsoft.Json;

namespace MinGo.Gitea.Infrastructure.Data;

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
        var jsonConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<Dictionary<string, string>>(v ?? "{}")
        );
        
        modelBuilder.Entity<ScheduledTrigger>()
            .HasKey(t => t.Id);
        
        modelBuilder.Entity<ScheduledTrigger>()
            .Property(t => t.Inputs)
            .HasConversion(jsonConverter)
            .HasColumnType("TEXT")
            .IsRequired();
        
        // 配置WorkflowRunHistory模型
        modelBuilder.Entity<WorkflowRunHistory>()
            .HasKey(h => h.Id);
        
        // 配置GiteaConfig模型
        modelBuilder.Entity<GiteaConfig>()
            .HasKey(c => c.Id);
    }
}