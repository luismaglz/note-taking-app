using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using NTA.Api.Contracts;

namespace NTA.Api.Users.Tests;

/// <summary>
/// Verifies the note CRUD HTTP contract end to end.
/// </summary>
public sealed class NotesEndpointsTests(NotesApiFactory factory) : IClassFixture<NotesApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task NoteCrud_CreatesReadsUpdatesAndDeletesAnOwnedNote()
    {
        using var client = factory.CreateClient();
        var unauthorizedResponse = await client.GetAsync("/notes");

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

        var authenticationResponse = await client.PostAsync("/auth/7", null);
        var authentication = await authenticationResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        Assert.Equal(HttpStatusCode.OK, authenticationResponse.StatusCode);
        Assert.NotNull(authentication);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/notes",
            new CreateNoteRequest("Draft", "Initial content", NoteVisibilityContract.Private, null));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);
        Assert.NotNull(created);

        var listResponse = await client.GetAsync("/notes");
        var notes = await listResponse.Content.ReadFromJsonAsync<NoteResponse[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(notes!, note => note.Id == created.Id);

        var readResponse = await client.GetAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"/notes/{created.Id}",
            new UpdateNoteRequest("Published", "Updated content", NoteVisibilityContract.Public, null));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Published", updated.Title);
        Assert.Equal(NoteVisibilityContract.Public, updated.Visibility);

        var deleteResponse = await client.DeleteAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
