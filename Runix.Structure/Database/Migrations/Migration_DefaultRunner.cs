using CSharpSqliteORM;
using GameLibrary.Logic.Database.Tables;

namespace Runix.Logic.Database.Migrations;

public class Migration_DefaultRunner : IDatabase_Migration
{
    public long migrationId => new DateTime(2026, 2, 8, 12, 4, 10).Ticks;

    public string Up()
    {
        return @$"
            ALTER TABLE {dbo_Runner.tableName} ADD COLUMN isDefault BIT Nullable;
        ";
    }
}
