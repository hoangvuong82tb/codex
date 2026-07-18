using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class DepartmentAppService : IDepartmentAppService
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public DepartmentAppService(
        IDepartmentRepository departmentRepository,
        IPersonRepository personRepository,
        IUnitOfWork unitOfWork,
        IAuditLogService auditLogService)
    {
        _departmentRepository = departmentRepository;
        _personRepository = personRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<DepartmentDto>> SearchAsync(string keyword, bool? isActive = null, CancellationToken cancellationToken = default)
    {
        var rows = await _departmentRepository.SearchAsync(keyword ?? string.Empty, isActive, cancellationToken);
        var peopleRows = await _personRepository.SearchAsync(null, string.Empty, null, cancellationToken);
        var peopleCountByDepartment = peopleRows.GroupBy(x => x.DepartmentId).ToDictionary(g => g.Key, g => g.Count());

        return rows.Select(x =>
        {
            var dto = MapToDto(x);
            dto.PeopleCount = peopleCountByDepartment.TryGetValue(x.Id, out var count) ? count : 0;
            return dto;
        }).ToList();
    }

    public async Task<DepartmentDto> SaveAsync(DepartmentDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.DepartmentCode))
        {
            throw new AppValidationException("Mã bộ phận không được rỗng.");
        }

        if (string.IsNullOrWhiteSpace(dto.DepartmentName))
        {
            throw new AppValidationException("Tên bộ phận không được rỗng.");
        }

        var normalizedCode = dto.DepartmentCode.Trim();
        if (await _departmentRepository.ExistsCodeAsync(normalizedCode, dto.Id, cancellationToken))
        {
            throw new AppValidationException("Mã bộ phận đã tồn tại.");
        }

        if (dto.ParentDepartmentId.HasValue && dto.ParentDepartmentId.Value == dto.Id && dto.Id > 0)
        {
            throw new AppValidationException("Bộ phận cha không hợp lệ.");
        }

        if (dto.Id <= 0)
        {
            var entity = MapToEntity(dto);
            await _departmentRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("ADD_DEPARTMENT", $"Thêm {entity.DepartmentCode}", cancellationToken: cancellationToken);
            return MapToDto(entity);
        }

        var existing = await _departmentRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy bộ phận.");

        existing.DepartmentCode = normalizedCode;
        existing.DepartmentName = dto.DepartmentName.Trim();
        existing.ParentDepartmentId = dto.ParentDepartmentId;
        existing.Level = Math.Max(dto.Level, 1);
        existing.Note = dto.Note?.Trim() ?? string.Empty;
        existing.IsActive = dto.IsActive;

        _departmentRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UPDATE_DEPARTMENT", $"Sửa {existing.DepartmentCode}", cancellationToken: cancellationToken);

        return MapToDto(existing);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _departmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy bộ phận.");

        if (await _departmentRepository.HasChildrenAsync(id, cancellationToken))
        {
            throw new AppValidationException("Bộ phận đang có bộ phận con, không thể xóa.");
        }

        if (await _departmentRepository.HasPeopleAsync(id, cancellationToken))
        {
            throw new AppValidationException("Bộ phận đang có nhân sự, không thể xóa.");
        }

        _departmentRepository.Delete(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_DEPARTMENT", $"Xóa {existing.DepartmentCode}", cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _departmentRepository.SearchAsync(string.Empty, true, cancellationToken);
        return rows.OrderBy(x => x.DepartmentCode)
            .Select(x => new LookupItemDto
            {
                Id = x.Id,
                Code = x.DepartmentCode,
                Name = x.DepartmentName
            }).ToList();
    }

    private static DepartmentDto MapToDto(Department entity) => new()
    {
        Id = entity.Id,
        DepartmentCode = entity.DepartmentCode,
        DepartmentName = entity.DepartmentName,
        ParentDepartmentId = entity.ParentDepartmentId,
        Level = entity.Level,
        Note = entity.Note,
        IsActive = entity.IsActive
    };

    private static Department MapToEntity(DepartmentDto dto) => new()
    {
        Id = dto.Id,
        DepartmentCode = dto.DepartmentCode.Trim(),
        DepartmentName = dto.DepartmentName.Trim(),
        ParentDepartmentId = dto.ParentDepartmentId,
        Level = Math.Max(dto.Level, 1),
        Note = dto.Note?.Trim() ?? string.Empty,
        IsActive = dto.IsActive
    };
}
