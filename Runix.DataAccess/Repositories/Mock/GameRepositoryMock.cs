using GameLibrary.DB.Tables;
using GameLibrary.Logic.Enums;
using Runix.Structure.DTOs;
using Runix.Structure.Interfaces.Repositories;

namespace Runix.DataAccess.Repositories.Mock;

public class GameRepositoryMock : IGameRepository
{
    private static readonly Random random = new Random();

    private static readonly string[] Adjectives =
    {
        "Swift", "Silent", "Dark", "Crimson", "Golden",
        "Frozen", "Lucky", "Wild", "Ancient", "Electric"
    };

    private static readonly string[] Nouns =
    {
        "Wolf", "Falcon", "Dragon", "Shadow", "Storm",
        "Hunter", "Phoenix", "Knight", "Raven", "Viper"
    };

    private int[] gameList;


    public GameRepositoryMock()
    {
        gameList = Enumerable.Range(0, random.Next(50, 100)).ToArray();
    }

    public Task DeleteGame(int gameId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<(int[], int)> GameGameList(string sql, int page, int take, CancellationToken token)
    {
        await Task.Delay(random.Next(100, 5000));
        return (gameList.Skip(page * take).Take(take).ToArray(), gameList.Length);
    }

    public async Task<GameDTO?> GetGame(int gameId, CancellationToken cancellationToken)
    {
        await Task.Delay(random.Next(100, 5000));
        return new GameDTO(new dbo_Game()
        {
            gameFolder = string.Empty,
            gameName = $"{Adjectives[random.Next(0, Adjectives.Length)]}{Nouns[random.Next(0, Nouns.Length)]}",
            status = (int)Game_Status.Active
        }, [], []);
    }

    public Task SaveGame(GameDTO game, CancellationToken cancellationToken, params string[] columns)
    {
        throw new NotImplementedException();
    }
}
