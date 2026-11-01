using System.Text.Json.Serialization;
using NTA.Api.Users.Contracts;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;
using NTA.Infrastructure.Sqlite;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "Note Taking App Users API";
    options.Version = "v1";
});
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSqliteInfrastructure(
    builder.Configuration.GetConnectionString("Notes") ?? "Data Source=note-taking-app.db");
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseOpenApi();
app.UseSwaggerUi(options => options.DocumentTitle = "Note Taking App Users API");

app.MapGet("/", () => Results.Redirect("/swagger"));

await app.Services.EnsureSqliteDatabaseCreatedAsync();

app.MapPost("/notes", async (
    CreateNoteRequest request,
    INoteRepository repository,
    TimeProvider timeProvider,
    CancellationToken cancellationToken) =>
{
    try
    {
        var note = Note.Create(
            new UserId(request.OwnerId),
            request.Title,
            request.Content,
            request.Visibility,
            request.TeamId is null ? null : new TeamId(request.TeamId.Value),
            timeProvider.GetUtcNow());

        await repository.AddAsync(note, cancellationToken);
        return Results.Created($"/notes/{note.Id.Value}", ToResponse(note));
    }
    catch (ArgumentException exception)
    {
        return BadRequest(exception);
    }
});

app.MapGet("/notes", async (
    int userId,
    int[]? teamIds,
    INoteRepository repository,
    CancellationToken cancellationToken) =>
{
    try
    {
        var accessibleNotes = (await repository.ListAsync(cancellationToken))
            .Where(note => note.CanBeViewedBy(new UserId(userId), CreateTeamIds(teamIds)))
            .Select(ToResponse);

        return Results.Ok(accessibleNotes);
    }
    catch (ArgumentException exception)
    {
        return BadRequest(exception);
    }
});

app.MapGet("/notes/{noteId:guid}", async (
    Guid noteId,
    int userId,
    int[]? teamIds,
    INoteRepository repository,
    CancellationToken cancellationToken) =>
{
    try
    {
        var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
        if (note is null || !note.CanBeViewedBy(new UserId(userId), CreateTeamIds(teamIds)))
        {
            return Results.NotFound();
        }

        return Results.Ok(ToResponse(note));
    }
    catch (ArgumentException exception)
    {
        return BadRequest(exception);
    }
});

app.MapPut("/notes/{noteId:guid}", async (
    Guid noteId,
    int userId,
    UpdateNoteRequest request,
    INoteRepository repository,
    TimeProvider timeProvider,
    CancellationToken cancellationToken) =>
{
    try
    {
        var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
        if (note is null)
        {
            return Results.NotFound();
        }

        if (note.OwnerId != new UserId(userId))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        note.Update(
            request.Title,
            request.Content,
            request.Visibility,
            request.TeamId is null ? null : new TeamId(request.TeamId.Value),
            timeProvider.GetUtcNow());
        await repository.UpdateAsync(note, cancellationToken);

        return Results.Ok(ToResponse(note));
    }
    catch (ArgumentException exception)
    {
        return BadRequest(exception);
    }
});

app.MapDelete("/notes/{noteId:guid}", async (
    Guid noteId,
    int userId,
    INoteRepository repository,
    CancellationToken cancellationToken) =>
{
    try
    {
        var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
        if (note is null)
        {
            return Results.NotFound();
        }

        if (note.OwnerId != new UserId(userId))
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        await repository.DeleteAsync(note.Id, cancellationToken);
        return Results.NoContent();
    }
    catch (ArgumentException exception)
    {
        return BadRequest(exception);
    }
});

app.Run();

static HashSet<TeamId> CreateTeamIds(IEnumerable<int>? values) =>
    values?.Select(value => new TeamId(value)).ToHashSet() ?? [];

static NoteResponse ToResponse(Note note) =>
    new(
        note.Id.Value,
        note.OwnerId.Value,
        note.Title,
        note.Content,
        note.Visibility,
        note.TeamId?.Value,
        note.CreatedAt,
        note.UpdatedAt);

static IResult BadRequest(ArgumentException exception) =>
    Results.BadRequest(new { error = exception.Message });

public partial class Program { }
