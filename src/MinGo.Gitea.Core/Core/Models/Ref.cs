using System.Text.Json.Serialization;

namespace MinGo.Gitea.Core.Models;

public class Ref
{
    public required string Name { get; set; }
    public required string Type { get; set; } // branch or tag
    public required Commit Commit { get; set; }

    [JsonIgnore]
    public string CommitSha => Commit.Sha;
}
