using System.Globalization;
using System.Windows;
using ASMILE.BO.Application.FrontOffice.Rate;

namespace ASMILE.BO.App.Views;

public sealed record ExchangeRateEditorValues(string CurrencyCode, string CurrencyName, string Symbol, decimal BuyRate, decimal SellRate, decimal HotelRate, DateTime EffectiveFromUtc, DateTime? EffectiveToUtc, bool IsDefault, string? Notes);

public partial class ExchangeRateEditorWindow : Window
{
    public ExchangeRateEditorValues? Values { get; private set; }

    public ExchangeRateEditorWindow(ExchangeRateDto? source, bool copyMode)
    {
        InitializeComponent();
        Title = copyMode ? "Sao chép tỷ giá" : source is null ? "Thêm tỷ giá" : "Chỉnh sửa tỷ giá";
        if (source is not null)
        {
            CodeBox.Text = source.CurrencyCode; NameBox.Text = source.CurrencyName; SymbolBox.Text = source.Symbol; BuyBox.Text = source.BuyRate.ToString(CultureInfo.CurrentCulture); SellBox.Text = source.SellRate.ToString(CultureInfo.CurrentCulture); HotelBox.Text = source.HotelRate.ToString(CultureInfo.CurrentCulture); FromPicker.SelectedDate = source.EffectiveFromUtc.ToLocalTime().Date; ToPicker.SelectedDate = source.EffectiveToUtc?.ToLocalTime().Date; DefaultBox.IsChecked = source.IsDefault; NotesBox.Text = source.Notes;
        }
        else FromPicker.SelectedDate = DateTime.Today;
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (!TryDecimal(BuyBox.Text, out var buy) || !TryDecimal(SellBox.Text, out var sell) || !TryDecimal(HotelBox.Text, out var hotel)) { Warn("Mức tỷ giá phải là số hợp lệ."); return; }
        if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text) || FromPicker.SelectedDate is not DateTime from) { Warn("Vui lòng nhập mã, tên và ngày hiệu lực."); return; }
        var to = ToPicker.SelectedDate?.Date; if (to is DateTime end && end <= from.Date) { Warn("Ngày hết hiệu lực phải sau ngày bắt đầu."); return; }
        if (buy < 0 || sell < 0 || hotel < 0) { Warn("Tỷ giá không được âm."); return; }
        Values = new ExchangeRateEditorValues(CodeBox.Text.Trim(), NameBox.Text.Trim(), SymbolBox.Text.Trim(), buy, sell, hotel, from.ToUniversalTime(), to?.ToUniversalTime(), DefaultBox.IsChecked == true, string.IsNullOrWhiteSpace(NotesBox.Text) ? null : NotesBox.Text.Trim()); DialogResult = true;
    }
    private static bool TryDecimal(string value, out decimal result) => decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out result) || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    private static void Warn(string message) => MessageBox.Show(message, "Tỷ giá", MessageBoxButton.OK, MessageBoxImage.Warning);
    private void OnCancel(object sender, RoutedEventArgs e) => DialogResult = false;
}
