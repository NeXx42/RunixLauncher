using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using Runix.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto_umu : RunnerDto_Wine
{
    private readonly string prefixLoc;

    private readonly string version;

    private static string getRuntimeLocationRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/compatibilitytools.d");

    public RunnerDto_umu(dbo_Runner runner, dbo_RunnerConfig[] configValues) : base(runner, configValues)
    {
        version = runnerVersion;
        prefixLoc = Path.Combine(rootLoc, "prefixes").CreateDirectoryIfNotExists();
    }

    public override Task SetupRunner() => Task.CompletedTask;

    public override string GetWineConfigurationToolName(RunnerManager.SpecialLaunchRequest req)
    {
        switch (req)
        {
            case RunnerManager.SpecialLaunchRequest.WineConfig: return "winecfg";
            case RunnerManager.SpecialLaunchRequest.WineCMD: return "cmd";
            case RunnerManager.SpecialLaunchRequest.WineRegistry: return "regedit";
        }

        return string.Empty;
    }

    public override Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest game)
    {
        RunnerManager.LaunchArguments res = new RunnerManager.LaunchArguments() { command = "umu-run" };
        WineHelper.GetPrefixName(prefixRoot, game, out string winePrefix);

        string protonPath = Path.Combine(getRuntimeLocationRoot, version);

        if (game.path.EndsWith(".bat"))
        {
            res.arguments[RunnerManager.ArgumentType.Launcher].AddLast("cmd");
            res.arguments[RunnerManager.ArgumentType.Launcher].AddLast("/c");
        }

        AddDefaultArgumentsToInit(ref game, ref res);

        res.environmentArguments.Add("WINEPREFIX", winePrefix);
        res.environmentArguments.Add("PROTONPATH", protonPath);
        res.environmentArguments.Add("STEAM_RUNTIME", "1");
        res.environmentArguments.Add("GAMEID", game.gameConfig?.GetValue(Game_Config.Launcher_umu_Id) ?? "0");

        res.environmentArguments.Add("UMU_LOG", "debug");

        string homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        res.whiteListedDirs = [
            protonPath,
            winePrefix,
            Path.GetDirectoryName(game.path)!,
            Path.Combine(homeDir, ".local/share/umu"),
            Path.Combine(homeDir, ".cache", "mesa_shader_cache"),
            Path.Combine(homeDir, ".cache", "AMDVLK"),
            Path.Combine(homeDir, ".cache", "dxvk"),
            Path.Combine(homeDir, ".cache", "vkd3d"),
        ];

        return Task.FromResult(res);
    }

    public new static async Task<string[]?> GetRunnerVersions()
    {
        if (!Directory.Exists(getRuntimeLocationRoot))
        {
            await DependencyManager.OpenYesNoModal("UMU failure", "No runtimes found");
            return ["INVALID"];
        }

        string[] files = Directory.GetDirectories(getRuntimeLocationRoot);
        return files.Select(x => Path.GetFileName(x)).ToArray();
    }

    public override async Task SharePrefixDocuments(string path) => await WineHelper.SharePrefixDataFolders(Path.Combine(prefixRoot, WineHelper.SHARED_PREFIX_NAME), path, this);
}
