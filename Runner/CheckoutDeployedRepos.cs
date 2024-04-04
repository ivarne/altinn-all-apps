using System.Threading.Channels;
using allApps.Runner.Clients;

namespace allApps.Runner;

public class CheckoutDeployedReposJobStarter
{
    private readonly DeploymentInfoClient _deploymentInfoClient;
    private readonly ConcurrentJobScheduler _jobScheduler = new();

    public CheckoutDeployedReposJobStarter(bool useLoggingHandler)
    {
        _deploymentInfoClient = new DeploymentInfoClient(useLoggingHandler);
    }

    private async Task ScheduleJobs()
    {
        var orgs = await AllAltinnOrgs.GetAltinnOrgs();
        foreach (var org in orgs)
        {
            foreach (var environment in org.Environments.Where(e => e == "tt02" || e == "production"))
            {
                await foreach (var deployment in _deploymentInfoClient.GetDeploymentInfo(org.Name, environment))
                {
                    await _jobScheduler.AddJob(new CheckoutDeployedRepoJob()
                    {
                        Org = org.Name,
                        RepoName = deployment.RepoName,
                        Commit = deployment.Commit,
                        Environment = environment
                    });
                }
            }
        }
        _jobScheduler.LastJobAdded();
    }

    public async Task RunAll()
    {
        var schedulerTask = ScheduleJobs();

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

public class CheckoutDeployedRepoJob : IJob
{
    public required string Org { get; init; }
    public required string RepoName { get; init; }
    public required string Commit { get; init; }
    public required string Environment { get; init; }


    public string Name => $"CheckoutDeployedReposJob {Environment} {Org}/{RepoName}";
    public string RepoUrl => $"https://altinn.studio/repos/{Org}/{RepoName}";

    public async Task<JobResult> Run()
    {
        var repoPath = Path.Join(Environment, Org, RepoName);
        var git = new GitWrapper(repoPath);

        if (!Directory.Exists(Path.Join(repoPath, ".git")))
        {
            // check if repo exists

            Directory.CreateDirectory(repoPath);
            var gitCloneResult = await git.Clone(RepoUrl);
            if (!gitCloneResult.IsSuccess)
            {
                return new CheckoutResult() { IsSuccess = false, Name = Name, RepoName = RepoName, Commit = Commit, StdErr = gitCloneResult.StdErr, StdOut = gitCloneResult.StdOut };
            }
        }

        var gitFetchResult = await git.Fetch(Commit);
        if (!gitFetchResult.IsSuccess)
        {
            return new CheckoutResult() { IsSuccess = false, Name = Name, RepoName = RepoName, Commit = Commit, StdErr = gitFetchResult.StdErr, StdOut = gitFetchResult.StdOut };
        }

        var gitCheckoutResult = await git.Checkout(Commit);
        if (!gitCheckoutResult.IsSuccess)
        {
            return new CheckoutResult() { IsSuccess = false, Name = Name, RepoName = RepoName, Commit = Commit, StdErr = gitCheckoutResult.StdErr, StdOut = gitCheckoutResult.StdOut };
        }
        return new CheckoutResult() { IsSuccess = true, Name = Name, RepoName = RepoName, Commit = Commit };
    }

    private class CheckoutResult : JobResult
    {
        public required string RepoName { get; set; }
        public required string Commit { get; set; }

        public string? StdOut { get; set; }
        public string? StdErr { get; set; }

        public override string Output => $"""
            Repo: {RepoName}
            Commit: {Commit}
            StdOut: 
            {StdOut}
            StdErr:
            {StdErr}
            """;
    }
}