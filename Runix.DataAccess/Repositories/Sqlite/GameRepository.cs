using System.Data.SQLite;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Database.Tables;
using Runix.Structure.DTOs;
using Runix.Structure.Interfaces.Repositories;

namespace Runix.DataAccess.Repositories.Sqlite;

public class GameRepository : IGameRepository
{
    public async Task<(int[], int)> GameGameList(string filterRequest, int page, int take, CancellationToken cancellationToken)
    {
        filterRequest += $" LIMIT {take} OFFSET {take * page};";

        int? totalCount = null;
        int[] res = await Database_Manager.GetItemsGeneric(filterRequest, DeserializeDatabaseRequest, cancellationToken);

        return (res, totalCount ?? 0);

        async Task<int> DeserializeDatabaseRequest(SQLiteDataReader reader)
        {
            totalCount ??= Convert.ToInt32(reader["total_count"]);
            return Convert.ToInt32(reader["id"]);
        }
    }

    public async Task DeleteGame(int gameId, CancellationToken cancellationToken)
    {
        await Database_Manager.Delete<dbo_GameConfig>(SQLFilter.Equal(nameof(dbo_GameConfig.gameId), gameId));
        await Database_Manager.Delete<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), gameId));
        await Database_Manager.Delete<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), gameId));
    }

    public async Task SaveGame(GameDTO game, CancellationToken cancellationToken, params string[] columns)
    {
        if (game.id == -1)
        {
            await Database_Manager.InsertItem(game.CreateDatabaseObject());
            return;
        }

        await Database_Manager.Update(game.CreateDatabaseObject(), SQLFilter.Equal(nameof(dbo_Game.id), game.id), columns);
    }

    public async Task<GameDTO?> GetGame(int gameId, CancellationToken cancellationToken)
    {
        dbo_Game? dboGame = await Database_Manager.GetItem<dbo_Game>(SQLFilter.Equal(nameof(dbo_Game.id), gameId), cancellationToken);

        if (dboGame == null || cancellationToken.IsCancellationRequested)
            return null;

        dbo_GameTag[] dboTags = await Database_Manager.GetItems<dbo_GameTag>(SQLFilter.Equal(nameof(dbo_GameTag.GameId), gameId), cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            return null;

        dbo_GameConfig[] dboConfig = await Database_Manager.GetItems<dbo_GameConfig>(SQLFilter.Equal(nameof(dbo_GameConfig.gameId), gameId), cancellationToken);

        if (cancellationToken.IsCancellationRequested)
            return null;

        return new GameDTO(dboGame, dboTags, dboConfig);
    }
}
