namespace MinGo.Gitea.Core.Models;

/// <summary>平台 Web 页面 URL 构造（Gitea / GitHub）</summary>
public static class PlatformUrlBuilder
{
    /// <summary>
    /// 构造 workflow run 详情页 URL。
    /// 优先使用 API 返回的 htmlUrl；否则按 provider + serverUrl 拼接。
    /// </summary>
    public static string? BuildWorkflowRunUrl(
        ProviderType provider,
        string? serverUrl,
        string? owner,
        string? repo,
        long runId,
        string? htmlUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(htmlUrl))
        {
            return htmlUrl.Trim();
        }

        if (runId <= 0 || string.IsNullOrWhiteSpace(owner) || string.IsNullOrWhiteSpace(repo))
        {
            return null;
        }

        var webBase = GetWebBaseUrl(provider, serverUrl);
        if (string.IsNullOrEmpty(webBase))
        {
            return null;
        }

        return $"{webBase}/{owner.Trim('/')}/{repo.Trim('/')}/actions/runs/{runId}";
    }

    /// <summary>由连接配置推导平台 Web 根地址</summary>
    public static string? GetWebBaseUrl(ProviderType provider, string? serverUrl)
    {
        if (provider == ProviderType.GitHub)
        {
            return GetGitHubWebBaseUrl(serverUrl);
        }

        return string.IsNullOrWhiteSpace(serverUrl) ? null : serverUrl.TrimEnd('/');
    }

    private static string? GetGitHubWebBaseUrl(string? serverUrl)
    {
        if (string.IsNullOrWhiteSpace(serverUrl))
        {
            return "https://github.com";
        }

        var url = serverUrl.Trim().TrimEnd('/');

        if (url.Equals("https://api.github.com", StringComparison.OrdinalIgnoreCase)
            || url.Equals("http://api.github.com", StringComparison.OrdinalIgnoreCase))
        {
            return "https://github.com";
        }

        if (url.EndsWith("/api/v3", StringComparison.OrdinalIgnoreCase))
        {
            url = url[..^"/api/v3".Length].TrimEnd('/');
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var host = uri.Host;
        if (host.StartsWith("api.", StringComparison.OrdinalIgnoreCase) && host.Length > 4)
        {
            host = host[4..];
        }

        return $"{uri.Scheme}://{host}";
    }
}
