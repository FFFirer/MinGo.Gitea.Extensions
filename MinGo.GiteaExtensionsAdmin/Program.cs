using Microsoft.EntityFrameworkCore;
using MinGo.GiteaExtensions.Application.Services;
using MinGo.GiteaExtensions.Core.Models;
using MinGo.GiteaExtensions.Infrastructure;
using MinGo.GiteaExtensions.Infrastructure.Data;
using MinGo.GiteaExtensionsAdmin.Components;
using Serilog;
using Vite.AspNetCore;

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
    options.UseSqlite("Data Source=gitea_extensions.db"));

// 注册 Gitea 配置服务
builder.Services.AddScoped<GiteaConfigService>();

// 注册 Gitea API 客户端
builder.Services.AddScoped<MinGo.GiteaExtensions.Core.Interfaces.IGiteaApiClient, GiteaApiClient>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    // 暂时使用默认配置，实际使用时会从数据库读取
    var config = new GiteaConfig
    {
        ServerUrl = "",
        Token = ""
    };
    return new GiteaApiClient(httpClient, config);
});

// 注册工作流服务
builder.Services.AddScoped<WorkflowService>();

var app = builder.Build();

// 确保数据库迁移
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<GiteaExtensionsDbContext>();
        dbContext.Database.EnsureCreated();
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
