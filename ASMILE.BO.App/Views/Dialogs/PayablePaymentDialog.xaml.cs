using ASMILE.BO.Application.DTOs;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class PayablePaymentDialog : Window
{
    public ObservableCollection<PayableSummaryDto> SupplierOptions { get; }
    public PayableSummaryDto? SelectedSupplier { get; set; }
    public DateTime? PaymentDate { get; set; } = DateTime.Today;
    public string AmountText { get; set; } = string.Empty;
    public ObservableCollection<string> Methods { get; } = new(["Chuyển khoản", "Tiền mặt", "Quẹt thẻ"]);
    public string PaymentMethod { get; set; } = "Chuyển khoản";
    public string BankAccount { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    public CreatePaymentVoucherRequest? Result { get; private set; }

    public PayablePaymentDialog(IEnumerable<PayableSummaryDto> suppliers)
    {
        SupplierOptions = new ObservableCollection<PayableSummaryDto>(suppliers);
        InitializeComponent();
        DataContext = this;
        SelectedSupplier = SupplierOptions.FirstOrDefault();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (SelectedSupplier is null)
        {
            MessageBox.Show("Vui lòng chọn nhà cung cấp.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        Result = new CreatePaymentVoucherRequest
        {
            SupplierId = SelectedSupplier.SupplierId,
            PaymentDate = PaymentDate ?? DateTime.Today,
            Amount = amount,
            PaymentMethod = PaymentMethod,
            BankAccount = BankAccount,
            Note = Note
        };

        DialogResult = true;
    }
}
