using ASMILE.BO.Application.DTOs;
using ASMILE.BO.App.Services;
using ASMILE.BO.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class SupplierQuickCheckModuleViewModel : ObservableObject
{
    private readonly IWindowManagerService _windowManagerService;
    private readonly string _moduleKey;

    public SupplierQuickCheckModuleViewModel(
        string title,
        string supplierCode,
        string supplierName,
        DateTime fromDate,
        DateTime toDate,
        IReadOnlyList<PayableTransactionDto> transactions,
        SupplierQuickCheckKind kind,
        IWindowManagerService windowManagerService,
        string moduleKey)
    {
        Title = title;
        SupplierCode = supplierCode;
        SupplierName = supplierName;
        FromDate = fromDate;
        ToDate = toDate;
        Kind = kind;
        _windowManagerService = windowManagerService;
        _moduleKey = moduleKey;

        var filtered = transactions
            .Where(x => kind == SupplierQuickCheckKind.Import
                ? x.DocumentType is PayableDocumentType.ImportVoucher or PayableDocumentType.PurchaseInvoice or PayableDocumentType.OpeningBalance
                : x.DocumentType is PayableDocumentType.PaymentVoucher or PayableDocumentType.AdvancePayment)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.DocumentNo)
            .ToList();

        foreach (var row in filtered.Select((x, idx) => new SupplierQuickCheckRow(idx + 1, x)))
        {
            Rows.Add(row);
        }
    }

    public event Action? RequestClose;

    public string Title { get; }
    public string SupplierCode { get; }
    public string SupplierName { get; }
    public DateTime FromDate { get; }
    public DateTime ToDate { get; }
    public SupplierQuickCheckKind Kind { get; }
    public ObservableCollection<SupplierQuickCheckRow> Rows { get; } = new();

    public string HeaderSupplier => $"Nhà cung cấp: {SupplierCode} - {SupplierName}";
    public string HeaderPeriod => $"Kỳ: {FromDate:dd/MM/yyyy} - {ToDate:dd/MM/yyyy}";
    public decimal TotalPayable => Rows.Sum(x => x.PayableAmount);
    public decimal TotalPaid => Rows.Sum(x => x.PaidAmount);
    public decimal TotalAmount => Kind == SupplierQuickCheckKind.Import ? TotalPayable : TotalPaid;
    public string TotalLabel => Kind == SupplierQuickCheckKind.Import ? "Tổng phiếu nhập" : "Tổng thanh toán/tạm ứng";

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
}

public sealed class SupplierQuickCheckRow
{
    public SupplierQuickCheckRow(int lineNo, PayableTransactionDto row)
    {
        LineNo = lineNo;
        TransactionDate = row.TransactionDate;
        DocumentNo = row.DocumentNo;
        Description = row.Description;
        PayableAmount = Math.Max(0, row.CreditAmount);
        PaidAmount = Math.Max(0, row.DebitAmount);
        BalanceAmount = row.BalanceAmount;
        DocumentTypeText = row.DocumentType switch
        {
            PayableDocumentType.ImportVoucher => "Phiếu nhập",
            PayableDocumentType.PurchaseInvoice => "Hóa đơn mua",
            PayableDocumentType.PaymentVoucher => "Phiếu chi",
            PayableDocumentType.AdvancePayment => "Tạm ứng",
            PayableDocumentType.OpeningBalance => "Số dư đầu kỳ",
            _ => row.DocumentType.ToString()
        };
    }

    public int LineNo { get; }
    public DateTime TransactionDate { get; }
    public string DocumentNo { get; }
    public string DocumentTypeText { get; }
    public string Description { get; }
    public decimal PayableAmount { get; }
    public decimal PaidAmount { get; }
    public decimal BalanceAmount { get; }
}

public enum SupplierQuickCheckKind
{
    Import,
    Payment
}
