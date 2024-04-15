using allApps;
using allApps.Runner;
using allApps.Runner.Clients;

public class CheckoutAllReposStarter
{
    private readonly ConcurrentJobScheduler _jobScheduler = new();

    private readonly GiteaClient _giteaClient;
    private readonly string? _apiKey;
    private readonly DirectoryInfo _baseDir;

    public CheckoutAllReposStarter(GiteaClient giteaClient, DirectoryInfo baseDir, string? apiKey)
    {
        _giteaClient = giteaClient;
        _apiKey = apiKey;
        _baseDir = baseDir;
    }

    public async Task ScheduleJobs()
    {
        var allOrgs = await AllAltinnOrgs.GetAltinnOrgs();
        foreach (var org in allOrgs)
        {
            Directory.CreateDirectory(Path.Join(_baseDir.FullName, org.Name));
            var repos = await _giteaClient.OrgListReposAsync(org.Name, limit: 10000);
            foreach (var repo in repos)
            {
                await _jobScheduler.AddJob(new CheckoutReposJob()
                {
                    BaseDir = _baseDir,
                    Org = org,
                    Repo = repo,
                    ApiKey = _apiKey,
                });
            }
        }
        _jobScheduler.LastJobAdded();
    }

    public async Task CloneOrPullAllAppReposMaster()
    {
        var schedulerTask = ScheduleJobs();

        await foreach (var result in _jobScheduler.RunJobsAsync())
        {
            if (!result.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Completed: {result.Name} {(result.IsSuccess ? "with" : "without")} success");
                Console.ResetColor();
            }
        }
        await schedulerTask;
    }
}

internal class CheckoutReposJob : IJob
{
    public required DirectoryInfo BaseDir { get; init; }
    public required AltinnOrg Org { get; init; }
    public required Repository Repo { get; init; }
    public required string? ApiKey { get; init; }

    public string Name => $"Checkout or pull {Org.Name}/{Repo.Name}";

    public async Task<JobResult> Run()
    {
        var repoDir = Path.Join(BaseDir.FullName, Org.Name, Repo.Name);
        Directory.CreateDirectory(repoDir);
        var repo = new GitWrapper(repoDir);
        GitCommandResult? gitResult;
        if (Directory.Exists(Path.Join(repoDir, ".git")))
        {
            gitResult = await repo.Pull();
        }
        else
        {
            gitResult = await repo.Clone(Repo.Clone_url!, ApiKey);
        }

        return new CheckoutResult()
        {
            IsSuccess = gitResult.IsSuccess,
            Name = Name,
            RepoName = Repo.Name!,
            Branch = Repo.Default_branch!,
            StdOut = gitResult.StdOut,
            StdErr = gitResult.StdErr
        };
    }

    private class CheckoutResult : JobResult
    {
        public required string RepoName { get; set; }
        public required string Branch { get; set; }

        public string? StdOut { get; set; }
        public string? StdErr { get; set; }

        public override string Output => $"""
            Repo: {RepoName}
            Branch: {Branch}
            StdOut: 
            {StdOut}
            StdErr:
            {StdErr}
            """;
    }
}