using CSharpSqliteORM;
using GameLibrary.DB.Tables;

namespace GameLibrary.Logic.Database.Migrations;

public class Migration_UpdateRunnerRoots : IDatabase_Migration
{
    public long migrationId => new DateTime(2026, 4, 26, 17, 32, 10).Ticks;

    public string Up()
    {
        return @$"
            update Runner set runnerRoot = replace(concat(runnerRoot, runnerName), ' ', '');
        ";
    }
}
