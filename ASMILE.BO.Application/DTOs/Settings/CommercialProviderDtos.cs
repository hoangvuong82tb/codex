namespace ASMILE.BO.Application.DTOs.Settings;

public sealed record LicenseStatusDto(
    bool IsAvailable,
    bool IsValid,
    string Status,
    string ProviderStatus,
    string? LicenseId,
    string? CustomerName,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<string> Features,
    string? Detail);

public sealed record UpdateCheckResultDto(
    bool IsAvailable,
    bool IsSignatureValid,
    bool IsUpdateAvailable,
    string Status,
    string CurrentVersion,
    string? LatestVersion,
    string? Channel,
    DateTime? PublishedAtUtc,
    string? PackageUri,
    string? PackageSha256,
    string? Detail);

public sealed record UpdatePackageStageResultDto(
    bool Success,
    string Status,
    string? Version,
    string? FilePath,
    long? SizeBytes,
    string? PackageSha256,
    string? Detail);
