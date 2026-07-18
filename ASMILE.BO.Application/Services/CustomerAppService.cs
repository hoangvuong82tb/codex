using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Core.Interfaces.Repositories;
using ASMILE.BO.Core.Interfaces.Services;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class CustomerAppService : ICustomerAppService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditLogService _auditLogService;

    public CustomerAppService(ICustomerRepository customerRepository, IUnitOfWork unitOfWork, IAuditLogService auditLogService)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<CustomerDto>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        var rows = await _customerRepository.SearchAsync(keyword ?? string.Empty, cancellationToken);
        return rows.Select(MapToDto).ToList();
    }

    public async Task<CustomerDto> SaveAsync(CustomerDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.CustomerCode)) throw new AppValidationException("Mã khách hàng không được rỗng.");
        if (string.IsNullOrWhiteSpace(dto.CustomerName)) throw new AppValidationException("Tên khách hàng không được rỗng.");

        if (await _customerRepository.ExistsCodeAsync(dto.CustomerCode.Trim(), dto.Id, cancellationToken))
        {
            throw new AppValidationException("Mã khách hàng đã tồn tại.");
        }

        if (dto.Id == 0)
        {
            var entity = MapToEntity(dto);
            await _customerRepository.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _auditLogService.LogAsync("ADD_CUSTOMER", $"Thêm {entity.CustomerCode}", cancellationToken: cancellationToken);
            return MapToDto(entity);
        }

        var existing = await _customerRepository.GetByIdAsync(dto.Id, cancellationToken)
            ?? throw new AppValidationException("Không tìm thấy khách hàng.");

        existing.CustomerCode = dto.CustomerCode.Trim();
        existing.CustomerName = dto.CustomerName.Trim();
        existing.Phone = dto.Phone.Trim();
        existing.BankAccountNo = dto.BankAccountNo.Trim();
        existing.Address = dto.Address.Trim();
        existing.Note = dto.Note.Trim();
        existing.IsActive = dto.IsActive;

        _customerRepository.Update(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _auditLogService.LogAsync("UPDATE_CUSTOMER", $"Sửa {existing.CustomerCode}", cancellationToken: cancellationToken);
        return MapToDto(existing);
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

    private static CustomerDto MapToDto(Customer entity) => new()
    {
        Id = entity.Id,
        CustomerCode = entity.CustomerCode,
        CustomerName = entity.CustomerName,
        Phone = entity.Phone,
        BankAccountNo = entity.BankAccountNo,
        Address = entity.Address,
        Note = entity.Note,
        IsActive = entity.IsActive
    };

    private static Customer MapToEntity(CustomerDto dto) => new()
    {
        Id = dto.Id,
        CustomerCode = dto.CustomerCode.Trim(),
        CustomerName = dto.CustomerName.Trim(),
        Phone = dto.Phone.Trim(),
        BankAccountNo = dto.BankAccountNo.Trim(),
        Address = dto.Address.Trim(),
        Note = dto.Note.Trim(),
        IsActive = dto.IsActive
    };
}
