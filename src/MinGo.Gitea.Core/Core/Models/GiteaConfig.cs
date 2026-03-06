namespace MinGo.Gitea.Core.Models;

public class GiteaConfig
{
    public int Id { get; set; }
    public required string ServerUrl { get; set; }
    public required string Token { get; set; }
    public required string Username { get; set; }
}