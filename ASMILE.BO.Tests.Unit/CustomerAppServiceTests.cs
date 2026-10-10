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

namespace ASMILE.BO.Tests.Unit;

public sealed class CustomerAppServiceTests
{
    [Fact]
    public async Task Get_and_save_customer_updates_real_row_without_erasing_unedited_fields()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Customers.Add(new Customer
        {
            Id = 17,
            CustomerCode = "CUS-017",
            CustomerName = "Khách cũ",
            Phone = "0900000017",
            Email = "old@example.com",
            TaxCode = "0100170017",
            Address = "Địa chỉ cũ",
            BankAccountNo = "0123456789",
            Note = "Ghi chú cần giữ",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService());

        var customer = await service.GetAsync(17);
        customer.Should().NotBeNull();
        customer!.CustomerName = "Khách đã sửa";
        customer.Phone = "0911111111";
        customer.Email = "updated@example.com";
        customer.TaxCode = "0100170099";

        var saved = await service.SaveAsync(customer);
        var persisted = await db.Customers.FindAsync(17);

        saved.CustomerName.Should().Be("Khách đã sửa");
        persisted.Should().NotBeNull();
        persisted!.CustomerName.Should().Be("Khách đã sửa");
        persisted.Phone.Should().Be("0911111111");
        persisted.Email.Should().Be("updated@example.com");
        persisted.TaxCode.Should().Be("0100170099");
        persisted.Address.Should().Be("Địa chỉ cũ");
        persisted.BankAccountNo.Should().Be("0123456789");
        persisted.Note.Should().Be("Ghi chú cần giữ");
        db.AuditLogs.Should().Contain(x => x.Action == "UPDATE_CUSTOMER" && x.Detail.Contains("CUS-017"));
    }

