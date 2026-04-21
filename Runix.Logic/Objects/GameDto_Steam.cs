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

    public override async Task Launch()
    {
        await RunnerManager.RunSteamGame(this);
    }

    public override bool IsRunning() => RunnerManager.IsIdentifierRunning(gameName);
}
