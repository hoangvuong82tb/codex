using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Tests.Unit;

public sealed class ExportPipelineServiceTests
{
    [Fact]
    public async Task SaveRetentionPolicy_PersistsPolicyInCurrentCompanyAndBranch()
    {
        var companyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var companyContext = new TestCompanyContext(companyId, branchId);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options, companyContext);
        await db.Database.EnsureCreatedAsync();
        var service = new ExportPipelineService(db, companyContext);

        var saved = await service.SaveRetentionPolicyAsync(new ExportRetentionPolicyDto
        {
            ExportCode = "  invoice-pdf  ",
            RetentionDays = 90,
            IsActive = false
        });

        saved.ExportCode.Should().Be("invoice-pdf");
        var persisted = await db.ExportRetentionPolicies.AsNoTracking().SingleAsync();
        persisted.CompanyId.Should().Be(companyId);
        persisted.BranchId.Should().Be(branchId);
        persisted.RetentionDays.Should().Be(90);
        persisted.IsActive.Should().BeFalse();
        (await service.GetRetentionPoliciesAsync()).Should().ContainSingle(x => x.Id == saved.Id);
    }

    [Fact]
    public async Task CleanupRetention_DeletesOnlyCurrentTenantExecutionsAndTheirArtifacts()
    {
        var companyId = Guid.NewGuid();
        var otherCompanyId = Guid.NewGuid();
        var branchId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var companyContext = new TestCompanyContext(companyId, branchId);
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        await using var db = new AppDbContext(options, companyContext);
        await db.Database.EnsureCreatedAsync();
        db.ExportRetentionPolicies.AddRange(
            new ExportRetentionPolicy { CompanyId = companyId, BranchId = branchId, ExportCode = "invoice", RetentionDays = 30 },
            new ExportRetentionPolicy { CompanyId = otherCompanyId, BranchId = branchId, ExportCode = "invoice", RetentionDays = 30 });
        var currentExecution = new ExportExecution
        {
            CompanyId = companyId,
            BranchId = branchId,
            ExportCode = "invoice",
            ExportType = "PDF",
            Status = "Completed",
            CompletedAtUtc = now.AddDays(-45)
        };
        var otherExecution = new ExportExecution
        {
            CompanyId = otherCompanyId,
            BranchId = branchId,
            ExportCode = "invoice",
            ExportType = "PDF",
            Status = "Completed",
            CompletedAtUtc = now.AddDays(-45)
        };
        db.ExportExecutions.AddRange(currentExecution, otherExecution);
        db.ExportArtifacts.AddRange(
            new ExportArtifact { CompanyId = companyId, BranchId = branchId, ExportExecutionId = currentExecution.Id, FilePath = "a.pdf", FileType = "pdf", Hash = "a" },
            new ExportArtifact { CompanyId = otherCompanyId, BranchId = branchId, ExportExecutionId = otherExecution.Id, FilePath = "b.pdf", FileType = "pdf", Hash = "b" });
        await db.SaveChangesAsync();
        var service = new ExportPipelineService(db, companyContext);

        var deleted = await service.CleanupRetentionAsync(now);

        deleted.Should().Be(1);
        (await db.ExportExecutions.IgnoreQueryFilters().AsNoTracking().Select(x => x.CompanyId).ToListAsync()).Should().Equal(otherCompanyId);
        (await db.ExportArtifacts.IgnoreQueryFilters().AsNoTracking().Select(x => x.CompanyId).ToListAsync()).Should().Equal(otherCompanyId);
        (await service.GetExecutionsAsync()).Should().BeEmpty();
    }

    private sealed class TestCompanyContext(Guid companyId, Guid? branchId) : ICurrentCompanyContext
    {
        public Guid? CompanyId => companyId;
        public Guid? BranchId => branchId;
        public string CompanyName => "Test";
        public string? BranchName => "Test branch";
        public Task EnsureInitializedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetCompanyAsync(Guid value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SetBranchAsync(Guid? value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<CompanyContextOptionDto>> GetCompaniesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<CompanyContextOptionDto>>([]);
        public Task<IReadOnlyList<BranchContextOptionDto>> GetBranchesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<BranchContextOptionDto>>([]);
    }
}
