using NTA.Core.Authentication;
using NTA.Infrastructure.Sqlite;
using NSwag;
using NSwag.Generation.Processors.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApiDocument(options =>
{
    options.Title = "Note Taking App Management API";
    options.Version = "v1";
    options.AddSecurity("Bearer", [], new OpenApiSecurityScheme
    {
        Type = OpenApiSecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Enter a manager JWT issued by POST /auth/{userId}."
    });
    options.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("Bearer"));
});
builder.Services.AddSqliteInfrastructure(
    builder.Configuration.GetConnectionString("Notes") ?? "Data Source=note-taking-app.db");
builder.Services.AddCoreAuthentication(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseOpenApi();
app.UseSwaggerUi(options => options.DocumentTitle = "Note Taking App Management API");
app.UseAuthentication();
app.UseUserContext();
app.UseAuthorization();
app.MapControllers();

await app.Services.EnsureSqliteDatabaseCreatedAsync();

app.Run();

public partial class Program { }
