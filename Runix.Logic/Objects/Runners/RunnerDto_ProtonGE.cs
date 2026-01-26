using System.Diagnostics;
using System.Text.Json;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using Runix.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_ProtonGE : RunnerDto
{
    public const string GITHUB_NAME = "GloriousEggroll/proton-ge-custom";

    protected readonly string prefixRoot;
    protected readonly string rootLoc;

    protected readonly string version;
    protected readonly string binaryFolder;

    protected string? binaryPath;

    protected override string[] GetAcceptableExtensions() => ["exe"];
    public static async Task<string[]?> GetRunnerVersions() => await GithubVersionHelper.GetRunnerVersions(GITHUB_NAME);

    public RunnerDto_ProtonGE(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        rootLoc = GetRoot().CreateDirectoryIfNotExists();
        prefixRoot = Path.Combine(rootLoc, "prefixes").CreateDirectoryIfNotExists();

        version = runner.runnerVersion;
        binaryFolder = Path.Combine(rootLoc, "binaries", version);
    }


    public override async Task SetupRunner()
    {
        if (!Directory.Exists(binaryFolder))
        {
            await GithubVersionHelper.InstallWine(binaryFolder, GITHUB_NAME, version, (a) => a.GetProperty("content_type").GetString()?.Equals("application/gzip") ?? false);
        }
    }

    public override Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        string prefixName = "shared";
        string binaryRoot = Directory.GetDirectories(binaryFolder).First();

        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = Path.Combine(binaryRoot, "proton") };

        res.arguments.AddLast("run");

        AddDefaultArgumentsToInit(ref game, ref res);

        res.whiteListedDirs.Add(Path.GetDirectoryName(game.path)!);
        res.whiteListedDirs.Add(Path.Combine(prefixRoot, prefixName));
        res.whiteListedDirs.Add(rootLoc);
        res.whiteListedDirs.Add(binaryRoot);

        res.environmentArguments.Add("LD_LIBRARY_PATH", $"{Path.Combine(binaryRoot, "files", "lib")}:$LD_LIBRARY_PATH");
        res.environmentArguments.Add("STEAM_COMPAT_DATA_PATH", Path.Combine(prefixRoot, prefixName));
        res.environmentArguments.Add("WINEPREFIX", "$STEAM_COMPAT_DATA_PATH/pfx");
        res.environmentArguments.Add("STEAM_COMPAT_CLIENT_INSTALL_PATH", "$HOME/.steam/steam");


        AddLogging(res, game.gameConfig?.GetEnum(Game_Config.General_LoggingLevel, LoggingLevel.Off) ?? LoggingLevel.Off);

        return Task.FromResult(res);
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
}
