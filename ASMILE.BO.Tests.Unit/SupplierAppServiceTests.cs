using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Application.Services;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Repositories;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ASMILE.BO.Tests.Unit;

public sealed class SupplierAppServiceTests
{
    [Fact]
    public void Supplier_code_pattern_renders_date_tokens_and_advances_only_matching_sequences()
    {
        var date = new DateTime(2026, 10, 2);
        const string pattern = "NCC-{yyyyMMdd}-{000}";
        var existingCodes = new[] { "NCC-20261002-002", "NCC-20261002-007", "NCC-20250930-099" };

        var prefix = SequenceCodePattern.GetPrefix(pattern, date);
        var nextSequence = SequenceCodePattern.GetNextSequence(pattern, date, existingCodes);

        prefix.Should().Be("NCC-20261002-");
        nextSequence.Should().Be(8);
        SequenceCodePattern.Render(pattern, date, nextSequence).Should().Be("NCC-20261002-008");
    }

    [Fact]
    public async Task Save_supplier_requires_phone_by_default()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService());

        var act = () => service.SaveAsync(new SupplierDto
        {
            SupplierCode = "SUP-001",
            SupplierName = "Thiếu điện thoại"
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Số điện thoại nhà cung cấp*");
        db.Suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_supplier_allows_missing_phone_when_policy_is_disabled()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService(("Module.suppliers.requirePhone", "false")));

        var saved = await service.SaveAsync(new SupplierDto
        {
            SupplierCode = "SUP-002",
            SupplierName = "Không bắt buộc điện thoại"
        });

        saved.Phone.Should().BeEmpty();
        db.Suppliers.Should().ContainSingle(x => x.SupplierCode == "SUP-002");
    }

    [Fact]
    public async Task Save_supplier_persists_tax_code_and_enforces_required_tax_policy()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService(
                ("Module.suppliers.requirePhone", "false"),
                ("Module.suppliers.requireTaxCode", "true"),
                ("Module.suppliers.checkDuplicateTaxCode", "false")));

        var act = () => service.SaveAsync(new SupplierDto
        {
            SupplierCode = "SUP-003",
            SupplierName = "Thiếu mã số thuế"
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Mã số thuế nhà cung cấp*");
        db.Suppliers.Should().BeEmpty();

        var saved = await service.SaveAsync(new SupplierDto
        {
            SupplierCode = "SUP-003",
            SupplierName = "Có mã số thuế",
            TaxCode = "0100123456"
        });

        saved.TaxCode.Should().Be("0100123456");
        db.ChangeTracker.Clear();
        (await db.Suppliers.AsNoTracking().SingleAsync()).TaxCode.Should().Be("0100123456");
    }

    [Fact]
    public void Supplier_tax_code_migration_is_discoverable_and_adds_a_backfilled_column()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost;Database=MigrationAudit;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var db = new AppDbContext(options);
        var migrations = db.GetService<IMigrationsAssembly>();
        const string migrationId = "20261002160000_AddSupplierTaxCode";

        migrations.Migrations.Should().ContainKey(migrationId);
        var migration = migrations.CreateMigration(migrations.Migrations[migrationId], db.Database.ProviderName!);
        var addColumn = migration.UpOperations.OfType<AddColumnOperation>().Should().ContainSingle().Which;
        addColumn.Name.Should().Be("TaxCode");
        addColumn.Table.Should().Be("Suppliers");
        addColumn.IsNullable.Should().BeFalse();
        addColumn.DefaultValue.Should().Be("");
    }

    [Fact]
    public async Task Save_supplier_rejects_duplicate_tax_code_and_allows_it_when_check_is_disabled()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Suppliers.Add(new Supplier
        {
            Id = 1,
            SupplierCode = "SUP-001",
            SupplierName = "NCC hiện có",
            TaxCode = "0100123456"
        });
        await db.SaveChangesAsync();

        var service = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService(
                ("Module.suppliers.requirePhone", "false"),
                ("Module.suppliers.requireTaxCode", "false"),
                ("Module.suppliers.checkDuplicateTaxCode", "true")));
        var duplicate = new SupplierDto
        {
            SupplierCode = "SUP-002",
            SupplierName = "NCC trùng MST",
            TaxCode = " 0100123456 "
        };

        var act = () => service.SaveAsync(duplicate);
        await act.Should().ThrowAsync<Exception>().WithMessage("*Mã số thuế nhà cung cấp đã tồn tại*");
        db.Suppliers.Should().HaveCount(1);

        var permissiveService = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService(
                ("Module.suppliers.requirePhone", "false"),
                ("Module.suppliers.requireTaxCode", "false"),
                ("Module.suppliers.checkDuplicateTaxCode", "false")));

        var saved = await permissiveService.SaveAsync(duplicate);
        saved.TaxCode.Should().Be("0100123456");
        db.Suppliers.Should().HaveCount(2);
    }

    [Fact]
    public async Task Save_supplier_generates_code_from_settings_using_next_existing_sequence()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Suppliers.AddRange(
            new Supplier { Id = 1, SupplierCode = "NCC-00002", SupplierName = "NCC cũ 2" },
            new Supplier { Id = 2, SupplierCode = "NCC-00007", SupplierName = "NCC cũ 7" });
        await db.SaveChangesAsync();

        var service = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService(
                ("Module.suppliers.autoSupplierCode", "true"),
                ("Module.suppliers.supplierCodePattern", "NCC-{00000}"),
                ("Module.suppliers.requirePhone", "false")));

        var saved = await service.SaveAsync(new SupplierDto { SupplierName = "NCC mới" });

        saved.SupplierCode.Should().Be("NCC-00008");
        db.Suppliers.Should().Contain(x => x.SupplierCode == "NCC-00008");
    }

    [Fact]
    public async Task Save_supplier_still_requires_manual_code_when_auto_generation_is_disabled()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService(("Module.suppliers.requirePhone", "false")));

        var act = () => service.SaveAsync(new SupplierDto { SupplierName = "Thiếu mã" });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Mã nhà cung cấp không được rỗng*");
        db.Suppliers.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_supplier_rejects_invalid_auto_code_pattern_without_writing()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new SupplierAppService(
            new SupplierRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new SupplierSettingsService(
                ("Module.suppliers.autoSupplierCode", "true"),
                ("Module.suppliers.supplierCodePattern", "NCC-{yyyy}"),
                ("Module.suppliers.requirePhone", "false")));

        var act = () => service.SaveAsync(new SupplierDto { SupplierName = "NCC lỗi cấu hình" });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Quy tắc tự sinh mã nhà cung cấp không hợp lệ*");
        db.Suppliers.Should().BeEmpty();
    }

    private sealed class SupplierSettingsService(params (string Key, string Value)[] values) : ISettingsService
    {
        private readonly IReadOnlyDictionary<string, string> _values = values
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingDefinitionDto>>([]);

        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget)
            => Task.FromResult<IReadOnlyList<SettingValueDto>>([]);

        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(_values.TryGetValue(key, out var value)
                ? new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = value }
                : null);

        public Task SaveValueAsync(SaveSettingValueRequest request) => Task.CompletedTask;
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => Task.CompletedTask;
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => Task.CompletedTask;
    }
}
