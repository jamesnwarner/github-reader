using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace GitHubReader;

public class PullRequest
{
    [JsonProperty("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonProperty("merged_at")]
    public DateTime? MergedAt { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }
}