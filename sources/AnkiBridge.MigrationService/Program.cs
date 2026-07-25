using AnkiBridge.Infrastructure;
using AnkiBridge.MigrationService;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddPersistenceServices(dispatchDomainEvents: false);

builder.Services.Configure<MigrationServiceOptions>(
    builder.Configuration.GetSection(MigrationServiceOptions.SectionName));

builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
