using System.Text.Json;
using System.Text.Json.Serialization;
using Runner.Utils;

namespace allApps.Runner.Clients;

public static class AllAltinnOrgs
{
    public static async Task<List<AltinnOrg>> GetAltinnOrgs()
    {
        using var client = new HttpClient();

        var url = "https://altinncdn.no/orgs/altinn-orgs.json";
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCodeImproved();
        var orgs = JsonSerializer.Deserialize<AltinnOrgResponse>(await response.Content.ReadAsByteArrayAsync())!.Orgs;
        foreach (var (name, org) in orgs)
        {
            org.Name = name;
        }

        return orgs.Values.ToList();
    }

    private class AltinnOrgResponse
    {
        [JsonPropertyName("orgs")]
        public required Dictionary<string, AltinnOrg> Orgs { get; set; }
    }
}

public class AltinnOrg
{
    [JsonIgnore] // Set after parsing
    public string Name { get; set; } = default!;

    [JsonPropertyName("name")]
    public required Dictionary<string, string> FullNames { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("homepage")]
    public string? Homepage { get; set; }

    [JsonPropertyName("environments")]
    public List<string> Environments { get; set; } = new();
}