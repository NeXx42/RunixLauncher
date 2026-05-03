using System.Diagnostics;
using System.IO.IsolatedStorage;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;
using GameLibrary.Logic.GameEmbeds;
using GameLibrary.Logic.GameRunners;
using GameLibrary.Logic.Helpers;
using GameLibrary.Logic.Objects;
using Runix.Logic.Helpers;

namespace GameLibrary.Logic;

public static class RunnerManager
{
    private static SemaphoreSlim _mutex = new SemaphoreSlim(1, 1);

    private static int? cachedDefaultRunnerId;
    private static Dictionary<int, RunnerDto> cachedRunners = new Dictionary<int, RunnerDto>();
    private static Dictionary<string, ActiveProcess> activeGames = new Dictionary<string, ActiveProcess>();

    public static Action<string, bool>? onGameStatusChange;

    public static bool IsIdentifierRunning(string dir) => activeGames.ContainsKey(dir);

    public static string[] GetAcceptableTypes() => ["exe", "lnk", "AppImage", "bat"];
    public static bool IsUniversallyAcceptedExecutableFormat(string path)
    {
        string ext = Path.GetExtension(path);

        if (string.IsNullOrEmpty(ext))
            return false;

        ext = ext.Remove(0, 1);
        return GetAcceptableTypes().Any(x => x.Equals(ext, StringComparison.CurrentCultureIgnoreCase));
    }



    public static async Task Init()
    {
        await RecacheRunners();
    }


    public static async Task RunGame(LaunchRequest launchRequest)
    {
        if (activeGames.TryGetValue(launchRequest.identifier, out ActiveProcess? process))
        {
            if (process?.IsActive ?? false)
            {
                throw new Exception("Game is already running");
            }

            activeGames.Remove(launchRequest.identifier);
        }

        await _mutex.WaitAsync();

        try
        {
            RunnerDto? selectedRunner = GetRunnerProfile(launchRequest.runnerId);

            if (selectedRunner == null)
            {
                throw new Exception($"Couldn't find runner in the database. used id {launchRequest.runnerId ?? cachedDefaultRunnerId}");
            }

            if (launchRequest.gameId.HasValue && !File.Exists(launchRequest.path))
            {
                throw new Exception($"File doesn't exist - {launchRequest.path}");
            }

            if (!selectedRunner.IsValidExtension(launchRequest.path))
            {
                throw new Exception($"Invalid file for the runner - {launchRequest.path}");
            }

            if (!selectedRunner.IsInstalled(selectedRunner.runnerVersion))
            {
                throw new Exception($"The runner has not been installed\n\nDownload by editing the runner '{selectedRunner.runnerName}' and pressing download next to the version.");
            }

            Game? gameDto = await LibraryManager.GetGame(launchRequest.gameId, CancellationToken.None);

            LaunchArguments launchArguments = await selectedRunner.InitRunDetails(launchRequest);
            launchArguments.whiteListedDirs.AddRange(launchRequest.extraWhitelist ?? []);

            await HandleEmbeds(launchRequest.gameId, launchArguments, selectedRunner);

            launchArguments.gameId = gameDto?.gameId;
            launchArguments.identifier = launchRequest.identifier;
            launchArguments.loggingLevel = gameDto?.config.GetEnum(Game_Config.General_LoggingLevel, LoggingLevel.Off) ?? LoggingLevel.Off;

            if (gameDto?.config.GetBoolean(Game_Config.Launcher_UseSteamBridge, false) ?? false)
            {
                ExecuteRunRequestWithInjectedSteamRuntime(launchArguments, gameDto?.getAbsoluteLogFile);
            }
            else
            {
                ExecuteRunRequest(launchArguments, gameDto?.getAbsoluteLogFile);
            }

            if (gameDto != null)
            {
                if (string.IsNullOrEmpty(gameDto!.iconPath))
                {
                    await OverlayManager.LaunchOverlay(gameDto!.gameId);
                }

                await gameDto.UpdateLastPlayed();
            }
        }
        catch (Exception e)
        {
            await DependencyManager.OpenYesNoModal("Failed to launch game!", e.Message);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public static async Task RunSteamGame(Game_Steam game)
    {
        LaunchArguments dat = new LaunchArguments()
        {
            gameId = game.gameId,
            identifier = game.gameName,

            command = "steam"
        };
        dat.arguments[ArgumentType.Application].AddLast($"steam://rungameid/{game.appId}");

        ExecuteRunRequest(dat, null);
    }


    public static async Task RunWineTricks(int runnerId, SpecialLaunchRequest request)
    {
        if (IsIdentifierRunning($"{runnerId}_{request}"))
        {
            await DependencyManager.OpenYesNoModal("Already running", $"{request} is already running, close before trying again");
            return;
        }

        RunnerDto? runnerDto = GetRunnerProfile(runnerId);

        if (runnerDto == null)
            throw new Exception("No runner found");

        LaunchArguments req = await runnerDto.InitRunDetails(new LaunchRequest()
        {
            identifier = request.ToString(),
            runnerId = runnerId,
            customExecutable = request,
            path = ""
        });
        runnerDto.HandleSpecialLaunchRequest(req, request);

        req.loggingLevel = LoggingLevel.Off;
        ExecuteRunRequest(req, null);
    }




    private static async Task HandleEmbeds(int? gameId, LaunchArguments args, RunnerDto runnerDto)
    {
        ConfigProvider<RunnerDto.RunnerConfigValues> globalConfigValues = new ConfigProvider<RunnerDto.RunnerConfigValues>(runnerDto.globalRunnerValues); // new Dictionary<RunnerDto.RunnerConfigValues, string?>();
        List<IGameEmbed> embeds = new List<IGameEmbed>();

        if (gameId.HasValue)
        {
            Game? game = await LibraryManager.GetGame(gameId, CancellationToken.None);

            if (game != null)
            {
                if (game.config.GetBoolean(Game_Config.General_LocaleEmulation, false))
                    embeds.Add(new GameEmbed_Locale());
            }
        }


        if (ConfigHandler.isOnLinux)
        {
            if (ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Sandbox_Linux_Firejail_Enabled, false))
            {
                embeds.Add(new GameEmbed_Firejail());

                if (ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Sandbox_Linux_Firejail_Networking, false))
                {
                    await globalConfigValues.SaveBool(RunnerDto.RunnerConfigValues.Generic_Sandbox_BlockNetwork, true);
                }

                if (ConfigHandler.configProvider!.GetBoolean(ConfigKeys.Sandbox_Linux_Firejail_FileSystemIsolation, false))
                {
                    await globalConfigValues.SaveBool(RunnerDto.RunnerConfigValues.Generic_Sandbox_IsolateFilesystem, true);
                }
            }

            if (ConfigHandler.isFlatpak || true)
            {
                embeds.Add(new GameEmbed_Flatpak());
            }
        }

        foreach (IGameEmbed embed in embeds.OrderByDescending(x => x.getOrder))
            embed.Embed(args, globalConfigValues);
    }

