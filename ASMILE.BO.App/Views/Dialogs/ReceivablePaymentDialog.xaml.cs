using ASMILE.BO.Application.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class ReceivablePaymentDialog : Window
{
    public ObservableCollection<ReceivableSummaryDto> CustomerOptions { get; }
    public ReceivableSummaryDto? SelectedCustomer { get; set; }
    public DateTime? ReceiptDate { get; set; } = DateTime.Today;
    public string AmountText { get; set; } = string.Empty;
    public ObservableCollection<string> Methods { get; } = new(["Tiền mặt", "Chuyển khoản", "Quẹt thẻ"]);
    public string PaymentMethod { get; set; } = "Tiền mặt";
    public string BankAccount { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    public CreatePaymentReceiptRequest? Result { get; private set; }

    public ReceivablePaymentDialog(IEnumerable<ReceivableSummaryDto> customers)
    {
        CustomerOptions = new ObservableCollection<ReceivableSummaryDto>(customers);
        InitializeComponent();
        DataContext = this;
        SelectedCustomer = CustomerOptions.FirstOrDefault();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (SelectedCustomer is null)
        {
            MessageBox.Show("Vui lòng chọn khách hàng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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

        if (amount > SelectedCustomer.ClosingBalance)
        {
            MessageBox.Show("Số tiền không được vượt quá công nợ còn lại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Result = new CreatePaymentReceiptRequest
        {
            CustomerId = SelectedCustomer.CustomerId,
            ReceiptDate = ReceiptDate ?? DateTime.Today,
            Amount = amount,
            PaymentMethod = PaymentMethod,
            BankAccount = BankAccount,
            Note = Note
        };

        DialogResult = true;
    }
}
