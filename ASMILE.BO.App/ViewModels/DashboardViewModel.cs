using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class DashboardViewModel : ViewModelBase
{
    private readonly IInventoryAppService _inventoryAppService;

    public DashboardDto Dashboard { get; private set; } = new();

    public int TotalItems => Dashboard.TotalItems;
    public int TotalSuppliers => Dashboard.TotalSuppliers;
    public int TotalWarehouses => Dashboard.TotalWarehouses;
    public int ReceiptsThisMonth => Dashboard.ReceiptsThisMonth;
    public int IssuesThisMonth => Dashboard.IssuesThisMonth;
    public decimal InventoryValue => Dashboard.InventoryValue;

    public DashboardViewModel(IInventoryAppService inventoryAppService)
    {
        _inventoryAppService = inventoryAppService;
        _ = LoadAsync();
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            Dashboard = await _inventoryAppService.GetDashboardAsync();
            OnPropertyChanged(nameof(TotalItems));
            OnPropertyChanged(nameof(TotalSuppliers));
            OnPropertyChanged(nameof(TotalWarehouses));
            OnPropertyChanged(nameof(ReceiptsThisMonth));
            OnPropertyChanged(nameof(IssuesThisMonth));
            OnPropertyChanged(nameof(InventoryValue));
        }
        finally
        {
            IsBusy = false;
        }
    }
}

