using GameLibrary.Logic;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;
using Runix.Logic.Helpers;

namespace Runix.Logic.Objects.Runners;

public class RunnerDto_ProtonPlus : RunnerDto_Wine
{
    protected override string GetBinaryPath(string? version = null) => version ?? runnerVersion;
    public override Task<string[]?> GetRunnerVersion()
    {
        string[] candidates =
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".share", "steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".share", "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam", "debian-installation"),

            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".var", "app", "com.valvesoftware.Steam", ".steam", "steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
        };

        List<string> versions = new List<string>();

        foreach (string root in candidates)
        {
            string tools = Path.Combine(root, "compatibilitytools.d");

            if (Directory.Exists(tools))
                versions.AddRange(Directory.GetDirectories(tools));
        }

        return Task.FromResult(versions.ToArray())!;
    }

    public RunnerDto_ProtonPlus(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
    }

    public override bool IsInstalled(string? version) => !string.IsNullOrEmpty(version) && Directory.Exists(Path.Combine(runnerRoot, "binaries", version));

    public override Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        WineHelper.GetPrefixName(getPrefixRoot, game, out string winePrefix);
        string binaryRoot = GetBinaryPath();

        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = Path.Combine(binaryRoot, "proton") };

        res.arguments[RunnerManager.ArgumentType.Launcher].AddLast("run");

        AddLogging(res, game.gameConfig?.GetEnum(Game_Config.General_LoggingLevel, LoggingLevel.Off) ?? LoggingLevel.Off);
        AddDefaultArgumentsToInit(ref game, ref res);

        res.whiteListedDirs.Add(Path.GetDirectoryName(game.path)!);
        res.whiteListedDirs.Add(winePrefix);
        res.whiteListedDirs.Add(binaryRoot);

        Path.Combine(winePrefix, "pfx").CreateDirectoryIfNotExists();

        res.environmentArguments.Add("LD_LIBRARY_PATH", $"{Path.Combine(binaryRoot, "files", "lib")}:$LD_LIBRARY_PATH");
        res.environmentArguments.Add("STEAM_COMPAT_DATA_PATH", winePrefix);
        res.environmentArguments.Add("WINEPREFIX", Path.Combine(winePrefix, "pfx"));
        res.environmentArguments.Add("STEAM_COMPAT_CLIENT_INSTALL_PATH", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".steam/steam"));

        return Task.FromResult(res);
    }

    public override async Task SharePrefixDocuments(string path) => await WineHelper.SharePrefixDataFolders(Path.Combine(getPrefixRoot, WineHelper.SHARED_PREFIX_NAME, "pfx"), path, this);
}
