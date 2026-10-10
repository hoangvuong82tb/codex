using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Marketing;
using CoreMarketingSection = ASMILE.BO.Core.FrontOffice.Marketing.MarketingSection;
using CoreMarketingRecordStatus = ASMILE.BO.Core.FrontOffice.Marketing.MarketingRecordStatus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace ASMILE.BO.App.Views;

/// <summary>
/// Shared operational workspace for the Marketing cards that do not already have
/// an established master-data module. The active section changes in place so users
/// can move between related marketing data without duplicate windows.
/// </summary>
public partial class MarketingCenterWindow : Window, INotifyPropertyChanged
{
    private readonly ObservableCollection<MarketingRecord> records = new();
    private readonly ObservableCollection<MarketingRecord> visibleRecords = new();
    private readonly ICollectionView recordsView;
    private readonly IMarketingQueryService? queryService;
    private readonly IMarketingCommandService? commandService;
    private readonly IFrontOfficeExecutionContextAccessor? contextAccessor;
    private readonly IServiceScope? serviceScope;
    private string currentSection = "source";
    private MarketingRecord? selectedRecord;
    private string selectedCategory = "Tất cả";
    private int pageSize = 20;
    private int currentPage = 1;

    public MarketingCenterWindow(string initialSection)
    {
        var services = (System.Windows.Application.Current as App)?.Services;
        serviceScope = services?.CreateScope();
        var scopedServices = serviceScope?.ServiceProvider ?? services;
        queryService = scopedServices?.GetService<IMarketingQueryService>();
        commandService = scopedServices?.GetService<IMarketingCommandService>();
        contextAccessor = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        InitializeComponent();
        DataContext = this;
        recordsView = CollectionViewSource.GetDefaultView(visibleRecords);
        ShowSection(initialSection);
        Closed += (_, _) => serviceScope?.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<MarketingKpi> Kpis { get; } = new();
    public ICollectionView RecordsView => recordsView;

    public MarketingRecord? SelectedRecord
    {
        get => selectedRecord;
        set
        {
            if (ReferenceEquals(selectedRecord, value)) return;
            selectedRecord = value;
            OnPropertyChanged(nameof(SelectedRecord));
        }
    }

    public string CurrentTitle => GetDescriptor(currentSection).Title + " — Marketing Center";
    public string CurrentSubtitle => GetDescriptor(currentSection).Subtitle;
    public string ListTitle => "Danh sách " + GetDescriptor(currentSection).Title.ToLowerInvariant();
    public string ResultText => $"{GetFilteredRecords().Count:N0} mục";
    public string PageText
    {
        get
        {
            var total = GetFilteredRecords().Count;
            if (total == 0) return "Hiển thị 0 trên 0 mục";
            var first = (currentPage - 1) * pageSize + 1;
            var last = Math.Min(currentPage * pageSize, total);
            return $"Hiển thị {first}–{last} trên {total:N0} mục";
        }
    }
    public string StatusText { get; private set; } = "Sẵn sàng";

    public async void ShowSection(string section)
    {
        currentSection = IsKnownSection(section) ? section : "source";
        selectedCategory = "Tất cả";
        currentPage = 1;
        SearchBox?.Clear();
        if (StatusFilter is not null) StatusFilter.SelectedIndex = 0;

        await LoadRecordsAsync();
        LoadCategories();
        LoadKpis();
        UpdateSectionButtons();
        SelectedRecord = records.FirstOrDefault();
        if (RecordsGrid is not null) RecordsGrid.SelectedItem = SelectedRecord;
        if (queryService is null || commandService is null || contextAccessor?.Current is null)
            SetStatus($"{GetDescriptor(currentSection).Title}: chưa đủ property context/query/command thật.");
        else
            SetStatus($"{GetDescriptor(currentSection).Title}: đã tải {records.Count:N0} bản ghi từ dữ liệu thật; có thể thao tác CRUD.");

        OnPropertyChanged(nameof(CurrentTitle));
        OnPropertyChanged(nameof(CurrentSubtitle));
        OnPropertyChanged(nameof(ListTitle));
        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(PageText));
    }

