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

        BuildRows(summary, transactions);
    }

    private void BuildRows(ReceivableSummaryDto? summary, IEnumerable<ReceivableTransactionDto> transactions)
    {
        CompanyReconciliationRows.Clear();
        CustomerReconciliationRows.Clear();

        if (summary is null) return;

        var rows = new List<ReconciliationDetailRow>
        {
            new("", "", "Số dư đầu kỳ", 0, 0, summary.OpeningBalance)
        };

        foreach (var transaction in transactions.OrderBy(x => x.TransactionDate).ThenBy(x => x.DocumentNo))
        {
            rows.Add(new ReconciliationDetailRow(
                transaction.TransactionDate.ToString("dd/MM/yyyy"),
                transaction.DocumentNo,
                string.IsNullOrWhiteSpace(transaction.Description) ? transaction.DocumentType.ToString() : transaction.Description,
                transaction.DebitAmount,
                transaction.CreditAmount,
                transaction.BalanceAmount));
        }

        rows.Add(new ReconciliationDetailRow(
            "Tổng cộng", "", "",
            summary.TotalDebit,
            summary.TotalCredit,
            summary.ClosingBalance,
            true));

        foreach (var row in rows)
        {
            CompanyReconciliationRows.Add(row);
            CustomerReconciliationRows.Add(row);
        }
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
