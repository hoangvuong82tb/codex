using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class SupplierAppService : ISupplierAppService
{
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public SupplierAppService(ISupplierRepository supplierRepository, IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<SupplierDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var rows = await _supplierRepository.SearchAsync(keyword ?? string.Empty, cancellationToken);
        return rows.Select(MapToDto).ToList();
    }

    public async Task<SupplierDto> SaveAsync(SupplierDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.SupplierCode)) throw new AppValidationException("Mã nhà cung cấp không được rỗng.");
        if (string.IsNullOrWhiteSpace(dto.SupplierName)) throw new AppValidationException("Tên nhà cung cấp không được rỗng.");

        if (await _supplierRepository.ExistsCodeAsync(dto.SupplierCode.Trim(), dto.Id, cancellationToken))
        {
            throw new AppValidationException("Mã nhà cung cấp đã tồn tại.");
        }

        if (dto.Id == 0)
        {
            var entity = MapToEntity(dto);
            await _supplierRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("ADD_SUPPLIER", $"Thêm {entity.SupplierCode}", cancellationToken: cancellationToken);
            return MapToDto(entity);
        }

        var existing = await _supplierRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy nhà cung cấp.");

        existing.SupplierCode = dto.SupplierCode.Trim();
        existing.SupplierName = dto.SupplierName.Trim();
        existing.Phone = dto.Phone.Trim();
        existing.BankAccountNo = dto.BankAccountNo.Trim();
        existing.Address = dto.Address.Trim();
        existing.Note = dto.Note.Trim();
        existing.IsActive = dto.IsActive;

        _supplierRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UPDATE_SUPPLIER", $"Sửa {existing.SupplierCode}", cancellationToken: cancellationToken);
        return MapToDto(existing);
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

    private static SupplierDto MapToDto(Supplier entity) => new()
    {
        Id = entity.Id,
        SupplierCode = entity.SupplierCode,
        SupplierName = entity.SupplierName,
        Phone = entity.Phone,
        BankAccountNo = entity.BankAccountNo,
        Address = entity.Address,
        Note = entity.Note,
        IsActive = entity.IsActive
    };

    private static Supplier MapToEntity(SupplierDto dto) => new()
    {
        Id = dto.Id,
        SupplierCode = dto.SupplierCode.Trim(),
        SupplierName = dto.SupplierName.Trim(),
        Phone = dto.Phone.Trim(),
        BankAccountNo = dto.BankAccountNo.Trim(),
        Address = dto.Address.Trim(),
        Note = dto.Note.Trim(),
        IsActive = dto.IsActive
    };
}

