using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using GameLibrary.Logic.Enums;

namespace Runix.Structure.DTOs;

public class GameDTO
{
    public int id { get; set; }

    public string gameName { get; set; }
    public string gameFolder { get; set; }
    public string? executablePath { get; set; }
    public string? iconPath { get; set; }

    public int? libraryId { get; set; }
    public int? runnerId { get; set; }
    public Game_Status status { get; set; }

    public int? minsPlayed { get; set; }
    public DateTime? lastPlayed { get; set; }

    public HashSet<int> tags { get; set; }
    public Dictionary<string, string?> config;

    public GameDTO()
    {

    }

    public GameDTO(dbo_Game game, dbo_GameTag[] tags, dbo_GameConfig[] config)
    {
        this.id = game.id;
        this.gameName = game.gameName;
        this.gameFolder = game.gameFolder;
        this.executablePath = game.executablePath;
        this.iconPath = game.iconPath;
        this.libraryId = game.libraryId;
        this.runnerId = game.runnerId;
        this.status = (Game_Status)game.status;

        this.minsPlayed = game.minsPlayed;
        this.lastPlayed = game.lastPlayed;

        this.tags = tags.Select(x => x.TagId).ToHashSet();
        this.config = config.ToDictionary(x => x.configKey, x => x.configValue);
    }

    public dbo_Game CreateDatabaseObject() => new dbo_Game()
    {
        id = id,
        gameName = gameName,
        gameFolder = gameFolder,
        executablePath = executablePath,
        iconPath = iconPath,
        libraryId = libraryId,
        runnerId = runnerId,
        status = (int)status,

        lastPlayed = lastPlayed,
        minsPlayed = minsPlayed
    };
}
