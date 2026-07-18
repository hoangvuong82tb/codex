using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Core.Enums;
using System.Collections.ObjectModel;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class PayableDebtDetailDialog : Window
{
    public PayableDebtDetailDialog(
        string supplierCode,
        string supplierName,
        IReadOnlyList<PayableTransactionDto> transactions,
        DateTime fromDate,
        DateTime toDate)
    {
        InitializeComponent();

        var vm = new PayableDebtDetailDialogViewModel
        {
            SupplierCode = supplierCode,
            SupplierName = supplierName,
            FromDate = fromDate,
            ToDate = toDate
        };

        foreach (var t in transactions.OrderByDescending(x => x.TransactionDate).ThenByDescending(x => x.DocumentNo))
        {
            vm.Transactions.Add(new PayableDebtDetailRow(t));
        }

        DataContext = vm;
    }

    private void PrintPreview_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("Đang mở chế độ xem trước in chi tiết công nợ.", "Xem trước in", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}

public sealed class PayableDebtDetailDialogViewModel
{
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public string SupplierInfoText => $"Nhà cung cấp: {SupplierCode} - {SupplierName}";
    public string PeriodText => $"Từ {FromDate:dd/MM/yyyy} đến {ToDate:dd/MM/yyyy}";
    public string ClosingText => $"Còn phải trả: {Transactions.FirstOrDefault()?.BalanceAmount ?? 0:N0}";

    public ObservableCollection<PayableDebtDetailRow> Transactions { get; } = [];
}

public sealed class PayableDebtDetailRow
{
    public PayableDebtDetailRow(PayableTransactionDto row)
    {
        TransactionDate = row.TransactionDate;
        DocumentNo = row.DocumentNo;
        Description = row.Description;
        CreditAmount = row.CreditAmount;
        DebitAmount = row.DebitAmount;
        BalanceAmount = row.BalanceAmount;
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
        StatusText = row.Status switch
        {
            DebtStatus.Paid => "Đã trả",
            DebtStatus.Overdue => "Quá hạn",
            DebtStatus.PartiallyPaid => "Còn nợ",
            _ => "Mở"
        };
    }

    public DateTime TransactionDate { get; }
    public string DocumentNo { get; }
    public string DocumentTypeText { get; }
    public string Description { get; }
    public decimal CreditAmount { get; }
    public decimal DebitAmount { get; }
    public decimal BalanceAmount { get; }
    public string StatusText { get; }
}
