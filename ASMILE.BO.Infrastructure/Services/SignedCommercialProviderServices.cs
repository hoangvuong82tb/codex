using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Infrastructure.Services;

/// <summary>
/// Reads locally provisioned, RSA-signed commercial manifests.  Missing
/// configuration is an unavailable state; it is never converted into a
/// fabricated license or update result.
/// </summary>
public sealed class SignedLicenseService : ILicenseService
{
    private const long MaxManifestBytes = 1024 * 1024;
    private readonly IConfiguration _configuration;

    public SignedLicenseService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<LicenseStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var manifestPath = _configuration["Licensing:ManifestPath"];
        var publicKeyPem = _configuration["Licensing:PublicKeyPem"];
        if (string.IsNullOrWhiteSpace(manifestPath) || string.IsNullOrWhiteSpace(publicKeyPem))
        {
            return Unavailable("Thiếu Licensing:ManifestPath hoặc Licensing:PublicKeyPem.");
        }

        var document = await SignedManifestDocument.TryReadAsync(manifestPath, MaxManifestBytes, cancellationToken);
        if (!document.IsSuccess)
        {
            return Unavailable(document.Error!);
        }

        if (!SignedManifestVerifier.Verify(document.Payload, document.Signature, publicKeyPem))
        {
            return Invalid("Chữ ký license không hợp lệ.");
        }

        LicensePayload? payload;
        try
        {
            payload = document.Payload.Deserialize<LicensePayload>(SignedManifestDocument.JsonOptions);
        }
        catch (JsonException ex)
        {
            return Invalid($"Manifest license không đúng schema: {ex.Message}");
        }

        if (payload is null)
        {
            return Invalid("Manifest license rỗng.");
        }

        var expectedProduct = _configuration["Licensing:Product"] ?? "ASMILE.BO";
        if (!string.Equals(payload.Product, expectedProduct, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid($"License không dành cho sản phẩm {expectedProduct}.");
        }

        if (string.IsNullOrWhiteSpace(payload.LicenseId) || string.IsNullOrWhiteSpace(payload.CustomerName))
        {
            return Invalid("Manifest license thiếu LicenseId hoặc CustomerName.");
        }

        var now = DateTime.UtcNow;
        if (payload.ExpiresAtUtc <= now)
        {
            return new LicenseStatusDto(
                true,
                false,
                "Expired",
                "Manifest và chữ ký hợp lệ nhưng license đã hết hạn.",
                payload.LicenseId,
                payload.CustomerName,
                payload.ExpiresAtUtc,
                payload.Features ?? Array.Empty<string>(),
                "Cần cấp license mới trước khi tiếp tục vận hành.");
        }

        return new LicenseStatusDto(
            true,
            true,
            "Valid",
            "License manifest đã được xác thực bằng chữ ký RSA.",
            payload.LicenseId,
            payload.CustomerName,
            payload.ExpiresAtUtc,
            payload.Features ?? Array.Empty<string>(),
            null);
    }

    private static LicenseStatusDto Unavailable(string detail) => new(
        false,
        false,
        "NotConfigured",
        "Chưa có license provider khả dụng.",
        null,
        null,
        null,
        Array.Empty<string>(),
        detail);

    private static LicenseStatusDto Invalid(string detail) => new(
        true,
        false,
        "Invalid",
        "License provider đã đọc manifest nhưng không thể xác thực.",
        null,
        null,
        null,
        Array.Empty<string>(),
        detail);

    private sealed record LicensePayload(
        string Product,
        string LicenseId,
        string CustomerName,
        DateTime ExpiresAtUtc,
        IReadOnlyList<string>? Features);
}

public sealed class SignedUpdateManifestService : IUpdateManifestService
{
    private const long MaxManifestBytes = 1024 * 1024;
    private readonly IConfiguration _configuration;

    public SignedUpdateManifestService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<UpdateCheckResultDto> CheckAsync(
        string channel,
        Version currentVersion,
        CancellationToken cancellationToken = default)
    {
        var manifestPath = _configuration["Maintenance:UpdateManifestPath"];
        var publicKeyPem = _configuration["Maintenance:UpdatePublicKeyPem"];
        if (string.IsNullOrWhiteSpace(manifestPath) || string.IsNullOrWhiteSpace(publicKeyPem))
        {
            return Unavailable(currentVersion, "Thiếu Maintenance:UpdateManifestPath hoặc Maintenance:UpdatePublicKeyPem.");
        }

        var document = await SignedManifestDocument.TryReadAsync(manifestPath, MaxManifestBytes, cancellationToken);
        if (!document.IsSuccess)
        {
            return Unavailable(currentVersion, document.Error!);
        }

        if (!SignedManifestVerifier.Verify(document.Payload, document.Signature, publicKeyPem))
        {
            return Invalid(currentVersion, "Chữ ký update manifest không hợp lệ.");
        }

        UpdatePayload? payload;
        try
        {
            payload = document.Payload.Deserialize<UpdatePayload>(SignedManifestDocument.JsonOptions);
        }
        catch (JsonException ex)
        {
            return Invalid(currentVersion, $"Update manifest không đúng schema: {ex.Message}");
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Product) || string.IsNullOrWhiteSpace(payload.Version)
            || string.IsNullOrWhiteSpace(payload.PackageUri) || string.IsNullOrWhiteSpace(payload.PackageSha256))
        {
            return Invalid(currentVersion, "Update manifest thiếu product, version, packageUri hoặc packageSha256.");
        }

