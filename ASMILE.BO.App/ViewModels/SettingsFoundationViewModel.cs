using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class MasterDataItem { public Guid Id { get; set; } public string Code { get; set; } = ""; public string Name { get; set; } = ""; public bool IsActive { get; set; } = true; }
public sealed partial class FoSettingsViewModel : ViewModelBase
{
    public ObservableCollection<MasterDataItem> Buildings { get; } = new();
    public ObservableCollection<MasterDataItem> Amenities { get; } = new();
    public ObservableCollection<MasterDataItem> MarketSegments { get; } = new();
    public ObservableCollection<MasterDataItem> Sources { get; } = new();
    public ObservableCollection<MasterDataItem> Countries { get; } = new();
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private int _activeTab;
    [ObservableProperty] private string _newCode = "";
    [ObservableProperty] private string _newName = "";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public string[] Tabs { get; } = ["Toa Nha", "Tien Nghi", "Phan Khuc", "Nguon DP", "Quoc Gia"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Buildings.Clear(); Amenities.Clear(); MarketSegments.Clear(); Sources.Clear(); Countries.Clear();
            Buildings.Add(new MasterDataItem { Code = "A", Name = "Toa A", IsActive = true });
            Buildings.Add(new MasterDataItem { Code = "B", Name = "Toa B", IsActive = true });
            Amenities.Add(new MasterDataItem { Code = "WIFI", Name = "WiFi mien phi" });
            Amenities.Add(new MasterDataItem { Code = "POOL", Name = "Ho boi" });
            Amenities.Add(new MasterDataItem { Code = "GYM", Name = "Phong Gym" });
            MarketSegments.Add(new MasterDataItem { Code = "CORP", Name = "Corporate" });
            MarketSegments.Add(new MasterDataItem { Code = "LEIS", Name = "Leisure" });
            Sources.Add(new MasterDataItem { Code = "DIRECT", Name = "Direct" });
            Sources.Add(new MasterDataItem { Code = "OTA", Name = "Online Travel Agent" });
            Countries.Add(new MasterDataItem { Code = "VN", Name = "Viet Nam" });
            Countries.Add(new MasterDataItem { Code = "US", Name = "Hoa Ky" });
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SetTab(int idx) => ActiveTab = idx;
    [RelayCommand] private void AddItem()
    {
        if (string.IsNullOrWhiteSpace(NewCode) || string.IsNullOrWhiteSpace(NewName)) { ErrorMessage = "Vui long nhap Ma va Ten."; return; }
        var item = new MasterDataItem { Id = Guid.NewGuid(), Code = NewCode, Name = NewName };
        switch (ActiveTab) { case 0: Buildings.Add(item); break; case 1: Amenities.Add(item); break; case 2: MarketSegments.Add(item); break; case 3: Sources.Add(item); break; case 4: Countries.Add(item); break; }
        InfoMessage = $"Da them {NewCode} - {NewName}."; NewCode = ""; NewName = "";
    }

    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
