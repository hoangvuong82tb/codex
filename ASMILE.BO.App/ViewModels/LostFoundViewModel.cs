using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed class LostFoundDto
{
    public Guid LostFoundItemId { get; set; }
    public string ItemNumber { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FoundLocation { get; set; }
    public DateTime FoundDateTime { get; set; }
    public string? FoundByStaff { get; set; }
    public string? StoredLocation { get; set; }
    public string Status { get; set; } = "Found";
    public string? OwnerGuestName { get; set; }
    public string? Color { get; set; }
    public string? Brand { get; set; }
    public string? Notes { get; set; }
    public DateTime? ClaimedDate { get; set; }
    public ObservableCollection<string> History { get; set; } = new();
}

public sealed partial class LostFoundViewModel : ViewModelBase
{
    public ObservableCollection<LostFoundDto> Items { get; } = new();
    [ObservableProperty] private LostFoundDto? _selectedItem;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private string _infoMessage = string.Empty;
    [ObservableProperty] private string? _filterStatus;
    [ObservableProperty] private string? _filterCategory;
    [ObservableProperty] private string _searchText = "";
    [ObservableProperty] private int _totalFound;
    [ObservableProperty] private int _totalReturned;
    [ObservableProperty] private int _totalOutstanding;
    [ObservableProperty] private string _newItemName = "";
    [ObservableProperty] private string _newCategory = "Electronics";
    [ObservableProperty] private string _newLocation = "";
    [ObservableProperty] private string _newFoundBy = "";
    [ObservableProperty] private string _newStoredLocation = "";
    [ObservableProperty] private string _newColor = "";
    [ObservableProperty] private string _newBrand = "";
    [ObservableProperty] private string _matchGuestName = "";

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);
    public bool HasSelection => SelectedItem is not null;
    public string[] Categories { get; } = ["Electronics", "Jewelry", "Documents", "Cash", "Clothing", "Luggage", "Keys", "Medical", "Others"];

    public async Task InitializeAsync(CancellationToken ct) => await LoadAsync(ct);

    [RelayCommand] private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            Items.Clear();
            var i1 = new LostFoundDto { LostFoundItemId = Guid.NewGuid(), ItemNumber = "LAF-202607-000001", ItemName = "iPhone 15 Pro", Category = "Electronics", FoundLocation = "Phong 101", FoundDateTime = DateTime.UtcNow.AddHours(-3), FoundByStaff = "Mai Lan", StoredLocation = "Ke an toan - Ngan A3", Status = "Stored", Color = "Titanium", Brand = "Apple" }; i1.History.Add("Registered"); i1.History.Add("Stored at Ke an toan");
            var i2 = new LostFoundDto { LostFoundItemId = Guid.NewGuid(), ItemNumber = "LAF-202607-000002", ItemName = "Vi da", Category = "Documents", FoundLocation = "Sanh", FoundDateTime = DateTime.UtcNow.AddDays(-1), FoundByStaff = "Bao ve", StoredLocation = "Ke an toan - Ngan B1", Status = "Matched", OwnerGuestName = "Nguyen Van A" }; i2.History.Add("Registered"); i2.History.Add("Matched to Nguyen Van A");
            var i3 = new LostFoundDto { LostFoundItemId = Guid.NewGuid(), ItemNumber = "LAF-202607-000003", ItemName = "Dong ho Rolex", Category = "Jewelry", FoundLocation = "Phong 301", FoundDateTime = DateTime.UtcNow.AddDays(-2), FoundByStaff = "Le Thi H", StoredLocation = "Ke an toan VIP", Status = "Returned", OwnerGuestName = "Le Van C", ClaimedDate = DateTime.UtcNow.AddHours(-1), Brand = "Rolex" }; i3.History.Add("Registered"); i3.History.Add("Matched to Le Van C"); i3.History.Add("Claimed"); i3.History.Add("Returned");
            Items.Add(i1); Items.Add(i2); Items.Add(i3);
            TotalFound = Items.Count; TotalReturned = Items.Count(i => i.Status == "Returned"); TotalOutstanding = Items.Count(i => i.Status != "Returned" && i.Status != "Disposed" && i.Status != "Archived");
        }
        catch (Exception ex) { ErrorMessage = $"Loi: {ex.Message}"; }
        finally { IsLoading = false; }
    }

    [RelayCommand] private async Task RefreshAsync(CancellationToken ct) => await LoadAsync(ct);
    [RelayCommand] private void SelectItem(LostFoundDto? i) => SelectedItem = i;

    [RelayCommand] private void RegisterItem() { if (string.IsNullOrWhiteSpace(NewItemName)) { ErrorMessage = "Vui long nhap ten vat pham."; return; } var ni = new LostFoundDto { LostFoundItemId = Guid.NewGuid(), ItemNumber = $"LAF-202607-{Items.Count + 1:D6}", ItemName = NewItemName, Category = NewCategory, FoundLocation = NewLocation, FoundDateTime = DateTime.UtcNow, FoundByStaff = NewFoundBy, StoredLocation = NewStoredLocation, Status = "Stored", Color = NewColor, Brand = NewBrand }; ni.History.Add("Registered"); ni.History.Add($"Stored at {NewStoredLocation}"); Items.Insert(0, ni); TotalFound++; TotalOutstanding++; InfoMessage = $"Da dang ky {ni.ItemNumber} - {ni.ItemName}."; }
    [RelayCommand] private void MatchGuest() { if (SelectedItem is null || string.IsNullOrWhiteSpace(MatchGuestName)) return; SelectedItem.OwnerGuestName = MatchGuestName; SelectedItem.Status = "Matched"; SelectedItem.History.Add($"Matched to {MatchGuestName}"); InfoMessage = $"Da ghep {SelectedItem.ItemNumber} voi khach {MatchGuestName}."; MatchGuestName = ""; }
    [RelayCommand] private void ClaimItem() { if (SelectedItem is null) return; SelectedItem.Status = "Claimed"; SelectedItem.ClaimedDate = DateTime.UtcNow; SelectedItem.History.Add("Claimed"); InfoMessage = $"Khach da nhan lai {SelectedItem.ItemNumber}."; }
    [RelayCommand] private void ReturnItem() { if (SelectedItem is null) return; SelectedItem.Status = "Returned"; SelectedItem.History.Add("Returned"); TotalReturned++; TotalOutstanding--; InfoMessage = $"Da tra lai {SelectedItem.ItemNumber} cho khach."; }

    partial void OnSelectedItemChanged(LostFoundDto? v) => OnPropertyChanged(nameof(HasSelection));
    partial void OnErrorMessageChanged(string v) => OnPropertyChanged(nameof(HasError));
}
