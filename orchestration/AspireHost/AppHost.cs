var builder = DistributedApplication.CreateBuilder(args);

var sqliteDataDirectory = Path.Combine(builder.Environment.ContentRootPath, ".data");
Directory.CreateDirectory(sqliteDataDirectory);
var sqliteConnectionString = $"Data Source={Path.Combine(sqliteDataDirectory, "notes.db")}";

builder.AddContainer("notes-sqlite", "keinos/sqlite3", "latest")
    .WithBindMount(sqliteDataDirectory, "/data")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", "tail -f /dev/null");

builder.AddProject<Projects.NTA_Api_Users>("users-api")
    .WithEnvironment(
        "ConnectionStrings__Notes",
        sqliteConnectionString)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.NTA_Api_Management>("management-api")
    .WithEnvironment(
        "ConnectionStrings__Notes",
        sqliteConnectionString)
    .WithExternalHttpEndpoints();

builder.Build().Run();
