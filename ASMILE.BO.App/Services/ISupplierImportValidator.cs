using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;

namespace ASMILE.BO.App.Services;

public interface ISupplierImportValidator
{
    IReadOnlyList<SupplierImportValidationIssue> ValidateRow(
        SupplierImportRow row,
        IReadOnlyList<SupplierImportRow> allRows,
        IReadOnlyList<SupplierDto> existingSuppliers);

    void ValidateRows(
        IReadOnlyList<SupplierImportRow> rows,
        IReadOnlyList<SupplierDto> existingSuppliers,
        IDictionary<int, IReadOnlyList<SupplierImportValidationIssue>> target);
}

