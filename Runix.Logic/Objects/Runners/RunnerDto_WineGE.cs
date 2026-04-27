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
    protected string getWineLib => Path.Combine(Directory.GetDirectories(GetBinaryPath()).First(), "lib");


    public RunnerDto_WineGE(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
    }

    protected override string GetWineExecutable(RunnerManager.SpecialLaunchRequest? processName)
    {
        if (processName == RunnerManager.SpecialLaunchRequest.WineTricks)
            return "winetricks";

        return Path.Combine(Directory.GetDirectories(GetBinaryPath()).First(), "bin", "wine64");
    }

    public static new async Task<string[]?> GetRunnerVersions() => await GithubVersionHelper.GetRunnerVersions(GITHUB_NAME);
    public override bool IsInstalled(string? version) => !string.IsNullOrEmpty(version) && Directory.Exists(GetBinaryPath(version));

    public override async Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        var res = await base.InitRunDetails(game);
        res.whiteListedDirs.Add(GetBinaryPath());

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
