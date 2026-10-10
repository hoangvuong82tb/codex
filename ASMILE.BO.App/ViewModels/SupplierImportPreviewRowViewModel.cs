using ASMILE.BO.App.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.ViewModels;

public enum SupplierImportRowState
{
    Valid,
    Warning,
    Duplicate,
    Error
}

public partial class SupplierImportPreviewRowViewModel : ObservableObject
{
    [ObservableProperty] private bool isSelected = true;
    [ObservableProperty] private int rowNumber;
    [ObservableProperty] private string supplierCode = string.Empty;
    [ObservableProperty] private string supplierName = string.Empty;
    [ObservableProperty] private string phone = string.Empty;
    [ObservableProperty] private string bankAccountNo = string.Empty;
    [ObservableProperty] private string address = string.Empty;
    [ObservableProperty] private string note = string.Empty;
    [ObservableProperty] private string email = string.Empty;
    [ObservableProperty] private string taxCode = string.Empty;
    [ObservableProperty] private bool isActive = true;

    [ObservableProperty] private SupplierImportRowState state;
    [ObservableProperty] private string stateText = "Hợp lệ";
    [ObservableProperty] private string stateMessage = "Hợp lệ";
    [ObservableProperty] private string stateBackground = "#E8F5ED";
    [ObservableProperty] private string stateForeground = "#1E7C48";

    [ObservableProperty] private string issueSummary = "Hợp lệ";
    [ObservableProperty] private string issueTooltip = "Dữ liệu hợp lệ";
    [ObservableProperty] private bool hasBlockingError;

    public List<SupplierImportValidationIssue> Issues { get; } = [];

    public SupplierImportRow ToModel()
    {
        return new SupplierImportRow
        {
            RowNumber = RowNumber,
            SupplierCode = SupplierCode,
            SupplierName = SupplierName,
            Phone = Phone,
            BankAccountNo = BankAccountNo,
            Address = Address,
            Note = Note,
            Email = Email,
            TaxCode = TaxCode,
            IsActive = IsActive,
            IsActiveRaw = IsActive ? "true" : "false"
        };
    }
}
