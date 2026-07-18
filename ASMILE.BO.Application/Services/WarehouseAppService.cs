using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class WarehouseAppService : IWarehouseAppService
{
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public WarehouseAppService(IWarehouseRepository warehouseRepository, IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _warehouseRepository = warehouseRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<WarehouseDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var rows = await _warehouseRepository.SearchAsync(keyword ?? string.Empty, cancellationToken);
        var map = rows.ToDictionary(x => x.Id, x => x.WarehouseCode);

        return rows.Select(x => new WarehouseDto
        {
            Id = x.Id,
            WarehouseCode = x.WarehouseCode,
            WarehouseName = x.WarehouseName,
            Note = x.Note,
            DepartmentName = x.DepartmentName,
            ResponsiblePersonName = x.ResponsiblePersonName,
            ParentWarehouseId = x.ParentWarehouseId,
            ParentWarehouseCode = x.ParentWarehouseId.HasValue && map.TryGetValue(x.ParentWarehouseId.Value, out var parentCode)
                ? parentCode
                : string.Empty,
            IsConsumptionWarehouse = x.IsConsumptionWarehouse,
            IsActive = x.IsActive
        }).ToList();
    }

    public async Task<WarehouseDto> SaveAsync(WarehouseDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.WarehouseCode)) throw new AppValidationException("Mã kho không được rỗng.");
        if (string.IsNullOrWhiteSpace(dto.WarehouseName)) throw new AppValidationException("Tên kho không được rỗng.");
        if (dto.ParentWarehouseId.HasValue && dto.ParentWarehouseId.Value == dto.Id)
        {
            throw new AppValidationException("Kho không thể là cha của chính nó.");
        }

        if (await _warehouseRepository.ExistsCodeAsync(dto.WarehouseCode.Trim(), dto.Id, cancellationToken))
        {
            throw new AppValidationException("Mã kho đã tồn tại.");
        }

        if (dto.Id == 0)
        {
            var entity = new Warehouse
            {
                WarehouseCode = dto.WarehouseCode.Trim(),
                WarehouseName = dto.WarehouseName.Trim(),
                Note = dto.Note.Trim(),
                DepartmentName = dto.DepartmentName.Trim(),
                ResponsiblePersonName = dto.ResponsiblePersonName.Trim(),
                ParentWarehouseId = dto.ParentWarehouseId,
                IsConsumptionWarehouse = dto.IsConsumptionWarehouse,
                IsActive = dto.IsActive
            };

            await _warehouseRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("ADD_WAREHOUSE", $"Thêm {entity.WarehouseCode}", cancellationToken: cancellationToken);
            dto.Id = entity.Id;
            return dto;
        }

        var existing = await _warehouseRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy kho.");

        existing.WarehouseCode = dto.WarehouseCode.Trim();
        existing.WarehouseName = dto.WarehouseName.Trim();
        existing.Note = dto.Note.Trim();
        existing.DepartmentName = dto.DepartmentName.Trim();
        existing.ResponsiblePersonName = dto.ResponsiblePersonName.Trim();
        existing.ParentWarehouseId = dto.ParentWarehouseId;
        existing.IsConsumptionWarehouse = dto.IsConsumptionWarehouse;
        existing.IsActive = dto.IsActive;

        _warehouseRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UPDATE_WAREHOUSE", $"Sửa {existing.WarehouseCode}", cancellationToken: cancellationToken);
        return dto;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _warehouseRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy kho.");

        if (await _warehouseRepository.HasChildrenAsync(id, cancellationToken))
        {
            throw new AppValidationException("Kho đang có kho con, không thể xóa.");
        }

        if (await _warehouseRepository.IsUsedInTransactionsAsync(id, cancellationToken))
        {
            throw new AppValidationException("Kho đã phát sinh giao dịch, không thể xóa.");
        }

        _warehouseRepository.Delete(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("DELETE_WAREHOUSE", $"Xóa {existing.WarehouseCode}", cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<LookupItemDto>> GetLookupAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _warehouseRepository.SearchAsync(string.Empty, cancellationToken);
        return rows.Where(x => x.IsActive)
            .OrderBy(x => x.WarehouseCode)
            .Select(x => new LookupItemDto { Id = x.Id, Code = x.WarehouseCode, Name = x.WarehouseName })
            .ToList();
    }
}

