using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using NTA.Api.Contracts;
using NTA.Domain.Models;

namespace NTA.Api.Users.Tests;

public sealed class NotesEndpointsTests(NotesApiFactory factory) : IClassFixture<NotesApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task NoteCrud_CreatesReadsUpdatesAndDeletesAnOwnedNote()
    {
        using var client = factory.CreateClient();
        var createResponse = await client.PostAsJsonAsync(
            "/notes",
            new CreateNoteRequest(7, "Draft", "Initial content", NoteVisibility.Private, null));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);
        Assert.NotNull(created);

        var listResponse = await client.GetAsync("/notes?userId=7");
        var notes = await listResponse.Content.ReadFromJsonAsync<NoteResponse[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(notes!, note => note.Id == created.Id);

        var readResponse = await client.GetAsync($"/notes/{created.Id}?userId=7");

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"/notes/{created.Id}?userId=7",
            new UpdateNoteRequest("Published", "Updated content", NoteVisibility.Public, null));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Published", updated.Title);
        Assert.Equal(NoteVisibility.Public, updated.Visibility);

        var deleteResponse = await client.DeleteAsync($"/notes/{created.Id}?userId=7");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync($"/notes/{created.Id}?userId=7");

        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