    [Fact]
    public async Task Save_customer_rejects_duplicate_code_before_writing()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Customers.AddRange(
            new Customer { Id = 1, CustomerCode = "CUS-001", CustomerName = "Một" },
            new Customer { Id = 2, CustomerCode = "CUS-002", CustomerName = "Hai" });
        await db.SaveChangesAsync();

        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService());

        var act = () => service.SaveAsync(new CustomerDto
        {
            Id = 2,
            CustomerCode = "CUS-001",
            CustomerName = "Không được ghi",
            Phone = "0900000002"
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*đã tồn tại*");
        (await db.Customers.FindAsync(2))!.CustomerName.Should().Be("Hai");
    }

    [Fact]
    public async Task Save_customer_rejects_duplicate_phone_after_normalizing_vietnamese_format()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Customers.Add(new Customer
        {
            Id = 1,
            CustomerCode = "CUS-001",
            CustomerName = "Khách hiện có",
            Phone = "090 123-4567"
        });
        await db.SaveChangesAsync();

        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(
                ("Module.customers.requirePhone", "false"),
                ("Module.customers.requireTaxCode", "false"),
                ("Module.customers.checkDuplicatePhone", "true"),
                ("Module.customers.checkDuplicateTaxCode", "false")));

        var act = () => service.SaveAsync(new CustomerDto
        {
            CustomerCode = "CUS-002",
            CustomerName = "Khách trùng điện thoại",
            Phone = "+84 (90) 123.4567"
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Số điện thoại khách hàng đã tồn tại*");
        db.Customers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Save_customer_rejects_duplicate_tax_code_ignoring_case_and_outer_spaces()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Customers.Add(new Customer
        {
            Id = 1,
            CustomerCode = "CUS-001",
            CustomerName = "Khách hiện có",
            TaxCode = " 0100123456 "
        });
        await db.SaveChangesAsync();

        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(
                ("Module.customers.requirePhone", "false"),
                ("Module.customers.requireTaxCode", "false"),
                ("Module.customers.checkDuplicatePhone", "false"),
                ("Module.customers.checkDuplicateTaxCode", "true")));

        var act = () => service.SaveAsync(new CustomerDto
        {
            CustomerCode = "CUS-002",
            CustomerName = "Khách trùng MST",
            TaxCode = "0100123456"
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Mã số thuế khách hàng đã tồn tại*");
        db.Customers.Should().HaveCount(1);
    }

    [Fact]
    public async Task Save_customer_can_edit_an_existing_legacy_duplicate_without_changing_contact_values()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Customers.AddRange(
            new Customer { Id = 1, CustomerCode = "CUS-001", CustomerName = "Khách một", Phone = "0901234567" },
            new Customer { Id = 2, CustomerCode = "CUS-002", CustomerName = "Khách hai", Phone = "090 123-4567" });
        await db.SaveChangesAsync();

        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(("Module.customers.requirePhone", "false")));
        var dto = await service.GetAsync(2);
        dto.Should().NotBeNull();
        dto!.Note = "Cập nhật ghi chú";

        var saved = await service.SaveAsync(dto);

        saved.Note.Should().Be("Cập nhật ghi chú");
        (await db.Customers.FindAsync(2))!.Phone.Should().Be("090 123-4567");
    }

    [Fact]
    public async Task Save_customer_requires_phone_when_customer_policy_is_enabled()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(("Module.customers.requirePhone", "true")));

        var act = () => service.SaveAsync(new CustomerDto
        {
            CustomerCode = "CUS-003",
            CustomerName = "Thiếu điện thoại"
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Số điện thoại khách hàng*");
        db.Customers.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_customer_requires_tax_code_only_when_customer_policy_is_enabled()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(
                ("Module.customers.requirePhone", "false"),
                ("Module.customers.requireTaxCode", "true")));

        var act = () => service.SaveAsync(new CustomerDto
        {
            CustomerCode = "CUS-004",
            CustomerName = "Thiếu mã số thuế"
        });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Mã số thuế khách hàng*");
        db.Customers.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_customer_allows_missing_tax_code_when_policy_is_disabled()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(
                ("Module.customers.requirePhone", "false"),
                ("Module.customers.requireTaxCode", "false")));

        var saved = await service.SaveAsync(new CustomerDto
        {
            CustomerCode = "CUS-005",
            CustomerName = "Không bắt buộc MST"
        });

        saved.TaxCode.Should().BeEmpty();
        db.Customers.Should().ContainSingle(x => x.CustomerCode == "CUS-005");
    }

    [Fact]
    public async Task Save_customer_generates_next_code_from_system_settings()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Customers.AddRange(
            new Customer { Id = 1, CustomerCode = "KH-00002", CustomerName = "Khách 2" },
            new Customer { Id = 2, CustomerCode = "KH-00009", CustomerName = "Khách 9" });
        await db.SaveChangesAsync();

        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(
                ("Module.customers.autoCustomerCode", "true"),
                ("Module.customers.customerCodePattern", "KH-{00000}"),
                ("Module.customers.requirePhone", "false")));

        var saved = await service.SaveAsync(new CustomerDto { CustomerName = "Khách mới" });

        saved.CustomerCode.Should().Be("KH-00010");
        db.Customers.Should().Contain(x => x.CustomerCode == "KH-00010");
    }

    [Fact]
    public async Task Save_customer_still_requires_manual_code_when_auto_generation_is_disabled()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(("Module.customers.requirePhone", "false")));

        var act = () => service.SaveAsync(new CustomerDto { CustomerName = "Thiếu mã" });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Mã khách hàng không được rỗng*");
        db.Customers.Should().BeEmpty();
    }

    [Fact]
    public async Task Save_customer_rejects_invalid_auto_code_pattern_without_writing()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        var service = new CustomerAppService(
            new CustomerRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new CustomerSettingsService(
                ("Module.customers.autoCustomerCode", "true"),
                ("Module.customers.customerCodePattern", "KH-{yyyyMMdd}"),
                ("Module.customers.requirePhone", "false")));

        var act = () => service.SaveAsync(new CustomerDto { CustomerName = "Khách lỗi cấu hình" });

        await act.Should().ThrowAsync<Exception>().WithMessage("*Quy tắc tự sinh mã khách hàng không hợp lệ*");
        db.Customers.Should().BeEmpty();
    }

    private sealed class CustomerSettingsService(params (string Key, string Value)[] values) : ISettingsService
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
