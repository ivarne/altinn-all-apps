namespace allApps;

using System;
using System.Diagnostics;

public class GitWrapper
{
    private readonly string _workingDirectory;
    public string WorkingDirectory => _workingDirectory;

    public GitWrapper(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }
    public async Task<GitCommandResult> Clone(string repositoryUrl)
    {
        return await ExecuteGitCommand($"clone {repositoryUrl} .");
    }

    public async Task<GitCommandResult> AddRemote(string remoteName, string remoteUrl)
    {
        return await ExecuteGitCommand($"remote add {remoteName} {remoteUrl}");
    }

    public async Task<GitCommandResult> Checkout(string branchName)
    {
        return await ExecuteGitCommand($"checkout {branchName}");
    }

    public async Task<GitCommandResult> Commit(string message)
    {
        return await ExecuteGitCommand($"commit -m \"{message}\"");
    }

    public async Task<GitCommandResult> Push(string remoteName, string branchName)
    {
        return await ExecuteGitCommand($"push {remoteName} {branchName}");
    }

    public async Task<GitCommandResult> Fetch(string commit)
    {
        return await ExecuteGitCommand($"fetch origin {commit}");
    }

    public async Task<GitCommandResult> Pull()
    {
        return await ExecuteGitCommand($"pull");
    }

    private async Task<GitCommandResult> ExecuteGitCommand(string command)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _workingDirectory,
        };

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            return new GitCommandResult
            {
                ExitCode = process.ExitCode,
                StdOut = output,
                StdErr = error
            };
        }
    }
}

public class GitCommandResult
{
    public required int ExitCode { get; set; }
    public required string StdOut { get; set; }
    public required string StdErr { get; set; }
    public bool IsSuccess => ExitCode == 0;
}