    private static bool IsKnownSection(string value) => value is "source" or "segment" or "agency" or "member" or "promotion";

    private async Task LoadRecordsAsync()
    {
        records.Clear();
        var context = contextAccessor?.Current;
        if (queryService is not null && context is not null)
        {
            try
            {
                var rows = await queryService.SearchAsync(
                    context.Property.PropertyId.Value,
                    new MarketingSearchFilter(ParseSection(currentSection)),
                    CancellationToken.None);
                foreach (var row in rows)
                    records.Add(Map(row));
            }
            catch (Exception ex)
            {
                SetStatus($"Không thể tải dữ liệu Marketing thật: {ex.Message}");
            }
        }
        RefreshView();
    }

    private void LoadCategories()
    {
        if (CategoryList is null) return;
        var categories = new List<string> { "Tất cả" };
        categories.AddRange(records.Select(record => record.Group).Distinct().OrderBy(item => item));
        CategoryList.ItemsSource = categories;
        CategoryList.SelectedIndex = 0;
    }

    private void LoadKpis()
    {
        var active = records.Count(x => x.Status == "Đang hoạt động");
        var paused = records.Count(x => x.Status == "Tạm dừng");
        var revenue = records.Sum(x => x.Revenue);
        Kpis.Clear();
        Kpis.Add(new MarketingKpi(records.Count.ToString("N0"), "Tổng bản ghi", "#F5F8FE", "#D8E4F4", "#2766DF"));
        Kpis.Add(new MarketingKpi(active.ToString("N0"), "Đang hoạt động", "#EDFCF2", "#A9E9BD", "#169B46"));
        Kpis.Add(new MarketingKpi(paused.ToString("N0"), "Tạm dừng", "#FFF7EB", "#FFD59C", "#D97606"));
        Kpis.Add(new MarketingKpi("—", "Tỷ lệ chuyển đổi", "#EFF7FF", "#A9D5FF", "#1675D1"));
        Kpis.Add(new MarketingKpi(revenue.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")), "Doanh thu theo dõi", "#F8F1FF", "#DCC2FF", "#7C3AED"));
    }

    private bool MatchesFilter(object item)
    {
        if (item is not MarketingRecord record) return false;
        var search = SearchBox?.Text?.Trim() ?? string.Empty;
        var status = (StatusFilter?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả trạng thái";
        var matchesSearch = string.IsNullOrWhiteSpace(search)
                            || record.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || record.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || record.Group.Contains(search, StringComparison.OrdinalIgnoreCase)
                            || record.Owner.Contains(search, StringComparison.OrdinalIgnoreCase);
        var matchesStatus = status == "Tất cả trạng thái" || record.Status == status;
        return matchesSearch && matchesStatus && (selectedCategory == "Tất cả" || record.Group == selectedCategory);
    }

    private List<MarketingRecord> GetFilteredRecords() => records.Where(MatchesFilter).ToList();

    private void Section_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string section }) ShowSection(section);
    }

