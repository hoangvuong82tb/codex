using System.Net;
using System.Security.Cryptography;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Security;
using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Downloads a package referenced by a freshly re-verified signed manifest and stages it
/// only after its SHA-256 matches. This deliberately does not extract or install packages.
/// </summary>
public sealed class UpdatePackageStagingService : IUpdatePackageStagingService
{
    private const long MaxPackageBytes = 1024L * 1024 * 1024;
    private const int MaxRedirects = 5;
    private readonly IUpdateManifestService _manifestService;
    private readonly IAuthorizationService _authorizationService;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public UpdatePackageStagingService(
        IUpdateManifestService manifestService,
        IAuthorizationService authorizationService,
        HttpClient httpClient,
        IConfiguration configuration)
    {
        _manifestService = manifestService;
        _authorizationService = authorizationService;
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<UpdatePackageStageResultDto> StageLatestAsync(
        string channel,
        Version currentVersion,
        CancellationToken cancellationToken = default)
    {
        if (!_authorizationService.HasPermission(Permission.ManageServices))
        {
            return Failure("PermissionDenied", "Bạn không có quyền tải gói cập nhật hệ thống.");
        }

        var verified = await _manifestService.CheckAsync(channel, currentVersion, cancellationToken);
        if (!verified.IsAvailable || !verified.IsSignatureValid || !verified.IsUpdateAvailable
            || !Version.TryParse(verified.LatestVersion, out var latestVersion)
            || latestVersion <= currentVersion
            || !Version.TryParse(verified.CurrentVersion, out var verifiedCurrentVersion)
            || verifiedCurrentVersion != currentVersion
            || !Uri.TryCreate(verified.PackageUri, UriKind.Absolute, out var packageUri)
            || !IsAllowedHttpsUri(packageUri)
            || !IsSha256(verified.PackageSha256))
        {
            return Failure("ManifestNotVerified", "Không có gói cập nhật mới hơn phiên bản đang chạy với manifest, HTTPS URI và SHA-256 hợp lệ đã được xác minh.");
        }

        var stagingDirectory = GetStagingDirectory();
        string? temporaryPath = null;
        try
        {
            Directory.CreateDirectory(stagingDirectory);
            var extension = GetSafeExtension(packageUri);
            var finalPath = Path.Combine(stagingDirectory, $"ASMILE-BO-{latestVersion}{extension}");
            temporaryPath = Path.Combine(stagingDirectory, $".{Guid.NewGuid():N}.part");

            using var response = await SendFollowingHttpsRedirectsAsync(packageUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Failure("DownloadFailed", $"Máy chủ cập nhật trả về HTTP {(int)response.StatusCode}.", latestVersion.ToString());
            }

            if (response.Content.Headers.ContentLength is > MaxPackageBytes)
            {
                return Failure("PackageTooLarge", "Gói cập nhật vượt quá giới hạn tải 1 GiB.", latestVersion.ToString());
            }

            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
            long totalBytes = 0;
            byte[] digest;
            await using (var destination = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                options: FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                var buffer = new byte[81920];
                while (true)
                {
                    var read = await source.ReadAsync(buffer.AsMemory(), cancellationToken);
                    if (read == 0)
                    {
                        break;
                    }

                    if (totalBytes > MaxPackageBytes - read)
                    {
                        return Failure("PackageTooLarge", "Gói cập nhật vượt quá giới hạn tải 1 GiB.", latestVersion.ToString());
                    }

                    totalBytes += read;
                    hash.AppendData(buffer, 0, read);
                    await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                }

                await destination.FlushAsync(cancellationToken);
                digest = hash.GetHashAndReset();
            }

            var actualHash = Convert.ToHexString(digest);
            var expectedHash = Convert.FromHexString(verified.PackageSha256!);
            var actualHashBytes = Convert.FromHexString(actualHash);
            if (!CryptographicOperations.FixedTimeEquals(expectedHash, actualHashBytes))
            {
                return Failure("HashMismatch", "SHA-256 của gói tải về không khớp manifest đã ký.", latestVersion.ToString());
            }

            File.Move(temporaryPath, finalPath, overwrite: true);
            temporaryPath = null;
            return new UpdatePackageStageResultDto(
                true,
                "StagedVerified",
                latestVersion.ToString(),
                finalPath,
                totalBytes,
                actualHash,
                "Gói đã tải và xác minh SHA-256; chưa được giải nén hoặc cài đặt.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return Failure("DownloadFailed", $"Không thể tải và staging gói cập nhật: {ex.Message}", latestVersion.ToString());
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try
                {
                    File.Delete(temporaryPath);
                }
                catch (IOException)
                {
                    // Best-effort cleanup; the random .part name is never treated as staged.
                }
                catch (UnauthorizedAccessException)
                {
                    // Preserve the original download result while leaving the partial file identifiable.
                }
            }
        }
    }

    private async Task<HttpResponseMessage> SendFollowingHttpsRedirectsAsync(Uri initialUri, CancellationToken cancellationToken)
    {
        var currentUri = initialUri;
        for (var redirectCount = 0; redirectCount <= MaxRedirects; redirectCount++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!IsRedirect(response.StatusCode))
            {
                return response;
            }

            var location = response.Headers.Location;
            response.Dispose();
            if (location is null || redirectCount == MaxRedirects)
            {
                throw new HttpRequestException("Máy chủ cập nhật chuyển hướng quá số lần cho phép hoặc thiếu Location.");
            }

            var nextUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
            if (!IsAllowedHttpsUri(nextUri))
            {
                throw new HttpRequestException("Từ chối chuyển hướng update ra khỏi HTTPS an toàn.");
            }

            currentUri = nextUri;
        }

        throw new HttpRequestException("Máy chủ cập nhật chuyển hướng quá số lần cho phép.");
    }

    private string GetStagingDirectory()
    {
        var configured = _configuration["Maintenance:UpdateStagingPath"]?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured));
        }

        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ASMILE.BO",
            "Updates");
    }

    private static string GetSafeExtension(Uri packageUri)
        => Path.GetExtension(packageUri.AbsolutePath).ToLowerInvariant() switch
        {
            ".zip" => ".zip",
            ".msi" => ".msi",
            ".msix" => ".msix",
            ".msixbundle" => ".msixbundle",
            ".exe" => ".exe",
            _ => ".package"
        };

    private static bool IsAllowedHttpsUri(Uri uri)
        => uri.IsAbsoluteUri
            && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Fragment);

    private static bool IsSha256(string? value)
        => value is { Length: 64 } && value.All(Uri.IsHexDigit);

    private static bool IsRedirect(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.MovedPermanently
            or HttpStatusCode.Redirect
            or HttpStatusCode.RedirectMethod
            or HttpStatusCode.TemporaryRedirect
            or HttpStatusCode.PermanentRedirect;

    private static UpdatePackageStageResultDto Failure(string status, string detail, string? version = null)
        => new(false, status, version, null, null, null, detail);
}
