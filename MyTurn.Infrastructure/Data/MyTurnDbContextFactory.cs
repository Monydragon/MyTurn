using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyTurn.Infrastructure.Data;

public sealed class MyTurnDbContextFactory : IDesignTimeDbContextFactory<MyTurnDbContext>
{
    public MyTurnDbContext CreateDbContext(string[] args)
    {
        var databasePath = SqliteApplicationServices.GetDefaultDatabasePath();
        var options = new DbContextOptionsBuilder<MyTurnDbContext>()
            .UseSqlite(SqliteApplicationServices.CreateConnectionString(databasePath))
            .Options;

        return new MyTurnDbContext(options);
    }
}
