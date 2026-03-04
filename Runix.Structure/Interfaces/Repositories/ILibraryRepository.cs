namespace Runix.Structure.Interfaces.Repositories;

public interface ILibraryRepository
{
    public Task<int[]> GetLinkedGameNames(int libraryId, CancellationToken? cancellationToken);
}
