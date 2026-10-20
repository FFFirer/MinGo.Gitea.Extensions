using Microsoft.EntityFrameworkCore;
using MinGo.Gitea.Application.Services;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure;
using MinGo.Gitea.Infrastructure.Data;
using MinGo.Gitea.Admin.Components;
using Serilog;
using Vite.AspNetCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();
builder.Services.AddViteServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient();
builder.Services.AddHttpClient("GiteaApiClient");
builder.Services.AddHttpClient("GitHubApiClient");

// 配置数据库
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var dataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "data");
Directory.CreateDirectory(dataDirectory);
var dbPath = Path.Combine(dataDirectory, "gitea_extensions.db");
builder.Services.AddDbContext<GiteaExtensionsDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}", s => s.MigrationsAssembly("MinGo.Gitea.Core")));

// 连接配置（多 Gitea / GitHub）
builder.Services.AddScoped<ProviderConnectionService>();
builder.Services.AddScoped<GiteaConfigService>();

// Actions 客户端工厂（按连接创建 Gitea / GitHub）
builder.Services.AddScoped<ActionsClientFactory>();

// 兼容：单客户端工厂
builder.Services.AddScoped<GiteaApiClientFactory>();

// 注册默认 IActionsApiClient（取第一个启用连接；无配置时使用空 Gitea 客户端）
builder.Services.AddScoped<MinGo.Gitea.Core.Interfaces.IActionsApiClient>(provider =>
{
    var factory = provider.GetRequiredService<ActionsClientFactory>();
    var configService = provider.GetRequiredService<ProviderConnectionService>();
    try
    {
        var config = configService.GetConfigAsync().GetAwaiter().GetResult();
        if (config != null)
        {
            return factory.Create(config);
        }
    }
    catch
    {
        // ignore
    }

    return factory.Create(new ProviderConnection
    {
        ServerUrl = "",
        Token = "",
        Username = ""
    });
});

// 注册工作流服务（多连接）
builder.Services.AddScoped<WorkflowService>();

// 注册工作流状态更新服务
builder.Services.AddScoped<WorkflowStatusUpdaterService>();

// 添加定时任务服务
builder.Services.AddHostedService<WorkflowStatusUpdateBackgroundService>();

var app = builder.Build();

// 确保数据库迁移
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<GiteaExtensionsDbContext>();
        if (app.Environment.IsDevelopment())
        {
            dbContext.Database.Migrate();
        }
        else
        {
            dbContext.Database.EnsureCreated();
        }

        // 确保 ProviderConnections 表存在（兼容旧库）
        var connService = scope.ServiceProvider.GetRequiredService<ProviderConnectionService>();
        connService.EnsureDefaultSchemaAsync().GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        Log.Error(ex, "数据库初始化失败");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

if(app.Environment.IsDevelopment())
{
    app.UseViteDevelopmentServer(true);
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

public class WorkflowStatusUpdateBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowStatusUpdateBackgroundService> _logger;
    private readonly int _updateIntervalMinutes = 1;

    public WorkflowStatusUpdateBackgroundService(IServiceProvider serviceProvider, ILogger<WorkflowStatusUpdateBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow status update background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("Running workflow status update");

                using (var scope = _serviceProvider.CreateScope())
                {
                    var updaterService = scope.ServiceProvider.GetRequiredService<WorkflowStatusUpdaterService>();
                    await updaterService.UpdateWorkflowStatusesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in workflow status update background service");
            }

            await Task.Delay(TimeSpan.FromMinutes(_updateIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Workflow status update background service stopped");
    }
}
