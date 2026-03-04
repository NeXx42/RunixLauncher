using Runix.Structure.DTOs;

namespace Runix.Structure.Interfaces.Repositories;

public interface IGameRepository
{
    public Task<(int[], int)> GameGameList(string sql, int page, int take, CancellationToken token);

    public Task<GameDTO?> GetGame(int gameId, CancellationToken cancellationToken);
    public Task DeleteGame(int gameId, CancellationToken cancellationToken);
    public Task SaveGame(GameDTO game, CancellationToken cancellationToken, params string[] columns);
}
