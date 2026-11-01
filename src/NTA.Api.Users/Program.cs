using NTA.Infrastructure.Sqlite;
using NSwag;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "Note Taking App Users API";
    options.Version = "v1";
});
builder.Services.AddSqliteInfrastructure(
    builder.Configuration.GetConnectionString("Notes") ?? "Data Source=note-taking-app.db");
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseOpenApi();
app.UseSwaggerUi(options => options.DocumentTitle = "Note Taking App Users API");

app.MapGet("/", () => Results.Redirect("/swagger"));
app.MapControllers();

await app.Services.EnsureSqliteDatabaseCreatedAsync();

app.Run();

public partial class Program { }
