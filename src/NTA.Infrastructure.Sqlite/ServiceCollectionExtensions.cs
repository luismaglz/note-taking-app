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
        await dbContext.Database.ExecuteSqlRawAsync(
            """CREATE TABLE IF NOT EXISTS "Users" ("Id" INTEGER NOT NULL CONSTRAINT "PK_Users" PRIMARY KEY)""",
            cancellationToken);

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Users.AddRange(
            Enumerable.Range(1, 10).Select(id => new SqliteUser { Id = id }));
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
