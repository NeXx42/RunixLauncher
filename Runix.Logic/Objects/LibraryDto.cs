using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using GameLibrary.Logic.Enums;

namespace GameLibrary.Logic.Objects;


public class LibraryDto
{
    public readonly int libraryId;

    public string alias { protected set; get; }
    public string root { protected set; get; }

    public Library_ExternalProviders? externalType { protected set; get; }

    public string getName => string.IsNullOrEmpty(alias) ? root : alias;

    public LibraryDto(dbo_Libraries lib)
    {
        libraryId = lib.libaryId;
        root = lib.rootPath;
        externalType = lib.libraryExternalType.HasValue ? (Library_ExternalProviders)lib.libraryExternalType : null;
    }

    private async Task UpdateDatabaseEntry(params string[] columns)
    {
        await Database_Manager.Update(new dbo_Libraries()
        {
            rootPath = root,
            libaryId = libraryId,
            alias = alias,
            libraryExternalType = (int?)externalType

        }, SQLFilter.Equal(nameof(dbo_Libraries.libaryId), libraryId), columns);
    }

    public async Task UpdateAlias(string to)
    {
        alias = to;
        await UpdateDatabaseEntry(nameof(dbo_Libraries.alias));
    }
}
