using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using ASMILE.BO.Application.FrontOffice.Foundation.Contexts;
using ASMILE.BO.Application.FrontOffice.Reporting;
using ASMILE.BO.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ASMILE.BO.App.Views;

public partial class FolioBusinessDateBackfillWindow : Window
{
    private readonly IFolioBusinessDateBackfillService? _backfill;
    private readonly IFrontOfficeExecutionContextAccessor? _executionContext;
    private readonly IServiceScope? _serviceScope;
    private readonly ObservableCollection<BackfillRow> _rows;
    private bool _isSubmitting;

    public FolioBusinessDateBackfillWindow(IReadOnlyList<FolioBusinessDateGapRow> candidates)
    {
        InitializeComponent();
        Owner = System.Windows.Application.Current.MainWindow;
        var services = (System.Windows.Application.Current as App)?.Services;
        _serviceScope = services?.CreateScope();
        var scopedServices = _serviceScope?.ServiceProvider ?? services;
        _backfill = scopedServices?.GetService<IFolioBusinessDateBackfillService>();
        _executionContext = scopedServices?.GetService<IFrontOfficeExecutionContextAccessor>();
        _rows = new ObservableCollection<BackfillRow>(candidates.Select(x => new BackfillRow(x)));
        BackfillGrid.ItemsSource = _rows;
        StatusText.Text = $"Đang hiển thị {_rows.Count:N0} dòng thiếu ngày. Chỉ các dòng được chọn mới được cập nhật.";
        Closed += (_, _) => _serviceScope?.Dispose();
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private async void OnApply(object sender, RoutedEventArgs e)
    {
        if (_isSubmitting) return;
        var selected = _rows.Where(x => x.IsSelected).ToList();
        if (selected.Count == 0)
        {
            StatusText.Text = "Vui lòng chọn ít nhất một dòng Folio.";
            return;
        }
        if (string.IsNullOrWhiteSpace(ApprovalReasonBox.Text) || string.IsNullOrWhiteSpace(ApprovalReferenceBox.Text))
        {
            StatusText.Text = "Vui lòng nhập lý do và số tham chiếu phê duyệt.";
            return;
        }

        var items = new List<FolioBusinessDateBackfillItem>(selected.Count);
        foreach (var row in selected)
        {
            if (!DateOnly.TryParse(row.BusinessDateText, CultureInfo.CurrentCulture, DateTimeStyles.None, out var businessDate))
            {
                StatusText.Text = $"Ngày hạch toán của bút toán {row.EntryShortId} không hợp lệ. Dùng định dạng dd/MM/yyyy.";
                return;
            }
            items.Add(new FolioBusinessDateBackfillItem(row.EntryId, businessDate));
        }
        if (_backfill is null)
        {
            StatusText.Text = "Chưa kết nối được nghiệp vụ backfill Folio.";
            return;
        }

        _isSubmitting = true;
        ApplyButton.IsEnabled = false;
        try
        {
            var propertyId = _executionContext?.Current?.Property.PropertyId.Value;
            if (propertyId is null)
            {
                StatusText.Text = "Chưa xác định được cơ sở đang thao tác.";
                return;
            }

            var result = await _backfill.ApplyAsync(new FolioBusinessDateBackfillCommand(
                propertyId.Value, items, ApprovalReasonBox.Text.Trim(), ApprovalReferenceBox.Text.Trim()), default);
            if (!result.IsSuccess || result.Value is null)
            {
                StatusText.Text = result.Error?.Message ?? "Không thể backfill ngày hạch toán Folio.";
                return;
            }

            MessageBox.Show($"Đã backfill {result.Value.UpdatedCount:N0} dòng Folio.\nĐã có sẵn: {result.Value.AlreadyAppliedCount:N0} dòng.", "Hoàn tất", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        finally
        {
            _isSubmitting = false;
            ApplyButton.IsEnabled = true;
        }
    }

    private sealed class BackfillRow : INotifyPropertyChanged
    {
        private bool _isSelected = true;
        public Guid EntryId { get; }
        public string EntryShortId { get; }
        public string EntryType { get; }
        public string AmountDisplay { get; }
        public string SuggestedBusinessDateDisplay { get; }
        public string Description { get; }
        public string BusinessDateText { get; set; }
        public bool IsSelected
        {
            get => _isSelected;
            set { if (_isSelected == value) return; _isSelected = value; OnPropertyChanged(); }
        }

        public BackfillRow(FolioBusinessDateGapRow row)
        {
            EntryId = row.EntryId;
            EntryShortId = row.EntryId.ToString("N")[..12];
            EntryType = row.EntryType;
            AmountDisplay = row.Amount.ToString("N0", CultureInfo.CurrentCulture);
            SuggestedBusinessDateDisplay = row.SuggestedBusinessDate.ToString("dd/MM/yyyy", CultureInfo.CurrentCulture);
            BusinessDateText = SuggestedBusinessDateDisplay;
            Description = row.Description;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
