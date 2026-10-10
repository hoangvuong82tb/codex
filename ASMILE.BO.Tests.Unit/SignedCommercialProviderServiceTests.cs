using System.Security.Cryptography;
using System.Text;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Tests.Unit;

public sealed class SignedCommercialProviderServiceTests
{
    [Fact]
    public async Task License_provider_accepts_a_valid_signed_manifest_and_rejects_tampering()
    {
        using var rsa = RSA.Create(2048);
        var directory = CreateTempDirectory();
        try
        {
            var payload = "{\"customerName\":\"Demo Hotel\",\"expiresAtUtc\":\"2099-01-01T00:00:00Z\",\"features\":[\"FO\"],\"licenseId\":\"LIC-001\",\"product\":\"ASMILE.BO\"}";
            var path = WriteManifest(directory, "license.json", payload, rsa);
            var configuration = Configuration(new Dictionary<string, string?>
            {
                ["Licensing:ManifestPath"] = path,
                ["Licensing:PublicKeyPem"] = rsa.ExportRSAPublicKeyPem(),
                ["Licensing:Product"] = "ASMILE.BO"
            });

            var service = new SignedLicenseService(configuration);
            var valid = await service.GetStatusAsync();
            valid.IsValid.Should().BeTrue();
            valid.LicenseId.Should().Be("LIC-001");
            valid.Features.Should().Contain("FO");

            File.WriteAllText(path, File.ReadAllText(path).Replace("Demo Hotel", "Tampered Hotel", StringComparison.Ordinal));
            var tampered = await service.GetStatusAsync();
            tampered.IsValid.Should().BeFalse();
            tampered.Status.Should().Be("Invalid");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Update_provider_only_reports_a_verified_update_with_a_valid_package_hash()
    {
        using var rsa = RSA.Create(2048);
        var directory = CreateTempDirectory();
        try
        {
            var payload = "{\"channel\":\"Stable\",\"packageSha256\":\"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef\",\"packageUri\":\"https://updates.example.test/asmile-bo-2.0.0.zip\",\"product\":\"ASMILE.BO\",\"publishedAtUtc\":\"2026-09-29T00:00:00Z\",\"version\":\"2.0.0\"}";
            var path = WriteManifest(directory, "update.json", payload, rsa);
            var configuration = Configuration(new Dictionary<string, string?>
            {
                ["Maintenance:UpdateManifestPath"] = path,
                ["Maintenance:UpdatePublicKeyPem"] = rsa.ExportRSAPublicKeyPem(),
                ["Maintenance:Product"] = "ASMILE.BO"
            });

            var service = new SignedUpdateManifestService(configuration);
            var result = await service.CheckAsync("Stable", new Version(1, 0, 0));
            result.IsSignatureValid.Should().BeTrue();
            result.IsUpdateAvailable.Should().BeTrue();
            result.LatestVersion.Should().Be("2.0.0");

            var wrongChannel = await service.CheckAsync("Beta", new Version(1, 0, 0));
            wrongChannel.Status.Should().Be("ChannelMismatch");
            wrongChannel.IsUpdateAvailable.Should().BeFalse();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("http://updates.example.test/package.zip")]
    [InlineData("file:///C:/updates/package.zip")]
    [InlineData("ftp://updates.example.test/package.zip")]
    [InlineData("https://user:password@updates.example.test/package.zip")]
    [InlineData("https://updates.example.test/package.zip#installer")]
    public async Task Update_provider_rejects_non_https_or_ambiguous_package_uris(string packageUri)
    {
        using var rsa = RSA.Create(2048);
        var directory = CreateTempDirectory();
        try
        {
            var payload = $$"""{"channel":"Stable","packageSha256":"0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef","packageUri":"{{packageUri}}","product":"ASMILE.BO","publishedAtUtc":"2026-09-29T00:00:00Z","version":"2.0.0"}""";
            var path = WriteManifest(directory, "update.json", payload, rsa);
            var configuration = Configuration(new Dictionary<string, string?>
            {
                ["Maintenance:UpdateManifestPath"] = path,
                ["Maintenance:UpdatePublicKeyPem"] = rsa.ExportRSAPublicKeyPem(),
                ["Maintenance:Product"] = "ASMILE.BO"
            });

            var result = await new SignedUpdateManifestService(configuration).CheckAsync("Stable", new Version(1, 0, 0));

            result.Status.Should().Be("Invalid");
            result.IsSignatureValid.Should().BeFalse();
            result.IsUpdateAvailable.Should().BeFalse();
            result.PackageUri.Should().BeNull();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "asmile-commercial-provider-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string WriteManifest(string directory, string fileName, string payloadJson, RSA rsa)
    {
        var signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(payloadJson), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        var path = Path.Combine(directory, fileName);
        File.WriteAllText(path, $"{{\"payload\":{payloadJson},\"signature\":\"{signature}\"}}");
        return path;
    }
}
