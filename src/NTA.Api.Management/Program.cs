using NTA.Core.Authentication;
using NTA.Infrastructure.Sqlite;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddSqliteInfrastructure(
    builder.Configuration.GetConnectionString("Notes") ?? "Data Source=note-taking-app.db");
builder.Services.AddCoreAuthentication(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseUserContext();
app.UseAuthorization();
app.MapControllers();

await app.Services.EnsureSqliteDatabaseCreatedAsync();

app.Run();

public partial class Program { }
