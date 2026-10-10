using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Application.Services;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;
using System.Data;

namespace ASMILE.BO.Application.Services;

public sealed class PersonAppService : IPersonAppService
{
    private readonly IPersonRepository _personRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;
    private readonly ISettingsService _settingsService;

    public PersonAppService(
        IPersonRepository personRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService,
        ISettingsService settingsService)
    {
        _personRepository = personRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
        _settingsService = settingsService;
    }

    public async Task<IReadOnlyList<PersonDto>> SearchAsync(int? departmentId, string keyword, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var rows = await _personRepository.SearchAsync(departmentId, keyword ?? string.Empty, isActive, cancellationToken);
        var departments = await _departmentRepository.SearchAsync(string.Empty, null, cancellationToken);
        var deptMap = departments.ToDictionary(x => x.Id, x => x);

        return rows.Select(x => MapToDto(x, deptMap.GetValueOrDefault(x.DepartmentId))).ToList();
    }

    public async Task<PersonDto> SaveAsync(PersonDto dto, CancellationToken cancellationToken = default)
    {
        Person? savedEntity = null;
        string? auditAction = null;
        string? auditDetails = null;

        await _unitOfWork.ExecuteInTransactionAsync(async transactionToken =>
        {
            var personCode = dto.PersonCode?.Trim() ?? string.Empty;
            if (dto.Id <= 0
                && string.IsNullOrWhiteSpace(personCode)
                && await IsEmployeeCodeGenerationEnabledAsync(transactionToken))
            {
                var patternSetting = await _settingsService.GetValueAsync(
                    "Module.departments.employeeCodePattern", SettingScope.App, "departments");
                personCode = await GeneratePersonCodeAsync(patternSetting?.Value ?? "NV-{00000}", transactionToken);
            }

            if (string.IsNullOrWhiteSpace(personCode))
            {
                throw new AppValidationException("Mã nhân sự không được rỗng.");
            }

            if (string.IsNullOrWhiteSpace(dto.FullName))
            {
                throw new AppValidationException("Tên nhân sự không được rỗng.");
            }

            if (dto.DepartmentId <= 0)
            {
                throw new AppValidationException("Vui lòng chọn bộ phận.");
            }

            if (await _personRepository.ExistsCodeAsync(personCode, dto.Id, transactionToken))
            {
                throw new AppValidationException("Mã nhân sự đã tồn tại.");
            }

            if (dto.Id <= 0)
            {
                savedEntity = MapToEntity(dto, personCode);
                savedEntity.CreatedAt = DateTime.UtcNow;
                await _personRepository.AddAsync(savedEntity, transactionToken);
                await _unitOfWork.SaveChangesAsync(transactionToken);
                auditAction = "ADD_PERSON";
                auditDetails = $"Thêm {savedEntity.PersonCode}";
                return;
            }

            savedEntity = await _personRepository.GetByIdAsync(dto.Id, transactionToken)
                ?? throw new AppValidationException("Không tìm thấy nhân sự.");

            savedEntity.PersonCode = personCode;
            savedEntity.FullName = dto.FullName.Trim();
            savedEntity.DepartmentId = dto.DepartmentId;
            savedEntity.Position = dto.Position?.Trim() ?? string.Empty;
            savedEntity.Phone = dto.Phone?.Trim() ?? string.Empty;
            savedEntity.Email = dto.Email?.Trim() ?? string.Empty;
            savedEntity.Note = dto.Note?.Trim() ?? string.Empty;
            savedEntity.IsActive = dto.IsActive;

            _personRepository.Update(savedEntity);
            await _unitOfWork.SaveChangesAsync(transactionToken);
            auditAction = "UPDATE_PERSON";
            auditDetails = $"Sửa {savedEntity.PersonCode}";
        }, IsolationLevel.Serializable, cancellationToken);

        await _auditLogService.LogAsync(auditAction!, auditDetails!, cancellationToken: cancellationToken);
        return await BuildDtoAsync(savedEntity!, cancellationToken);
    }

    private async Task<bool> IsEmployeeCodeGenerationEnabledAsync(CancellationToken cancellationToken)
    {
        var setting = await _settingsService.GetValueAsync(
            "Module.departments.autoEmployeeCode", SettingScope.App, "departments");
        return bool.TryParse(setting?.Value, out var enabled) && enabled;
    }

    private async Task<string> GeneratePersonCodeAsync(string pattern, CancellationToken cancellationToken)
    {
        if (!SequenceCodePattern.IsValid(pattern))
        {
            throw new AppValidationException("Quy tắc tự sinh mã nhân sự không hợp lệ; cần đúng một token số dạng {00000}.");
        }

        var now = DateTime.Now;
        var existingCodes = await _personRepository.GetCodesStartingWithAsync(
            SequenceCodePattern.GetPrefix(pattern, now), cancellationToken);
        var nextSequence = SequenceCodePattern.GetNextSequence(pattern, now, existingCodes);
        var generatedCode = SequenceCodePattern.Render(pattern, now, nextSequence);
        if (generatedCode.Length > 50)
        {
            throw new AppValidationException("Mã nhân sự được sinh vượt quá giới hạn 50 ký tự.");
        }

        return generatedCode;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _personRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy nhân sự.");

        _personRepository.Delete(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_PERSON", $"Xóa {existing.PersonCode}", cancellationToken: cancellationToken);
    }

    private async Task<PersonDto> BuildDtoAsync(Person entity, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(entity.DepartmentId, cancellationToken);
        return MapToDto(entity, department);
    }

    private static PersonDto MapToDto(Person entity, Department? department) => new()
    {
        Id = entity.Id,
        PersonCode = entity.PersonCode,
        FullName = entity.FullName,
        DepartmentId = entity.DepartmentId,
        DepartmentCode = department?.DepartmentCode ?? string.Empty,
        DepartmentName = department?.DepartmentName ?? string.Empty,
        Position = entity.Position,
        Phone = entity.Phone,
        Email = entity.Email,
        Note = entity.Note,
        IsActive = entity.IsActive,
        CreatedAt = entity.CreatedAt
    };

    private static Person MapToEntity(PersonDto dto, string personCode) => new()
    {
        Id = dto.Id,
        PersonCode = personCode,
        FullName = dto.FullName.Trim(),
        DepartmentId = dto.DepartmentId,
        Position = dto.Position?.Trim() ?? string.Empty,
        Phone = dto.Phone?.Trim() ?? string.Empty,
        Email = dto.Email?.Trim() ?? string.Empty,
        Note = dto.Note?.Trim() ?? string.Empty,
        IsActive = dto.IsActive
    };
}
