using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace NTA.Api.Management.Tests;

/// <summary>
/// Hosts the Management API with an isolated SQLite database for integration tests.
/// </summary>
public sealed class ManagementAuthFactory : WebApplicationFactory<Program>, IAsyncDisposable
{
    private readonly string databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.db");

    /// <inheritdoc />
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:Notes", $"Data Source={databasePath}");

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (File.Exists(databasePath))
        {
            File.Delete(databasePath);
        }
    }
}
