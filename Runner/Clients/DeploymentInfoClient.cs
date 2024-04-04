namespace allApps.Runner;

using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using global::Runner.Utils;

public class DeploymentInfoClient
{
    private readonly HttpClient _httpClient;

    // Constructor
    public DeploymentInfoClient(bool useLoggingHandler)
    {
        _httpClient = useLoggingHandler ? new HttpClient(new LoggingHandler(new HttpClientHandler())) : new HttpClient();
    }

    public async IAsyncEnumerable<DeploymentInfo> GetDeploymentInfo(string org, string environment)
    {
        var url = environment == "production" ?
            $"https://{org}.apps.altinn.no/kuberneteswrapper/api/v1/deployments" :
            $"https://{org}.apps.{environment}.altinn.no/kuberneteswrapper/api/v1/deployments";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCodeImproved();
        var deployments = JsonSerializer.Deserialize<List<DeploymentInfo>>(await response.Content.ReadAsByteArrayAsync())!;
        foreach (var deployment in deployments.Where(d => d.Release != "kuberneteswrapper" && d.Release is not null))
        {
            var (commit, commitUrl) = await GetReleaseCommit(org, deployment.RepoName, deployment.Version);
            if (commit is not null)
            {
                Console.WriteLine($"Found commit in {environment} for {org}/{deployment.RepoName} at version {deployment.Version}");
                deployment.Commit = commit;
                yield return deployment;
            }
            else
            {
                Console.Error.WriteLine($"Could not find commit in {environment} for {org}/{deployment.RepoName} at version {deployment.Version}\n{commitUrl}");
            }
        }
    }

    public async Task<(string?, string url)> GetReleaseCommit(string org, string app, string version)
    {
        var url = $"https://altinn.studio/designer/api/{org}/{app}/releases";
        var response = await _httpClient.GetAsync(url);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (null, url);
        }

        response.EnsureSuccessStatusCodeImproved();
        var releases = JsonSerializer.Deserialize<StudioReleaseResponse>(await response.Content.ReadAsByteArrayAsync())!.Releases;

        return (releases.FirstOrDefault(r => r.TagName == version)?.TargetCommitish, url);

    }
    private class StudioReleaseResponse
    {
        [JsonPropertyName("results")]
        public required List<StudioRelease> Releases { get; set; }
    }
    
}

public class StudioRelease
{
    [JsonPropertyName("tagName")]
    public required string TagName { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("body")]
    public required string Body { get; set; }

    [JsonPropertyName("targetCommitish")]
    public required string TargetCommitish { get; set; }

    [JsonPropertyName("build")]
    public required StudioBuild Build { get; set; }

    [JsonPropertyName("created")]
    public required DateTime Created { get; set; }

    [JsonPropertyName("createdBy")]
    public required string CreatedBy { get; set; }

    [JsonPropertyName("app")]
    public required string App { get; set; }

    [JsonPropertyName("org")]
    public required string Org { get; set; }
}

public class StudioBuild
{
    [JsonPropertyName("id")]
    public required string Id { get; set; }

    [JsonPropertyName("status")]
    public required string Status { get; set; }

    [JsonPropertyName("result")]
    public required string Result { get; set; }

    [JsonPropertyName("started")]
    public required DateTime? Started { get; set; }

    [JsonPropertyName("finished")]
    public required DateTime? Finished { get; set; }
}




public class DeploymentInfo
{
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    [JsonPropertyName("release")]
    public required string Release { get; set; }

    public string RepoName => Release.Split('-', 2).ElementAtOrDefault(1) ?? throw new InvalidOperationException($"Could not parse repo name from {Release}");

    [JsonIgnore] // Set after parsing
    public string Commit { get; set; } = default!;
}