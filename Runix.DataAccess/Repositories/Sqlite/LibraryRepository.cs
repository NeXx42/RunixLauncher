using System.Data.SQLite;
using CSharpSqliteORM;
using GameLibrary.DB.Tables;
using Runix.Structure.Interfaces.Repositories;

namespace Runix.DataAccess.Repositories.Sqlite;

public class LibraryRepository : ILibraryRepository
{
    public async Task<int[]> GetLinkedGameNames(int libraryId, CancellationToken? cancellationToken)
    {
        string paramName = Database_Manager.GetGenericParameterName();
        string sql = $"SELECT {nameof(dbo_Game.id)} FROM {dbo_Game.tableName} WHERE {nameof(dbo_Game.libraryId)} = @{paramName}";

        return await Database_Manager.ExecuteSQLQuery(sql, Deserializer, cancellationToken, new SQLiteParameter(paramName, libraryId));

        async Task<int> Deserializer(SQLiteDataReader reader)
        {
            return (int)(long)reader[nameof(dbo_Game.id)];
        }
    }
}