    public static Process ExecuteRunRequest(LaunchArguments req, string? logFile)
    {
        if (req.loggingLevel == LoggingLevel.Off)
            logFile = null;

        ProcessStartInfo info = new ProcessStartInfo
        {
            FileName = req.command,
            WorkingDirectory = req.workingDirectory,

            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            RedirectStandardInput = true,
            ErrorDialog = true
        };



        foreach (ArgumentType type in Enum.GetValues(typeof(ArgumentType))) // maintain order
        {
            if (!req.arguments.TryGetValue(type, out LinkedList<string>? subArguments) || subArguments == null)
                continue;

            foreach (string arg in subArguments)
            {
                if (string.IsNullOrEmpty(arg))
                    continue;

                info.ArgumentList.Add(arg);
            }
        }


        foreach (var arg in req.environmentArguments)
        {
            try
            {
                info.EnvironmentVariables[arg.Key] = arg.Value;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        Process process = new Process
        {
            StartInfo = info,
            EnableRaisingEvents = true
        };

        if (!string.IsNullOrEmpty(req.identifier))
        {
            // ActiveProcess - starts the process
            activeGames.Add(req.identifier, new ActiveProcess(req.identifier, req.gameId, process, logFile));
            onGameStatusChange?.Invoke(req.identifier, true);
        }
        else
        {
            process.Start();
        }

        return process;
    }


    public static Process ExecuteRunRequestWithInjectedSteamRuntime(LaunchArguments req, string? logFile)
    {
        if (!(ConfigHandler.configProvider?.TryGetValue(ConfigKeys.Steam_BridgeShortcutId, out string shortcutId) ?? false))
            throw new Exception("Could not find steambridge id. Please validate the steambridge from within the settings page.");

        using (StreamWriter writer = new StreamWriter(SteamHelper.CreateSteamBridge()))
        {
            writer.WriteLine("#!/bin/bash");

            foreach (var env in req.environmentArguments)
            {
                writer.WriteLine($"export {env.Key}=\"{env.Value}\"");
            }

            writer.WriteLine($"cd \"{req.workingDirectory}\"");

            writer.WriteLine("\n");
            writer.Write($"exec \"{req.command}\" ");

            foreach (ArgumentType type in Enum.GetValues(typeof(ArgumentType))) // maintain order
            {
                if (!req.arguments.TryGetValue(type, out LinkedList<string>? subArguments) || subArguments == null)
                    continue;

                foreach (string arg in subArguments)
                {
                    if (string.IsNullOrEmpty(arg))
                        continue;

                    writer.Write($"\"{arg.Replace("\"", "\\\"")}\" ");
                }
            }
        }


        Process process = new Process
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = "steam",
                Arguments = $"steam://launch/{shortcutId}",

                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                ErrorDialog = true
            },
            EnableRaisingEvents = true
        };

        if (!string.IsNullOrEmpty(req.identifier))
        {
            // ActiveProcess - starts the process
            activeGames.Add(req.identifier, new ActiveProcess(req.identifier, req.gameId, process, logFile));
            onGameStatusChange?.Invoke(req.identifier, true);
        }
        else
        {
            process.Start();
        }

        return process;
    }


    public static void KillProcess(string identifier)
    {
        if (activeGames.ContainsKey(identifier))
            activeGames[identifier].Dispose();
    }

    public static async Task OnExitProcess(string identifier)
    {
        if (activeGames.TryGetValue(identifier, out ActiveProcess? process) && process != null)
        {
            Game? game = await LibraryManager.GetGame(process.getGameId, CancellationToken.None);

            if (game != null)
                await game!.UpdatePlayTime(process.GetPlayMinutes());

            process?.Dispose();
            activeGames.Remove(identifier);
        }

        onGameStatusChange?.Invoke(identifier, false);
    }




    // db stuff

    public static async Task CreateProfile(string path, RunnerDto.RunnerType type, string version)
    {
        path = Path.Combine(path, $"{type}_{Guid.NewGuid()}");

        dbo_Runner profile = new dbo_Runner()
        {
            runnerId = -1,
            runnerName = $"{type} NEW",
            runnerRoot = path,
            runnerType = (int)type,
            runnerVersion = version,

            isDefault = !cachedDefaultRunnerId.HasValue
        };

        await Database_Manager.InsertItem(profile);
        await RecacheRunners();
    }

    public static async Task RecacheRunners()
    {
        HashSet<int> existingRunners = cachedRunners.Keys.ToHashSet();
        dbo_Runner[] runnerDbs = await Database_Manager.GetItems<dbo_Runner>(SQLFilter.OrderAsc(nameof(dbo_Runner.runnerId)));

        foreach (dbo_Runner runner in runnerDbs)
        {
            if (cachedRunners.ContainsKey(runner.runnerId))
            {
                existingRunners.Remove(runner.runnerId);
                continue;
            }

            cachedRunners.Add(runner.runnerId, await GetRunnerProfile(runner));
        }

        foreach (int existing in existingRunners)
        {
            cachedRunners.Remove(existing);
        }

        cachedDefaultRunnerId = runnerDbs.FirstOrDefault(x => x.isDefault == true)?.runnerId;
    }

    public static async Task<RunnerDto> GetRunnerProfile(dbo_Runner? runnerDb)
    {
        if (runnerDb == null)
        {
            throw new Exception("Runner not found");
        }

        dbo_RunnerConfig[] globalConfig = await Database_Manager.GetItems<dbo_RunnerConfig>(SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), runnerDb.runnerId));
        return RunnerDto.Create(runnerDb, globalConfig);
    }

    public static RunnerDto? GetRunnerProfile(int? id) => id.HasValue || cachedDefaultRunnerId.HasValue ? cachedRunners[id ?? cachedDefaultRunnerId!.Value] : null;
    public static RunnerDto[] GetRunnerProfiles() => cachedRunners.Values.ToArray();

    public static async Task<int> GetGameCountForRunner(int runnerId)
        => await Database_Manager.GetCount<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.runnerId), runnerId));

    public static async Task<bool> DeleteRunnerProfile(int id)
    {
        RunnerDto runner = cachedRunners[id];

        try
        {
            Directory.Delete(runner.runnerRoot, true);
        }
        catch
        {
            if (!await DependencyManager.OpenYesNoModal("Failed deletion", "do you want to proceed with the record deletion?"))
                return false;
        }

        await LibraryManager.ClearRunner(id);

        await Database_Manager.Delete<dbo_RunnerConfig>(SQLFilter.Equal(nameof(dbo_RunnerConfig.runnerId), id));
        await Database_Manager.Delete<dbo_Runner>(SQLFilter.Equal(nameof(dbo_Runner.runnerId), id));


        dbo_Game temp = new dbo_Game()
        {
            gameFolder = "",
            gameName = "",
            status = 0,

            runnerId = null
        };

        await Database_Manager.Update(temp, SQLFilter.Equal(nameof(dbo_Game.runnerId), id), nameof(dbo_Game.runnerId));
        await RecacheRunners();

        return true;
    }

    public static async Task SetDefaultRunner(int runnerId)
    {
        string sql = $"UPDATE {dbo_Runner.tableName} SET {nameof(dbo_Runner.isDefault)} = case when {nameof(dbo_Runner.runnerId)} = {runnerId} then 1 else null end";
        await Database_Manager.ExecuteSQLNonQuery(sql, CancellationToken.None);

        cachedDefaultRunnerId = runnerId;

        foreach (RunnerDto runner in cachedRunners.Values)
            runner.SetIsDefault(runner.runnerId == runnerId);
    }


    // data

    public enum SpecialLaunchRequest
    {
        None,
        WineConfig,
        WineTricks,
        WineCMD,
        WineRegistry,
        WineJoystick
    }

    public struct LaunchRequest
    {
        public required string identifier;

        public int? gameId;
        public int? runnerId;

        public string path;
        public SpecialLaunchRequest? customExecutable;
        public string[]? extraWhitelist;

        public ConfigProvider<Game_Config>? gameConfig;
    }

    public enum ArgumentType
    {
        FireJail = -99,
        Launcher = 0,
        LocaleEmulator = 1,
        Application = 10,
        Flatpak = -9999
    }

    public class LaunchArguments
    {
        public int? gameId;
        public string? identifier;
        public LoggingLevel loggingLevel;

        public List<string> whiteListedDirs = new List<string>();

        public required string command;
        public string? workingDirectory;

        public Dictionary<ArgumentType, LinkedList<string>> arguments = new Dictionary<ArgumentType, LinkedList<string>>()
        {
            { ArgumentType.Launcher, new LinkedList<string>() },
            { ArgumentType.Application, new LinkedList<string>() },
        };

        public Dictionary<string, string> environmentArguments = new Dictionary<string, string>();

        public LaunchArguments()
        {
            // default required environment variables
            TryAddEnvironmentVariable("HOME");
            TryAddEnvironmentVariable("DISPLAY");
            TryAddEnvironmentVariable("WAYLAND_DISPLAY");
            TryAddEnvironmentVariable("XDG_RUNTIME_DIR");
            TryAddEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS");
            TryAddEnvironmentVariable("PULSE_SERVER");

            void TryAddEnvironmentVariable(string name)
            {
                if (environmentArguments.ContainsKey(name))
                    return;

                string? inherited = Environment.GetEnvironmentVariable(name);

                if (!string.IsNullOrEmpty(inherited))
                    environmentArguments.Add(name, inherited);
            }
        }
    }

    private class ActiveProcess : IDisposable
    {
        private readonly int? gameId;
        private readonly string identifier;
        private readonly Process process;

        private readonly DateTime startTime;

        private StreamWriter? writer;
        private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);

        public int? getGameId => gameId;
        public bool IsActive => !process.HasExited;

        public int GetPlayMinutes() => (int)(DateTime.UtcNow - startTime).TotalMinutes;

        public ActiveProcess(string identifier, int? gameId, Process process, string? logFile)
        {
            if (!string.IsNullOrEmpty(logFile))
            {
                writer = new StreamWriter(logFile, System.Text.Encoding.ASCII, new FileStreamOptions()
                {
                    Mode = FileMode.Create,
                    Access = FileAccess.ReadWrite,
                    Share = FileShare.ReadWrite
                });

                process.OutputDataReceived += OnOutput;
                process.ErrorDataReceived += OnOutput;
            }

            this.gameId = gameId;
            this.identifier = identifier;
            this.process = process;
            this.startTime = DateTime.UtcNow;

            process.Exited += HandleExit;

            process.Start();

            if (!string.IsNullOrEmpty(logFile))
            {
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
        }

        private async void OnOutput(object sender, DataReceivedEventArgs args)
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                if (writer != null)
                {
                    await _writeLock.WaitAsync();

                    try
                    {
                        await writer.WriteLineAsync(args.Data);
                        await writer.FlushAsync();
                    }
                    catch { }
                    finally
                    {
                        _writeLock.Release();
                    }
                }

                Console.WriteLine(args.Data);
            }
        }

        private void HandleExit(object? sender, EventArgs args)
        {
            _ = RunnerManager.OnExitProcess(identifier);
        }


        public void Dispose()
        {
            KillProcessTree(process.Id);
            writer?.Close();
        }


        [DllImport("libc")]
        private static extern int kill(int pid, int sig);

        public static void KillProcessTree(int pid)
        {
            var children = new List<int>();
            try
            {
                var psi = new ProcessStartInfo("pgrep", $"-P {pid}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                using var pgrep = Process.Start(psi);
                string? line;

                while ((line = pgrep!.StandardOutput.ReadLine()) != null)
                {
                    if (int.TryParse(line, out int childPid))
                        children.Add(childPid);
                }

                pgrep.WaitForExit();
            }
            catch { }

            foreach (var child in children)
                KillProcessTree(child);

            try
            {
                kill(pid, 9);
            }
            catch { }
        }
    }
}
