using System.Diagnostics;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using Runix.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_Wine : RunnerDto
{
    protected readonly string rootLoc;
    protected readonly string prefixRoot;

    protected virtual string getSharedPrefixFolder => Path.Combine(prefixRoot, "shared");

    public static Task<string[]?> GetRunnerVersions() => Task.FromResult<string[]?>(null);


    protected virtual string GetWineExecutable(string processName) => processName;
    protected override string[] GetAcceptableExtensions() => ["exe", "bat"];

    public RunnerDto_Wine(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        rootLoc = GetRoot().CreateDirectoryIfNotExists();
        Path.Combine(rootLoc, "binaries").CreateDirectoryIfNotExists();
        Path.Combine(rootLoc, "prefixes").CreateDirectoryIfNotExists();

        prefixRoot = Path.Combine(rootLoc, "prefixes").CreateDirectoryIfNotExists();
        getSharedPrefixFolder.CreateDirectoryIfNotExists();
    }

    public override async Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = GetWineExecutable(game.customExecutable ?? "wine64") };
        WineHelper.GetPrefixName(prefixRoot, game, out string winePrefix);

        if (game.gameConfig?.GetBoolean(Game_Config.Wine_ConsoleLaunched, false) ?? false)
        {
            res.command = GetWineExecutable("wineconsole");
        }
        else if (game.gameConfig?.GetBoolean(Game_Config.Wine_ExplorerLaunch, false) ?? false)
        {
            res.arguments.AddLast("explorer");
            res.arguments.AddLast("/desktop=Game,800x600");
        }

        AddDefaultArgumentsToInit(ref game, ref res);

        res.whiteListedDirs.Add(Path.GetDirectoryName(game.path)!);
        res.whiteListedDirs.Add(winePrefix);
        res.whiteListedDirs.Add(rootLoc);

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

    public override async Task SharePrefixDocuments(string path) => await WineHelper.SharePrefixDataFolders(prefixRoot, string.Empty, path, this);
}
