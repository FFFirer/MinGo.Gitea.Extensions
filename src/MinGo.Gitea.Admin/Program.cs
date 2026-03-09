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

// 配置数据库
builder.Services.AddDbContext<GiteaExtensionsDbContext>(options =>
    options.UseSqlite("Data Source=gitea_extensions.db", s => s.MigrationsAssembly("MinGo.Gitea.Core")));

// 注册 Gitea 配置服务
builder.Services.AddScoped<GiteaConfigService>();

// 注册 Gitea API 客户端工厂
builder.Services.AddScoped<GiteaApiClientFactory>();

// 注册 Gitea API 客户端
builder.Services.AddScoped<MinGo.Gitea.Core.Interfaces.IGiteaApiClient>(provider =>
{
    var factory = provider.GetRequiredService<GiteaApiClientFactory>();
    try
    {
        return factory.Create();
    }
    catch
    {
        // 如果配置不存在，使用默认配置
        var httpClient = provider.GetRequiredService<HttpClient>();
        var config = new GiteaConfig
        {
            ServerUrl = "",
            Token = "",
            Username = ""
        };
        return factory.Create(config);
    }
});

// 注册工作流服务
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
            // 开发环境自动应用迁移
            dbContext.Database.Migrate();
        }
        else
        {
            // 生产环境仅确保数据库存在
            dbContext.Database.EnsureCreated();
        }
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
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
    private readonly int _updateIntervalMinutes = 1; // 默认检查间隔为1分钟

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

            // 等待指定的间隔时间
            await Task.Delay(TimeSpan.FromMinutes(_updateIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("Workflow status update background service stopped");
    }
}