        var expectedProduct = _configuration["Maintenance:Product"] ?? "ASMILE.BO";
        if (!string.Equals(payload.Product, expectedProduct, StringComparison.OrdinalIgnoreCase))
        {
            return Invalid(currentVersion, $"Update manifest không dành cho sản phẩm {expectedProduct}.");
        }

        if (!string.Equals(payload.Channel, channel, StringComparison.OrdinalIgnoreCase))
        {
            return new UpdateCheckResultDto(
                true,
                true,
                false,
                "ChannelMismatch",
                currentVersion.ToString(),
                payload.Version,
                payload.Channel,
                payload.PublishedAtUtc,
                payload.PackageUri,
                payload.PackageSha256,
                $"Manifest thuộc channel {payload.Channel}; channel hiện tại là {channel}.");
        }

        if (!Version.TryParse(payload.Version, out var latestVersion))
        {
            return Invalid(currentVersion, "Version trong update manifest không hợp lệ.");
        }

        if (!IsSha256(payload.PackageSha256) || !IsAllowedPackageUri(payload.PackageUri))
        {
            return Invalid(currentVersion, "PackageUri phải là HTTPS hợp lệ, không chứa thông tin đăng nhập hoặc fragment; packageSha256 phải là SHA-256 hợp lệ.");
        }

        var hasUpdate = latestVersion > currentVersion;
        return new UpdateCheckResultDto(
            true,
            true,
            hasUpdate,
            hasUpdate ? "UpdateAvailable" : "UpToDate",
            currentVersion.ToString(),
            latestVersion.ToString(),
            payload.Channel,
            payload.PublishedAtUtc,
            payload.PackageUri,
            payload.PackageSha256,
            hasUpdate ? payload.Notes : "Bản hiện tại đã là bản mới nhất theo manifest đã ký.");
    }

    private static bool IsSha256(string value)
        => value.Length == 64 && value.All(Uri.IsHexDigit);

    private static bool IsAllowedPackageUri(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && string.IsNullOrEmpty(uri.UserInfo)
            && string.IsNullOrEmpty(uri.Fragment);

    private static UpdateCheckResultDto Unavailable(Version currentVersion, string detail) => new(
        false,
        false,
        false,
        "NotConfigured",
        currentVersion.ToString(),
        null,
        null,
        null,
        null,
        null,
        detail);

    private static UpdateCheckResultDto Invalid(Version currentVersion, string detail) => new(
        true,
        false,
        false,
        "Invalid",
        currentVersion.ToString(),
        null,
        null,
        null,
        null,
        null,
        detail);

    private sealed record UpdatePayload(
        string Product,
        string Channel,
        string Version,
        DateTime PublishedAtUtc,
        string PackageUri,
        string PackageSha256,
        string? Notes);
}

internal static class SignedManifestVerifier
{
    public static bool Verify(JsonElement payload, string signatureBase64, string publicKeyPem)
    {
        if (string.IsNullOrWhiteSpace(signatureBase64) || string.IsNullOrWhiteSpace(publicKeyPem))
        {
            return false;
        }

        try
        {
            var signature = Convert.FromBase64String(signatureBase64);
            using var rsa = RSA.Create();
            rsa.ImportFromPem(publicKeyPem);
            return rsa.VerifyData(
                SignedManifestDocument.Canonicalize(payload),
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }
}

internal sealed class SignedManifestDocument
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private SignedManifestDocument(JsonElement payload, string signature)
    {
        Payload = payload;
        Signature = signature;
    }

    public JsonElement Payload { get; }
    public string Signature { get; }
    public bool IsSuccess => Error is null;
    public string? Error { get; private init; }

    public static async Task<SignedManifestDocument> TryReadAsync(string path, long maxBytes, CancellationToken cancellationToken)
    {
        try
        {
            var fullPath = Path.GetFullPath(path.Trim());
            var fileInfo = new FileInfo(fullPath);
            if (!fileInfo.Exists)
            {
                return Failure($"Không tìm thấy manifest: {fullPath}");
            }

            if (fileInfo.Length <= 0 || fileInfo.Length > maxBytes)
            {
                return Failure("Kích thước manifest không hợp lệ hoặc vượt giới hạn 1 MB.");
            }

            await using var stream = File.OpenRead(fullPath);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("payload", out var payload)
                || payload.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("signature", out var signature)
                || signature.ValueKind != JsonValueKind.String)
            {
                return Failure("Manifest phải có payload object và signature base64.");
            }

            return new SignedManifestDocument(payload.Clone(), signature.GetString() ?? string.Empty);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (IOException ex)
        {
            return Failure($"Không đọc được manifest: {ex.Message}");
        }
        catch (JsonException ex)
        {
            return Failure($"Manifest không phải JSON hợp lệ: {ex.Message}");
        }
        catch (UnauthorizedAccessException ex)
        {
            return Failure($"Không có quyền đọc manifest: {ex.Message}");
        }
        catch (ArgumentException ex)
        {
            return Failure($"Đường dẫn manifest không hợp lệ: {ex.Message}");
        }
    }

    public static byte[] Canonicalize(JsonElement element)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            WriteCanonical(writer, element);
        }

        return stream.ToArray();
    }

    private static void WriteCanonical(Utf8JsonWriter writer, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(writer, property.Value);
                }
                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var value in element.EnumerateArray())
                {
                    WriteCanonical(writer, value);
                }
                writer.WriteEndArray();
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }

    private static SignedManifestDocument Failure(string error)
        => new(default, string.Empty) { Error = error };
}
