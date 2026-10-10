using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;
using System.Data;

namespace ASMILE.BO.Application.Services;

public sealed class ItemAppService : IItemAppService
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ISettingsService _settingsService;

    public ItemAppService(IItemRepository itemRepository, IUnitOfWork unitOfWork, IAuditLogService auditLogService, ISettingsService settingsService)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<ItemDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var rows = await _itemRepository.SearchAsync(keyword ?? string.Empty, cancellationToken);
        return rows.Select(MapToDto).ToList();
    }

    public async Task<ItemDto> SaveAsync(ItemDto dto, CancellationToken cancellationToken = default)
    {
        ItemDto? saved = null;
        string? auditAction = null;
        string? auditDetails = null;

        await _unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var itemCode = dto.ItemCode?.Trim() ?? string.Empty;
            if (dto.Id == 0 && string.IsNullOrWhiteSpace(itemCode) && await IsAutoProductCodeEnabledAsync())
            {
                var patternSetting = await _settingsService.GetValueAsync("Module.items.productCodePattern", SettingScope.App, "items");
                itemCode = await GenerateProductCodeAsync(patternSetting?.Value ?? "HH-{00000}", transactionToken);
            }

            var unit = dto.Unit?.Trim() ?? string.Empty;
            var groupName = dto.GroupName?.Trim() ?? string.Empty;
            var vatRate = dto.VatRate;
            if (dto.Id == 0 && string.IsNullOrWhiteSpace(unit))
                unit = await GetNewItemDefaultAsync("defaultUnit", "Cái");
            if (dto.Id == 0 && string.IsNullOrWhiteSpace(groupName))
                groupName = await GetNewItemDefaultAsync("defaultProductGroup", "Hàng hóa");
            if (dto.Id == 0 && vatRate is null)
                vatRate = await GetNewItemVatRateAsync();

            if (string.IsNullOrWhiteSpace(itemCode)) throw new AppValidationException("Mã hàng không được rỗng.");
            if (string.IsNullOrWhiteSpace(dto.ItemName)) throw new AppValidationException("Tên hàng không được rỗng.");
            if (string.IsNullOrWhiteSpace(unit)) throw new AppValidationException("Đơn vị tính không được rỗng.");
            if (vatRate is null || dto.RetailPrice < 0 || vatRate is < 0 or > 100)
                throw new AppValidationException("Giá bán hoặc VAT không hợp lệ.");
            if (dto.MinibarStandardQuantity < 0 || dto.MinibarMinStock < 0 || dto.MinibarMaxStock < 0 || dto.MinibarMaxStock < dto.MinibarMinStock)
                throw new AppValidationException("Định mức tồn minibar không hợp lệ.");
            if (dto.IsMinibarItem)
            {
                if (string.IsNullOrWhiteSpace(dto.MinibarCategory))
                    throw new AppValidationException("Hàng minibar phải có nhóm minibar.");
                if (dto.RetailPrice <= 0)
                    throw new AppValidationException("Hàng minibar phải có giá bán lớn hơn 0.");
                if (dto.MinibarStandardQuantity <= 0)
                    throw new AppValidationException("Hàng minibar phải có định mức lớn hơn 0.");
                if (dto.MinibarMinStock > dto.MinibarStandardQuantity || dto.MinibarMaxStock < dto.MinibarStandardQuantity)
                    throw new AppValidationException("Min/max tồn minibar phải bao quanh định mức chuẩn.");
            }

            if (await _itemRepository.ExistsCodeAsync(itemCode, dto.Id, transactionToken))
                throw new AppValidationException("Mã hàng đã tồn tại.");
            if (await _itemRepository.ExistsNameAsync(dto.ItemName.Trim(), dto.Id, transactionToken))
                throw new AppValidationException("Tên hàng đã tồn tại.");

            if (dto.Id == 0)
            {
                var entity = MapToEntity(dto, itemCode, unit, groupName, vatRate.Value);
                await _itemRepository.AddAsync(entity, transactionToken);
                await _unitOfWork.SaveChangesAsync(transactionToken);
                saved = MapToDto(entity);
                auditAction = "ADD_ITEM";
                auditDetails = $"Thêm {entity.ItemCode}";
                return;
            }

            var existing = await _itemRepository.GetByIdAsync(dto.Id, transactionToken)
                ?? throw new AppValidationException("Không tìm thấy hàng hóa.");
            existing.ItemCode = itemCode;
            existing.ItemName = dto.ItemName.Trim();
            existing.Unit = unit;
            existing.GroupName = groupName;
            existing.SupplierCode = dto.SupplierCode.Trim();
            existing.SupplierName = dto.SupplierName.Trim();
            existing.RetailPrice = dto.RetailPrice;
            existing.VatRate = vatRate.Value;
            existing.IsMinibarItem = dto.IsMinibarItem;
            existing.MinibarCategory = dto.MinibarCategory.Trim();
            existing.MinibarStandardQuantity = dto.MinibarStandardQuantity;
            existing.MinibarMinStock = dto.MinibarMinStock;
            existing.MinibarMaxStock = dto.MinibarMaxStock;
            existing.IsActive = dto.IsActive;
            _itemRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            saved = MapToDto(existing);
            auditAction = "UPDATE_ITEM";
            auditDetails = $"Sửa {existing.ItemCode}";
        }, IsolationLevel.Serializable, cancellationToken);

        await _auditLogService.LogAsync(auditAction!, auditDetails!, cancellationToken: cancellationToken);
        return saved!;
    }

    private async Task<bool> IsAutoProductCodeEnabledAsync()
    {
        var setting = await _settingsService.GetValueAsync("Module.items.autoProductCode", SettingScope.App, "items");
        return bool.TryParse(setting?.Value, out var enabled) && enabled;
    }

    private async Task<string> GetNewItemDefaultAsync(string key, string fallback)
    {
        var setting = await _settingsService.GetValueAsync($"Module.items.{key}", SettingScope.App, "items");
        return string.IsNullOrWhiteSpace(setting?.Value) ? fallback : setting.Value.Trim();
    }

    private async Task<decimal> GetNewItemVatRateAsync()
    {
        var setting = await _settingsService.GetValueAsync("Module.items.defaultVat", SettingScope.App, "items");
        var configuredValue = (setting?.Value ?? "10%").Trim().TrimEnd('%').Trim();
        if ((!decimal.TryParse(configuredValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var vatRate)
                && !decimal.TryParse(configuredValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), out vatRate))
            || vatRate is < 0 or > 100)
        {
            throw new AppValidationException("Thuế VAT mặc định không hợp lệ; hãy kiểm tra lại Cài đặt hàng hóa.");
        }

        return vatRate;
    }

    private async Task<string> GenerateProductCodeAsync(string pattern, CancellationToken cancellationToken)
    {
        if (!SequenceCodePattern.IsValid(pattern))
            throw new AppValidationException("Quy tắc tự sinh mã hàng không hợp lệ; cần đúng một token số dạng {00000}.");

        var now = DateTime.Now;
        var existingCodes = await _itemRepository.GetCodesStartingWithAsync(SequenceCodePattern.GetPrefix(pattern, now), cancellationToken);
        var nextSequence = SequenceCodePattern.GetNextSequence(pattern, now, existingCodes);
        var generatedCode = SequenceCodePattern.Render(pattern, now, nextSequence);
        if (generatedCode.Length > 50)
            throw new AppValidationException("Mã hàng được sinh vượt quá giới hạn 50 ký tự.");
        return generatedCode;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _itemRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy hàng hóa.");

        if (await _itemRepository.IsUsedInTransactionsAsync(id, cancellationToken))
        {
            throw new AppValidationException("Hàng hóa đã phát sinh giao dịch, không thể xóa.");
        }

        _itemRepository.Delete(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_ITEM", $"Xóa {existing.ItemCode}", cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _itemRepository.SearchAsync(string.Empty, cancellationToken);
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.ItemCode)
            .Select(x => new LookupItemDto { Id = x.Id, Code = x.ItemCode, Name = x.ItemName })
            .ToList();
    }

    private static ItemDto MapToDto(Item entity) => new()
    {
        Id = entity.Id,
        ItemCode = entity.ItemCode,
        ItemName = entity.ItemName,
        Unit = entity.Unit,
        GroupName = entity.GroupName,
        SupplierCode = entity.SupplierCode,
        SupplierName = entity.SupplierName,
        RetailPrice = entity.RetailPrice,
        VatRate = entity.VatRate,
        IsMinibarItem = entity.IsMinibarItem,
        MinibarCategory = entity.MinibarCategory,
        MinibarStandardQuantity = entity.MinibarStandardQuantity,
        MinibarMinStock = entity.MinibarMinStock,
        MinibarMaxStock = entity.MinibarMaxStock,
        IsActive = entity.IsActive
    };

    private static Item MapToEntity(ItemDto dto, string itemCode, string unit, string groupName, decimal vatRate) => new()
    {
        Id = dto.Id,
        ItemCode = itemCode,
        ItemName = dto.ItemName.Trim(),
        Unit = unit,
        GroupName = groupName,
        SupplierCode = dto.SupplierCode.Trim(),
        SupplierName = dto.SupplierName.Trim(),
        RetailPrice = dto.RetailPrice,
        VatRate = vatRate,
        IsMinibarItem = dto.IsMinibarItem,
        MinibarCategory = dto.MinibarCategory.Trim(),
        MinibarStandardQuantity = dto.MinibarStandardQuantity,
        MinibarMinStock = dto.MinibarMinStock,
        MinibarMaxStock = dto.MinibarMaxStock,
        IsActive = dto.IsActive
    };
}

