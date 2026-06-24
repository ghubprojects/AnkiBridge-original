using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddDockerComposeEnvironment("compose");

//var redis = builder.AddRedis("redis");

var postgres = builder.AddPostgres("postgres")
    .WithImageTag("latest")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgWeb();

var database = postgres.AddDatabase("ankibridgedb");

var blobs = builder.AddAzureStorage("storage")
    .RunAsEmulator(azurite => azurite.WithLifetime(ContainerLifetime.Persistent))
    .AddBlobs("blobs");

var migrationService = builder.AddProject<AnkiBridge_MigrationService>("migrationservice")
    .WithReference(database).WaitFor(database);

builder.AddProject<AnkiBridge_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    //.WithReference(redis)
    .WithReference(database).WaitFor(database)
    .WithReference(blobs).WaitFor(blobs)
    .WaitForCompletion(migrationService);

builder.Build().Run();
