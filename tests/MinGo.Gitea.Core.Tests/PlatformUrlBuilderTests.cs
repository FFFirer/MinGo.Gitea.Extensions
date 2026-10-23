using MinGo.Gitea.Core.Models;
using Xunit;

namespace MinGo.Gitea.Core.Tests;

public class PlatformUrlBuilderTests
{
    [Fact]
    public void BuildWorkflowRunUrl_PrefersHtmlUrl()
    {
        var url = PlatformUrlBuilder.BuildWorkflowRunUrl(
            ProviderType.GitHub,
            "https://api.github.com",
            "o",
            "r",
            99,
            "https://github.com/o/r/actions/runs/99");

        Assert.Equal("https://github.com/o/r/actions/runs/99", url);
    }

    [Theory]
    [InlineData("https://api.github.com", "https://github.com")]
    [InlineData("https://api.github.com/", "https://github.com")]
    [InlineData("https://github.example.com/api/v3", "https://github.example.com")]
    [InlineData("https://github.example.com", "https://github.example.com")]
    [InlineData("https://api.github.example.com", "https://github.example.com")]
    public void GetWebBaseUrl_GitHub_DerivesFromServerUrl(string serverUrl, string expected)
    {
        var webBase = PlatformUrlBuilder.GetWebBaseUrl(ProviderType.GitHub, serverUrl);
        Assert.Equal(expected, webBase);
    }

    [Fact]
    public void GetWebBaseUrl_GitHub_EmptyServerUrl_DefaultsToGitHubCom()
    {
        Assert.Equal("https://github.com", PlatformUrlBuilder.GetWebBaseUrl(ProviderType.GitHub, null));
    }

    [Fact]
    public void GetWebBaseUrl_Gitea_UsesServerUrl()
    {
        Assert.Equal(
            "https://gitea.example.com",
            PlatformUrlBuilder.GetWebBaseUrl(ProviderType.Gitea, "https://gitea.example.com/"));
    }

    [Fact]
    public void BuildWorkflowRunUrl_Gitea_FromServerUrl()
    {
        var url = PlatformUrlBuilder.BuildWorkflowRunUrl(
            ProviderType.Gitea,
            "https://gitea.example.com",
            "alice",
            "demo",
            12345);

        Assert.Equal("https://gitea.example.com/alice/demo/actions/runs/12345", url);
    }

    [Fact]
    public void BuildWorkflowRunUrl_GitHub_FromApiUrl()
    {
        var url = PlatformUrlBuilder.BuildWorkflowRunUrl(
            ProviderType.GitHub,
            "https://api.github.com",
            "octo",
            "hello",
            98765432101);

        Assert.Equal("https://github.com/octo/hello/actions/runs/98765432101", url);
    }

    [Fact]
    public void BuildWorkflowRunUrl_Ghe_FromApiV3()
    {
        var url = PlatformUrlBuilder.BuildWorkflowRunUrl(
            ProviderType.GitHub,
            "https://github.example.com/api/v3",
            "team",
            "repo",
            42);

        Assert.Equal("https://github.example.com/team/repo/actions/runs/42", url);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void BuildWorkflowRunUrl_InvalidRunId_ReturnsNull(long runId)
    {
        Assert.Null(PlatformUrlBuilder.BuildWorkflowRunUrl(
            ProviderType.Gitea,
            "https://gitea.example.com",
            "o",
            "r",
            runId));
    }

    [Fact]
    public void BuildWorkflowRunUrl_MissingOwnerOrRepo_ReturnsNull()
    {
        Assert.Null(PlatformUrlBuilder.BuildWorkflowRunUrl(
            ProviderType.Gitea,
            "https://gitea.example.com",
            "",
            "r",
            1));
    }
}
