using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Security;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace ASMILE.BO.Tests.Unit;

public sealed class UpdatePackageStagingServiceTests
{
    [Fact]
    public async Task Real_signed_manifest_flows_through_reverification_download_hash_and_staging()
    {
        using var rsa = RSA.Create(2048);
        var root = CreateTempDirectory();
        var package = "release-package-payload"u8.ToArray();
        var packageHash = Convert.ToHexString(SHA256.HashData(package));
        var packageUri = "https://updates.example.test/asmile-bo-2.0.0.zip";
        var payloadJson = JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["channel"] = "Stable",
            ["packageSha256"] = packageHash,
            ["packageUri"] = packageUri,
            ["product"] = "ASMILE.BO",
            ["publishedAtUtc"] = DateTime.UtcNow,
            ["version"] = "2.0.0"
        });
        using var payloadDocument = JsonDocument.Parse(payloadJson);
        var signature = Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(payloadJson), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
        var manifestPath = Path.Combine(root, "update-manifest.json");
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(new
        {
            payload = payloadDocument.RootElement,
            signature
        }));
        var stagingPath = Path.Combine(root, "staged");
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Maintenance:UpdateManifestPath"] = manifestPath,
                ["Maintenance:UpdatePublicKeyPem"] = rsa.ExportRSAPublicKeyPem(),
                ["Maintenance:Product"] = "ASMILE.BO",
                ["Maintenance:UpdateStagingPath"] = stagingPath
            })
            .Build();
        var handler = new DelegateHandler((request, _) =>
        {
            request.RequestUri!.ToString().Should().Be(packageUri);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(package)
            });
        });

        try
        {
            var manifestService = new SignedUpdateManifestService(configuration);
            var service = new UpdatePackageStagingService(manifestService, new TestAuthorizationService(true), new HttpClient(handler), configuration);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            result.Success.Should().BeTrue();
            result.Status.Should().Be("StagedVerified");
            result.PackageSha256.Should().Be(packageHash);
            File.ReadAllBytes(result.FilePath!).Should().Equal(package);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Stages_only_after_rechecking_manifest_and_matching_package_hash()
    {
        var directory = CreateTempDirectory();
        var package = "signed-package-content"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(package));
        var handler = new DelegateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(package)
        }));
        try
        {
            var manifest = new FakeUpdateManifestService(VerifiedUpdate(hash));
            var service = CreateService(directory, manifest, handler);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            manifest.CallCount.Should().Be(1);
            result.Success.Should().BeTrue();
            result.Status.Should().Be("StagedVerified");
            result.Version.Should().Be("2.0.0");
            result.SizeBytes.Should().Be(package.Length);
            File.ReadAllBytes(result.FilePath!).Should().Equal(package);
            result.PackageSha256.Should().Be(hash);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Deletes_temporary_file_when_package_hash_does_not_match()
    {
        var directory = CreateTempDirectory();
        var package = "tampered-package"u8.ToArray();
        var handler = new DelegateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(package)
        }));
        try
        {
            var service = CreateService(directory, new FakeUpdateManifestService(VerifiedUpdate(new string('0', 64))), handler);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            result.Success.Should().BeFalse();
            result.Status.Should().Be("HashMismatch");
            Directory.GetFiles(directory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Rejects_http_downgrade_redirect()
    {
        var directory = CreateTempDirectory();
        var handler = new DelegateHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://updates.example.test/package.zip") }
        }));
        try
        {
            var service = CreateService(directory, new FakeUpdateManifestService(VerifiedUpdate(new string('A', 64))), handler);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            result.Success.Should().BeFalse();
            result.Status.Should().Be("DownloadFailed");
            Directory.GetFiles(directory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Does_not_make_network_request_when_manifest_is_not_verified()
    {
        var directory = CreateTempDirectory();
        var handler = new DelegateHandler((_, _) => throw new InvalidOperationException("Network must not be called."));
        try
        {
            var invalid = VerifiedUpdate(new string('A', 64)) with { IsSignatureValid = false };
            var service = CreateService(directory, new FakeUpdateManifestService(invalid), handler);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            result.Success.Should().BeFalse();
            result.Status.Should().Be("ManifestNotVerified");
            Directory.GetFiles(directory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Theory]
    [InlineData("1.0.0", "1.0.0")]
    [InlineData("0.9.0", "1.0.0")]
    [InlineData("2.0.0", "9.9.9")]
    public async Task Rejects_provider_result_that_is_not_a_forward_update_for_the_running_version(
        string latestVersion,
        string reportedCurrentVersion)
    {
        var directory = CreateTempDirectory();
        var handler = new DelegateHandler((_, _) => throw new InvalidOperationException("Network must not be called."));
        try
        {
            var inconsistent = VerifiedUpdate(new string('A', 64)) with
            {
                LatestVersion = latestVersion,
                CurrentVersion = reportedCurrentVersion
            };
            var service = CreateService(directory, new FakeUpdateManifestService(inconsistent), handler);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            result.Success.Should().BeFalse();
            result.Status.Should().Be("ManifestNotVerified");
            Directory.GetFiles(directory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Requires_manage_services_permission_before_manifest_or_network_access()
    {
        var directory = CreateTempDirectory();
        var handler = new DelegateHandler((_, _) => throw new InvalidOperationException("Network must not be called."));
        try
        {
            var manifest = new FakeUpdateManifestService(VerifiedUpdate(new string('A', 64)));
            var service = CreateService(directory, manifest, handler, hasPermission: false);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            result.Status.Should().Be("PermissionDenied");
            manifest.CallCount.Should().Be(0);
            Directory.GetFiles(directory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task Rejects_package_larger_than_configured_safety_limit()
    {
        var directory = CreateTempDirectory();
        var handler = new DelegateHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent("small"u8.ToArray())
            };
            response.Content.Headers.ContentLength = 1024L * 1024 * 1024 + 1;
            return Task.FromResult(response);
        });
        try
        {
            var service = CreateService(directory, new FakeUpdateManifestService(VerifiedUpdate(new string('A', 64))), handler);

            var result = await service.StageLatestAsync("Stable", new Version(1, 0, 0));

            result.Success.Should().BeFalse();
            result.Status.Should().Be("PackageTooLarge");
            Directory.GetFiles(directory).Should().BeEmpty();
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static UpdatePackageStagingService CreateService(
        string stagingPath,
        IUpdateManifestService manifest,
        HttpMessageHandler handler,
        bool hasPermission = true)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Maintenance:UpdateStagingPath"] = stagingPath })
            .Build();
        return new UpdatePackageStagingService(manifest, new TestAuthorizationService(hasPermission), new HttpClient(handler), configuration);
    }

    private static UpdateCheckResultDto VerifiedUpdate(string hash)
        => new(
            true,
            true,
            true,
            "UpdateAvailable",
            "1.0.0",
            "2.0.0",
            "Stable",
            DateTime.UtcNow,
            "https://updates.example.test/asmile-bo-2.0.0.zip",
            hash,
            "Signed test manifest");

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "asmile-update-stage-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class FakeUpdateManifestService(UpdateCheckResultDto result) : IUpdateManifestService
    {
        public int CallCount { get; private set; }

        public Task<UpdateCheckResultDto> CheckAsync(string channel, Version currentVersion, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class TestAuthorizationService(bool hasPermission) : IAuthorizationService
    {
        public bool HasPermission(string permissionCode) => hasPermission;
        public void Demand(string permissionCode) { }
        public Task<bool> CanAccessAsync(string permissionCode) => Task.FromResult(hasPermission);
        public bool HasPermission(Permission permission) => hasPermission;
        public void Demand(Permission permission) { }
        public Task<bool> ConfirmDangerousActionAsync(string title, string message) => Task.FromResult(true);
    }

    private sealed class DelegateHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}
