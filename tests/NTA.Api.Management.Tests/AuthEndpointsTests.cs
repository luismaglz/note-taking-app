using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using NTA.Api.Contracts;

namespace NTA.Api.Management.Tests;

/// <summary>
/// Verifies manager token issuance for seeded users.
/// </summary>
public sealed class AuthEndpointsTests(ManagementAuthFactory factory) : IClassFixture<ManagementAuthFactory>
{
    /// <summary>
    /// Returns a manager-role token for a seeded user.
    /// </summary>
    [Fact]
    public async Task Authenticate_ReturnsManagerTokenForSeededUser()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsync("/auth/1", null);
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(authentication);
        Assert.Contains("manager", DecodePayload(authentication.AccessToken), StringComparison.Ordinal);
    }

    private static string DecodePayload(string token)
    {
        var payload = token.Split('.')[1]
            .Replace('-', '+')
            .Replace('_', '/');
        payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');

        using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
        return document.RootElement.ToString();
    }
}
