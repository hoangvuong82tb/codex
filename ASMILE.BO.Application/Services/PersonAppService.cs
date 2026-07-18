using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class PersonAppService : IPersonAppService
{
    private readonly IPersonRepository _personRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public PersonAppService(
        IPersonRepository personRepository,
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _personRepository = personRepository;
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
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
        if (string.IsNullOrWhiteSpace(dto.PersonCode))
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

        var normalizedCode = dto.PersonCode.Trim();
        if (await _personRepository.ExistsCodeAsync(normalizedCode, dto.Id, cancellationToken))
        {
            throw new AppValidationException("Mã nhân sự đã tồn tại.");
        }

        if (dto.Id <= 0)
        {
            var entity = MapToEntity(dto);
            entity.CreatedAt = DateTime.UtcNow;
            await _personRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("ADD_PERSON", $"Thêm {entity.PersonCode}", cancellationToken: cancellationToken);
            return await BuildDtoAsync(entity, cancellationToken);
        }

        var existing = await _personRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy nhân sự.");

        existing.PersonCode = normalizedCode;
        existing.FullName = dto.FullName.Trim();
        existing.DepartmentId = dto.DepartmentId;
        existing.Position = dto.Position?.Trim() ?? string.Empty;
        existing.Phone = dto.Phone?.Trim() ?? string.Empty;
        existing.Email = dto.Email?.Trim() ?? string.Empty;
        existing.Note = dto.Note?.Trim() ?? string.Empty;
        existing.IsActive = dto.IsActive;

        _personRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UPDATE_PERSON", $"Sửa {existing.PersonCode}", cancellationToken: cancellationToken);
        return await BuildDtoAsync(existing, cancellationToken);
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

    private static Person MapToEntity(PersonDto dto) => new()
    {
        Id = dto.Id,
        PersonCode = dto.PersonCode.Trim(),
        FullName = dto.FullName.Trim(),
        DepartmentId = dto.DepartmentId,
        Position = dto.Position?.Trim() ?? string.Empty,
        Phone = dto.Phone?.Trim() ?? string.Empty,
        Email = dto.Email?.Trim() ?? string.Empty,
        Note = dto.Note?.Trim() ?? string.Empty,
        IsActive = dto.IsActive
    };
}
