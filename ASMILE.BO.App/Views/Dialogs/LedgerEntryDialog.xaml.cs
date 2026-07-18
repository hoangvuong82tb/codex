using ASMILE.BO.Application.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class LedgerEntryDialog : Window
{
    public DateTime? EntryDate { get; set; } = DateTime.Today;
    public string DocumentNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ObservableCollection<string> AccountCodes { get; }
    public string DebitAccount { get; set; } = string.Empty;
    public string CreditAccount { get; set; } = string.Empty;
    public string AmountText { get; set; } = string.Empty;

    public CreateLedgerEntryRequest? Result { get; private set; }

    public LedgerEntryDialog(IEnumerable<LedgerAccountDto> accounts)
    {
        AccountCodes = new ObservableCollection<string>(accounts.Select(x => x.AccountCode));
        DebitAccount = AccountCodes.FirstOrDefault() ?? string.Empty;
        CreditAccount = AccountCodes.Skip(1).FirstOrDefault() ?? DebitAccount;
        InitializeComponent();
        DataContext = this;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var amount)
            && !decimal.TryParse(AmountText, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
        {
            MessageBox.Show("Số tiền không hợp lệ.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (amount <= 0)
        {
            MessageBox.Show("Số tiền phải lớn hơn 0.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (DebitAccount == CreditAccount)
        {
            MessageBox.Show("TK Nợ và TK Có phải khác nhau.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new CreateLedgerEntryRequest
        {
            EntryDate = EntryDate ?? DateTime.Today,
            DocumentNo = DocumentNo,
            Description = Description,
            DebitAccount = DebitAccount,
            CreditAccount = CreditAccount,
            Amount = amount,
            DocumentType = LedgerDocumentType.Adjustment
        };

        DialogResult = true;
    }
}
