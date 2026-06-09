namespace AnkiBridge.MigrationService;

public sealed class MigrationServiceOptions
{
    public const string SectionName = "MigrationService";

    public bool RunMigrations { get; init; } = true;

    public bool RunSeeders { get; init; } = true;
}