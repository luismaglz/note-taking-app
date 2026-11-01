using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NTA.Api.Users.Tests;

/// <summary>
/// Hosts the Users API with an isolated SQLite database for integration tests.
/// </summary>
public sealed class NotesApiFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string databasePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        ".test-data",
        $"{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        builder.UseSetting("ConnectionStrings:Notes", $"Data Source={databasePath}");
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        foreach (var path in new[] { databasePath, $"{databasePath}-shm", $"{databasePath}-wal" })
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        var directory = Path.GetDirectoryName(databasePath);
        if (directory is not null && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
        {
            Directory.Delete(directory);
        }
    }
}
