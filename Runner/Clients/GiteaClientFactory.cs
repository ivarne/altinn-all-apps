using allApps.Runner.Clients;

namespace allApps.Runner;

public static class GiteaClientFactory
{
    public static GiteaClient CreateClient(string apiKey, bool useLoggingHandler)
    {
        var giteaHttpClient = useLoggingHandler ? new HttpClient(new LoggingHandler(new HttpClientHandler())) : new HttpClient();
        giteaHttpClient.BaseAddress = new Uri("https://altinn.studio/repos/api/v1/");
        giteaHttpClient.DefaultRequestHeaders.Authorization = new("token", apiKey);

        return new GiteaClient(giteaHttpClient);
    }
}

public class LoggingHandler : DelegatingHandler
{
    public LoggingHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Console.WriteLine("Request:");
        Console.WriteLine(request.ToString());
        if (request.Content != null)
        {
            Console.WriteLine(await request.Content.ReadAsStringAsync());
        }
        Console.WriteLine();

        HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

        Console.WriteLine("Response:");
        Console.WriteLine(response.ToString());
        if (response.Content != null)
        {
            Console.WriteLine(await response.Content.ReadAsStringAsync());
        }
        Console.WriteLine();

        return response;
    }
}