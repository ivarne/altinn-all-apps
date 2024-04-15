namespace allApps.Runner;

using System.Threading.Channels;

public abstract class JobResult
{
    public required bool IsSuccess { get; set; }
    public required string Name { get; set; }
    public abstract string Output { get; }
}

public interface IJob
{
    Task<JobResult> Run();
    string Name { get; }
}

public class ConcurrentJobScheduler
{
    private readonly Channel<IJob> _jobs = Channel.CreateUnbounded<IJob>();
    private readonly int _maxConcurrentJobs;

    public ConcurrentJobScheduler(int maxConcurrentJobs = 8)
    {
        _maxConcurrentJobs = maxConcurrentJobs;
    }

    public async ValueTask AddJob(IJob job)
    {
        await _jobs.Writer.WriteAsync(job);
    }
    public void LastJobAdded()
    {
        _jobs.Writer.Complete();
    }

    public async IAsyncEnumerable<JobResult> RunJobsAsync()
    {
        var runningTasks = new List<Task<JobResult>>();

        await foreach (var job in _jobs.Reader.ReadAllAsync())
        {
            Console.WriteLine($"Starting: {job.Name}");
            runningTasks.Add(job.Run());

            if (runningTasks.Count >= _maxConcurrentJobs)
            {
                var completedTask = await Task.WhenAny(runningTasks);
                var result = completedTask.Result;
                LogResult(result);
                yield return result;
                runningTasks.Remove(completedTask);
            }
        }

        Console.WriteLine("Waiting for remaining jobs to complete");

        foreach (var remainingResult in await Task.WhenAll(runningTasks))
        {
            LogResult(remainingResult);
            yield return remainingResult;
        }
    }

    private static void LogResult(JobResult result)
    {
        if (!result.IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Completed: {result.Name} {(result.IsSuccess ? "with" : "without")} success");
            Console.ResetColor();
            Console.WriteLine(result.Output);
        }
    }
}
