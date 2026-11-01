using NTA.DemoSeeder;

var managementApiBaseUrl = GetRequiredBaseUrl("ManagementApi__BaseUrl");
var usersApiBaseUrl = GetRequiredBaseUrl("UsersApi__BaseUrl");

using var managementClient = new HttpClient { BaseAddress = managementApiBaseUrl };
using var usersClient = new HttpClient { BaseAddress = usersApiBaseUrl };

try
{
    await new DemoDataSeeder(managementClient, usersClient).SeedAsync(CancellationToken.None);
    Console.WriteLine("Demo data seeding completed.");
    return 0;
}
catch (HttpRequestException exception)
{
    Console.Error.WriteLine($"Demo data seeding failed while calling an API: {exception.Message}");
    return 1;
}
catch (InvalidOperationException exception)
{
    Console.Error.WriteLine($"Demo data seeding failed: {exception.Message}");
    return 1;
}
catch (System.Text.Json.JsonException exception)
{
    Console.Error.WriteLine($"Demo data seeding received an invalid API response: {exception.Message}");
    return 1;
}

static Uri GetRequiredBaseUrl(string environmentVariable)
{
    var value = Environment.GetEnvironmentVariable(environmentVariable);
    if (!Uri.TryCreate(value, UriKind.Absolute, out var baseUrl))
    {
        throw new InvalidOperationException(
            $"Required environment variable '{environmentVariable}' must contain an absolute API URL.");
    }

    return baseUrl;
}
