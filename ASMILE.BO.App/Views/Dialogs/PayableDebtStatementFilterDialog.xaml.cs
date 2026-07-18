using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ASMILE.BO.App.Views.Dialogs;

public partial class PayableDebtStatementFilterDialog : Window
{
    private readonly PayableDebtStatementFilterDialogViewModel _viewModel;

    public PayableDebtStatementFilterDialog(
        string supplierCode,
        string supplierName,
        DateTime defaultFromDate,
        DateTime defaultToDate,
        string windowTitle = "Chọn kỳ đối chiếu công nợ",
        string headerText = "Chọn kỳ đối chiếu công nợ",
        string hintText = "Dữ liệu đối chiếu sẽ gồm phiếu nhập, phiếu chi và số dư còn lại trong khoảng ngày đã chọn.",
        string applyText = "Xem đối chiếu")
    {
        InitializeComponent();
        _viewModel = new PayableDebtStatementFilterDialogViewModel
        {
            SupplierCode = supplierCode,
            SupplierName = supplierName,
            FromDate = defaultFromDate,
            ToDate = defaultToDate,
            WindowTitle = windowTitle,
            HeaderText = headerText,
            HintText = hintText,
            ApplyText = applyText
        };
        DataContext = _viewModel;
    }

    public DateTime FromDate => _viewModel.FromDate.Date;
    public DateTime ToDate => _viewModel.ToDate.Date;

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.FromDate.Date > _viewModel.ToDate.Date)
        {
            MessageBox.Show("Từ ngày không được lớn hơn đến ngày.", "Kỳ đối chiếu không hợp lệ", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        DialogResult = true;
    }
}

public sealed class PayableDebtStatementFilterDialogViewModel : INotifyPropertyChanged
{
    private DateTime _fromDate;
    private DateTime _toDate;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SupplierCode { get; init; } = string.Empty;
    public string SupplierName { get; init; } = string.Empty;
    public string WindowTitle { get; init; } = "Chọn kỳ đối chiếu công nợ";
    public string HeaderText { get; init; } = "Chọn kỳ đối chiếu công nợ";
    public string HintText { get; init; } = "Dữ liệu đối chiếu sẽ gồm phiếu nhập, phiếu chi và số dư còn lại trong khoảng ngày đã chọn.";
    public string ApplyText { get; init; } = "Xem đối chiếu";
    public string SupplierText => $"Nhà cung cấp: {SupplierCode} - {SupplierName}";

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (_fromDate == value)
            {
                return;
            }

            _fromDate = value;
            OnPropertyChanged();
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (_toDate == value)
            {
                return;
            }

            _toDate = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
