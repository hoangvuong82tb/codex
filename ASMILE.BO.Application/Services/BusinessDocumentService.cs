using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Shared.Exceptions;

namespace ASMILE.BO.Application.Services;

public sealed class BusinessDocumentService : IBusinessDocumentService
{
    private readonly IBusinessDocumentRepository _repository;

    public BusinessDocumentService(IBusinessDocumentRepository repository)
    {
        _repository = repository;
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsBySupplierAsync(Guid supplierId)
    {
        return _repository.GetBySupplierAsync(supplierId);
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByCustomerAsync(Guid customerId)
    {
        return _repository.GetByCustomerAsync(customerId);
    }

    public Task<IReadOnlyList<BusinessDocumentDto>> GetDocumentsByDateAsync(DateTime date)
    {
        return _repository.GetByDateAsync(date);
    }

    public Task<BusinessDocumentDto?> GetDocumentAsync(Guid documentId)
    {
        return _repository.GetByIdAsync(documentId);
    }

    public async Task PrintAsync(Guid documentId)
    {
        var document = await _repository.GetByIdAsync(documentId);
        if (document is null)
        {
            throw new AppValidationException("Không tìm thấy chứng từ để in.");
        }

        // Placeholder cho pipeline in ấn thực tế.
        await Task.CompletedTask;
    }

    public async Task ExportPdfAsync(Guid documentId)
    {
        var document = await _repository.GetByIdAsync(documentId);
        if (document is null)
        {
            throw new AppValidationException("Không tìm thấy chứng từ để xuất PDF.");
        }

        // Placeholder cho pipeline xuất PDF thực tế.
        await Task.CompletedTask;
    }
}