    private void Search_TextChanged(object sender, TextChangedEventArgs e) => RefreshView();
    private void StatusFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshView();

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryList?.SelectedItem is not string category) return;
        selectedCategory = category;
        RefreshView();
    }

    private void RefreshView()
    {
        var filtered = GetFilteredRecords();
        var totalPages = Math.Max(1, (filtered.Count + pageSize - 1) / pageSize);
        currentPage = Math.Min(currentPage, totalPages);
        visibleRecords.Clear();
        foreach (var row in filtered.Skip((currentPage - 1) * pageSize).Take(pageSize))
            visibleRecords.Add(row);
        recordsView?.Refresh();
        OnPropertyChanged(nameof(ResultText));
        OnPropertyChanged(nameof(PageText));
    }

    private void RecordsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is MarketingRecord record) SelectedRecord = record;
    }

    private void ClearSelection_Click(object sender, RoutedEventArgs e)
    {
        RecordsGrid.SelectedItem = null;
        SelectedRecord = null;
        SetStatus("Đã ẩn chi tiết. Chọn một dòng để xem lại thông tin.");
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetPropertyId(out var propertyId) || commandService is null)
        {
            SetStatus("Không thể thêm: thiếu property context hoặc command Marketing thật.");
            return;
        }

        var editor = new MarketingEditorWindow(GetDescriptor(currentSection).Title, ParseSection(currentSection), null);
        if (editor.ShowDialog() != true || editor.Values is null) return;

        try
        {
            await commandService.CreateAsync(propertyId, new CreateMarketingRecordRequest(
                ParseSection(currentSection), editor.Values.Code, editor.Values.Name, editor.Values.GroupName,
                editor.Values.Owner, editor.Values.Notes), CancellationToken.None);
            await ReloadAfterMutationAsync("Đã thêm bản ghi Marketing thật.");
        }
        catch (Exception ex) { SetStatus($"Không thể thêm bản ghi Marketing: {ex.Message}"); }
    }

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRecord is null || !TryGetPropertyId(out var propertyId) || commandService is null)
        {
            SetStatus("Hãy chọn một bản ghi và bảo đảm property context/command thật đã sẵn sàng.");
            return;
        }

        var editor = new MarketingEditorWindow(GetDescriptor(currentSection).Title, SelectedRecord.Section, SelectedRecord);
        if (editor.ShowDialog() != true || editor.Values is null) return;

        try
        {
            await commandService.UpdateAsync(propertyId, new UpdateMarketingRecordRequest(
                SelectedRecord.RecordId, editor.Values.Code, editor.Values.Name, editor.Values.GroupName,
                editor.Values.Owner, editor.Values.Notes, SelectedRecord.RowVersion), CancellationToken.None);
            await ReloadAfterMutationAsync("Đã cập nhật bản ghi Marketing thật.");
        }
        catch (Exception ex) { SetStatus($"Không thể cập nhật bản ghi Marketing: {ex.Message}"); }
    }

    private async void ToggleStatus_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedRecord is null || !TryGetPropertyId(out var propertyId) || commandService is null)
        {
            SetStatus("Hãy chọn một bản ghi và bảo đảm property context/command thật đã sẵn sàng.");
            return;
        }

        var nextStatus = SelectedRecord.Status == "Đang hoạt động"
            ? CoreMarketingRecordStatus.Paused
            : CoreMarketingRecordStatus.Active;
        try
        {
            await commandService.ChangeStatusAsync(propertyId,
                new ChangeMarketingRecordStatusRequest(SelectedRecord.RecordId, nextStatus, SelectedRecord.RowVersion),
                CancellationToken.None);
            await ReloadAfterMutationAsync("Đã đổi trạng thái bản ghi Marketing thật.");
        }
        catch (Exception ex) { SetStatus($"Không thể đổi trạng thái Marketing: {ex.Message}"); }
    }

    private void ClearFilter_Click(object sender, RoutedEventArgs e)
    {
        SearchBox.Clear();
        StatusFilter.SelectedIndex = 0;
        CategoryList.SelectedIndex = 0;
        SetStatus("Đã xóa bộ lọc.");
    }

    private void PreviousPage_Click(object sender, RoutedEventArgs e)
    {
        if (currentPage <= 1) return;
        currentPage--;
        RefreshView();
    }

    private void NextPage_Click(object sender, RoutedEventArgs e)
    {
        var totalPages = Math.Max(1, (GetFilteredRecords().Count + pageSize - 1) / pageSize);
        if (currentPage >= totalPages) return;
        currentPage++;
        RefreshView();
    }

    private void PageSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedItem: ComboBoxItem item } && int.TryParse(item.Content?.ToString()?.Split(' ')[0], out var value))
        {
            pageSize = value;
            currentPage = 1;
            RefreshView();
            OnPropertyChanged(nameof(PageText));
        }
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadRecordsAsync();
        LoadCategories();
        LoadKpis();
        SetStatus($"Đã làm mới dữ liệu Marketing thật: {records.Count:N0} bản ghi.");
    }

    private async Task ReloadAfterMutationAsync(string message)
    {
        await LoadRecordsAsync();
        LoadCategories();
        LoadKpis();
        SelectedRecord = records.FirstOrDefault();
        RefreshView();
        SetStatus(message);
    }

    private bool TryGetPropertyId(out Guid propertyId)
    {
        propertyId = Guid.Empty;
        var context = contextAccessor?.Current;
        if (context is null || context.Property.PropertyId.Value == Guid.Empty) return false;
        propertyId = context.Property.PropertyId.Value;
        return true;
    }

    private void Export_Click(object sender, RoutedEventArgs e)
    {
        var rows = GetFilteredRecords();
        if (rows.Count == 0)
        {
            SetStatus("Không có dữ liệu Marketing thật phù hợp bộ lọc để xuất.");
            return;
        }

        var dialog = new SaveFileDialog
        {
            Title = "Xuất dữ liệu Marketing",
            Filter = "CSV UTF-8 (*.csv)|*.csv|Tất cả tệp (*.*)|*.*",
            FileName = $"marketing-{DateTime.Now:yyyyMMdd-HHmmss}.csv",
            AddExtension = true,
            DefaultExt = ".csv",
            OverwritePrompt = true
        };
        if (dialog.ShowDialog(this) != true) return;

        try
        {
            var csv = new StringBuilder();
            csv.AppendLine("Mã,Phân hệ,Tên,Nhóm,Người phụ trách,Trạng thái,Doanh thu,Ghi chú,Cập nhật");
            foreach (var row in rows)
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(row.Id),
                    Csv(row.Section.ToString()),
                    Csv(row.Name),
                    Csv(row.Group),
                    Csv(row.Owner),
                    Csv(row.Status),
                    Csv(row.Revenue.ToString("0.##", CultureInfo.InvariantCulture)),
                    Csv(row.Notes),
                    Csv(row.Updated)
                }));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            SetStatus($"Đã xuất {rows.Count:N0} bản ghi Marketing thật theo bộ lọc hiện tại.");
        }
        catch (Exception ex)
        {
            SetStatus("Không thể xuất dữ liệu Marketing: " + ex.Message);
        }
    }

    private static string Csv(string? value)
    {
        var text = value ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void UpdateSectionButtons()
    {
        var buttons = new Dictionary<string, Button>
        {
            ["source"] = SourceButton,
            ["segment"] = SegmentButton,
            ["agency"] = AgencyButton,
            ["member"] = MemberButton,
            ["promotion"] = PromotionButton
        };
        foreach (var (key, button) in buttons)
        {
            var isActive = key == currentSection;
            button.Background = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(isActive ? "#EAF2FF" : "#F2F6FB")!;
            button.BorderBrush = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(isActive ? "#266BEA" : "#D7E2F0")!;
            button.Foreground = (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(isActive ? "#1355C6" : "#183455")!;
            button.FontWeight = isActive ? FontWeights.SemiBold : FontWeights.Normal;
        }
    }

    private void SetStatus(string value)
    {
        StatusText = value;
        OnPropertyChanged(nameof(StatusText));
    }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private static CoreMarketingSection ParseSection(string section) => section switch
    {
        "segment" => CoreMarketingSection.Segment,
        "agency" => CoreMarketingSection.Agency,
        "member" => CoreMarketingSection.Member,
        "promotion" => CoreMarketingSection.Promotion,
        _ => CoreMarketingSection.Source
    };

    private static MarketingRecord Map(MarketingRecordDto row) => new()
    {
        RecordId = row.MarketingRecordId,
        Section = row.Section,
        RowVersion = row.RowVersion,
        Id = row.Code,
        Name = row.Name,
        Group = row.GroupName,
        Owner = row.Owner,
        Revenue = row.Revenue,
        Status = row.Status switch
        {
            CoreMarketingRecordStatus.Active => "Đang hoạt động",
            CoreMarketingRecordStatus.Paused => "Tạm dừng",
            _ => "Kết thúc"
        },
        Updated = row.UpdatedAtUtc.ToLocalTime().ToString("dd/MM/yyyy HH:mm"),
        Notes = row.Notes ?? string.Empty
    };

    private static MarketingDescriptor GetDescriptor(string section) => section switch
    {
        "segment" => new MarketingDescriptor("Phân khúc", "Quản lý phân khúc thị trường và hiệu quả của từng nhóm khách.", "SEG"),
        "agency" => new MarketingDescriptor("Đại lý", "Quản lý đại lý, đối tác nguồn khách và hiệu quả hợp tác.", "AGY"),
        "member" => new MarketingDescriptor("Thành viên", "Theo dõi hạng thành viên, quyền lợi và mức độ gắn kết.", "MEM"),
        "promotion" => new MarketingDescriptor("Khuyến mại", "Thiết lập và theo dõi hiệu quả của các chương trình khuyến mại.", "PRM"),
        _ => new MarketingDescriptor("Nguồn khách", "Quản lý nguồn tạo đặt phòng và hiệu quả chuyển đổi.", "SRC")
    };

    private static string BuildName(string section, int index, string group) => section switch
    {
        "source" => $"{group} — nguồn khách {index}",
        "segment" => $"Phân khúc {group} {index}",
        "agency" => $"Đại lý {group} {index}",
        "member" => $"Hạng {group} {index}",
        _ => $"Ưu đãi {group} {index}"
    };

    private static string BuildNote(string section, string group) => section switch
    {
        "source" => $"Nguồn {group} được theo dõi để phân tích chuyển đổi đặt phòng.",
        "segment" => $"Nhóm khách {group} được sử dụng khi phân tích nhu cầu và doanh thu.",
        "agency" => $"Đại lý thuộc nhóm {group}; theo dõi doanh số và điều kiện hợp tác.",
        "member" => $"Hạng thành viên {group}; theo dõi quyền lợi, tích điểm và ưu đãi.",
        _ => $"Chương trình {group}; theo dõi thời hạn, đối tượng và kết quả áp dụng."
    };

    private sealed record MarketingDescriptor(string Title, string Subtitle, string Prefix);
}

public sealed class MarketingKpi
{
    public MarketingKpi(string value, string label, string background, string border, string accent)
    {
        Value = value;
        Label = label;
        Background = new System.Windows.Media.BrushConverter().ConvertFromString(background) as System.Windows.Media.Brush;
        Border = new System.Windows.Media.BrushConverter().ConvertFromString(border) as System.Windows.Media.Brush;
        Accent = new System.Windows.Media.BrushConverter().ConvertFromString(accent) as System.Windows.Media.Brush;
    }

    public string Value { get; }
    public string Label { get; }
    public System.Windows.Media.Brush? Background { get; }
    public System.Windows.Media.Brush? Border { get; }
    public System.Windows.Media.Brush? Accent { get; }
}

public sealed class MarketingRecord : INotifyPropertyChanged
{
    private string status = string.Empty;
    private string updated = string.Empty;
    private string notes = string.Empty;

    public Guid RecordId { get; init; }
    public CoreMarketingSection Section { get; init; }
    public byte[]? RowVersion { get; init; }
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Group { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public decimal Revenue { get; init; }
    public string RevenueText => Revenue.ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + " đ";
    public string Status { get => status; set { status = value; OnPropertyChanged(nameof(Status)); } }
    public string Updated { get => updated; set { updated = value; OnPropertyChanged(nameof(Updated)); } }
    public string Notes { get => notes; set { notes = value; OnPropertyChanged(nameof(Notes)); } }

    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
