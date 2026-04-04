using CSharpSqliteORM;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;

namespace GameLibrary.Logic.Objects;

public class RunnerDto
{
    private static Dictionary<int, string[]?> cachedVersion = new Dictionary<int, string[]?>();

    public enum RunnerType
    {
        None = -1,
        Native = 0,
        Wine = 1,
        Wine_GE = 2,
        umu_Launcher = 3,
        Proton_GE = 4,
    }

    public enum RunnerConfigValues
    {
        Wine_Prefix,
        Wine_SharedDocuments,

        Generic_Sandbox_BlockNetwork,
        Generic_Sandbox_IsolateFilesystem,
    }


    public int runnerId { private set; get; }
    public RunnerType runnerType { set; get; }

    public string runnerName { set; get; }

    public string runnerRoot { set; get; }
    public string runnerVersion { set; get; }

    public bool isDefault { set; get; }

    public ConfigProvider<RunnerConfigValues> globalRunnerValues { private set; get; }

    public RunnerDto(dbo_Runner runner, dbo_RunnerConfig[] configValues)
    {
        this.runnerId = runner.runnerId;
        this.runnerType = (RunnerType)runner.runnerType;

        this.runnerName = runner.runnerName;

        this.runnerRoot = runner.runnerRoot;
        this.runnerVersion = runner.runnerVersion;

        this.isDefault = runner.isDefault ?? false;

        globalRunnerValues = new ConfigProvider<RunnerConfigValues>(configValues.Select(x => (x.settingKey, x.settingValue)), SaveConfigValue, DeleteConfigValue);
    }

    public static RunnerDto Create(dbo_Runner runner, dbo_RunnerConfig[] configValues)
    {
        switch ((RunnerType)runner.runnerType)
        {
            case RunnerType.Wine: return new RunnerDto_Wine(runner, configValues);
            case RunnerType.Wine_GE: return new RunnerDto_WineGE(runner, configValues);
            case RunnerType.umu_Launcher: return new RunnerDto_umu(runner, configValues);
            case RunnerType.Proton_GE: return new RunnerDto_ProtonGE(runner, configValues);

            default: return new RunnerDto(runner, configValues);
        }
    }

    // database

    public async Task UpdateDatabaseEntry(params string[] columns)
    {
        dbo_Runner dbo = new dbo_Runner()
        {
            runnerId = runnerId,
            runnerType = (int)runnerType,

            runnerName = runnerName,

            runnerRoot = runnerRoot,
            runnerVersion = runnerVersion,
        };

        await Database_Manager.Update(dbo, SQLFilter.Equal(nameof(dbo_Runner.runnerId), runnerId), columns);
    }

    private async Task SaveConfigValue(string key, string val)
    {
        dbo_RunnerConfig dbo = new dbo_RunnerConfig()
        {
            runnerId = runnerId,
            settingKey = key.ToString(),
            settingValue = val
        };

        await Database_Manager.AddOrUpdate(dbo, SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerId).Equal(nameof(dbo_RunnerConfig.settingKey), key), nameof(dbo_RunnerConfig.settingValue));
    }

    private async Task DeleteConfigValue(string key)
        => await Database_Manager.Delete<dbo_RunnerConfig>(SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerId).Equal(nameof(dbo_RunnerConfig.settingKey), key));


    // matchers

    public static async Task<string[]?> GetVersionsForRunnerTypes(int typeId)
    {
        if (cachedVersion.ContainsKey(typeId))
            return cachedVersion[typeId];

        string[]? result = null;

        switch ((RunnerType)typeId)
        {
            case RunnerType.Wine: result = await RunnerDto_Wine.GetRunnerVersions(); break;
            case RunnerType.Wine_GE: result = await RunnerDto_WineGE.GetRunnerVersions(); break;
            case RunnerType.umu_Launcher: result = await RunnerDto_umu.GetRunnerVersions(); break;
            case RunnerType.Proton_GE: result = await RunnerDto_ProtonGE.GetRunnerVersions(); break;
        }

        cachedVersion.Add(typeId, result);
        return result;
    }

    // logic

    public string GetRoot() => Path.Combine(runnerRoot, runnerName.Replace(" ", string.Empty));
    public virtual async Task SharePrefixDocuments(string path) => throw new Exception("Invalid profile");

    public bool IsValidExtension(string path)
    {
        string[] allowed = GetAcceptableExtensions();

        if (allowed.Length == 0)
            return true;

        foreach (string extension in allowed)
            if (path.EndsWith($".{extension}", StringComparison.CurrentCultureIgnoreCase))
                return true;

        return false;
    }

    protected void AddDefaultArgumentsToInit(ref RunnerManager.LaunchRequest game, ref RunnerManager.LaunchArguments res)
    {
        if (File.Exists(game.path))
        {
            res.workingDirectory = Path.GetDirectoryName(game.path);
        }

        if (game.gameConfig?.GetBoolean(Game_Config.Wine_ConsoleLaunched, false) ?? false)
        {
            string windowsPath = $"Z:/{game.path.Replace("\\", " / ")}";

            res.arguments[RunnerManager.ArgumentType.Launcher].AddLast("cmd");
            res.arguments[RunnerManager.ArgumentType.Launcher].AddLast("/c");
            res.arguments[RunnerManager.ArgumentType.Launcher].AddLast($"\"cd {Path.GetDirectoryName(windowsPath)} && {Path.GetFileName(windowsPath)} \"");
        }
        else
        {
            if (game.gameConfig?.GetBoolean(Game_Config.Wine_ExplorerLaunch, false) ?? false)
            {
                string? size = ConfigHandler.configProvider?.GetValue(ConfigKeys.Launcher_VirtualDesktopSize);
                size ??= "1920x1080";

                res.arguments[RunnerManager.ArgumentType.Launcher].AddLast("explorer");
                res.arguments[RunnerManager.ArgumentType.Launcher].AddLast($"/desktop=Game,{size}");
            }

            res.arguments[RunnerManager.ArgumentType.Application].AddLast(game.path);
        }


        if (game.gameConfig?.TryGetValue(Game_Config.General_Arguments, out string customArgs) ?? false)
        {
            string[] args = customArgs.Split(" ");

            foreach (string arg in args)
                res.arguments[RunnerManager.ArgumentType.Application].AddLast(arg);
        }

        if (game.gameConfig?.GetBoolean(Enums.Game_Config.Wine_Windowed, false) ?? false)
        {
            res.arguments[RunnerManager.ArgumentType.Application].AddLast("-windowed");
            res.arguments[RunnerManager.ArgumentType.Application].AddLast("-window");
            res.arguments[RunnerManager.ArgumentType.Application].AddLast("-w");
        }
    }

    public void SetIsDefault(bool to) => isDefault = to;
    public virtual string GetWineConfigurationToolName(RunnerManager.SpecialLaunchRequest req) => string.Empty;

    // Launching

    protected virtual string[] GetAcceptableExtensions() => [];

    public virtual Task SetupRunner() => Task.CompletedTask;

    public virtual Task<RunnerManager.LaunchArguments> InitRunDetails(RunnerManager.LaunchRequest req)
    {
        var args = new RunnerManager.LaunchArguments() { command = req.path };
        AddDefaultArgumentsToInit(ref req, ref args);

        args.whiteListedDirs.Add(req.path);

        return Task.FromResult(args);
    }
}
