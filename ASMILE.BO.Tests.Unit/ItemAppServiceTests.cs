using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Application.Services;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Infrastructure.Persistence;
using ASMILE.BO.Infrastructure.Repositories;
using ASMILE.BO.Shared.Exceptions;
using ASMILE.BO.Tests.Unit.TestSupport;
using FluentAssertions;

namespace ASMILE.BO.Tests.Unit;

public sealed class ItemAppServiceTests
{
    [Fact]
    public async Task SaveAsync_rejects_minibar_item_without_commercial_setup()
    {
        var service = CreateService();

        var action = () => service.SaveAsync(new ItemDto
        {
            ItemCode = "WATER",
            ItemName = "Nước đóng chai",
            Unit = "Chai",
            IsMinibarItem = true,
            RetailPrice = 25_000m,
            MinibarStandardQuantity = 2,
            MinibarMinStock = 1,
            MinibarMaxStock = 4
        });

        await action.Should().ThrowAsync<AppValidationException>()
            .WithMessage("Hàng minibar phải có nhóm minibar.");
    }

    [Fact]
    public async Task SaveAsync_accepts_minibar_item_with_price_vat_category_and_stock_bounds()
    {
        var repository = new FakeItemRepository();
        var service = CreateService(repository);

        var saved = await service.SaveAsync(new ItemDto
        {
            ItemCode = "WATER",
            ItemName = "Nước đóng chai",
            Unit = "Chai",
            GroupName = "Nước giải khát",
            IsMinibarItem = true,
            MinibarCategory = "Nước",
            RetailPrice = 25_000m,
            VatRate = 8m,
            MinibarStandardQuantity = 2,
            MinibarMinStock = 1,
            MinibarMaxStock = 4,
            IsActive = true
        });

        saved.Id.Should().Be(1);
        saved.IsMinibarItem.Should().BeTrue();
        saved.MinibarCategory.Should().Be("Nước");
        saved.RetailPrice.Should().Be(25_000m);
        repository.Rows.Should().ContainSingle(x => x.ItemCode == "WATER");
    }

    [Fact]
    public async Task SaveAsync_generates_next_product_code_from_module_settings()
    {
        var repository = new FakeItemRepository();
        repository.Rows.Add(new Item { Id = 1, ItemCode = "HH-00002", ItemName = "Nước 2" });
        repository.Rows.Add(new Item { Id = 2, ItemCode = "HH-00009", ItemName = "Nước 9" });
        var service = CreateService(repository,
            ("Module.items.autoProductCode", "true"),
            ("Module.items.productCodePattern", "HH-{00000}"));

        var saved = await service.SaveAsync(new ItemDto { ItemName = "Nước mới", Unit = "Chai" });

        saved.ItemCode.Should().Be("HH-00010");
        repository.Rows.Should().ContainSingle(x => x.ItemCode == "HH-00010");
    }

    [Fact]
    public async Task SaveAsync_generates_product_code_from_existing_database_rows()
    {
        await using var db = TestDbContextFactory.CreateContext(null, seedAccountingCore: false);
        db.Items.AddRange(
            new Item { Id = 1, ItemCode = "HH-00002", ItemName = "Nước 2", Unit = "Chai" },
            new Item { Id = 2, ItemCode = "HH-00009", ItemName = "Nước 9", Unit = "Chai" });
        await db.SaveChangesAsync();

        var service = new ItemAppService(
            new ItemRepository(db),
            new EfUnitOfWork(db),
            TestDbContextFactory.AuditLogService(db),
            new FakeSettingsService(
                ("Module.items.autoProductCode", "true"),
                ("Module.items.productCodePattern", "HH-{00000}"),
                ("Module.items.defaultProductGroup", "Đồ uống"),
                ("Module.items.defaultUnit", "Chai")));

        var saved = await service.SaveAsync(new ItemDto { ItemName = "Nước mới" });

        saved.ItemCode.Should().Be("HH-00010");
        saved.GroupName.Should().Be("Đồ uống");
        db.Items.Should().ContainSingle(x => x.ItemCode == "HH-00010" && x.Unit == "Chai");
    }

