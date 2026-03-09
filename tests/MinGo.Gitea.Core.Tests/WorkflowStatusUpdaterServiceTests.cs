using MinGo.Gitea.Application.Services;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace MinGo.Gitea.Core.Tests;

public class WorkflowStatusUpdaterServiceTests
{
    private Mock<WorkflowService> _mockWorkflowService;
    private readonly Mock<ILogger<WorkflowStatusUpdaterService>> _mockLogger;
    private GiteaExtensionsDbContext _dbContext;
    private WorkflowStatusUpdaterService _service;

    public WorkflowStatusUpdaterServiceTests()
    {
        _mockWorkflowService = new Mock<WorkflowService>();
        _mockLogger = new Mock<ILogger<WorkflowStatusUpdaterService>>();
    }

    private void SetupTestContext()
    {
        // 创建内存数据库
        var options = new DbContextOptionsBuilder<GiteaExtensionsDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDatabase-{Guid.NewGuid()}")
            .Options;

        _dbContext = new GiteaExtensionsDbContext(options);
        _mockWorkflowService = new Mock<WorkflowService>(null, _dbContext);
        _service = new WorkflowStatusUpdaterService(_mockWorkflowService.Object, _dbContext, _mockLogger.Object);
    }

    [Fact]
    public async Task UpdateWorkflowStatusesAsync_ShouldUpdateStatuses()
    {
        SetupTestContext();
        // 准备测试数据
        var history = new WorkflowRunHistory
        {
            Id = 1,
            Owner = "test-owner",
            Repo = "test-repo",
            WorkflowId = "1",
            WorkflowName = "Test Workflow",
            Ref = "main",
            CommitSha = "test-sha",
            RunId = 123,
            Status = "running",
            Conclusion = "",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            StartedAt = DateTime.Now,
            EndedAt = null,
            TriggerType = "manual"
        };

        _dbContext.WorkflowRunHistories.Add(history);
        await _dbContext.SaveChangesAsync();

        // 模拟 API 响应
        var mockRun = new WorkflowRun
        {
            Id = 123,
            Name = "Test Workflow",
            Status = "completed",
            Conclusion = "success",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Ref = "main",
            CommitSha = "test-sha"
        };

        _mockWorkflowService.Setup(s => s.GetWorkflowRunAsync("test-owner", "test-repo", 123))
            .ReturnsAsync(mockRun);

        // 执行测试
        await _service.UpdateWorkflowStatusesAsync();

        // 验证结果
        var updatedHistory = await _dbContext.WorkflowRunHistories.FirstAsync(h => h.Id == 1);
        Assert.Equal("completed", updatedHistory.Status);
        Assert.Equal("success", updatedHistory.Conclusion);
        Assert.NotNull(updatedHistory.EndedAt);
    }

    [Fact]
    public async Task UpdateWorkflowStatusesAsync_ShouldHandleApiError()
    {
        SetupTestContext();
        // 准备测试数据
        var history = new WorkflowRunHistory
        {
            Id = 1,
            Owner = "test-owner",
            Repo = "test-repo",
            WorkflowId = "1",
            WorkflowName = "Test Workflow",
            Ref = "main",
            CommitSha = "test-sha",
            RunId = 123,
            Status = "running",
            Conclusion = "",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            StartedAt = DateTime.Now,
            EndedAt = null,
            TriggerType = "manual"
        };

        _dbContext.WorkflowRunHistories.Add(history);
        await _dbContext.SaveChangesAsync();

        // 模拟 API 错误
        _mockWorkflowService.Setup(s => s.GetWorkflowRunAsync("test-owner", "test-repo", 123))
            .ThrowsAsync(new Exception("API Error"));

        // 执行测试
        await _service.UpdateWorkflowStatusesAsync();

        // 验证结果 - 记录应该保持不变
        var updatedHistory = await _dbContext.WorkflowRunHistories.FirstAsync(h => h.Id == 1);
        Assert.Equal("running", updatedHistory.Status);
    }

    [Fact]
    public async Task UpdateWorkflowStatusesAsync_ShouldHandleNullDbContext()
    {
        // 创建服务时不提供数据库上下文
        var mockApiClient = new Mock<MinGo.Gitea.Core.Interfaces.IGiteaApiClient>();
        var workflowService = new WorkflowService(mockApiClient.Object, null);
        var service = new WorkflowStatusUpdaterService(workflowService, null, _mockLogger.Object);

        // 执行测试 - 应该不会抛出异常
        await service.UpdateWorkflowStatusesAsync();

        // 验证日志是否记录了警告
        _mockLogger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Database context is not available")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateWorkflowStatusesAsync_ShouldOnlyUpdateNonCompletedWorkflows()
    {
        SetupTestContext();
        // 准备测试数据 - 一个运行中，一个已完成
        var runningHistory = new WorkflowRunHistory
        {
            Id = 1,
            Owner = "test-owner",
            Repo = "test-repo",
            WorkflowId = "1",
            WorkflowName = "Test Workflow",
            Ref = "main",
            CommitSha = "test-sha",
            RunId = 123,
            Status = "running",
            Conclusion = "",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            StartedAt = DateTime.Now,
            EndedAt = null,
            TriggerType = "manual"
        };

        var completedHistory = new WorkflowRunHistory
        {
            Id = 2,
            Owner = "test-owner",
            Repo = "test-repo",
            WorkflowId = "1",
            WorkflowName = "Test Workflow",
            Ref = "main",
            CommitSha = "test-sha",
            RunId = 456,
            Status = "completed",
            Conclusion = "success",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            StartedAt = DateTime.Now,
            EndedAt = DateTime.Now,
            TriggerType = "manual"
        };

        _dbContext.WorkflowRunHistories.AddRange(runningHistory, completedHistory);
        await _dbContext.SaveChangesAsync();

        // 模拟 API 响应
        var mockRun = new WorkflowRun
        {
            Id = 123,
            Name = "Test Workflow",
            Status = "completed",
            Conclusion = "success",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            Ref = "main",
            CommitSha = "test-sha"
        };

        _mockWorkflowService.Setup(s => s.GetWorkflowRunAsync("test-owner", "test-repo", 123))
            .ReturnsAsync(mockRun);

        // 执行测试
        await _service.UpdateWorkflowStatusesAsync();

        // 验证结果 - 只有运行中的记录被更新
        var updatedRunningHistory = await _dbContext.WorkflowRunHistories.FirstAsync(h => h.Id == 1);
        var updatedCompletedHistory = await _dbContext.WorkflowRunHistories.FirstAsync(h => h.Id == 2);

        Assert.Equal("completed", updatedRunningHistory.Status);
        Assert.Equal("success", updatedRunningHistory.Conclusion);
        Assert.NotNull(updatedRunningHistory.EndedAt);

        // 已完成的记录应该保持不变
        Assert.Equal("completed", updatedCompletedHistory.Status);
        Assert.Equal("success", updatedCompletedHistory.Conclusion);
    }
}