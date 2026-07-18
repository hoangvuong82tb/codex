using ASMILE.BO.Application.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class ReconciliationDetailDialog : Window
{
    public ObservableCollection<ReconciliationDetailRow> CompanyReconciliationRows { get; } = [];
    public ObservableCollection<ReconciliationDetailRow> CustomerReconciliationRows { get; } = [];

    public ReconciliationDetailDialog(
        ReceivableSummaryDto? summary,
        IEnumerable<ReceivableTransactionDto> transactions,
        DateTime? fromDate,
        DateTime? toDate)
    {
        InitializeComponent();
        DataContext = this;

        BuildMockRows();
    }

    private void BuildMockRows()
    {
        CompanyReconciliationRows.Clear();
        CustomerReconciliationRows.Clear();

        var company = new[]
        {
            new ReconciliationDetailRow("", "", "Số dư đầu kỳ", 0, 0, 100000000),
            new ReconciliationDetailRow("05/04/2026", "INV0001", "Hóa đơn bán hàng", 100000000, 0, 200000000),
            new ReconciliationDetailRow("10/04/2026", "INV0002", "Hóa đơn bán hàng", 120000000, 0, 320000000),
            new ReconciliationDetailRow("15/04/2026", "PT0001", "Phiếu thu", 0, 120000000, 200000000),
            new ReconciliationDetailRow("20/04/2026", "INV0003", "Hóa đơn bán hàng", 130000000, 0, 330000000),
            new ReconciliationDetailRow("25/04/2026", "PT0002", "Phiếu thu", 0, 100000000, 230000000),
            new ReconciliationDetailRow("28/04/2026", "PT0003", "Phiếu thu", 0, 80000000, 150000000),
            new ReconciliationDetailRow("30/04/2026", "ADJ0001", "Điều chỉnh công nợ", 0, 30000000, 120000000),
            new ReconciliationDetailRow("Tổng cộng", "", "", 420000000, 330000000, 120000000, true),
        };

        var customer = new[]
        {
            new ReconciliationDetailRow("", "", "Số dư đầu kỳ", 0, 0, 100000000),
            new ReconciliationDetailRow("05/04/2026", "HD0001", "Hóa đơn", 100000000, 0, 200000000),
            new ReconciliationDetailRow("10/04/2026", "HD0002", "Hóa đơn", 120000000, 0, 320000000),
            new ReconciliationDetailRow("15/04/2026", "PC0001", "Phiếu chi", 0, 120000000, 200000000),
            new ReconciliationDetailRow("20/04/2026", "HD0003", "Hóa đơn", 130000000, 0, 330000000),
            new ReconciliationDetailRow("25/04/2026", "PC0002", "Phiếu chi", 0, 100000000, 230000000),
            new ReconciliationDetailRow("28/04/2026", "PC0003", "Phiếu chi", 0, 80000000, 150000000),
            new ReconciliationDetailRow("30/04/2026", "PC0004", "Phiếu chi", 0, 30000000, 120000000),
            new ReconciliationDetailRow("Tổng cộng", "", "", 420000000, 330000000, 120000000, true),
        };

        foreach (var row in company) CompanyReconciliationRows.Add(row);
        foreach (var row in customer) CustomerReconciliationRows.Add(row);
    }
}

public sealed class ReconciliationDetailRow
{
    private static readonly CultureInfo Vi = CultureInfo.GetCultureInfo("vi-VN");

    public string DateText { get; }
    public string DocumentNo { get; }
    public string Description { get; }
    public decimal Debit { get; }
    public decimal Credit { get; }
    public decimal Balance { get; }
    public bool IsTotal { get; }

    public string DebitText => FormatAmount(Debit);
    public string CreditText => FormatAmount(Credit);
    public string BalanceText => FormatAmount(Balance);

    public ReconciliationDetailRow(string dateText, string documentNo, string description, decimal debit, decimal credit, decimal balance, bool isTotal = false)
    {
        DateText = dateText;
        DocumentNo = documentNo;
        Description = description;
        Debit = debit;
        Credit = credit;
        Balance = balance;
        IsTotal = isTotal;
    }

    private static string FormatAmount(decimal value)
    {
        return value == 0 ? "-" : value.ToString("N0", Vi);
    }
}
