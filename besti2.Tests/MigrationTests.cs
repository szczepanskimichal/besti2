using besti2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace besti2.Tests;

public class MigrationTests
{
    // check if the newest migration file matches the snapshot, so a forgotten migration file
    // (snapshot committed, migration not) turns CI red instead of breaking production
    [Fact]
    public void LastMigration_MatchesModelSnapshot()
    {
        // no database is needed, the connection string is never opened
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=none;Database=none")
            .Options;
        using var db = new AppDbContext(options);

        var migrations = db.GetService<IMigrationsAssembly>();
        var differ = db.GetService<IMigrationsModelDiffer>();
        var initializer = db.GetService<IModelRuntimeInitializer>();

        var lastMigration = migrations.CreateMigration(migrations.Migrations.Last().Value, db.Database.ProviderName!);
        var migrationModel = Finalize(lastMigration.TargetModel!, initializer);
        var snapshotModel = Finalize(migrations.ModelSnapshot!.Model, initializer);

        Assert.False(
            differ.HasDifferences(migrationModel.GetRelationalModel(), snapshotModel.GetRelationalModel()),
            $"The snapshot does not match the newest migration {migrations.Migrations.Last().Key}. Is a migration file missing?");
    }

    private static IModel Finalize(IModel model, IModelRuntimeInitializer initializer)
    {
        if (model is IMutableModel mutable)
            model = mutable.FinalizeModel();
        return initializer.Initialize(model, designTime: true);
    }
}