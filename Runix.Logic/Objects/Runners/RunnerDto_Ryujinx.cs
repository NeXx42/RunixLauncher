using GameLibrary.Logic;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;

namespace Runix.Logic.Objects.Runners;

public class RunnerDto_Ryujinx : RunnerDto
{
    public const string installedVersionName = "BINARY.AppImage";

    public string getRunnerBinary => IsInstalled(runnerVersion) ? Path.Combine(runnerRoot, runnerVersion) : runnerVersion;

    public RunnerDto_Ryujinx(dbo_Runner runner, dbo_RunnerConfig[] config) : base(runner, config)
    {
    }

    public override Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest req)
    {
        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = getRunnerBinary };
        res.arguments[RunnerManager.ArgumentType.Application].AddFirst(req.path);

        res.whiteListedDirs.Add(Path.GetDirectoryName(req.path)!);
        res.whiteListedDirs.Add(runnerRoot);

        return Task.FromResult(res);
    }

    public override bool IsInstalled(string? version) => string.IsNullOrEmpty(version) || version == installedVersionName;

    public override LoadingTask DownloadVersion(string version)
    {
        return new LoadingTask("Moving", $"Moving ryujinx to {runnerRoot}...", MoveAsync);

        async Task MoveAsync()
        {
            File.Move(version, Path.Combine(runnerRoot.CreateDirectoryIfNotExists(), installedVersionName), true);

            runnerVersion = installedVersionName;
            await UpdateDatabaseEntry(nameof(dbo_Runner.runnerVersion));
        }
    }

    public override Task LaunchLauncher() => RunnerManager.RunNative(getRunnerBinary);
}
