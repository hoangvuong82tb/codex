using ASMILE.BO.App.Services;
using ASMILE.BO.App.Models;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class AssetManagementViewModel : ViewModelBase
{
    private readonly IFixedAssetService _fixedAssetService;
    private readonly IToolAssetService _toolAssetService;
    private readonly IReportEngine _reportEngine;
    private readonly INotificationService _notificationService;
    private readonly IDialogService _dialogService;
    private readonly ICurrentUserService _currentUserService;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private CancellationTokenSource? _refreshCts;
    private CancellationTokenSource? _filterDebounceCts;
    private CancellationTokenSource? _reportCts;

    public ObservableCollection<FixedAssetDto> FixedAssets { get; } = new();
    public ObservableCollection<FixedAssetCategoryDto> FixedAssetCategories { get; } = new();
    public ObservableCollection<FixedAssetDepreciationDto> Depreciations { get; } = new();
    public ObservableCollection<FixedAssetScheduleDto> DepreciationSchedule { get; } = new();
    public ObservableCollection<FixedAssetAdjustmentDto> Adjustments { get; } = new();
    public ObservableCollection<ToolAssetDto> ToolAssets { get; } = new();
    public ObservableCollection<ToolAssetCategoryDto> ToolAssetCategories { get; } = new();
    public ObservableCollection<ToolAllocationScheduleDto> AllocationSchedules { get; } = new();
    public ObservableCollection<AssetValidationMessage> ValidationMessages { get; } = new();
    public ObservableCollection<AssetReportRow> ReportRows { get; } = new();
    public ObservableCollection<ReportExecutionDto> ReportHistory { get; } = new();
    public ObservableCollection<EnterpriseReportOption> ReportCodes { get; } = new()
    {
        new("FIXED_ASSET_REGISTER", "Sổ tài sản cố định"),
        new("DEPRECIATION_SCHEDULE", "Bảng tính khấu hao"),
        new("ASSET_INCREASE_DECREASE", "Tăng giảm tài sản"),
        new("TOOL_ASSET_REGISTER", "Sổ công cụ dụng cụ"),
        new("TOOL_ASSET_ALLOCATION", "Phân bổ CCDC"),
        new("PREPAID_EXPENSE_SCHEDULE", "Chi phí trả trước")
    };
    public ObservableCollection<string> ExportFormats { get; } = new() { "Excel", "PDF", "CSV" };
    public ObservableCollection<EnterpriseReportOption> FavoriteReports { get; } = new();
    public ObservableCollection<AssetAdjustmentOption> AdjustmentTypes { get; } = new()
    {
        new("Tăng nguyên giá", "Increase"),
        new("Giảm nguyên giá", "Decrease")
    };

    [ObservableProperty] private FixedAssetDto? selectedFixedAsset;
    [ObservableProperty] private FixedAssetCategoryDto? selectedFixedAssetCategory;
    [ObservableProperty] private FixedAssetDepreciationDto? selectedDepreciation;
    [ObservableProperty] private ToolAssetDto? selectedToolAsset;
    [ObservableProperty] private ToolAssetCategoryDto? selectedToolAssetCategory;
    [ObservableProperty] private ToolAllocationScheduleDto? selectedAllocationSchedule;
    [ObservableProperty] private DateTime? fromDate = DateTime.Today.AddMonths(-1);
    [ObservableProperty] private DateTime? toDate = DateTime.Today;
    [ObservableProperty] private string keyword = string.Empty;
    [ObservableProperty] private string periodCode = DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    [ObservableProperty] private string selectedReportCode = "FIXED_ASSET_REGISTER";
    [ObservableProperty] private string selectedExportFormat = "Excel";
    [ObservableProperty] private bool isReportLoading;
    [ObservableProperty] private int selectedTabIndex;
    [ObservableProperty] private decimal totalOriginalCost;
    [ObservableProperty] private decimal totalAccumulatedDepreciation;
    [ObservableProperty] private decimal totalRemainingValue;
    [ObservableProperty] private decimal totalToolOriginalCost;
    [ObservableProperty] private decimal totalAllocatedAmount;
    [ObservableProperty] private decimal totalToolRemainingAmount;
    [ObservableProperty] private string postingVisibilityText = "Chọn tài sản hoặc CCDC để xem bút toán và nguồn chứng từ.";
    [ObservableProperty] private string statusText = "Quy trình TSCĐ/CCDC dùng dữ liệu đã lưu.";
    [ObservableProperty] private QuickFixedAssetEntry quickFixedAsset = new();
    [ObservableProperty] private QuickToolAssetEntry quickToolAsset = new();
    [ObservableProperty] private DateTime lastRefreshAt = DateTime.Now;

    public string LastRefreshText => $"Cập nhật {LastRefreshAt:HH:mm:ss}";
    public string FixedAssetValueText => $"{TotalRemainingValue:N0}";
    public string ToolAssetValueText => $"{TotalToolRemainingAmount:N0}";

    public AssetManagementViewModel(
        IFixedAssetService fixedAssetService,
        IToolAssetService toolAssetService,
        IReportEngine reportEngine,
        INotificationService notificationService,
        IDialogService dialogService,
        ICurrentUserService currentUserService)
    {
        _fixedAssetService = fixedAssetService;
        _toolAssetService = toolAssetService;
        _reportEngine = reportEngine;
        _notificationService = notificationService;
        _dialogService = dialogService;
        _currentUserService = currentUserService;
        FavoriteReports.ResetWith(ReportCodes.Where(x => x.Code is "FIXED_ASSET_REGISTER" or "TOOL_ASSET_ALLOCATION"));
        _ = RefreshAsync();
    }

    partial void OnKeywordChanged(string value) => QueueFilterRefresh();
    partial void OnFromDateChanged(DateTime? value) => QueueFilterRefresh();
    partial void OnToDateChanged(DateTime? value) => QueueFilterRefresh();
    partial void OnSelectedReportCodeChanged(string value) => _ = RefreshReportHistoryAsync(value, CancellationToken.None);

    partial void OnSelectedFixedAssetChanged(FixedAssetDto? value)
    {
        SelectedFixedAssetCategory = value is null ? SelectedFixedAssetCategory : FixedAssetCategories.FirstOrDefault(x => x.Id == value.CategoryId) ?? SelectedFixedAssetCategory;
        PostingVisibilityText = value is null
            ? "Chọn TSCĐ để xem bút toán và nguồn chứng từ."
            : $"TSCĐ -> bút toán {FormatGuid(value.JournalHeaderId)} | nguồn FixedAsset {value.AssetCode} | trạng thái {value.Status} | phương pháp {value.DepreciationMethod}.";
        if (!IsBusy)
        {
            _ = RefreshFixedAssetDetailsAsync(value?.Id);
        }
    }

    partial void OnSelectedToolAssetChanged(ToolAssetDto? value)
    {
        SelectedToolAssetCategory = value is null ? SelectedToolAssetCategory : ToolAssetCategories.FirstOrDefault(x => x.Id == value.CategoryId) ?? SelectedToolAssetCategory;
        PostingVisibilityText = value is null
            ? "Chọn CCDC để xem bút toán và nguồn chứng từ."
            : $"CCDC -> bút toán {FormatGuid(value.JournalHeaderId)} | nguồn ToolAsset {value.ToolCode} | trạng thái {value.Status} | phân bổ {value.AllocatedAmount:N0}/{value.OriginalCost:N0}.";
        if (!IsBusy)
        {
            _ = RefreshToolAssetDetailsAsync(value?.Id);
        }
    }

    partial void OnSelectedDepreciationChanged(FixedAssetDepreciationDto? value)
    {
        if (value is not null)
        {
            PostingVisibilityText = $"Khấu hao -> bút toán {FormatGuid(value.JournalHeaderId)} | nguồn FixedAssetDepreciation {value.AssetCode}-{value.DepreciationPeriod} | trạng thái {value.Status}.";
        }
    }

    partial void OnSelectedAllocationScheduleChanged(ToolAllocationScheduleDto? value)
    {
        if (value is not null)
        {
            PostingVisibilityText = $"Phân bổ -> bút toán {FormatGuid(value.JournalHeaderId)} | nguồn ToolAllocation {value.ToolCode}-{value.AllocationPeriod} | trạng thái {value.Status}.";
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        _refreshCts?.Cancel();
        _refreshCts = new CancellationTokenSource();
        var token = _refreshCts.Token;
        await _operationGate.WaitAsync(token);
        IsBusy = true;
        try
        {
            await LoadCoreAsync(token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Làm mới", ex.Message);
            StatusText = "Không tải được quy trình TSCĐ/CCDC.";
        }
        finally
        {
            IsBusy = false;
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task CreateFixedAssetAsync()
    {
        if (SelectedFixedAssetCategory is null)
        {
            AddValidation("Error", "TSCĐ", "Chọn nhóm TSCĐ trước khi tạo nhanh.");
            return;
        }

        await RunWorkflowActionAsync("Tạo TSCĐ", token => _fixedAssetService.CreateAssetAsync(new CreateFixedAssetRequest
        {
            AssetCode = QuickFixedAsset.AssetCode,
            AssetName = QuickFixedAsset.AssetName,
            CategoryId = SelectedFixedAssetCategory.Id,
            PurchaseDate = QuickFixedAsset.PurchaseDate,
            StartDepreciationDate = QuickFixedAsset.StartDepreciationDate,
            OriginalCost = QuickFixedAsset.OriginalCost,
            SalvageValue = QuickFixedAsset.SalvageValue,
            UsefulLifeMonths = QuickFixedAsset.UsefulLifeMonths,
            Quantity = 1,
            Notes = QuickFixedAsset.Notes,
            CounterAccountCode = QuickFixedAsset.CounterAccountCode,
            ActivateImmediately = false,
            CreatedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task ApproveFixedAssetAsync() => await PostFixedAssetAsync();

    [RelayCommand]
    private async Task PostFixedAssetAsync()
    {
        if (SelectedFixedAsset is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Ghi sổ TSCĐ", token => _fixedAssetService.ActivateAssetAsync(SelectedFixedAsset.Id, QuickFixedAsset.CounterAccountCode, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task CancelFixedAssetApprovalAsync() => await ReverseFixedAssetAsync();

    [RelayCommand]
    private async Task ReverseFixedAssetAsync()
    {
        if (SelectedFixedAsset is null || !_dialogService.Confirm("Đảo ghi tăng TSCĐ đã chọn?", "Đảo bút toán TSCĐ"))
        {
            return;
        }

        await RunWorkflowActionAsync("Đảo bút toán TSCĐ", token => _fixedAssetService.ReverseAssetAsync(SelectedFixedAsset.Id, "Reverse from Asset workflow", CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task DisposeFixedAssetAsync()
    {
        if (SelectedFixedAsset is null || !_dialogService.Confirm("Ghi giảm/thanh lý TSCĐ đã chọn?", "Ghi giảm TSCĐ"))
        {
            return;
        }

        await RunWorkflowActionAsync("Ghi giảm TSCĐ", token => _fixedAssetService.DisposeAssetAsync(new DisposeFixedAssetRequest
        {
            FixedAssetId = SelectedFixedAsset.Id,
            DisposalDate = DateTime.Today,
            DisposalAmount = SelectedFixedAsset.RemainingValue,
            Notes = "Dispose from Asset workflow",
            DisposedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task AdjustFixedAssetAsync()
    {
        if (SelectedFixedAsset is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Điều chỉnh TSCĐ", token => _fixedAssetService.AdjustAssetAsync(new AdjustFixedAssetRequest
        {
            FixedAssetId = SelectedFixedAsset.Id,
            AdjustmentDate = DateTime.Today,
            AdjustmentType = QuickFixedAsset.AdjustmentType.Value,
            Amount = QuickFixedAsset.AdjustmentAmount,
            Description = QuickFixedAsset.Notes,
            AdjustedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task PreviewDepreciationAsync()
    {
        await RunWorkflowActionAsync("Xem trước khấu hao", token => _fixedAssetService.RunDepreciationAsync(new DepreciationRunRequest
        {
            PeriodCode = PeriodCode,
            DepreciationDate = ToDate ?? DateTime.Today,
            FixedAssetId = SelectedFixedAsset?.Id,
            PostImmediately = false,
            CreatedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task PostSelectedDepreciationAsync()
    {
        if (SelectedDepreciation is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Ghi sổ khấu hao", token => _fixedAssetService.PostDepreciationAsync(SelectedDepreciation.Id, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task ReverseDepreciationAsync()
    {
        if (SelectedDepreciation is null || !_dialogService.Confirm("Đảo dòng khấu hao đã chọn?", "Đảo khấu hao"))
        {
            return;
        }

        await RunWorkflowActionAsync("Đảo khấu hao", token => _fixedAssetService.ReverseDepreciationAsync(SelectedDepreciation.Id, "Reverse from Asset workflow", CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task BatchDepreciationAsync()
    {
        await RunWorkflowActionAsync("Chạy khấu hao hàng loạt", token => _fixedAssetService.RunDepreciationAsync(new DepreciationRunRequest
        {
            PeriodCode = PeriodCode,
            DepreciationDate = ToDate ?? DateTime.Today,
            FixedAssetId = null,
            PostImmediately = true,
            CreatedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task CreateToolAssetAsync()
    {
        if (SelectedToolAssetCategory is null)
        {
            AddValidation("Error", "CCDC", "Chọn nhóm CCDC trước khi tạo nhanh.");
            return;
        }

        await RunWorkflowActionAsync("Tạo CCDC", token => _toolAssetService.CreateToolAsync(new CreateToolAssetRequest
        {
            ToolCode = QuickToolAsset.ToolCode,
            ToolName = QuickToolAsset.ToolName,
            CategoryId = SelectedToolAssetCategory.Id,
            PurchaseDate = QuickToolAsset.PurchaseDate,
            OriginalCost = QuickToolAsset.OriginalCost,
            Quantity = QuickToolAsset.Quantity,
            AllocationMonths = QuickToolAsset.AllocationMonths,
            Notes = QuickToolAsset.Notes,
            CreatedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task IssueToolAssetAsync()
    {
        if (SelectedToolAsset is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Xuất dùng CCDC", token => _toolAssetService.IssueToolAsync(new IssueToolRequest
        {
            ToolAssetId = SelectedToolAsset.Id,
            IssueDate = DateTime.Today,
            Quantity = QuickToolAsset.IssueQuantity <= 0 ? SelectedToolAsset.Quantity : QuickToolAsset.IssueQuantity,
            Description = QuickToolAsset.Notes,
            PostedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task RecoverToolAssetAsync()
    {
        if (SelectedToolAsset is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Thu hồi CCDC", token => _toolAssetService.RecoverToolAsync(SelectedToolAsset.Id, QuickToolAsset.IssueQuantity <= 0 ? SelectedToolAsset.Quantity : QuickToolAsset.IssueQuantity, QuickToolAsset.Notes, token));
    }

    [RelayCommand]
    private async Task DisposeToolAssetAsync()
    {
        if (SelectedToolAsset is null || !_dialogService.Confirm("Thanh lý CCDC đã chọn?", "Ghi giảm CCDC"))
        {
            return;
        }

        await RunWorkflowActionAsync("Ghi giảm CCDC", token => _toolAssetService.DisposeToolAsync(SelectedToolAsset.Id, QuickToolAsset.IssueQuantity <= 0 ? SelectedToolAsset.Quantity : QuickToolAsset.IssueQuantity, QuickToolAsset.Notes, token));
    }

    [RelayCommand]
    private async Task PreviewAllocationAsync()
    {
        if (SelectedToolAsset is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Xem lịch phân bổ", token => _toolAssetService.BuildAllocationScheduleAsync(SelectedToolAsset.Id, token));
    }

    [RelayCommand]
    private async Task RunAllocationAsync()
    {
        await RunWorkflowActionAsync("Chạy phân bổ", token => _toolAssetService.RunAllocationAsync(new AllocationRunRequest
        {
            AllocationPeriod = PeriodCode,
            AllocationDate = ToDate ?? DateTime.Today,
            ToolAssetId = SelectedToolAsset?.Id,
            PostedBy = CurrentUserName()
        }, token));
    }

    [RelayCommand]
    private async Task PostSelectedAllocationAsync()
    {
        if (SelectedAllocationSchedule is null)
        {
            return;
        }

        await RunWorkflowActionAsync("Ghi sổ phân bổ", token => _toolAssetService.PostAllocationAsync(SelectedAllocationSchedule.Id, CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task ReverseAllocationAsync()
    {
        if (SelectedAllocationSchedule is null || !_dialogService.Confirm("Đảo dòng phân bổ CCDC đã chọn?", "Đảo phân bổ CCDC"))
        {
            return;
        }

        await RunWorkflowActionAsync("Đảo phân bổ CCDC", token => _toolAssetService.ReverseAllocationAsync(SelectedAllocationSchedule.Id, "Reverse from Asset workflow", CurrentUserName(), token));
    }

    [RelayCommand]
    private async Task RunReportAsync()
    {
        _reportCts?.Cancel();
        _reportCts = new CancellationTokenSource();
        var token = _reportCts.Token;
        IsReportLoading = true;
        var gateAcquired = false;
        try
        {
            await _operationGate.WaitAsync(token);
            gateAcquired = true;
            var result = await _reportEngine.ExecuteAsync(BuildReportRequest(), token);
            ReportRows.ResetWith(result.Rows.Select(x => new AssetReportRow(result.Columns, x)).Take(2000));
            await RefreshReportHistoryAsync(SelectedReportCode, token);
            StatusText = $"Báo cáo {result.ReportName}: {result.RowCount:N0} dòng.";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Đã hủy báo cáo TSCĐ/CCDC.";
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Báo cáo", ex.Message);
        }
        finally
        {
            if (gateAcquired)
            {
                _operationGate.Release();
            }
            IsReportLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportReportAsync()
    {
        _reportCts?.Cancel();
        _reportCts = new CancellationTokenSource();
        var token = _reportCts.Token;
        IsReportLoading = true;
        var gateAcquired = false;
        try
        {
            await _operationGate.WaitAsync(token);
            gateAcquired = true;
            var request = BuildReportRequest();
            var format = SelectedExportFormat.ToLowerInvariant();
            var bytes = format switch
            {
                "excel" => await _reportEngine.ExportExcelAsync(request, token),
                "pdf" => await _reportEngine.ExportPdfAsync(request, token),
                "csv" => await _reportEngine.ExportCsvAsync(request, token),
                _ => await _reportEngine.ExportExcelAsync(request, token)
            };
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ASMILE BO", "Reports");
            Directory.CreateDirectory(folder);
            var extension = format == "excel" ? "xlsx" : format;
            var path = Path.Combine(folder, $"{SelectedReportCode}_{DateTime.Now:yyyyMMdd_HHmmss}.{extension}");
            await File.WriteAllBytesAsync(path, bytes, token);
            _notificationService.Show($"Đã xuất báo cáo {Path.GetFileName(path)}.");
            await RefreshReportHistoryAsync(SelectedReportCode, token);
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Báo cáo", ex.Message);
        }
        finally
        {
            if (gateAcquired)
            {
                _operationGate.Release();
            }
            IsReportLoading = false;
        }
    }

    [RelayCommand]
    private void ToggleFavoriteReport()
    {
        var option = ReportCodes.FirstOrDefault(x => x.Code == SelectedReportCode);
        if (option is null)
        {
            return;
        }

        var existing = FavoriteReports.FirstOrDefault(x => x.Code == SelectedReportCode);
        if (existing is not null)
        {
            FavoriteReports.Remove(existing);
        }
        else
        {
            FavoriteReports.Add(option);
        }
    }

    [RelayCommand]
    private void OpenSelectedDrilldown()
    {
        if (SelectedTabIndex <= 1 && SelectedFixedAsset is not null)
        {
            PostingVisibilityText = $"TSCĐ -> bút toán/giá trị/khấu hao | {SelectedFixedAsset.AssetCode} | bút toán {FormatGuid(SelectedFixedAsset.JournalHeaderId)} | khấu hao {Depreciations.Count:N0} | điều chỉnh {Adjustments.Count:N0}.";
            return;
        }

        if (SelectedToolAsset is not null)
        {
            PostingVisibilityText = $"CCDC -> bút toán/phân bổ/nguồn | {SelectedToolAsset.ToolCode} | bút toán {FormatGuid(SelectedToolAsset.JournalHeaderId)} | dòng phân bổ {AllocationSchedules.Count:N0}.";
        }
    }

    public void PasteFixedAssetRows(string clipboardText)
    {
        var first = ParseClipboardRows(clipboardText).FirstOrDefault();
        if (first is null)
        {
            return;
        }

        if (first.Count > 0) QuickFixedAsset.AssetCode = first[0];
        if (first.Count > 1) QuickFixedAsset.AssetName = first[1];
        if (first.Count > 2 && decimal.TryParse(first[2], NumberStyles.Any, CultureInfo.CurrentCulture, out var cost)) QuickFixedAsset.OriginalCost = cost;
        AddValidation("Info", "Paste", "Đã đọc dòng Excel vào quick entry TSCĐ.");
    }

    public void PasteToolAssetRows(string clipboardText)
    {
        var first = ParseClipboardRows(clipboardText).FirstOrDefault();
        if (first is null)
        {
            return;
        }

        if (first.Count > 0) QuickToolAsset.ToolCode = first[0];
        if (first.Count > 1) QuickToolAsset.ToolName = first[1];
        if (first.Count > 2 && decimal.TryParse(first[2], NumberStyles.Any, CultureInfo.CurrentCulture, out var cost)) QuickToolAsset.OriginalCost = cost;
        AddValidation("Info", "Paste", "Đã đọc dòng Excel vào quick entry CCDC.");
    }

    private async Task LoadCoreAsync(CancellationToken token)
    {
        ValidationMessages.Clear();
        var fixedCategories = await _fixedAssetService.GetCategoriesAsync(token);
        var toolCategories = await _toolAssetService.GetCategoriesAsync(token);
        var fixedAssets = await _fixedAssetService.GetAssetsAsync(token);
        var toolAssets = await _toolAssetService.GetToolsAsync(token);
        var fixedSummary = await _fixedAssetService.GetSummaryAsync(token);
        var toolSummary = await _toolAssetService.GetSummaryAsync(token);

        FixedAssetCategories.ResetWith(fixedCategories);
        ToolAssetCategories.ResetWith(toolCategories);
        FixedAssets.ResetWith(ApplyAssetFilter(fixedAssets));
        ToolAssets.ResetWith(ApplyToolFilter(toolAssets));
        SelectedFixedAssetCategory ??= FixedAssetCategories.FirstOrDefault(x => x.IsActive) ?? FixedAssetCategories.FirstOrDefault();
        SelectedToolAssetCategory ??= ToolAssetCategories.FirstOrDefault(x => x.IsActive) ?? ToolAssetCategories.FirstOrDefault();
        SelectedFixedAsset ??= FixedAssets.FirstOrDefault();
        SelectedToolAsset ??= ToolAssets.FirstOrDefault();

        TotalOriginalCost = fixedSummary.TotalOriginalCost;
        TotalAccumulatedDepreciation = fixedSummary.TotalAccumulatedDepreciation;
        TotalRemainingValue = fixedSummary.TotalRemainingValue;
        TotalToolOriginalCost = toolSummary.TotalOriginalCost;
        TotalAllocatedAmount = toolSummary.TotalAllocatedAmount;
        TotalToolRemainingAmount = toolSummary.TotalRemainingAmount;

        await LoadFixedAssetDetailsAsync(SelectedFixedAsset?.Id, token);
        await LoadToolAssetDetailsAsync(SelectedToolAsset?.Id, token);
        BuildInlineValidation();
        await RefreshReportHistoryAsync(SelectedReportCode, token);
        LastRefreshAt = DateTime.Now;
        StatusText = $"Đã tải {FixedAssets.Count:N0} TSCĐ và {ToolAssets.Count:N0} CCDC.";
        OnPropertyChanged(nameof(LastRefreshText));
        OnPropertyChanged(nameof(FixedAssetValueText));
        OnPropertyChanged(nameof(ToolAssetValueText));
    }

    private async Task LoadFixedAssetDetailsAsync(Guid? assetId, CancellationToken cancellationToken = default)
    {
        try
        {
            Depreciations.ResetWith(await _fixedAssetService.GetDepreciationsAsync(assetId, 300, cancellationToken));
            Adjustments.ResetWith(await _fixedAssetService.GetAdjustmentsAsync(assetId, 100, cancellationToken));
            if (assetId.HasValue)
            {
                DepreciationSchedule.ResetWith((await _fixedAssetService.GetDepreciationScheduleAsync(assetId.Value, cancellationToken)).Take(300));
            }
            else
            {
                DepreciationSchedule.Clear();
            }
            SelectedDepreciation ??= Depreciations.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Khấu hao", ex.Message);
        }
    }

    private async Task RefreshFixedAssetDetailsAsync(Guid? assetId)
    {
        await _operationGate.WaitAsync();
        try
        {
            await LoadFixedAssetDetailsAsync(assetId);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task LoadToolAssetDetailsAsync(Guid? toolAssetId, CancellationToken cancellationToken = default)
    {
        try
        {
            AllocationSchedules.ResetWith(await _toolAssetService.GetAllocationSchedulesAsync(toolAssetId, 300, cancellationToken));
            SelectedAllocationSchedule ??= AllocationSchedules.FirstOrDefault();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            AddValidation("Error", "Phân bổ", ex.Message);
        }
    }

    private async Task RefreshToolAssetDetailsAsync(Guid? toolAssetId)
    {
        await _operationGate.WaitAsync();
        try
        {
            await LoadToolAssetDetailsAsync(toolAssetId);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private IEnumerable<FixedAssetDto> ApplyAssetFilter(IEnumerable<FixedAssetDto> values)
    {
        var query = values;
        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            query = query.Where(x => x.AssetCode.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.AssetName.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
                                     || (x.CategoryName?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query.OrderBy(x => x.AssetCode);
    }

    private IEnumerable<ToolAssetDto> ApplyToolFilter(IEnumerable<ToolAssetDto> values)
    {
        var query = values;
        if (!string.IsNullOrWhiteSpace(Keyword))
        {
            query = query.Where(x => x.ToolCode.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
                                     || x.ToolName.Contains(Keyword, StringComparison.OrdinalIgnoreCase)
                                     || (x.CategoryName?.Contains(Keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        return query.OrderBy(x => x.ToolCode);
    }

    private void QueueFilterRefresh()
    {
        _filterDebounceCts?.Cancel();
        _filterDebounceCts = new CancellationTokenSource();
        var token = _filterDebounceCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(350, token);
                await App.Current.Dispatcher.InvokeAsync(async () => await RefreshAsync());
            }
            catch (OperationCanceledException)
            {
            }
        }, token);
    }

    private async Task RunWorkflowActionAsync<T>(string scope, Func<CancellationToken, Task<T>> action)
        => await RunWorkflowActionAsync(scope, async token => _ = await action(token));

    private async Task RunWorkflowActionAsync(string scope, Func<CancellationToken, Task> action)
    {
        _refreshCts?.Cancel();
        using var cts = new CancellationTokenSource();
        var token = cts.Token;
        await _operationGate.WaitAsync(token);
        IsBusy = true;
        try
        {
            await action(token);
            _notificationService.Show($"{scope} hoàn tất.");
            await LoadCoreAsync(token);
        }
        catch (Exception ex)
        {
            AddValidation("Error", scope, ex.Message);
        }
        finally
        {
            IsBusy = false;
            _operationGate.Release();
        }
    }

    private void BuildInlineValidation()
    {
        ValidationMessages.Clear();
        if (FixedAssets.GroupBy(x => x.AssetCode).Any(x => x.Count() > 1))
        {
            AddValidation("Warning", "TSCĐ", "Có mã TSCĐ bị trùng trong dữ liệu lọc.");
        }
        if (ToolAssets.GroupBy(x => x.ToolCode).Any(x => x.Count() > 1))
        {
            AddValidation("Warning", "CCDC", "Có mã CCDC bị trùng trong dữ liệu lọc.");
        }
        if (FixedAssets.Any(x => x.Status is "Draft" && x.OriginalCost <= 0))
        {
            AddValidation("Error", "TSCĐ", "Có TSCĐ nháp thiếu nguyên giá hợp lệ.");
        }
        if (ToolAssets.Any(x => x.Status is "Allocating" && x.RemainingAmount < 0))
        {
            AddValidation("Error", "CCDC", "Có CCDC phân bổ âm giá trị còn lại.");
        }
        if (Depreciations.Any(x => x.Status == "Draft"))
        {
            AddValidation("Info", "Khấu hao", "Có dòng khấu hao nháp chờ ghi sổ.");
        }
        if (AllocationSchedules.Any(x => x.Status == "Draft"))
        {
            AddValidation("Info", "Phân bổ", "Có dòng phân bổ CCDC nháp chờ ghi sổ.");
        }
    }

    private async Task RefreshReportHistoryAsync(string? reportCode, CancellationToken cancellationToken)
    {
        var history = await _reportEngine.GetExecutionHistoryAsync(reportCode, 20, cancellationToken);
        ReportHistory.ResetWith(history);
    }

    private ReportRequestDto BuildReportRequest()
    {
        var parameters = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["fromDate"] = (FromDate ?? DateTime.Today.AddMonths(-1)).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["toDate"] = (ToDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["period"] = PeriodCode
        };

        return new ReportRequestDto
        {
            ReportCode = SelectedReportCode,
            ExecutedBy = CurrentUserName(),
            Parameters = parameters
        };
    }

    private static IEnumerable<IReadOnlyList<string>> ParseClipboardRows(string clipboardText)
    {
        return clipboardText
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => (IReadOnlyList<string>)line.Split('\t').Select(x => x.Trim()).ToList());
    }

    private static string FormatGuid(Guid? id) => id?.ToString() ?? "chưa có";

    private void AddValidation(string severity, string scope, string message)
    {
        ValidationMessages.Insert(0, new AssetValidationMessage(LocalizeSeverity(severity), LocalizeScope(scope), message));
        while (ValidationMessages.Count > 24)
        {
            ValidationMessages.RemoveAt(ValidationMessages.Count - 1);
        }
    }

    private static string LocalizeSeverity(string severity)
        => severity switch
        {
            "Error" => "Lỗi",
            "Warning" => "Cảnh báo",
            "Info" => "Thông tin",
            _ => severity
        };

    private static string LocalizeScope(string scope)
        => scope switch
        {
            "Refresh" => "Làm mới",
            "Depreciation" => "Khấu hao",
            "Allocation" => "Phân bổ",
            "Paste" => "Dán dữ liệu",
            _ => scope
        };

    private string CurrentUserName()
        => _currentUserService.CurrentUser?.DisplayName ?? "asset-ui";
}

public sealed partial class QuickFixedAssetEntry : ObservableObject
{
    [ObservableProperty] private string assetCode = $"TSCD-{DateTime.Now:yyyyMMddHHmm}";
    [ObservableProperty] private string assetName = string.Empty;
    [ObservableProperty] private DateTime purchaseDate = DateTime.Today;
    [ObservableProperty] private DateTime startDepreciationDate = DateTime.Today;
    [ObservableProperty] private decimal originalCost;
    [ObservableProperty] private decimal salvageValue;
    [ObservableProperty] private int usefulLifeMonths = 36;
    [ObservableProperty] private string counterAccountCode = string.Empty;
    [ObservableProperty] private AssetAdjustmentOption adjustmentType = new("Tăng nguyên giá", "Increase");
    [ObservableProperty] private decimal adjustmentAmount;
    [ObservableProperty] private string notes = string.Empty;
}

public sealed partial class QuickToolAssetEntry : ObservableObject
{
    [ObservableProperty] private string toolCode = $"CCDC-{DateTime.Now:yyyyMMddHHmm}";
    [ObservableProperty] private string toolName = string.Empty;
    [ObservableProperty] private DateTime purchaseDate = DateTime.Today;
    [ObservableProperty] private decimal originalCost;
    [ObservableProperty] private decimal quantity = 1;
    [ObservableProperty] private decimal issueQuantity = 1;
    [ObservableProperty] private int allocationMonths = 12;
    [ObservableProperty] private string notes = string.Empty;
}

public sealed record AssetValidationMessage(string Severity, string Scope, string Message);

public sealed record AssetAdjustmentOption(string Label, string Value);

public sealed class AssetReportRow
{
    public AssetReportRow(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row)
    {
        Column1 = Read(columns, row, 0);
        Column2 = Read(columns, row, 1);
        Column3 = Read(columns, row, 2);
        Column4 = Read(columns, row, 3);
        Amount1 = ReadAny(columns, row, "OriginalCost", "Amount");
        Amount2 = ReadAny(columns, row, "RemainingValue", "RemainingAmount", "RemainingAfter");
        Status = ReadAny(columns, row, "Status", "JournalStatus");
        JournalStatus = Read(columns, row, "JournalStatus");
    }

    public string Column1 { get; }
    public string Column2 { get; }
    public string Column3 { get; }
    public string Column4 { get; }
    public string Amount1 { get; }
    public string Amount2 { get; }
    public string Status { get; }
    public string JournalStatus { get; }

    private static string Read(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row, int index)
        => index >= 0 && index < columns.Count ? Read(columns, row, columns[index].Name) : string.Empty;

    private static string ReadAny(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row, params string[] names)
    {
        foreach (var name in names)
        {
            var value = Read(columns, row, name);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string Read(IReadOnlyList<ReportColumnDto> columns, ReportRowDto row, string name)
    {
        var column = columns.FirstOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
        if (column is null || !row.Values.TryGetValue(column.Name, out var value) || value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            DateTime date => date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            decimal dec => dec.ToString("N0", CultureInfo.InvariantCulture),
            double dbl => dbl.ToString("N2", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }
}

file static class AssetCollectionExtensions
{
    public static void ResetWith<T>(this ObservableCollection<T> collection, IEnumerable<T> values)
    {
        collection.Clear();
        foreach (var value in values)
        {
            collection.Add(value);
        }
    }
}
