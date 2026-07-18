using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Services;
using ASMILE.BO.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class PayableDebtDetailModuleViewModel : ObservableObject
{
    private readonly IWindowManagerService _windowManagerService;
    private readonly string _moduleKey;
    private readonly Action<PayableDebtPaymentSelection>? _onApplyPayment;
    private readonly IReadOnlyList<PayableTransactionDto> _allTransactions;
    private readonly bool _includePaymentRows;

    public PayableDebtDetailModuleViewModel(
        string supplierCode,
        string supplierName,
        IReadOnlyList<PayableTransactionDto> transactions,
        DateTime fromDate,
        DateTime toDate,
        IWindowManagerService windowManagerService,
        string moduleKey,
        Action<PayableDebtPaymentSelection>? onApplyPayment = null,
        bool includePaymentRows = false)
    {
        SupplierCode = supplierCode;
        SupplierName = supplierName;
        FromDate = fromDate;
        ToDate = toDate;
        _windowManagerService = windowManagerService;
        _moduleKey = moduleKey;
        _onApplyPayment = onApplyPayment;
        _includePaymentRows = includePaymentRows;
        _allTransactions = transactions
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.DocumentNo)
            .ToList();
        BuildDisplayRows();
    }

    public event Action? RequestClose;

    public string SupplierCode { get; }
    public string SupplierName { get; }
    public DateTime FromDate { get; }
    public DateTime ToDate { get; }
    public ObservableCollection<PayableDebtDetailModuleRow> Transactions { get; } = new();

    public string HeaderSupplier => $"Nhà cung cấp: {SupplierCode} - {SupplierName}";
    public string HeaderPeriod => $"Kỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}";
    public decimal TotalPayable => _allTransactions
        .Where(x => x.DocumentType is PayableDocumentType.ImportVoucher or PayableDocumentType.PurchaseInvoice or PayableDocumentType.OpeningBalance)
        .Sum(x => x.CreditAmount);
    public decimal TotalPaid => _allTransactions
        .Where(x => x.DocumentType is PayableDocumentType.PaymentVoucher or PayableDocumentType.AdvancePayment)
        .Sum(x => x.DebitAmount);
    public decimal EndingBalance => Math.Max(0, TotalPayable - TotalPaid);
    public bool HasPaymentAction => _onApplyPayment is not null;
    public decimal SelectedPaymentTotal => Transactions.Where(x => x.IsSelected).Sum(x => x.LineRemaining);
    public int SelectedCount => Transactions.Count(x => x.IsSelected);

    [RelayCommand]
    private void Close()
    {
        RequestClose?.Invoke();
        var opened = _windowManagerService.OpenWindows.FirstOrDefault(x => x.Module.Key == _moduleKey);
        if (opened is not null)
        {
            _windowManagerService.Close(opened);
        }
    }

    [RelayCommand(CanExecute = nameof(CanApplyPayment))]
    private void ApplyPayment()
    {
        var selectedRows = Transactions.Where(x => x.IsSelected).ToList();
        if (selectedRows.Count == 0)
        {
            return;
        }

        _onApplyPayment?.Invoke(new PayableDebtPaymentSelection(
            SupplierCode,
            SupplierName,
            selectedRows.Select(x => x.DocumentNo).ToArray(),
            selectedRows.Sum(x => x.LineRemaining)));

        Close();
    }

    private bool CanApplyPayment() => HasPaymentAction && SelectedCount > 0 && SelectedPaymentTotal > 0;

    [RelayCommand]
    private void ToggleSelectAll()
    {
        var selectable = Transactions.Where(x => x.LineRemaining > 0).ToList();
        var shouldSelect = selectable.Any(x => !x.IsSelected);
        foreach (var row in selectable)
        {
            row.IsSelected = shouldSelect;
        }
        OnPropertyChanged(nameof(SelectedPaymentTotal));
        OnPropertyChanged(nameof(SelectedCount));
        ApplyPaymentCommand.NotifyCanExecuteChanged();
    }

    private void BuildDisplayRows()
    {
        Transactions.Clear();

        if (_includePaymentRows)
        {
            BuildRunningStatementRows();
            return;
        }

        // Chỉ hiển thị chứng từ phát sinh công nợ phải trả khi dùng màn hình này để chọn phiếu cần thanh toán.
        var payableDocs = _allTransactions
            .Where(x => x.DocumentType is PayableDocumentType.ImportVoucher or PayableDocumentType.PurchaseInvoice or PayableDocumentType.OpeningBalance)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.DocumentNo)
            .ToList();

        var outstandingByDoc = payableDocs.ToDictionary(
            x => x.DocumentNo,
            x => Math.Max(0, x.CreditAmount),
            StringComparer.OrdinalIgnoreCase);
        var paidByDoc = payableDocs.ToDictionary(
            x => x.DocumentNo,
            _ => 0m,
            StringComparer.OrdinalIgnoreCase);

        // Nghiệp vụ chuẩn:
        // 1) Cấn trừ đúng các phiếu được chỉ định trong ghi chú phiếu chi.
        // 2) Nếu còn dư mới cấn trừ tiếp các phiếu chưa thanh toán (FIFO).
        var paymentRows = _allTransactions
            .Where(x => x.DocumentType is PayableDocumentType.PaymentVoucher or PayableDocumentType.AdvancePayment)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.DocumentNo)
            .ToList();

        foreach (var payment in paymentRows)
        {
            var remaining = Math.Max(0, payment.DebitAmount);
            if (remaining <= 0)
            {
                continue;
            }

            var referencedDocNos = ExtractReferencedDocumentNos(payment.Description);
            foreach (var refDocNo in referencedDocNos)
            {
                if (remaining <= 0)
                {
                    break;
                }

                if (!outstandingByDoc.TryGetValue(refDocNo, out var outstanding) || outstanding <= 0)
                {
                    continue;
                }

                var alloc = Math.Min(outstanding, remaining);
                outstandingByDoc[refDocNo] = outstanding - alloc;
                paidByDoc[refDocNo] += alloc;
                remaining -= alloc;
            }

            if (remaining <= 0)
            {
                continue;
            }

            foreach (var doc in payableDocs)
            {
                if (remaining <= 0)
                {
                    break;
                }

                var outstanding = outstandingByDoc[doc.DocumentNo];
                if (outstanding <= 0)
                {
                    continue;
                }

                var alloc = Math.Min(outstanding, remaining);
                outstandingByDoc[doc.DocumentNo] = outstanding - alloc;
                paidByDoc[doc.DocumentNo] += alloc;
                remaining -= alloc;
            }
        }

        foreach (var doc in payableDocs)
        {
            var linePayable = Math.Max(0, doc.CreditAmount);
            var linePaid = Math.Min(linePayable, paidByDoc[doc.DocumentNo]);
            var lineRemaining = Math.Max(0, linePayable - linePaid);

            var row = new PayableDebtDetailModuleRow(doc, linePaid, lineRemaining);
            row.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PayableDebtDetailModuleRow.IsSelected))
                {
                    OnPropertyChanged(nameof(SelectedPaymentTotal));
                    OnPropertyChanged(nameof(SelectedCount));
                    ApplyPaymentCommand.NotifyCanExecuteChanged();
                }
            };
            Transactions.Add(row);
        }

        var ordered = Transactions
            .OrderByDescending(x => x.CanSelect)
            .ThenByDescending(x => x.LineRemaining)
            .ThenByDescending(x => x.TransactionDate)
            .ThenByDescending(x => x.DocumentNo)
            .ToList();
        Transactions.Clear();
        foreach (var row in ordered)
        {
            Transactions.Add(row);
        }
    }

    private void BuildRunningStatementRows()
    {
        var runningBalance = 0m;
        foreach (var tx in _allTransactions.OrderBy(x => x.TransactionDate).ThenBy(x => x.DocumentNo))
        {
            runningBalance = Math.Max(0, runningBalance + tx.CreditAmount - tx.DebitAmount);
            Transactions.Add(new PayableDebtDetailModuleRow(tx, Math.Max(0, tx.DebitAmount), runningBalance, includePaymentRow: true));
        }
    }

    private static List<string> ExtractReferencedDocumentNos(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return new List<string>();
        }

        var matches = Regex.Matches(description, @"\b[A-Z]{2,6}-[A-Z0-9]+\b", RegexOptions.IgnoreCase);
        var result = new List<string>(matches.Count);
        foreach (Match match in matches)
        {
            if (!match.Success)
            {
                continue;
            }

            var docNo = match.Value.Trim();
            if (docNo.Length == 0 || result.Any(x => string.Equals(x, docNo, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            result.Add(docNo);
        }

        return result;
    }
}

public sealed partial class PayableDebtDetailModuleRow : ObservableObject
{
    public PayableDebtDetailModuleRow(PayableTransactionDto row, decimal paidAmount, decimal lineRemaining, bool includePaymentRow = false)
    {
        TransactionDate = row.TransactionDate;
        DocumentNo = row.DocumentNo;
        Description = row.Description;
        PaidAmount = paidAmount;
        PayableAmount = Math.Max(0, row.CreditAmount);
        BalanceAmount = row.BalanceAmount;
        LineRemaining = Math.Max(0, lineRemaining);
        IsPaymentRow = includePaymentRow && row.DocumentType is PayableDocumentType.PaymentVoucher or PayableDocumentType.AdvancePayment;
        DocumentTypeText = row.DocumentType switch
        {
            PayableDocumentType.ImportVoucher => "Phiếu nhập",
            PayableDocumentType.PurchaseInvoice => "Hóa đơn mua",
            PayableDocumentType.PaymentVoucher => "Phiếu chi",
            PayableDocumentType.AdvancePayment => "Tạm ứng",
            PayableDocumentType.Adjustment => "Điều chỉnh",
            PayableDocumentType.OpeningBalance => "Số dư đầu kỳ",
            _ => row.DocumentType.ToString()
        };
        StatusText = IsPaymentRow ? "Đã trả" : LineRemaining <= 0 ? "Đã trả" : "Còn nợ";
    }

    public DateTime TransactionDate { get; }
    public string DocumentNo { get; }
    public string DocumentTypeText { get; }
    public string Description { get; }
    public decimal PayableAmount { get; }
    public decimal PaidAmount { get; }
    public decimal BalanceAmount { get; }
    public decimal LineRemaining { get; }
    public string StatusText { get; }
    public bool IsPaymentRow { get; }
    public bool CanSelect => !IsPaymentRow && PayableAmount > 0 && LineRemaining > 0;
    [ObservableProperty] private bool isSelected;

    partial void OnIsSelectedChanged(bool value)
    {
        if (!CanSelect && value)
        {
            IsSelected = false;
        }
    }
}

public sealed record PayableDebtPaymentSelection(
    string SupplierCode,
    string SupplierName,
    IReadOnlyList<string> DocumentNos,
    decimal TotalAmount);
