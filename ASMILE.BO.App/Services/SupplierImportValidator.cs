using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Models;
using System.Text.RegularExpressions;

namespace ASMILE.BO.App.Services;

public sealed class SupplierImportValidator : ISupplierImportValidator
{
    private static readonly Regex PhoneRegex = new(@"^[0-9+()\-\s]{8,15}$", RegexOptions.Compiled);

    public IReadOnlyList<SupplierImportValidationIssue> ValidateRow(
        SupplierImportRow row,
        IReadOnlyList<SupplierImportRow> allRows,
        IReadOnlyList<SupplierDto> existingSuppliers)
    {
        var issues = new List<SupplierImportValidationIssue>();

        if (string.IsNullOrWhiteSpace(row.SupplierCode))
        {
            issues.Add(Create("required_code", "Thiếu dữ liệu bắt buộc: Mã NCC.", SupplierImportIssueSeverity.Error));
        }

        if (string.IsNullOrWhiteSpace(row.SupplierName))
        {
            issues.Add(Create("required_name", "Thiếu dữ liệu bắt buộc: Tên nhà cung cấp.", SupplierImportIssueSeverity.Error));
        }

        var normalizedCode = Normalize(row.SupplierCode);
        if (!string.IsNullOrWhiteSpace(normalizedCode))
        {
            if (allRows.Count(x => Normalize(x.SupplierCode) == normalizedCode) > 1)
            {
                issues.Add(Create("dup_file_code", "Mã NCC bị trùng trong file nhập.", SupplierImportIssueSeverity.Error));
            }

            if (existingSuppliers.Any(x => Normalize(x.SupplierCode) == normalizedCode))
            {
                issues.Add(Create("dup_db_code", "Mã NCC đã tồn tại trong hệ thống.", SupplierImportIssueSeverity.Error));
            }
        }

        var normalizedPhone = Normalize(row.Phone);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            issues.Add(Create("missing_phone", "Thiếu số điện thoại (cảnh báo).", SupplierImportIssueSeverity.Warning));
        }
        else
        {
            if (!PhoneRegex.IsMatch(row.Phone.Trim()))
            {
                issues.Add(Create("invalid_phone", "Sai định dạng số điện thoại.", SupplierImportIssueSeverity.Error));
            }

            if (allRows.Count(x => Normalize(x.Phone) == normalizedPhone) > 1)
            {
                issues.Add(Create("dup_file_phone", "Số điện thoại bị trùng trong file nhập.", SupplierImportIssueSeverity.Warning));
            }

            if (existingSuppliers.Any(x => Normalize(x.Phone) == normalizedPhone))
            {
                issues.Add(Create("dup_db_phone", "Số điện thoại đã tồn tại trong hệ thống.", SupplierImportIssueSeverity.Warning));
            }
        }

        if (!TryParseActive(row.IsActiveRaw, out var isActive))
        {
            issues.Add(Create("invalid_active", "Sai định dạng Kích hoạt (hỗ trợ: true/false, 1/0, yes/no, có/không).", SupplierImportIssueSeverity.Error));
            row.IsActive = true;
        }
        else
        {
            row.IsActive = isActive;
        }

        return issues;
    }

    public void ValidateRows(
        IReadOnlyList<SupplierImportRow> rows,
        IReadOnlyList<SupplierDto> existingSuppliers,
        IDictionary<int, IReadOnlyList<SupplierImportValidationIssue>> target)
    {
        target.Clear();
        foreach (var row in rows)
        {
            target[row.RowNumber] = ValidateRow(row, rows, existingSuppliers);
        }
    }

    private static SupplierImportValidationIssue Create(string code, string message, SupplierImportIssueSeverity severity)
    {
        return new SupplierImportValidationIssue { Code = code, Message = message, Severity = severity };
    }

    private static string Normalize(string value)
    {
        return (value ?? string.Empty).Trim().ToUpperInvariant();
    }

    private static bool TryParseActive(string value, out bool isActive)
    {
        var normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            isActive = true;
            return true;
        }

        if (normalized is "1" or "true" or "yes" or "y" or "co" or "có" or "active" or "on")
        {
            isActive = true;
            return true;
        }

        if (normalized is "0" or "false" or "no" or "n" or "khong" or "không" or "inactive" or "off")
        {
            isActive = false;
            return true;
        }

        isActive = false;
        return false;
    }
}
