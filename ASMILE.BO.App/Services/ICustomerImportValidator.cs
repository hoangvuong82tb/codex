using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface ICustomerImportValidator
{
    IReadOnlyList<SupplierImportValidationIssue> ValidateRow(
        SupplierImportRow row,
        IReadOnlyList<SupplierImportRow> allRows,
        IReadOnlyList<SupplierDto> existingCustomers);

    void ValidateRows(
        IReadOnlyList<SupplierImportRow> rows,
        IReadOnlyList<SupplierDto> existingCustomers,
        IDictionary<int, IReadOnlyList<SupplierImportValidationIssue>> target);
}

