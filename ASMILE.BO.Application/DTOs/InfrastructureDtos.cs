namespace ASMILE.BO.Application.DTOs;

public sealed record InfrastructureHealthReportDto(
    string Status,
    DateTime CheckedAt,
    IReadOnlyList<InfrastructureHealthCheckDto> Checks);

public sealed record InfrastructureHealthCheckDto(
    string Name,
    string Status,
    string Message);

public sealed record MigrationStatusDto(
    bool CanConnect,
    IReadOnlyList<string> PendingMigrations,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> SeedWarnings);

/// <summary>
/// Read-only database preflight evidence. The fingerprint is derived from the
/// sorted table names visible to the configured connection and is intended to
/// prevent applying migrations to an unverified database target.
/// </summary>
public sealed record DatabasePreflightDto(
    bool CanConnect,
    string Provider,
    string Server,
    string Database,
    string Schema,
    string SchemaFingerprint,
    IReadOnlyList<string> PendingMigrations,
    IReadOnlyList<string> AppliedMigrations,
    IReadOnlyList<string> SeedWarnings);
