using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text.Json;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using Runix.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_ProtonGE : RunnerDto_Wine
{
    public const string GITHUB_NAME = "GloriousEggroll/proton-ge-custom";
    public static async new Task<string[]?> GetRunnerVersions() => await GithubVersionHelper.GetRunnerVersions(GITHUB_NAME);

    public RunnerDto_ProtonGE(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
    }

    public override bool IsInstalled(string? version) => !string.IsNullOrEmpty(version) && Directory.Exists(Path.Combine(runnerRoot, "binaries", version));
    public override LoadingTask DownloadVersion(string version) => GithubVersionHelper.InstallWine(GetBinaryPath(version), GITHUB_NAME, version, (a) => a.GetProperty("content_type").GetString()?.Equals("application/gzip") ?? false);

    public override Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        WineHelper.GetPrefixName(getPrefixRoot, game, out string winePrefix);
        string binaryRoot = Directory.GetDirectories(GetBinaryPath()).First();

        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = Path.Combine(binaryRoot, "proton") };

        res.arguments[RunnerManager.ArgumentType.Launcher].AddLast("run");

        AddDefaultArgumentsToInit(ref game, ref res);

        res.whiteListedDirs.Add(Path.GetDirectoryName(game.path)!);
        res.whiteListedDirs.Add(winePrefix);
        res.whiteListedDirs.Add(binaryRoot);

        Path.Combine(winePrefix, "pfx").CreateDirectoryIfNotExists();

        res.environmentArguments.Add("LD_LIBRARY_PATH", $"{Path.Combine(binaryRoot, "files", "lib")}:$LD_LIBRARY_PATH");
        res.environmentArguments.Add("STEAM_COMPAT_DATA_PATH", winePrefix);
        res.environmentArguments.Add("WINEPREFIX", Path.Combine(winePrefix, "pfx"));
        res.environmentArguments.Add("STEAM_COMPAT_CLIENT_INSTALL_PATH", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam"));


        AddLogging(res, game.gameConfig?.GetEnum(Game_Config.General_LoggingLevel, LoggingLevel.Off) ?? LoggingLevel.Off);

        return Task.FromResult(res);
    }

    public override async Task SharePrefixDocuments(string path) => await WineHelper.SharePrefixDataFolders(Path.Combine(runnerRoot, WineHelper.SHARED_PREFIX_NAME, "pfx"), path, this);
}
