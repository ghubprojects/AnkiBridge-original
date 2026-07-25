using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgresPassword = builder.AddParameter("postgres-password", secret: true)
    .WithDescription("Password for the local PostgreSQL development resource.");

var pixabayApiKey = builder.AddParameter("pixabay-api-key", secret: true)
    .WithDescription("Pixabay API key used by the Web image search provider.");

var pexelsApiKey = builder.AddParameter("pexels-api-key", secret: true)
    .WithDescription("Pexels API key used by the Web image search provider.");

var postgres = builder.AddPostgres("postgres", password: postgresPassword)
    .WithImageTag("17.10-alpine")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var database = postgres.AddDatabase("ankibridgedb");

var blobs = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite =>
        azurite.WithLifetime(ContainerLifetime.Persistent).WithDataVolume())
    .AddBlobs("blobs");

var migrationService = builder.AddProject<AnkiBridge_MigrationService>("migrationservice")
    .WithReference(database)
    .WaitFor(database);

builder.AddProject<AnkiBridge_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(database).WaitFor(database)
    .WithReference(blobs).WaitFor(blobs)
    .WaitForCompletion(migrationService)
    .WithEnvironment("Images__Pixabay__ApiKey", pixabayApiKey)
    .WithEnvironment("Images__Pexels__ApiKey", pexelsApiKey);

builder.Build().Run();
