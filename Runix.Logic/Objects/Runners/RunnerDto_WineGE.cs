using System.Diagnostics;
using System.Text.Json;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using Runix.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_WineGE : RunnerDto_Wine
{
    public const string GITHUB_NAME = "GloriousEggroll/wine-ge-custom";

    protected readonly string version;
    protected readonly string binaryFolder;

    protected string? binaryPath;

    protected string getWineLib => Path.Combine(Directory.GetDirectories(binaryFolder).First(), "lib");
    protected override string GetWineExecutable(RunnerManager.SpecialLaunchRequest? processName)
    {
        if (processName == RunnerManager.SpecialLaunchRequest.WineTricks)
            return "winetricks";

        return Path.Combine(Directory.GetDirectories(binaryFolder).First(), "bin", "wine64");
    }

    public RunnerDto_WineGE(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        version = runner.runnerVersion;
        binaryFolder = Path.Combine(rootLoc, "binaries", version);
    }

    public static new async Task<string[]?> GetRunnerVersions() => await GithubVersionHelper.GetRunnerVersions(GITHUB_NAME);

    public override async Task SetupRunner()
    {
        if (!Directory.Exists(binaryFolder))
        {
            await GithubVersionHelper.InstallWine(binaryFolder, GITHUB_NAME, version, (a) => a.GetProperty("content_type").GetString()?.Equals("application/x-xz") ?? false);
        }
    }


    public override async Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        var res = await base.InitRunDetails(game);
        res.whiteListedDirs.Add(binaryFolder);

        res.environmentArguments.Add("LD_LIBRARY_PATH", getWineLib);
        return res;
    }

    protected override void AddLogging(RunnerManager.LaunchArguments args, LoggingLevel level)
    {
        base.AddLogging(args, level);

        switch (level)
        {
            case LoggingLevel.High:
            case LoggingLevel.Everything:
                args.environmentArguments.Add("DXVK_LOG_LEVEL", "info");
                break;
        }
    }
}
