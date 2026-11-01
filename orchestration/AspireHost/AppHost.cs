var builder = DistributedApplication.CreateBuilder(args);

var sqliteDataDirectory = Path.Combine(builder.Environment.ContentRootPath, ".data");
Directory.CreateDirectory(sqliteDataDirectory);
var sqliteConnectionString = $"Data Source={Path.Combine(sqliteDataDirectory, "notes.db")}";

var sqlite = builder.AddContainer("notes-sqlite", "keinos/sqlite3", "latest")
    .WithBindMount(sqliteDataDirectory, "/data")
    .WithEntrypoint("/bin/sh")
    .WithArgs("-c", "tail -f /dev/null");

var usersApi = builder.AddProject<Projects.NTA_Api_Users>("users-api")
    .WithEnvironment(
        "ConnectionStrings__Notes",
        sqliteConnectionString)
    .WaitFor(sqlite)
    .WithExternalHttpEndpoints();

var managementApi = builder.AddProject<Projects.NTA_Api_Management>("management-api")
    .WithEnvironment(
        "ConnectionStrings__Notes",
        sqliteConnectionString)
    .WaitFor(sqlite)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.NTA_DemoSeeder>("demo-seeder")
    .WithEnvironment("ManagementApi__BaseUrl", managementApi.GetEndpoint("http"))
    .WithEnvironment("UsersApi__BaseUrl", usersApi.GetEndpoint("http"))
    .WaitFor(managementApi)
    .WaitFor(usersApi);

builder.Build().Run();
