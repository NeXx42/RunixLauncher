using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using Runix.Structure.DTOs;

namespace GameLibrary.Logic.Objects;

public class Game_Steam : Game
{
    public long appId { private set; get; }

    public Game_Steam(GameDTO game) : base(game)
    {
        appId = long.Parse(game.executablePath!);
    }

    public override string getAbsoluteFolderLocation => folderPath;

    public override async Task<string?> FetchIconFilePath()
    {
        if (string.IsNullOrEmpty(iconPath))
            return null;

        if (iconPath.StartsWith("https://"))
        {
            using (HttpClient client = new HttpClient())
            {
                string folder = getAbsoluteFolderLocation;

                if (!Directory.Exists(folder))
                    return null;

                byte[] bytes = await client.GetByteArrayAsync(iconPath);

                iconPath = Path.Combine(folder, $"{Guid.NewGuid()}.png");
                await File.CreateText(iconPath).DisposeAsync();
                await File.WriteAllBytesAsync(iconPath, bytes);

                await UpdateDatabaseEntry(nameof(dbo_Game.iconPath));
            }
        }

        return iconPath;
    }

    public override async Task Launch()
    {
        await RunnerManager.RunSteamGame(this);
    }

    public override bool IsRunning() => RunnerManager.IsIdentifierRunning(gameName);
}
