using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace Runix.Logic.Database.Migrations;

public class Migration_LibraryAlias : IDatabase_Migration
{
    public long migrationId => new DateTime(2026, 2, 7, 21, 42, 10).Ticks;

    public string Up()
    {
        return @$"
            ALTER TABLE {dbo_Libraries.tableName} ADD COLUMN alias TEXT Nullable;
        ";
    }
}
