using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using Runix.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_Wine : RunnerDto
{
    protected string getPrefixRoot => Path.Combine(runnerRoot, "prefixes").CreateDirectoryIfNotExists();

    protected string GetBinaryPath(string? version = null) => Path.Combine(runnerRoot, "binaries", string.IsNullOrEmpty(version) ? runnerVersion : version);
    public static Task<string[]?> GetRunnerVersions() => Task.FromResult<string[]?>(null);

    protected virtual string GetWineExecutable(RunnerManager.SpecialLaunchRequest? launchRequest) => "wine";

    public override void HandleSpecialLaunchRequest(RunnerManager.LaunchArguments args, RunnerManager.SpecialLaunchRequest req)
    {
        LinkedList<string> launch = new LinkedList<string>();

        switch (req)
        {
            case RunnerManager.SpecialLaunchRequest.WineConfig:
                launch.AddFirst("winecfg");
                break;
            case RunnerManager.SpecialLaunchRequest.WineCMD:
                launch.AddFirst("cmd");
                break;
            case RunnerManager.SpecialLaunchRequest.WineTricks:
                args.command = "winetricks";
                break;
            case RunnerManager.SpecialLaunchRequest.WineRegistry:
                launch.AddFirst("regedit");
                break;
            case RunnerManager.SpecialLaunchRequest.WineJoystick:
                launch.AddFirst("control");
                launch.AddLast("joy.cpl");
                break;
        }

        args.arguments[RunnerManager.ArgumentType.Application] = launch;
    }

    protected override string[] GetAcceptableExtensions() => ["exe", "bat"];


    public RunnerDto_Wine(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
    }

    public override async Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = GetWineExecutable(game.customExecutable) };
        WineHelper.GetPrefixName(getPrefixRoot, game, out string winePrefix);

        AddDefaultArgumentsToInit(ref game, ref res);

        res.whiteListedDirs.Add(Path.GetDirectoryName(game.path)!);
        res.whiteListedDirs.Add(winePrefix);
        res.whiteListedDirs.Add(runnerRoot);

        res.environmentArguments.Add("WINEPREFIX", winePrefix);

        AddLogging(res, game.gameConfig?.GetEnum(Game_Config.General_LoggingLevel, LoggingLevel.Off) ?? LoggingLevel.Off);
        return res;
    }

    protected virtual void AddLogging(RunnerManager.LaunchArguments args, LoggingLevel level)
    {
        string? logString = null;

        switch (level)
        {
            case LoggingLevel.Low:
                logString = "+err";
                break;

            case LoggingLevel.High:
                logString = "+err,+warn";
                break;

            case LoggingLevel.Everything:
                logString = "+all";
                break;
        }

        if (string.IsNullOrEmpty(logString))
            return;

        args.environmentArguments.Add("WINEDEBUG", logString);
    }


    protected virtual async Task RunWine(params string[] args)
    {
        await RunnerManager.ExecuteRunRequest(await InitRunDetails(new RunnerManager.LaunchRequest()
        {
            identifier = "wine",
            path = "reg",
        }), null).WaitForExitAsync();
    }

    public override async Task SharePrefixDocuments(string path) => await WineHelper.SharePrefixDataFolders(Path.Combine(runnerRoot, WineHelper.SHARED_PREFIX_NAME), path, this);

    public override async Task CleanProfile(Game? game)
    {
        if (!(game?.config?.GetBoolean(Game_Config.Wine_IsolatedPrefix, false) ?? false))
        {
            await DependencyManager.OpenMessageDialog("Cannot clean.", "Cannot clean the shared profile.");
            return;
        }

        string path = Path.Combine(getPrefixRoot, game.gameId.ToString());

        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return;

        Directory.Delete(path, true);
    }
}
