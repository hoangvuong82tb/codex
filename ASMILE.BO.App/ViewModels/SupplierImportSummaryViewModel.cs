using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMILE.BO.App.ViewModels;

public partial class SupplierImportSummaryViewModel : ObservableObject
{
    [ObservableProperty] private int totalRows;
    [ObservableProperty] private int validRows;
    [ObservableProperty] private int warningRows;
    [ObservableProperty] private int duplicateRows;
    [ObservableProperty] private int errorRows;
}