    [Fact]
    public async Task SaveAsync_rejects_invalid_product_code_pattern_without_writing()
    {
        var repository = new FakeItemRepository();
        var service = CreateService(repository,
            ("Module.items.autoProductCode", "true"),
            ("Module.items.productCodePattern", "HH-{yyyyMMdd}"));

        var act = () => service.SaveAsync(new ItemDto { ItemName = "Nước mới", Unit = "Chai" });

        await act.Should().ThrowAsync<AppValidationException>()
            .WithMessage("Quy tắc tự sinh mã hàng không hợp lệ*");
        repository.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_still_requires_manual_code_when_auto_generation_is_disabled()
    {
        var repository = new FakeItemRepository();
        var service = CreateService(repository);

        var act = () => service.SaveAsync(new ItemDto { ItemName = "Nước mới", Unit = "Chai" });

        await act.Should().ThrowAsync<AppValidationException>().WithMessage("Mã hàng không được rỗng.");
        repository.Rows.Should().BeEmpty();
    }

    [Fact]
    public async Task SaveAsync_applies_configured_group_and_unit_defaults_to_new_items()
    {
        var repository = new FakeItemRepository();
        var service = CreateService(repository,
            ("Module.items.defaultProductGroup", "Đồ uống"),
            ("Module.items.defaultUnit", "Chai"));

        var saved = await service.SaveAsync(new ItemDto { ItemCode = "WATER", ItemName = "Nước" });

        saved.GroupName.Should().Be("Đồ uống");
        saved.Unit.Should().Be("Chai");
        repository.Rows.Should().ContainSingle(x => x.GroupName == "Đồ uống" && x.Unit == "Chai");
    }

    [Fact]
    public async Task SaveAsync_applies_configured_vat_default_only_when_rate_is_unspecified()
    {
        var repository = new FakeItemRepository();
        var service = CreateService(repository, ("Module.items.defaultVat", "8%"));

        var taxable = await service.SaveAsync(new ItemDto { ItemCode = "TAXABLE", ItemName = "Chịu thuế", Unit = "Cái" });
        var taxExempt = await service.SaveAsync(new ItemDto { ItemCode = "EXEMPT", ItemName = "Không chịu thuế", Unit = "Cái", VatRate = 0m });

        taxable.VatRate.Should().Be(8m);
        taxExempt.VatRate.Should().Be(0m);
    }

    [Fact]
    public async Task SaveAsync_rejects_invalid_configured_vat_default_without_writing()
    {
        var repository = new FakeItemRepository();
        var service = CreateService(repository, ("Module.items.defaultVat", "invalid"));

        var act = () => service.SaveAsync(new ItemDto { ItemCode = "TAXABLE", ItemName = "Chịu thuế", Unit = "Cái" });

        await act.Should().ThrowAsync<AppValidationException>()
            .WithMessage("Thuế VAT mặc định không hợp lệ*");
        repository.Rows.Should().BeEmpty();
    }

    private static ItemAppService CreateService(FakeItemRepository? repository = null, params (string Key, string Value)[] settings)
        => new(repository ?? new FakeItemRepository(), new FakeUnitOfWork(), new FakeAuditLogService(), new FakeSettingsService(settings));

    private sealed class FakeItemRepository : IItemRepository
    {
        public List<Item> Rows { get; } = [];
        public Task AddAsync(Item entity, CancellationToken cancellationToken = default)
        {
            entity.Id = Rows.Count + 1;
            Rows.Add(entity);
            return Task.CompletedTask;
        }
        public Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(Rows.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Item>>(Rows);
        public Task<IReadOnlyList<Item>> SearchAsync(string keyword, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Item>>(Rows);
        public Task<Item?> FindByCodeAsync(string itemCode, CancellationToken cancellationToken = default) => Task.FromResult(Rows.SingleOrDefault(x => x.ItemCode == itemCode));
        public Task<bool> ExistsCodeAsync(string itemCode, int excludingId = 0, CancellationToken cancellationToken = default) => Task.FromResult(Rows.Any(x => x.ItemCode == itemCode && x.Id != excludingId));
        public Task<bool> ExistsNameAsync(string itemName, int excludingId = 0, CancellationToken cancellationToken = default) => Task.FromResult(Rows.Any(x => x.ItemName == itemName && x.Id != excludingId));
        public Task<IReadOnlyList<string>> GetCodesStartingWithAsync(string prefix, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>(Rows.Where(x => x.ItemCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)).Select(x => x.ItemCode).ToArray());
        public Task<bool> IsUsedInTransactionsAsync(int itemId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public void Update(Item entity) { }
        public void Delete(Item entity) => Rows.Remove(entity);
    }

    private sealed class FakeSettingsService(params (string Key, string Value)[] values) : ISettingsService
    {
        private readonly IReadOnlyDictionary<string, string> _values = values.ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

        public Task<IReadOnlyList<SettingDefinitionDto>> GetDefinitionsAsync(SettingScope scope, string? scopeTarget) => Task.FromResult<IReadOnlyList<SettingDefinitionDto>>([]);
        public Task<IReadOnlyList<SettingValueDto>> GetValuesAsync(SettingScope scope, string? scopeTarget) => Task.FromResult<IReadOnlyList<SettingValueDto>>([]);
        public Task<SettingValueDto?> GetValueAsync(string key, SettingScope scope, string? scopeTarget)
            => Task.FromResult(_values.TryGetValue(key, out var value)
                ? new SettingValueDto { Key = key, Scope = scope, ScopeTarget = scopeTarget, Value = value }
                : null);
        public Task SaveValueAsync(SaveSettingValueRequest request) => Task.CompletedTask;
        public Task SaveValuesAsync(IReadOnlyList<SaveSettingValueRequest> requests) => Task.CompletedTask;
        public Task ResetToDefaultAsync(string key, SettingScope scope, string? scopeTarget) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) => operation(cancellationToken);
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task LogAsync(string action, string detail, string username = "system", CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
