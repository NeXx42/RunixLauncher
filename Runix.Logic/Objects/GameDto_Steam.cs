using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;

namespace GameLibrary.Logic.Objects;

public class Game_Steam : Game
{
    public long appId { private set; get; }

    public Game_Steam(dbo_Game game, dbo_GameTag[] tags, dbo_GameConfig[] config) : base(game, tags, config)
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
