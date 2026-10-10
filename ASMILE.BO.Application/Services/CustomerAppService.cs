using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;
using System.Net.Mail;
using System.Data;

namespace ASMILE.BO.Application.Services;

public sealed class CustomerAppService : ICustomerAppService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ISettingsService _settingsService;

    public CustomerAppService(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ISettingsService settingsService)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
    }

    public async Task<CustomerDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _customerRepository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var rows = await _customerRepository.SearchAsync(keyword ?? string.Empty, cancellationToken);
        return rows.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto> SaveAsync(CustomerDto dto, CancellationToken cancellationToken = default)
    {
        CustomerDto? saved = null;
        string? auditAction = null;
        string? auditDetails = null;

        await _unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var customerCode = dto.CustomerCode?.Trim() ?? string.Empty;
            if (dto.Id == 0
                && string.IsNullOrWhiteSpace(customerCode)
                && await IsCustomerSettingEnabledAsync("autoCustomerCode", defaultValue: false))
            {
                var patternSetting = await _settingsService.GetValueAsync("Module.customers.customerCodePattern", SettingScope.App, "customers");
                customerCode = await GenerateCustomerCodeAsync(patternSetting?.Value ?? "KH-{00000}", transactionToken);
            }

            if (string.IsNullOrWhiteSpace(customerCode)) throw new AppValidationException("Mã khách hàng không được rỗng.");
            if (string.IsNullOrWhiteSpace(dto.CustomerName)) throw new AppValidationException("Tên khách hàng không được rỗng.");

            if (await IsCustomerSettingEnabledAsync("requirePhone", defaultValue: true)
                && string.IsNullOrWhiteSpace(dto.Phone))
            {
                throw new AppValidationException("Số điện thoại khách hàng không được rỗng.");
            }

            if (await IsCustomerSettingEnabledAsync("requireTaxCode", defaultValue: false)
                && string.IsNullOrWhiteSpace(dto.TaxCode))
            {
                throw new AppValidationException("Mã số thuế khách hàng không được rỗng.");
            }

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                try
                {
                    _ = new MailAddress(dto.Email.Trim());
                }
                catch (FormatException)
                {
                    throw new AppValidationException("Email khách hàng không hợp lệ.");
                }
            }

            if (await _customerRepository.ExistsCodeAsync(customerCode, dto.Id, transactionToken))
            {
                throw new AppValidationException("Mã khách hàng đã tồn tại.");
            }

            var existing = dto.Id == 0
                ? null
                : await _customerRepository.GetByIdAsync(dto.Id, transactionToken)
                    ?? throw new AppValidationException("Không tìm thấy khách hàng.");

            if (await IsCustomerSettingEnabledAsync("checkDuplicatePhone", defaultValue: true)
                && !HasSamePhone(dto.Phone, existing?.Phone)
                && await _customerRepository.ExistsPhoneAsync(dto.Phone.Trim(), dto.Id, transactionToken))
            {
                throw new AppValidationException("Số điện thoại khách hàng đã tồn tại.");
            }

            if (await IsCustomerSettingEnabledAsync("checkDuplicateTaxCode", defaultValue: true)
                && !HasSameTaxCode(dto.TaxCode, existing?.TaxCode)
                && await _customerRepository.ExistsTaxCodeAsync(dto.TaxCode.Trim(), dto.Id, transactionToken))
            {
                throw new AppValidationException("Mã số thuế khách hàng đã tồn tại.");
            }

            if (dto.Id == 0)
            {
                var entity = MapToEntity(dto, customerCode);
                await _customerRepository.AddAsync(entity, transactionToken);
                await _unitOfWork.SaveChangesAsync(transactionToken);
                saved = MapToDto(entity);
                auditAction = "ADD_CUSTOMER";
                auditDetails = $"Thêm {entity.CustomerCode}";
                return;
            }

            existing!.CustomerCode = customerCode;
            existing.CustomerName = dto.CustomerName.Trim();
            existing.Phone = dto.Phone.Trim();
            existing.Email = dto.Email.Trim();
            existing.TaxCode = dto.TaxCode.Trim();
            existing.BankAccountNo = dto.BankAccountNo.Trim();
            existing.Address = dto.Address.Trim();
            existing.Note = dto.Note.Trim();
            existing.IsActive = dto.IsActive;

            _customerRepository.Update(existing);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            saved = MapToDto(existing);
            auditAction = "UPDATE_CUSTOMER";
            auditDetails = $"Sửa {existing.CustomerCode}";
        }, IsolationLevel.Serializable, cancellationToken);

        await _auditLogService.LogAsync(auditAction!, auditDetails!, cancellationToken: cancellationToken);
        return saved!;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _customerRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy khách hàng.");

        _customerRepository.Delete(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_CUSTOMER", $"Xóa {existing.CustomerCode}", cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _customerRepository.SearchAsync(string.Empty, cancellationToken);
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.CustomerCode)
            .Select(x => new LookupItemDto { Id = x.Id, Code = x.CustomerCode, Name = x.CustomerName })
            .ToList();
    }

    private async Task<bool> IsCustomerSettingEnabledAsync(string key, bool defaultValue)
    {
        var setting = await _settingsService.GetValueAsync($"Module.customers.{key}", SettingScope.App, "customers");
        return bool.TryParse(setting?.Value, out var value) ? value : defaultValue;
    }

    private async Task<string> GenerateCustomerCodeAsync(string pattern, CancellationToken cancellationToken)
    {
        if (!SequenceCodePattern.IsValid(pattern))
        {
            throw new AppValidationException("Quy tắc tự sinh mã khách hàng không hợp lệ; cần đúng một token số dạng {00000}.");
        }

        var now = DateTime.Now;
        var existingCodes = await _customerRepository.GetCodesStartingWithAsync(
            SequenceCodePattern.GetPrefix(pattern, now), cancellationToken);
        var nextSequence = SequenceCodePattern.GetNextSequence(pattern, now, existingCodes);
        var generatedCode = SequenceCodePattern.Render(pattern, now, nextSequence);
        if (generatedCode.Length > 50)
        {
            throw new AppValidationException("Mã khách hàng được sinh vượt quá giới hạn 50 ký tự.");
        }

        return generatedCode;
    }

    private static bool HasSamePhone(string value, string? existingValue)
        => NormalizePhone(value).Equals(NormalizePhone(existingValue ?? string.Empty), StringComparison.Ordinal);

    private static string NormalizePhone(string value)
    {
        var normalized = value.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("(", string.Empty, StringComparison.Ordinal)
            .Replace(")", string.Empty, StringComparison.Ordinal)
            .Replace("+", string.Empty, StringComparison.Ordinal)
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Replace("/", string.Empty, StringComparison.Ordinal);
        return normalized.StartsWith("84", StringComparison.Ordinal) && normalized.Length > 2
            ? $"0{normalized[2..]}"
            : normalized;
    }

    private static bool HasSameTaxCode(string value, string? existingValue)
        => string.Equals(value.Trim(), existingValue?.Trim(), StringComparison.OrdinalIgnoreCase);

    private static CustomerDto MapToDto(Customer entity) => new()
    {
        Id = entity.Id,
        CustomerCode = entity.CustomerCode,
        CustomerName = entity.CustomerName,
        Phone = entity.Phone,
        Email = entity.Email,
        TaxCode = entity.TaxCode,
        BankAccountNo = entity.BankAccountNo,
        Address = entity.Address,
        Note = entity.Note,
        IsActive = entity.IsActive
    };

    private static Customer MapToEntity(CustomerDto dto, string customerCode) => new()
    {
        Id = dto.Id,
        CustomerCode = customerCode,
        CustomerName = dto.CustomerName.Trim(),
        Phone = dto.Phone.Trim(),
        Email = dto.Email.Trim(),
        TaxCode = dto.TaxCode.Trim(),
        BankAccountNo = dto.BankAccountNo.Trim(),
        Address = dto.Address.Trim(),
        Note = dto.Note.Trim(),
        IsActive = dto.IsActive
    };
}
