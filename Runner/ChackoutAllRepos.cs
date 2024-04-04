using allApps;
using allApps.Runner;
using allApps.Runner.Clients;

public class CheckoutAllReposStarter
{
    private readonly ConcurrentJobScheduler _jobScheduler = new();

    private readonly GiteaClient _giteaClient;

    public CheckoutAllReposStarter(GiteaClient giteaClient)
    {
        _giteaClient = giteaClient;
    }

    public async Task ScheduleJobs(DirectoryInfo baseDir)
    {
        var allOrgs = await AllAltinnOrgs.GetAltinnOrgs();
        foreach (var org in allOrgs.Take(1))
        {
            Directory.CreateDirectory(Path.Join(baseDir.FullName, org.Name));
            var repos = await _giteaClient.OrgListReposAsync(org.Name, limit: 10000);
            foreach (var repo in repos)
            {
                await _jobScheduler.AddJob(new CheckoutReposJob()
                {
                    BaseDir = baseDir,
                    Org = org,
                    Repo = repo,
                });
            }
        }
        _jobScheduler.LastJobAdded();
    }

    public async Task CloneOrPullAllAppReposMaster(DirectoryInfo baseDir)
    {
        var schedulerTask = ScheduleJobs(baseDir);

        await foreach (var result in _jobScheduler.RunJobsAsync())
        {
            if (!result.IsSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            Console.WriteLine($"Completed: {result.Name} {(result.IsSuccess ? "with" : "without")} success");
        }
        await schedulerTask;
    }
}

internal class CheckoutReposJob : IJob
{
    public required DirectoryInfo BaseDir { get; set; }
    public required AltinnOrg Org { get; set; }
    public required Repository Repo { get; internal set; }

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
            gitResult = await repo.Clone(Repo.Clone_url!);
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