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
    public DbSet<WorkflowTriggerTemplate> WorkflowTriggerTemplates { get; set; }

    public GiteaExtensionsDbContext(DbContextOptions<GiteaExtensionsDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // 配置ScheduledTrigger模型
        var jsonConverter = new ValueConverter<Dictionary<string, string>, string>(
            v => JsonConvert.SerializeObject(v ?? new Dictionary<string, string>()),
            v => JsonConvert.DeserializeObject<Dictionary<string, string>>(v ?? "{}") ?? new Dictionary<string, string>()
        );
        
        var workflowConverter = new ValueConverter<MinGo.Gitea.Core.Models.Workflow, string>(
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<MinGo.Gitea.Core.Models.Workflow>(v) ?? new MinGo.Gitea.Core.Models.Workflow { Id = "", Name = "", Path = "", State = "" }
        );
        
        var refConverter = new ValueConverter<MinGo.Gitea.Core.Models.Ref, string>(
            v => JsonConvert.SerializeObject(v),
            v => JsonConvert.DeserializeObject<MinGo.Gitea.Core.Models.Ref>(v) ?? new MinGo.Gitea.Core.Models.Ref { Name = "", Type = "branch", Commit = new MinGo.Gitea.Core.Models.Commit { Sha = "", Url = "" } }
        );
        
        modelBuilder.Entity<ScheduledTrigger>()
            .HasKey(t => t.Id);
        
        modelBuilder.Entity<ScheduledTrigger>()
            .Property(t => t.Inputs)
            .HasConversion(jsonConverter)
            .HasColumnType("TEXT")
            .IsRequired();
        
        modelBuilder.Entity<ScheduledTrigger>()
            .Property(t => t.Workflow)
            .HasConversion(workflowConverter)
            .HasColumnType("TEXT")
            .IsRequired();
        
        modelBuilder.Entity<ScheduledTrigger>()
            .Property(t => t.Ref)
            .HasConversion(refConverter)
            .HasColumnType("TEXT")
            .IsRequired();
        
        // 配置WorkflowRunHistory模型
        modelBuilder.Entity<WorkflowRunHistory>()
            .HasKey(h => h.Id);
        
        // 配置GiteaConfig模型
        modelBuilder.Entity<GiteaConfig>()
            .HasKey(c => c.Id);

        // 配置WorkflowTriggerTemplate模型
        modelBuilder.Entity<WorkflowTriggerTemplate>()
            .HasKey(t => t.Id);
    }
}