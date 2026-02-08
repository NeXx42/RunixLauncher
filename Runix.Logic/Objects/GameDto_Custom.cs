using System.Diagnostics;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.Objects;

public class GameDto_Custom : GameDto
{
    public GameDto_Custom(dbo_Game game, dbo_GameTag[] tags, dbo_GameConfig[] config) : base(game, tags, config)
    {
        runnerType = RunnerManager.GetRunnerProfile(game.runnerId)?.runnerType ?? RunnerDto.RunnerType.None;
    }

    public override async Task Launch()
    {
        await RunnerManager.RunGame(new RunnerManager.LaunchRequest()
        {
            identifier = gameName,

            gameId = gameId,
            path = getAbsoluteBinaryLocation,
            runnerId = runnerId,

            gameConfig = config
        });
    }

    public override bool IsRunning() => RunnerManager.IsIdentifierRunning(gameName);

    public override async Task ChangeBinaryLocation(string? path)
    {
        string newAbsolutePath = Path.Combine(getAbsoluteFolderLocation, path!);

        if (!File.Exists(newAbsolutePath))
        {
            return;
        }

        this.binaryPath = path;
        await UpdateDatabaseEntry(nameof(dbo_Game.executablePath));
    }

    public override async Task ChangeRunnerId(int? runnerId)
    {
        this.runnerId = runnerId;
        runnerType = RunnerManager.GetRunnerProfile(runnerId)?.runnerType; // ....

        await UpdateDatabaseEntry(nameof(dbo_Game.runnerId));
    }


    public override string PromoteTempFile(string path)
    {
        string extension = Path.GetExtension(path);
        string newName = $"{Guid.NewGuid()}{extension}";

        File.Move(path, Path.Combine(getAbsoluteFolderLocation, newName));
        return newName;
    }

    // misc

    public override (int? selected, string[] options)? GetPossibleBinaries()
    {
        if (!Directory.Exists(getAbsoluteFolderLocation))
            return (null, []);

        List<string> binaries = Directory.GetFiles(getAbsoluteFolderLocation).Where(RunnerManager.IsUniversallyAcceptedExecutableFormat).Select(x => Path.GetFileName(x)).ToList();
        return (binaries.IndexOf(binaryPath!), binaries.ToArray());
    }

    public override Task<string?> FetchIconFilePath() => Task.FromResult(string.IsNullOrEmpty(iconPath) ? null : getAbsoluteIconPath);

    public override (string msg, Func<Task> resolution)[] GetWarnings()
    {
        List<(string, Func<Task>)> warnings = new List<(string, Func<Task>)>();

        switch (runnerType)
        {
            case RunnerDto.RunnerType.Wine:
            case RunnerDto.RunnerType.Wine_GE:
            case RunnerDto.RunnerType.Proton_GE:
            case RunnerDto.RunnerType.umu_Launcher:
                if (folderPath.Contains(',') || folderPath.Contains('!'))
                    CreateFixer("Illegal Folder", "This will rename the folder to remove the illegal characters", ResolveFolderPath);
                break;

            case RunnerDto.RunnerType.AppImage:
                UnixFileMode info = new FileInfo(getAbsoluteBinaryLocation).UnixFileMode;

                if (!info.HasFlag(UnixFileMode.UserExecute))
                    CreateFixer("Not executable", "This file isnt marked as executable, Would you like to make it?", MakeAppImageExecutable);

                break;
        }

        return warnings.ToArray();

        void CreateFixer(string title, string desc, Func<Task> body)
        {
            warnings.Add((
                title,
                async () => await DependencyManager.OpenYesNoModalAsync(title, desc, body, "Fixing")
            ));
        }
    }

    private async Task ResolveFolderPath()
    {
        if (!Directory.Exists(getAbsoluteFolderLocation))
            return;

        string existing = getAbsoluteFolderLocation;

        folderPath = folderPath.Replace(",", string.Empty).Replace("!", string.Empty);
        Directory.Move(existing, getAbsoluteFolderLocation);

        await UpdateDatabaseEntry(nameof(dbo_Game.gameFolder));
    }

    private async Task MakeAppImageExecutable()
    {
        if (ConfigHandler.isOnLinux)
        {
#pragma warning disable CA1416 // Validate platform compatibility
            File.SetUnixFileMode(getAbsoluteBinaryLocation, new FileInfo(getAbsoluteBinaryLocation).UnixFileMode | UnixFileMode.UserExecute);
#pragma warning restore CA1416 // Validate platform compatibility
        }
    }
}