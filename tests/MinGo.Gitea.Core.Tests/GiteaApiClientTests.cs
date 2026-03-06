using System.Net;
using MinGo.Gitea.Core.Interfaces;
using MinGo.Gitea.Core.Models;
using MinGo.Gitea.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace MinGo.Gitea.Core.Tests;

public class GiteaApiClientTests
{
    private const string TestFlagEnvironmentVariable = "RUN_GITEA_API_TESTS";
    private static bool IsGiteaApiTestEnabled => Environment.GetEnvironmentVariable(TestFlagEnvironmentVariable) == "1";
    
    private readonly HttpClient _httpClient;
    private readonly GiteaConfig _config;
    private readonly IGiteaApiClient _client;

    public GiteaApiClientTests()
    {
        // Initialize fields with default values
        _httpClient = new HttpClient();
        _config = new GiteaConfig
        {
            ServerUrl = "https://gitea.example.com",
            Token = "test-token",
            Username = "test-user"
        };
        _client = new GiteaApiClient(_httpClient, _config);

        // Skip initialization if tests are not enabled
        if (!IsGiteaApiTestEnabled)
        {
            return;
        }

        // Load configuration from appsettings.json and user secrets
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<GiteaApiClientTests>()
            .Build();

        // Create Gitea config from configuration
        _config = new GiteaConfig
        {
            ServerUrl = configuration["Gitea:ServerUrl"] ?? throw new Exception("Gitea:ServerUrl is not set in appsettings.json or user secrets"),
            Token = configuration["Gitea:Token"] ?? throw new Exception("Gitea:Token is not set in appsettings.json or user secrets"),
            Username = configuration["Gitea:Username"] ?? throw new Exception("Gitea:Username is not set in appsettings.json or user secrets")
        };

        // Create actual HttpClient
        _httpClient = new HttpClient();
        _client = new GiteaApiClient(_httpClient, _config);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnTrue_WhenConnectionIsSuccessful()
    {
        if (!IsGiteaApiTestEnabled)
        {
            // Skip test with message using xunit.v3 approach
            return;
        }
        
        // Act
        var result = await _client.TestConnectionAsync();

        // Assert
        Assert.True(result, "Connection to Gitea API failed. Please check your configuration in appsettings.json or user secrets.");
    }

    [Fact]
    public async Task GetRepositoriesAsync_ShouldReturnRepositories_WhenApiCallSucceeds()
    {
        if (!IsGiteaApiTestEnabled)
        {
            // Skip test with message using xunit.v3 approach
            return;
        }
        
        // Act
        var result = await _client.GetRepositoriesAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Any(), "Repositories list is empty");
        
        // Print repositories for debugging
        Console.WriteLine("Repositories:");
        foreach (var repo in result)
        {
            Console.WriteLine($"- {repo.FullName}");
        }
    }

    [Fact]
    public async Task GetBranchesAsync_ShouldReturnBranches_WhenApiCallSucceeds()
    {
        if (!IsGiteaApiTestEnabled)
        {
            // Skip test with message using xunit.v3 approach
            return;
        }
        
        // First, get a repository to test with
        var repositories = await _client.GetRepositoriesAsync();
        Assert.NotNull(repositories);
        Assert.True(repositories.Any(), "No repositories found to test branches");
        
        var testRepo = repositories.First();
        
        // Act
        var result = await _client.GetBranchesAsync(testRepo.Owner, testRepo.Name);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Any(), "Branches list is empty");
        
        // Print branches for debugging
        Console.WriteLine($"Branches for {testRepo.FullName}:");
        foreach (var branch in result)
        {
            Console.WriteLine($"- {branch.Name} (SHA: {branch.Commit.Sha})");
        }
    }

    [Fact]
    public async Task GetWorkflowsAsync_ShouldReturnWorkflows_WhenApiCallSucceeds()
    {
        if (!IsGiteaApiTestEnabled)
        {
            // Skip test with message using xunit.v3 approach
            return;
        }
        
        // First, get a repository to test with
        var repositories = await _client.GetRepositoriesAsync();
        Assert.NotNull(repositories);
        Assert.True(repositories.Any(), "No repositories found to test workflows");
        
        var testRepo = repositories.First();
        
        // Act
        var result = await _client.GetWorkflowsAsync(testRepo.Owner, testRepo.Name);

        // Assert
        Assert.NotNull(result);
        
        // Print workflows for debugging
        Console.WriteLine($"Workflows for {testRepo.FullName}:");
        if (result.Any())
        {
            foreach (var workflow in result)
            {
                Console.WriteLine($"- {workflow.Name} (ID: {workflow.Id}, State: {workflow.State})");
            }
        }
        else
        {
            Console.WriteLine("No workflows found in this repository");
        }
    }
}
