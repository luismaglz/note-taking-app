using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NTA.Domain.Interfaces;

namespace NTA.Infrastructure.Sqlite;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqliteInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<NoteDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<INoteRepository, SqliteNoteRepository>();

        return services;
    }

    public static async Task EnsureSqliteDatabaseCreatedAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NoteDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
