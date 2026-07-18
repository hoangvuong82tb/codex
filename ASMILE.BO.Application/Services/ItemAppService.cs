using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class ItemAppService : IItemAppService
{
    private readonly IItemRepository _itemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public ItemAppService(IItemRepository itemRepository, IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _itemRepository = itemRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<ItemDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var rows = await _itemRepository.SearchAsync(keyword ?? string.Empty, cancellationToken);
        return rows.Select(MapToDto).ToList();
    }

    public async Task<ItemDto> SaveAsync(ItemDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.ItemCode)) throw new AppValidationException("Mã hàng không được rỗng.");
        if (string.IsNullOrWhiteSpace(dto.ItemName)) throw new AppValidationException("Tên hàng không được rỗng.");
        if (string.IsNullOrWhiteSpace(dto.Unit)) throw new AppValidationException("Đơn vị tính không được rỗng.");

        if (await _itemRepository.ExistsCodeAsync(dto.ItemCode.Trim(), dto.Id, cancellationToken))
        {
            throw new AppValidationException("Mã hàng đã tồn tại.");
        }

        if (await _itemRepository.ExistsNameAsync(dto.ItemName.Trim(), dto.Id, cancellationToken))
        {
            throw new AppValidationException("Tên hàng đã tồn tại.");
        }

        if (dto.Id == 0)
        {
            var entity = MapToEntity(dto);
            await _itemRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("ADD_ITEM", $"Thêm {entity.ItemCode}", cancellationToken: cancellationToken);
            return MapToDto(entity);
        }

        var existing = await _itemRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy hàng hóa.");

        existing.ItemCode = dto.ItemCode.Trim();
        existing.ItemName = dto.ItemName.Trim();
        existing.Unit = dto.Unit.Trim();
        existing.GroupName = dto.GroupName.Trim();
        existing.SupplierCode = dto.SupplierCode.Trim();
        existing.SupplierName = dto.SupplierName.Trim();
        existing.IsActive = dto.IsActive;

        _itemRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UPDATE_ITEM", $"Sửa {existing.ItemCode}", cancellationToken: cancellationToken);
        return MapToDto(existing);
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
        IsActive = entity.IsActive
    };

    private static Item MapToEntity(ItemDto dto) => new()
    {
        Id = dto.Id,
        ItemCode = dto.ItemCode.Trim(),
        ItemName = dto.ItemName.Trim(),
        Unit = dto.Unit.Trim(),
        GroupName = dto.GroupName.Trim(),
        SupplierCode = dto.SupplierCode.Trim(),
        SupplierName = dto.SupplierName.Trim(),
        IsActive = dto.IsActive
    };
}

