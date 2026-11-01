using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using NTA.Api.Contracts;

namespace NTA.DemoSeeder;

/// <summary>
/// Creates the deterministic demonstration data set by using the public application APIs.
/// </summary>
public sealed class DemoDataSeeder
{
    private const int ManagerUserId = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient managementClient;
    private readonly HttpClient usersClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="DemoDataSeeder"/> class.
    /// </summary>
    public DemoDataSeeder(HttpClient managementClient, HttpClient usersClient)
    {
        this.managementClient = managementClient;
        this.usersClient = usersClient;
    }

    /// <summary>
    /// Ensures the complete demonstration data set exists without changing non-fixture data.
    /// </summary>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var managerToken = await GetTokenAsync(managementClient, ManagerUserId, cancellationToken);
        managementClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);

        foreach (var user in DemoFixtures.Users)
        {
            await EnsureUserAsync(user, cancellationToken);
        }

        foreach (var team in DemoFixtures.Teams)
        {
            await EnsureTeamAsync(team, cancellationToken);
            foreach (var memberId in team.MemberIds)
            {
                await EnsureTeamMemberAsync(team.Id, memberId, cancellationToken);
            }
        }

        var noteIds = new Dictionary<string, Guid>(StringComparer.Ordinal);
        foreach (var note in DemoFixtures.Notes)
        {
            var userToken = await GetTokenAsync(usersClient, note.OwnerId, cancellationToken);
            usersClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            noteIds.Add(note.Key, await EnsureNoteAsync(note, cancellationToken));
        }

        foreach (var grant in DemoFixtures.Grants)
        {
            var note = DemoFixtures.Notes.Single(item => item.Key == grant.NoteKey);
            var ownerToken = await GetTokenAsync(usersClient, note.OwnerId, cancellationToken);
            usersClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            await EnsureAccessGrantAsync(noteIds[grant.NoteKey], grant, cancellationToken);
        }
    }

    private async Task EnsureUserAsync(DemoUser fixture, CancellationToken cancellationToken)
    {
        var users = await GetRequiredAsync<UserResponse[]>(managementClient, "users", cancellationToken);
        var existing = users.SingleOrDefault(user => user.Id == fixture.Id);
        if (existing is not null)
        {
            EnsureFixtureName("user", fixture.Id, fixture.Name, existing.Name);
            return;
        }

        await PostExpectedAsync(
            managementClient,
            "users",
            new CreateUserRequest(fixture.Id, fixture.Name),
            HttpStatusCode.Created,
            cancellationToken);
    }

    private async Task EnsureTeamAsync(DemoTeam fixture, CancellationToken cancellationToken)
    {
        var teams = await GetRequiredAsync<TeamResponse[]>(managementClient, "teams", cancellationToken);
        var existing = teams.SingleOrDefault(team => team.Id == fixture.Id);
        if (existing is not null)
        {
            EnsureFixtureName("team", fixture.Id, fixture.Name, existing.Name);
            return;
        }

        await PostExpectedAsync(
            managementClient,
            "teams",
            new CreateTeamRequest(fixture.Id, fixture.Name),
            HttpStatusCode.Created,
            cancellationToken);
    }

    private async Task EnsureTeamMemberAsync(int teamId, int userId, CancellationToken cancellationToken)
    {
        var members = await GetRequiredAsync<TeamMemberResponse[]>(
            managementClient,
            $"teams/{teamId}/members",
            cancellationToken);
        if (members.Any(member => member.UserId == userId))
        {
            return;
        }

        using var response = await managementClient.PostAsJsonAsync(
            $"teams/{teamId}/members",
            new AddTeamMemberRequest(userId),
            JsonOptions,
            cancellationToken);
        if (response.StatusCode is not HttpStatusCode.OK and not HttpStatusCode.Conflict)
        {
            await ThrowUnexpectedResponseAsync(response, $"adding user '{userId}' to team '{teamId}'");
        }
    }

    private async Task<Guid> EnsureNoteAsync(DemoNote fixture, CancellationToken cancellationToken)
    {
        var notes = await GetRequiredAsync<NoteResponse[]>(usersClient, "notes", cancellationToken);
        var existing = notes.SingleOrDefault(note => note.OwnerId == fixture.OwnerId && note.Title == fixture.Title);
        if (existing is not null)
        {
            return existing.Id;
        }

        using var response = await usersClient.PostAsJsonAsync(
            "notes",
            new CreateNoteRequest(fixture.Title, fixture.Content),
            JsonOptions,
            cancellationToken);
        await EnsureExpectedStatusAsync(response, HttpStatusCode.Created, $"creating note '{fixture.Title}'");
        var created = await ReadRequiredAsync<NoteResponse>(
            response,
            $"creating note '{fixture.Title}'",
            cancellationToken);
        return created.Id;
    }

    private async Task EnsureAccessGrantAsync(
        Guid noteId,
        DemoGrant fixture,
        CancellationToken cancellationToken)
    {
        using var response = await usersClient.PostAsJsonAsync(
            $"notes/{noteId}/access-grants",
            new CreateNoteAccessGrantRequest(fixture.PrincipalKind, fixture.PrincipalId, fixture.Permission),
            JsonOptions,
            cancellationToken);
        if (response.StatusCode is not HttpStatusCode.Created and not HttpStatusCode.Conflict)
        {
            await ThrowUnexpectedResponseAsync(response, $"granting access to note '{noteId}'");
        }
    }

    private static async Task<string> GetTokenAsync(
        HttpClient client,
        int userId,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsync($"auth/{userId}", null, cancellationToken);
        await EnsureExpectedStatusAsync(response, HttpStatusCode.OK, $"authenticating user '{userId}'");
        return (await ReadRequiredAsync<AuthenticationResponse>(
            response,
            $"authenticating user '{userId}'",
            cancellationToken)).AccessToken;
    }

    private static async Task<T> GetRequiredAsync<T>(
        HttpClient client,
        string path,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(path, cancellationToken);
        await EnsureExpectedStatusAsync(response, HttpStatusCode.OK, $"getting '{path}'");
        return await ReadRequiredAsync<T>(response, $"getting '{path}'", cancellationToken);
    }

    private static async Task PostExpectedAsync<T>(
        HttpClient client,
        string path,
        T content,
        HttpStatusCode expectedStatus,
        CancellationToken cancellationToken)
    {
        using var response = await client.PostAsJsonAsync(path, content, JsonOptions, cancellationToken);
        await EnsureExpectedStatusAsync(response, expectedStatus, $"posting to '{path}'");
    }

    private static async Task EnsureExpectedStatusAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus,
        string operation)
    {
        if (response.StatusCode != expectedStatus)
        {
            await ThrowUnexpectedResponseAsync(response, operation);
        }
    }

    private static async Task<T> ReadRequiredAsync<T>(
        HttpResponseMessage response,
        string operation,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return content ?? throw new InvalidOperationException($"The API returned an empty response while {operation}.");
    }

    private static async Task ThrowUnexpectedResponseAsync(HttpResponseMessage response, string operation)
    {
        var responseBody = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"The API returned {(int)response.StatusCode} ({response.ReasonPhrase}) while {operation}. Response: {responseBody}");
    }

    private static void EnsureFixtureName(string kind, int id, string expectedName, string actualName)
    {
        if (!string.Equals(expectedName, actualName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Demo fixture {kind} '{id}' already exists with name '{actualName}', expected '{expectedName}'.");
        }
    }
}
