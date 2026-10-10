using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;
using System.Data;

namespace ASMILE.BO.Application.Services;

public sealed class SupplierAppService : ISupplierAppService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ISettingsService _settingsService;

    public SupplierAppService(
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ISettingsService settingsService)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<SupplierDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var rows = await _supplierRepository.SearchAsync(keyword ?? string.Empty, cancellationToken);
        return rows.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto> SaveAsync(SupplierDto dto, CancellationToken cancellationToken = default)
    {
        SupplierDto? saved = null;
        string? auditAction = null;
        string? auditDetails = null;

        await _unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var supplierCode = dto.SupplierCode?.Trim() ?? string.Empty;
            if (dto.Id == 0
                && string.IsNullOrWhiteSpace(supplierCode)
                && await IsSupplierSettingEnabledAsync("autoSupplierCode", defaultValue: false))
            {
                var patternSetting = await _settingsService.GetValueAsync("Module.suppliers.supplierCodePattern", SettingScope.App, "suppliers");
                supplierCode = await GenerateSupplierCodeAsync(patternSetting?.Value ?? "NCC-{00000}", transactionToken);
            }

            if (string.IsNullOrWhiteSpace(supplierCode)) throw new AppValidationException("Mã nhà cung cấp không được rỗng.");
            if (string.IsNullOrWhiteSpace(dto.SupplierName)) throw new AppValidationException("Tên nhà cung cấp không được rỗng.");
            if (await IsSupplierSettingEnabledAsync("requirePhone", defaultValue: true)
                && string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new AppValidationException("Số điện thoại nhà cung cấp không được rỗng.");
            }

            if (await _supplierRepository.ExistsCodeAsync(supplierCode, dto.Id, transactionToken))
            {
                throw new AppValidationException("Mã nhà cung cấp đã tồn tại.");
            }

            var existing = dto.Id == 0
                ? null
                : await _supplierRepository.GetByIdAsync(dto.Id, transactionToken)
                    ?? throw new AppValidationException("Không tìm thấy nhà cung cấp.");

            if (await IsSupplierSettingEnabledAsync("requireTaxCode", defaultValue: false)
                && string.IsNullOrWhiteSpace(dto.TaxCode))
            {
                throw new AppValidationException("Mã số thuế nhà cung cấp không được rỗng.");
            }

            if (await IsSupplierSettingEnabledAsync("checkDuplicateTaxCode", defaultValue: true)
                && !HasSameTaxCode(dto.TaxCode, existing?.TaxCode)
                && await _supplierRepository.ExistsTaxCodeAsync(dto.TaxCode.Trim(), dto.Id, transactionToken))
            {
                throw new AppValidationException("Mã số thuế nhà cung cấp đã tồn tại.");
            }

            if (dto.Id == 0)
            {
                var entity = MapToEntity(dto, supplierCode);
                await _supplierRepository.AddAsync(entity, transactionToken);
                await _unitOfWork.SaveChangesAsync(transactionToken);
                saved = MapToDto(entity);
                auditAction = "ADD_SUPPLIER";
                auditDetails = $"Thêm {entity.SupplierCode}";
                return;
            }

            existing!.SupplierCode = supplierCode;
            existing.SupplierName = dto.SupplierName.Trim();
            existing.Phone = dto.Phone.Trim();
            existing.TaxCode = dto.TaxCode.Trim();
            existing.BankAccountNo = dto.BankAccountNo.Trim();
            existing.Address = dto.Address.Trim();
            existing.Note = dto.Note.Trim();
            existing.IsActive = dto.IsActive;

            _supplierRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            saved = MapToDto(existing);
            auditAction = "UPDATE_SUPPLIER";
            auditDetails = $"Sửa {existing.SupplierCode}";
        }, IsolationLevel.Serializable, cancellationToken);

        await _auditLogService.LogAsync(auditAction!, auditDetails!, cancellationToken: cancellationToken);
        return saved!;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _supplierRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy nhà cung cấp.");

        if (await _supplierRepository.IsUsedInReceiptsAsync(existing.SupplierCode, cancellationToken))
        {
            throw new AppValidationException("Nhà cung cấp đã phát sinh phiếu nhập, không thể xóa.");
        }

        _supplierRepository.Delete(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_SUPPLIER", $"Xóa {existing.SupplierCode}", cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _supplierRepository.SearchAsync(string.Empty, cancellationToken);
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.SupplierCode)
            .Select(x => new LookupItemDto { Id = x.Id, Code = x.SupplierCode, Name = x.SupplierName })
            .ToList();
    }

    private async Task<bool> IsSupplierSettingEnabledAsync(string key, bool defaultValue)
    {
        var setting = await _settingsService.GetValueAsync($"Module.suppliers.{key}", SettingScope.App, "suppliers");
        return bool.TryParse(setting?.Value, out var value) ? value : defaultValue;
    }

    private async Task<string> GenerateSupplierCodeAsync(string pattern, CancellationToken cancellationToken)
    {
        if (!SequenceCodePattern.IsValid(pattern))
        {
            throw new AppValidationException("Quy tắc tự sinh mã nhà cung cấp không hợp lệ; cần đúng một token số dạng {00000}.");
        }

        var now = DateTime.Now;
        var existingCodes = await _supplierRepository.GetCodesStartingWithAsync(
            SequenceCodePattern.GetPrefix(pattern, now), cancellationToken);
        var nextSequence = SequenceCodePattern.GetNextSequence(pattern, now, existingCodes);
        var generatedCode = SequenceCodePattern.Render(pattern, now, nextSequence);
        if (generatedCode.Length > 50)
        {
            throw new AppValidationException("Mã nhà cung cấp được sinh vượt quá giới hạn 50 ký tự.");
        }

        return generatedCode;
    }

    private static bool HasSameTaxCode(string value, string? existingValue)
        => string.Equals(value.Trim(), existingValue?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static SupplierDto MapToDto(Supplier entity) => new()
    {
        Id = entity.Id,
        SupplierCode = entity.SupplierCode,
        SupplierName = entity.SupplierName,
        Phone = entity.Phone,
        TaxCode = entity.TaxCode,
        BankAccountNo = entity.BankAccountNo,
        Address = entity.Address,
        Note = entity.Note,
        IsActive = entity.IsActive
    };

    private static Supplier MapToEntity(SupplierDto dto, string supplierCode) => new()
    {
        Id = dto.Id,
        SupplierCode = supplierCode,
        SupplierName = dto.SupplierName.Trim(),
        Phone = dto.Phone.Trim(),
        TaxCode = dto.TaxCode.Trim(),
        BankAccountNo = dto.BankAccountNo.Trim(),
        Address = dto.Address.Trim(),
        Note = dto.Note.Trim(),
        IsActive = dto.IsActive
    };
}

