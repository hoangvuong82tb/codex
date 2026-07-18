using System.Globalization;
using System.Text.Json;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Entities;
using ASMILE.BO.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ASMILE.BO.Infrastructure.Services;

public sealed class BankStatementImportTemplateHandler : IImportTemplateHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] DateColumns = ["Date", "TransactionDate", "Ngày", "Ngay", "Ngày giao dịch"];
    private static readonly string[] ValueDateColumns = ["ValueDate", "Ngày hiệu lực", "Ngay hieu luc"];
    private static readonly string[] ReferenceColumns = ["ReferenceNo", "Reference", "Số tham chiếu", "So tham chieu", "Mã giao dịch", "Ma giao dich"];
    private static readonly string[] DescriptionColumns = ["Description", "Nội dung", "Noi dung", "Diễn giải", "Dien giai"];
    private static readonly string[] DebitColumns = ["Debit", "DebitAmount", "Thu", "Phát sinh có", "Phat sinh co"];
    private static readonly string[] CreditColumns = ["Credit", "CreditAmount", "Chi", "Phát sinh nợ", "Phat sinh no"];
    private static readonly string[] BalanceColumns = ["Balance", "Số dư", "So du"];
    private static readonly string[] AccountColumns = ["BankAccountId", "AccountId", "Tài khoản", "Tai khoan"];

    private readonly AppDbContext _dbContext;

    public BankStatementImportTemplateHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public string TargetEntity => "BankStatement";
    public IReadOnlyList<string> RequiredColumns { get; } = [];
    public IReadOnlyList<string> KeyColumns { get; } = ["Date", "ReferenceNo", "Debit", "Credit"];

    public async Task ValidateRowAsync(ImportRow row, IReadOnlyDictionary<string, string> values, IReadOnlyList<ImportRow> allRows, CancellationToken cancellationToken = default)
    {
        var dateText = GetValue(values, DateColumns);
        var valueDateText = GetValue(values, ValueDateColumns);
        var referenceNo = GetValue(values, ReferenceColumns);
        var description = GetValue(values, DescriptionColumns);
        var debit = ParseDecimal(GetValue(values, DebitColumns));
        var credit = ParseDecimal(GetValue(values, CreditColumns));
        var balance = ParseDecimal(GetValue(values, BalanceColumns));
        var accountIdText = GetValue(values, AccountColumns);

        if (!TryParseDate(dateText, out var transactionDate))
        {
            AddError(row, "Date", "INVALID_DATE", "Ngày giao dịch không hợp lệ.");
        }

        var valueDate = TryParseDate(valueDateText, out var parsedValueDate) ? parsedValueDate : transactionDate;
        if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(referenceNo))
        {
            AddError(row, "Description", "DESCRIPTION_REQUIRED", "Cần nội dung hoặc số tham chiếu để đối chiếu.");
        }

        if (debit < 0 || credit < 0)
        {
            AddError(row, "Amount", "NEGATIVE_AMOUNT", "Số tiền sao kê không được âm.");
        }

        if (debit == 0 && credit == 0)
        {
            AddError(row, "Amount", "AMOUNT_REQUIRED", "Dòng sao kê cần phát sinh thu hoặc chi.");
        }

        var duplicateInFile = allRows
            .Where(x => x.Id != row.Id)
            .Select(x => ParseValues(x.RawJson))
            .Count(x => string.Equals(GetValue(x, DateColumns), dateText, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(GetValue(x, ReferenceColumns), referenceNo, StringComparison.OrdinalIgnoreCase)
                        && ParseDecimal(GetValue(x, DebitColumns)) == debit
                        && ParseDecimal(GetValue(x, CreditColumns)) == credit);
        if (duplicateInFile > 0)
        {
            AddWarning(row, "ReferenceNo", "DUPLICATE_IN_FILE", "Dòng sao kê có khả năng bị trùng trong file.");
        }

        if (!string.IsNullOrWhiteSpace(referenceNo))
        {
            var duplicateInDb = await _dbContext.BankStatementLines.AsNoTracking()
                .AnyAsync(x => x.ReferenceNo == referenceNo && x.TransactionDate == transactionDate && x.DebitAmount == debit && x.CreditAmount == credit, cancellationToken);
            if (duplicateInDb)
            {
                AddWarning(row, "ReferenceNo", "DUPLICATE_IN_DATABASE", "Giao dịch sao kê này có thể đã được import trước đó.");
            }
        }

        row.NormalizedJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["BankAccountId"] = accountIdText,
            ["TransactionDate"] = transactionDate.ToString("O", CultureInfo.InvariantCulture),
            ["ValueDate"] = valueDate.ToString("O", CultureInfo.InvariantCulture),
            ["ReferenceNo"] = referenceNo,
            ["Description"] = description,
            ["DebitAmount"] = debit.ToString(CultureInfo.InvariantCulture),
            ["CreditAmount"] = credit.ToString(CultureInfo.InvariantCulture),
            ["Balance"] = balance.ToString(CultureInfo.InvariantCulture)
        }, JsonOptions);
    }

    public async Task<int> CommitAsync(ImportSession session, IReadOnlyList<ImportRow> rows, CancellationToken cancellationToken = default)
    {
        var validRows = rows.Where(x => x.Status is "Valid" or "Warning").Select(x => ParseValues(x.NormalizedJson ?? x.RawJson)).ToList();
        if (validRows.Count == 0)
        {
            return 0;
        }

        var accountId = validRows.Select(x => x.GetValueOrDefault("BankAccountId", string.Empty)).FirstOrDefault(x => Guid.TryParse(x, out _));
        if (!Guid.TryParse(accountId, out var bankAccountId))
        {
            throw new InvalidOperationException("BankAccountId là bắt buộc khi commit sao kê ngân hàng.");
        }

        var account = await _dbContext.BankAccounts.FirstOrDefaultAsync(x => x.Id == bankAccountId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy tài khoản ngân hàng để commit sao kê.");

        var lines = validRows
            .Select(x => new BankStatementLine
            {
                Id = Guid.NewGuid(),
                TransactionDate = DateTime.Parse(x.GetValueOrDefault("TransactionDate", string.Empty), null, DateTimeStyles.RoundtripKind).Date,
                ValueDate = DateTime.Parse(x.GetValueOrDefault("ValueDate", string.Empty), null, DateTimeStyles.RoundtripKind).Date,
                ReferenceNo = NormalizeOptional(x.GetValueOrDefault("ReferenceNo", string.Empty)),
                Description = NormalizeOptional(x.GetValueOrDefault("Description", string.Empty)),
                DebitAmount = ParseDecimal(x.GetValueOrDefault("DebitAmount", string.Empty)),
                CreditAmount = ParseDecimal(x.GetValueOrDefault("CreditAmount", string.Empty)),
                Balance = ParseDecimal(x.GetValueOrDefault("Balance", string.Empty)),
                MatchStatus = "Unmatched"
            })
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.ReferenceNo)
            .ToList();

        if (lines.Count == 0)
        {
            return 0;
        }

        var statement = new BankStatement
        {
            Id = Guid.NewGuid(),
            BankAccountId = account.Id,
            StatementNo = $"SKNH-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Random.Shared.Next(0, 9999):D4}",
            FromDate = lines.First().TransactionDate,
            ToDate = lines.Last().TransactionDate,
            OpeningBalance = lines.First().Balance - lines.First().DebitAmount + lines.First().CreditAmount,
            ClosingBalance = lines.Last().Balance,
            ImportedAt = DateTime.UtcNow,
            ImportedBy = session.CreatedBy,
            Status = "Imported",
            Lines = lines
        };

        _dbContext.BankStatements.Add(statement);
        foreach (var row in rows.Where(x => x.Status is "Valid" or "Warning"))
        {
            row.Status = "Committed";
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return lines.Count;
    }

    private static Dictionary<string, string> ParseValues(string json)
        => string.IsNullOrWhiteSpace(json)
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private static string GetValue(IReadOnlyDictionary<string, string> values, IEnumerable<string> aliases)
    {
        foreach (var alias in aliases)
        {
            var match = values.FirstOrDefault(x => string.Equals(x.Key, alias, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match.Key))
            {
                return match.Value?.Trim() ?? string.Empty;
            }
        }

        return string.Empty;
    }

    private static bool TryParseDate(string? value, out DateTime date)
        => DateTime.TryParse(value, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out date)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0m;
        }

        var normalized = value.Trim().Replace(" ", string.Empty);
        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.GetCultureInfo("vi-VN"), out var vi)
            ? vi
            : decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var inv) ? inv : 0m;
    }

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddError(ImportRow row, string columnName, string errorCode, string message)
        => AddCellIssue(row, columnName, "Error", errorCode, message);

    private static void AddWarning(ImportRow row, string columnName, string errorCode, string message)
        => AddCellIssue(row, columnName, "Warning", errorCode, message);

    private static void AddCellIssue(ImportRow row, string columnName, string severity, string errorCode, string message)
    {
        row.CellErrors.Add(new ImportCellError
        {
            Id = Guid.NewGuid(),
            ColumnName = columnName,
            CellAddress = string.Empty,
            Severity = severity,
            ErrorCode = errorCode,
            Message = message
        });
    }
}
