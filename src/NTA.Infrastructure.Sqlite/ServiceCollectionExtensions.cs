using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NTA.Domain.Interfaces;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Provides dependency-injection registration for SQLite infrastructure services.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<NoteDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<INoteRepository, SqliteNoteRepository>();
        services.AddScoped<INoteAccessPolicyRepository, SqliteNoteAccessPolicyRepository>();
        services.AddScoped<ITeamRepository, SqliteTeamRepository>();
        services.AddScoped<IUserRepository, SqliteUserRepository>();

        return services;
    }

    public static async Task EnsureSqliteDatabaseCreatedAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NoteDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await RemoveLegacyNoteSharingColumnsAsync(dbContext, cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """CREATE TABLE IF NOT EXISTS "Users" ("Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY, "Name" TEXT NOT NULL DEFAULT '')""",
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """CREATE TABLE IF NOT EXISTS "Teams" ("Id" INTEGER NOT NULL CONSTRAINT "PK_Teams" PRIMARY KEY, "Name" TEXT NOT NULL DEFAULT '')""",
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """CREATE TABLE IF NOT EXISTS "NoteAccessPolicies" ("NoteId" TEXT NOT NULL CONSTRAINT "PK_NoteAccessPolicies" PRIMARY KEY)""",
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """CREATE TABLE IF NOT EXISTS "NoteAccessGrants" ("NoteId" TEXT NOT NULL, "PrincipalKind" TEXT NOT NULL, "PrincipalId" INTEGER NOT NULL, "Permission" TEXT NOT NULL, CONSTRAINT "PK_NoteAccessGrants" PRIMARY KEY ("NoteId", "PrincipalKind", "PrincipalId", "Permission"))""",
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """CREATE TABLE IF NOT EXISTS "TeamMemberships" ("TeamId" INTEGER NOT NULL, "UserId" INTEGER NOT NULL, CONSTRAINT "PK_TeamMemberships" PRIMARY KEY ("TeamId", "UserId"))""",
            cancellationToken);
        await dbContext.Database.ExecuteSqlRawAsync(
            """INSERT INTO "NoteAccessPolicies" ("NoteId") SELECT "Id" FROM "Notes" WHERE NOT EXISTS (SELECT 1 FROM "NoteAccessPolicies" WHERE "NoteAccessPolicies"."NoteId" = "Notes"."Id")""",
            cancellationToken);

        var usersHaveNameColumn = await dbContext.Database.SqlQueryRaw<int>(
            """SELECT COUNT(*) AS "Value" FROM pragma_table_info('Users') WHERE name = 'Name'""")
            .SingleAsync(cancellationToken) > 0;
        if (!usersHaveNameColumn)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Users" ADD COLUMN "Name" TEXT NOT NULL DEFAULT ''""",
                cancellationToken);
        }

        var teamsHaveNameColumn = await dbContext.Database.SqlQueryRaw<int>(
            """SELECT COUNT(*) AS "Value" FROM pragma_table_info('Teams') WHERE name = 'Name'""")
            .SingleAsync(cancellationToken) > 0;
        if (!teamsHaveNameColumn)
        {
            await dbContext.Database.ExecuteSqlRawAsync(
                """ALTER TABLE "Teams" ADD COLUMN "Name" TEXT NOT NULL DEFAULT ''""",
                cancellationToken);
        }

        foreach (var user in await dbContext.Users.ToListAsync(cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(user.Name))
            {
                user.Name = $"User {user.Id}";
            }
        }

        if (!await dbContext.Users.AnyAsync(cancellationToken))
        {
            dbContext.Users.AddRange(
                Enumerable.Range(1, 10).Select(id => new SqliteUser { Id = id, Name = $"User {id}" }));
        }

        foreach (var team in await dbContext.Teams.ToListAsync(cancellationToken))
        {
            if (string.IsNullOrWhiteSpace(team.Name))
            {
                team.Name = $"Team {team.Id}";
            }
        }

        if (!await dbContext.Teams.AnyAsync(cancellationToken))
        {
            dbContext.Teams.Add(new SqliteTeam { Id = 1, Name = "Team 1" });
            dbContext.TeamMemberships.AddRange(
                new SqliteTeamMembership { TeamId = 1, UserId = 2 },
                new SqliteTeamMembership { TeamId = 1, UserId = 3 });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task RemoveLegacyNoteSharingColumnsAsync(
        NoteDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var connection = dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """PRAGMA table_info("Notes")""";

            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            {
                await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    columns.Add(reader.GetString(1));
                }
            }

            if (!columns.Contains("Visibility") && !columns.Contains("TeamId"))
            {
                return;
            }

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            command.Transaction = transaction;
            command.CommandText =
                """CREATE TABLE "Notes_NoteAccessPolicy" ("Id" TEXT NOT NULL CONSTRAINT "PK_Notes" PRIMARY KEY, "OwnerId" INTEGER NOT NULL, "Title" TEXT NOT NULL, "Content" TEXT NOT NULL, "CreatedAtUnixMilliseconds" INTEGER NOT NULL, "UpdatedAtUnixMilliseconds" INTEGER NOT NULL)""";
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.CommandText =
                """INSERT INTO "Notes_NoteAccessPolicy" ("Id", "OwnerId", "Title", "Content", "CreatedAtUnixMilliseconds", "UpdatedAtUnixMilliseconds") SELECT "Id", "OwnerId", "Title", "Content", "CreatedAtUnixMilliseconds", "UpdatedAtUnixMilliseconds" FROM "Notes" """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.CommandText = """DROP TABLE "Notes" """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            command.CommandText = """ALTER TABLE "Notes_NoteAccessPolicy" RENAME TO "Notes" """;
            await command.ExecuteNonQueryAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }
}
