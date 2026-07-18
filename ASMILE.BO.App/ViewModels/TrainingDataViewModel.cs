using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Media;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class TrainingDataViewModel : ViewModelBase, IDisposable
{
    private readonly ITrainingDataService _trainingDataService;
    private readonly CancellationTokenSource _disposeCts = new();
    private CancellationTokenSource? _runCts;
    private TrainingDataOperationResult? _lastOperationResult;

    public ObservableCollection<TrainingDataStepResult> StepLogs { get; } = new();
    public ObservableCollection<TrainingDataOperationResult> RunHistory { get; } = new();
    public ObservableCollection<TrainingDataSeedProfileDto> SeedProfiles { get; } = new();
    public ObservableCollection<TrainingDataSafetyWarningDto> PreflightWarnings { get; } = new();
    public ObservableCollection<TrainingDataDemoProfileDto> DemoProfiles { get; } = new();
    public ObservableCollection<ProfileCardState> ProfileCards { get; } = new();
    public ObservableCollection<PreviewStatCardItem> PreviewStatCards { get; } = new();
    public ObservableCollection<SafetyChecklistItem> SafetyChecklistItems { get; } = new();
    public ObservableCollection<AlertItem> AlertItems { get; } = new();
    public ObservableCollection<StatusBadgeItem> StatusBadges { get; } = new();
    public ObservableCollection<CompletionSummaryItem> CompletionSummary { get; } = new();

    [ObservableProperty] private TrainingDataStatusDto? status;
    [ObservableProperty] private TrainingDataPreflightDto? preflight;
    [ObservableProperty] private TrainingDataSeedProfileDto? selectedSeedProfile;
    [ObservableProperty] private TrainingDataDemoProfileDto? selectedDemoProfile;
    [ObservableProperty] private string confirmationText = string.Empty;
    [ObservableProperty] private string statusMessage = "Sẵn sàng";
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string errorText = string.Empty;
    [ObservableProperty] private string completionMessage = string.Empty;
    [ObservableProperty] private bool hasCompletion;
    [ObservableProperty] private string demoPreviewDescription = string.Empty;

    public string TitleText => "Onboarding dữ liệu mẫu";
    public string SubtitleText => "Khởi tạo dữ liệu demo cho nhà hàng và khách sạn";
    public string FullResetConfirmationHint => "RESET ALL BUSINESS DATA";
    public string TrainingOnlyConfirmationHint => "RESET TRAINING DATA";
    public string SelectedProfileDisplayName => SelectedDemoProfile?.DisplayName ?? "Chưa chọn mô hình";
    public bool HasWarnings => AlertItems.Count > 0;
    public bool HasStepLogs => StepLogs.Count > 0;
    public bool HasNoStepLogs => !HasStepLogs;
    public bool HasCompletionResult => HasCompletion && !string.IsNullOrWhiteSpace(CompletionMessage);
    public bool IsMarkerSchemaMissing => PreflightWarnings.Any(x => x.Code == "TRAINING_MARKER_TABLE_MISSING");
    public bool HasTrainingMarkers => (Preflight?.TrainingMarkerCount ?? 0) > 0;
    public bool CanOpenDashboard => false;
    public bool CanOpenLedger => false;
    public bool CanOpenInventory => false;
    public bool CanOpenDebt => false;
    public string QuickActionUnavailableHint => "Module chưa khả dụng";
    public string EnvironmentBadgeText => Preflight?.EnvironmentName ?? "Unknown";
    public string DatabaseBadgeText => Preflight?.ProviderName ?? "Unknown";
    public string MarkerStatusText => HasTrainingMarkers ? "Có marker training" : "Không có marker training";
    public string LastResetText => RunHistory.FirstOrDefault() is { } run
        ? $"Lần reset gần nhất: {run.FinishedAtUtc:dd/MM/yyyy HH:mm}"
        : "Chưa có lần reset trước đó";
    public string JournalStatusText => $"Số journal hiện có: {Status?.JournalCount ?? 0}";
    public string RiskBadgeText => (Preflight?.RiskLevel ?? "Low").ToUpperInvariant();
    public Brush RiskBadgeBrush => ResolveRiskBrush(Preflight?.RiskLevel);
    public Brush RiskBadgeForeground => Brushes.White;
    public string DestructiveResetStateText => Preflight?.AllowDestructiveReset == true ? "AVAILABLE" : "LOCKED";
    public Brush DestructiveResetStateBrush => Preflight?.AllowDestructiveReset == true
        ? new SolidColorBrush(Color.FromRgb(15, 118, 110))
        : new SolidColorBrush(Color.FromRgb(107, 114, 128));
    public bool ShowMigrationWarningCard => IsMarkerSchemaMissing;
    public Visibility EmptyStateVisibility => HasNoStepLogs ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LogTableVisibility => HasStepLogs ? Visibility.Visible : Visibility.Collapsed;

    public TrainingDataViewModel(ITrainingDataService trainingDataService)
    {
        _trainingDataService = trainingDataService;
        _ = LoadAsync(_disposeCts.Token);
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(_disposeCts.Token);

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task CheckStatusAsync() => ExecuteAsync(new TrainingDataOperationRequest { OperationType = TrainingDataOperationType.CheckStatus, ConfirmationText = ConfirmationText });

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task PreflightCheckAsync() => ExecuteAsync(new TrainingDataOperationRequest { OperationType = TrainingDataOperationType.PreflightCheck, ConfirmationText = ConfirmationText });

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task ClearBusinessDataAsync() => ExecuteAsync(new TrainingDataOperationRequest { OperationType = TrainingDataOperationType.ClearBusinessData, ConfirmationText = ConfirmationText });

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task SeedFoundationAsync() => ExecuteAsync(new TrainingDataOperationRequest
    {
        OperationType = TrainingDataOperationType.SeedFoundation,
        SeedProfile = ResolveDemoSeedProfile(),
        ConfirmationText = ConfirmationText
    });

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task SeedFullTrainingAsync() => ExecuteAsync(new TrainingDataOperationRequest
    {
        OperationType = TrainingDataOperationType.SeedFullTraining,
        SeedProfile = ResolveDemoSeedProfile(),
        ConfirmationText = ConfirmationText
    });

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task DeleteTrainingDataAsync() => ExecuteAsync(new TrainingDataOperationRequest { OperationType = TrainingDataOperationType.DeleteTrainingData, ConfirmationText = ConfirmationText });

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task ResetToBlankAsync() => ExecuteAsync(new TrainingDataOperationRequest
    {
        OperationType = TrainingDataOperationType.ResetToBlank,
        ResetScope = TrainingDataResetScope.BusinessAndOperationalLogs,
        ConfirmationText = ConfirmationText
    });

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private Task ResetTrainingOnlyAsync() => ExecuteAsync(new TrainingDataOperationRequest
    {
        OperationType = TrainingDataOperationType.ResetTrainingOnly,
        SeedProfile = ResolveDemoSeedProfile(),
        ConfirmationText = ConfirmationText
    });

    [RelayCommand(CanExecute = nameof(CanCancel))]
    private void CancelOperation()
    {
        _runCts?.Cancel();
        StatusMessage = "Đang yêu cầu hủy thao tác...";
    }

    [RelayCommand(CanExecute = nameof(CanExecuteActions))]
    private async Task SelectDemoProfileAsync(object? profileToken)
    {
        TrainingDataDemoProfileDto? profile = profileToken switch
        {
            TrainingDataDemoProfileDto dto => dto,
            ProfileCardState card => DemoProfiles.FirstOrDefault(x => x.Profile == card.Profile),
            _ => null
        };

        if (profile is null)
        {
            return;
        }

        SelectedDemoProfile = profile;
        await LoadPreviewAsync(profile.Profile, _disposeCts.Token);
        RefreshProfileSelection();
    }

    private bool CanExecuteActions() => !IsBusy;
    private bool CanCancel() => IsBusy;

    private TrainingDataSeedProfile ResolveDemoSeedProfile()
        => SelectedDemoProfile?.Profile ?? SelectedSeedProfile?.Profile ?? TrainingDataSeedProfile.RestaurantHotelFullDemo;

    private async Task ExecuteAsync(TrainingDataOperationRequest request)
    {
        if (IsBusy) return;

        _runCts?.Cancel();
        _runCts?.Dispose();
        _runCts = CancellationTokenSource.CreateLinkedTokenSource(_disposeCts.Token);

        IsBusy = true;
        HasError = false;
        ErrorText = string.Empty;
        HasCompletion = false;
        CompletionMessage = string.Empty;
        NotifyCommands();
        try
        {
            var result = await _trainingDataService.ExecuteAsync(request, _runCts.Token);
            _lastOperationResult = result;

            StepLogs.Clear();
            foreach (var step in result.Steps.OrderBy(x => x.StepOrder))
            {
                StepLogs.Add(step);
            }

            StatusMessage = result.Success
                ? $"Hoàn tất: xóa {result.TotalDeletedCount}, tạo {result.TotalCreatedCount}."
                : $"Thất bại: {result.ErrorMessage}";

            if (!result.Success)
            {
                HasError = true;
                ErrorText = result.ErrorMessage ?? "Có lỗi khi xử lý dữ liệu mẫu.";
            }
            else if (request.OperationType is TrainingDataOperationType.SeedFoundation or TrainingDataOperationType.SeedFullTraining or TrainingDataOperationType.ResetToBlank)
            {
                HasCompletion = true;
                CompletionMessage = request.OperationType == TrainingDataOperationType.ResetToBlank
                    ? $"Đã reset trắng dữ liệu nghiệp vụ. Xóa {result.TotalDeletedCount} bản ghi."
                    : $"Đã khởi tạo dữ liệu demo {SelectedProfileDisplayName.ToLowerInvariant()}.";
            }

            BuildCompletionSummary();
            await LoadStatusAndHistoryAsync(_runCts.Token);
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Đã hủy thao tác.";
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorText = ex.Message;
            StatusMessage = "Thao tác thất bại.";
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
            NotifyComputedState();
        }
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        NotifyCommands();
        try
        {
            SeedProfiles.Clear();
            foreach (var profile in await _trainingDataService.GetSeedProfilesAsync(cancellationToken))
            {
                SeedProfiles.Add(profile);
            }

            DemoProfiles.Clear();
            foreach (var demo in await _trainingDataService.GetDemoProfilesAsync(cancellationToken))
            {
                DemoProfiles.Add(demo);
            }

            SelectedDemoProfile = DemoProfiles.FirstOrDefault(x => x.Profile == TrainingDataSeedProfile.RestaurantHotelFullDemo) ?? DemoProfiles.FirstOrDefault();
            SelectedSeedProfile = SeedProfiles.FirstOrDefault(x => x.Profile == TrainingDataSeedProfile.RestaurantHotelFullDemo)
                ?? SeedProfiles.FirstOrDefault(x => x.Profile == TrainingDataSeedProfile.FullTraining)
                ?? SeedProfiles.FirstOrDefault();

            await BuildProfileCardsAsync(cancellationToken);
            if (SelectedDemoProfile is not null) await LoadPreviewAsync(SelectedDemoProfile.Profile, cancellationToken);
            await LoadStatusAndHistoryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorText = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyCommands();
            NotifyComputedState();
        }
    }

    private async Task BuildProfileCardsAsync(CancellationToken cancellationToken)
    {
        var previews = new Dictionary<TrainingDataSeedProfile, TrainingDataDemoPreviewDto>();
        foreach (var demo in DemoProfiles)
        {
            previews[demo.Profile] = await _trainingDataService.GetDemoPreviewAsync(demo.Profile, cancellationToken);
        }

        ProfileCards.Clear();
        foreach (var demo in DemoProfiles)
        {
            var preview = previews[demo.Profile];
            ProfileCards.Add(new ProfileCardState
            {
                Profile = demo.Profile,
                DisplayName = demo.DisplayName,
                Description = demo.Description,
                IconPath = ResolveProfileIcon(demo.Profile),
                FeatureChips = preview.KeyModules.Take(4).ToArray(),
                StatLines = preview.EstimatedCounts.Take(3).Select(x => $"• {x.EstimatedCount} {x.Name.ToLowerInvariant()}").ToArray()
            });
        }

        RefreshProfileSelection();
    }

    private async Task LoadPreviewAsync(TrainingDataSeedProfile profile, CancellationToken cancellationToken)
    {
        var preview = await _trainingDataService.GetDemoPreviewAsync(profile, cancellationToken);
        DemoPreviewDescription = preview.Description;
        PreviewStatCards.Clear();
        foreach (var item in preview.EstimatedCounts)
        {
            PreviewStatCards.Add(new PreviewStatCardItem
            {
                Name = item.Name,
                EstimatedCount = item.EstimatedCount,
                IconGlyph = ResolvePreviewGlyph(item.Name)
            });
        }
    }

    private async Task LoadStatusAndHistoryAsync(CancellationToken cancellationToken)
    {
        Status = await _trainingDataService.GetStatusAsync(cancellationToken);
        Preflight = await _trainingDataService.GetPreflightAsync(cancellationToken);

        PreflightWarnings.Clear();
        foreach (var warning in Preflight.Warnings) PreflightWarnings.Add(warning);

        RunHistory.Clear();
        foreach (var run in await _trainingDataService.GetRunHistoryAsync(50, cancellationToken)) RunHistory.Add(run);

        BuildAlerts();
        RebuildSafetyChecklist();
        RebuildStatusBadges();
        NotifyComputedState();
    }

    private void BuildAlerts()
    {
        AlertItems.Clear();
        foreach (var warning in PreflightWarnings)
        {
            AlertItems.Add(new AlertItem
            {
                Code = warning.Code,
                Severity = warning.Severity,
                Title = ResolveAlertTitle(warning.Code),
                Message = warning.Message,
                Timestamp = DateTime.Now,
                SeverityBrush = ResolveSeverityBrush(warning.Severity)
            });
        }
    }

    private void RebuildStatusBadges()
    {
        StatusBadges.Clear();
        StatusBadges.Add(CreateStatusBadge("Ready", "Success"));
        StatusBadges.Add(CreateStatusBadge(EnvironmentBadgeText, Preflight?.EnvironmentName == "Production" ? "Warning" : "Neutral"));
        StatusBadges.Add(CreateStatusBadge(DatabaseBadgeText, "Neutral"));
        StatusBadges.Add(CreateStatusBadge(SelectedProfileDisplayName, "Neutral"));

        if (IsMarkerSchemaMissing) StatusBadges.Add(CreateStatusBadge("Migration Required", "Danger"));
        else if (Preflight?.HasRealDataRisk == true) StatusBadges.Add(CreateStatusBadge("Warning", "Warning"));
        else if (Preflight is not null && !Preflight.AllowDestructiveReset) StatusBadges.Add(CreateStatusBadge("Locked", "Neutral"));
    }

    private void RebuildSafetyChecklist()
    {
        SafetyChecklistItems.Clear();
        var preflightAvailable = Preflight is not null;
        SafetyChecklistItems.Add(new SafetyChecklistItem("Migration đã sẵn sàng", preflightAvailable && !IsMarkerSchemaMissing));
        SafetyChecklistItems.Add(new SafetyChecklistItem("Đã chạy preflight", preflightAvailable));
        SafetyChecklistItems.Add(new SafetyChecklistItem("Destructive reset đang khóa", Preflight is not null && !Preflight.AllowDestructiveReset));
        SafetyChecklistItems.Add(new SafetyChecklistItem("Training marker khả dụng", HasTrainingMarkers && !IsMarkerSchemaMissing));
        SafetyChecklistItems.Add(new SafetyChecklistItem("Dữ liệu thật được bảo vệ", Preflight is not null && !Preflight.AllowResetWithRealDataRisk));
    }

    private void BuildCompletionSummary()
    {
        CompletionSummary.Clear();
        var result = _lastOperationResult;
        if (result is null) return;

        CompletionSummary.Add(new CompletionSummaryItem("Journal", Status?.JournalCount ?? 0));
        CompletionSummary.Add(new CompletionSummaryItem("Chứng từ", Status?.InventoryDocumentCount ?? 0));
        CompletionSummary.Add(new CompletionSummaryItem("Hàng hóa", Status?.ItemCount ?? 0));
        CompletionSummary.Add(new CompletionSummaryItem("VAT transactions", result.Steps.Sum(x => x.StepName.Contains("VAT", StringComparison.OrdinalIgnoreCase) ? x.CreatedCount : 0)));
    }

    private void RefreshProfileSelection()
    {
        foreach (var card in ProfileCards)
        {
            card.IsSelected = SelectedDemoProfile?.Profile == card.Profile
                || (SelectedDemoProfile?.Profile == TrainingDataSeedProfile.FullTraining && card.Profile == TrainingDataSeedProfile.RestaurantHotelFullDemo);
            card.BorderBrush = card.IsSelected ? new SolidColorBrush(Color.FromRgb(59, 130, 246)) : new SolidColorBrush(Color.FromRgb(221, 228, 240));
            card.BackgroundBrush = card.IsSelected ? new SolidColorBrush(Color.FromRgb(241, 246, 255)) : Brushes.White;
            card.SelectionGlyphVisibility = card.IsSelected ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static string ResolvePreviewGlyph(string name)
    {
        var key = name.ToLowerInvariant();
        if (key.Contains("đối tác") || key.Contains("khách hàng") || key.Contains("ncc")) return "\uE716";
        if (key.Contains("hàng hóa") || key.Contains("dịch vụ")) return "\uE8CB";
        if (key.Contains("chứng từ")) return "\uE8A5";
        if (key.Contains("journal")) return "\uE9D8";
        if (key.Contains("vat")) return "\uE8D2";
        return "\uE81E";
    }

    private static string ResolveAlertTitle(string code) => code switch
    {
        "PRODUCTION_GUARD" => "Production đang khóa destructive reset",
        "DESTRUCTIVE_DISABLED" => "Tùy chọn destructive reset đang tắt",
        "TRAINING_MARKER_TABLE_MISSING" => "Chưa apply migration TrainingDataMarkers",
        _ => code
    };

    private static string ResolveProfileIcon(TrainingDataSeedProfile profile) => profile switch
    {
        TrainingDataSeedProfile.RestaurantDemo => @"D:\ASMILE-BO\Windows_Icon\Icons\applications\commerce.ico",
        TrainingDataSeedProfile.HotelDemo => @"D:\ASMILE-BO\Windows_Icon\Icons\applications\office.ico",
        _ => @"D:\ASMILE-BO\Windows_Icon\Icons\applications\dataverse.ico"
    };

    private static StatusBadgeItem CreateStatusBadge(string text, string kind)
    {
        var (background, foreground, border) = kind switch
        {
            "Success" => (Color.FromRgb(220, 252, 231), Color.FromRgb(21, 128, 61), Color.FromRgb(134, 239, 172)),
            "Warning" => (Color.FromRgb(254, 243, 199), Color.FromRgb(161, 98, 7), Color.FromRgb(253, 224, 71)),
            "Danger" => (Color.FromRgb(254, 226, 226), Color.FromRgb(185, 28, 28), Color.FromRgb(252, 165, 165)),
            _ => (Color.FromRgb(241, 245, 249), Color.FromRgb(51, 65, 85), Color.FromRgb(203, 213, 225))
        };

        return new StatusBadgeItem(text, kind)
        {
            BackgroundBrush = new SolidColorBrush(background),
            ForegroundBrush = new SolidColorBrush(foreground),
            BorderBrush = new SolidColorBrush(border)
        };
    }

    private static Brush ResolveRiskBrush(string? level) => (level ?? "Low").ToLowerInvariant() switch
    {
        "high" => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
        "medium" => new SolidColorBrush(Color.FromRgb(217, 119, 6)),
        _ => new SolidColorBrush(Color.FromRgb(22, 163, 74))
    };

    private static Brush ResolveSeverityBrush(string severity) => severity.ToLowerInvariant() switch
    {
        "critical" or "error" => new SolidColorBrush(Color.FromRgb(220, 38, 38)),
        "warning" => new SolidColorBrush(Color.FromRgb(217, 119, 6)),
        _ => new SolidColorBrush(Color.FromRgb(37, 99, 235))
    };

    private void NotifyCommands()
    {
        CheckStatusCommand.NotifyCanExecuteChanged();
        PreflightCheckCommand.NotifyCanExecuteChanged();
        ClearBusinessDataCommand.NotifyCanExecuteChanged();
        SeedFoundationCommand.NotifyCanExecuteChanged();
        SeedFullTrainingCommand.NotifyCanExecuteChanged();
        DeleteTrainingDataCommand.NotifyCanExecuteChanged();
        ResetToBlankCommand.NotifyCanExecuteChanged();
        ResetTrainingOnlyCommand.NotifyCanExecuteChanged();
        CancelOperationCommand.NotifyCanExecuteChanged();
        SelectDemoProfileCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedDemoProfileChanged(TrainingDataDemoProfileDto? value)
    {
        OnPropertyChanged(nameof(SelectedProfileDisplayName));
        RebuildStatusBadges();
        RefreshProfileSelection();
    }

    partial void OnPreflightChanged(TrainingDataPreflightDto? value) => NotifyComputedState();
    partial void OnHasCompletionChanged(bool value) => OnPropertyChanged(nameof(HasCompletionResult));
    partial void OnCompletionMessageChanged(string value) => OnPropertyChanged(nameof(HasCompletionResult));

    private void NotifyComputedState()
    {
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(HasStepLogs));
        OnPropertyChanged(nameof(HasNoStepLogs));
        OnPropertyChanged(nameof(HasCompletionResult));
        OnPropertyChanged(nameof(IsMarkerSchemaMissing));
        OnPropertyChanged(nameof(HasTrainingMarkers));
        OnPropertyChanged(nameof(EnvironmentBadgeText));
        OnPropertyChanged(nameof(DatabaseBadgeText));
        OnPropertyChanged(nameof(MarkerStatusText));
        OnPropertyChanged(nameof(LastResetText));
        OnPropertyChanged(nameof(JournalStatusText));
        OnPropertyChanged(nameof(RiskBadgeText));
        OnPropertyChanged(nameof(RiskBadgeBrush));
        OnPropertyChanged(nameof(RiskBadgeForeground));
        OnPropertyChanged(nameof(DestructiveResetStateText));
        OnPropertyChanged(nameof(DestructiveResetStateBrush));
        OnPropertyChanged(nameof(ShowMigrationWarningCard));
        OnPropertyChanged(nameof(EmptyStateVisibility));
        OnPropertyChanged(nameof(LogTableVisibility));
    }

    public void Dispose()
    {
        _runCts?.Cancel();
        _runCts?.Dispose();
        _disposeCts.Cancel();
        _disposeCts.Dispose();
    }
}

public sealed class ProfileCardState : ObservableObject
{
    private bool _isSelected;
    private Brush _borderBrush = new SolidColorBrush(Color.FromRgb(221, 228, 240));
    private Brush _backgroundBrush = Brushes.White;
    private Visibility _selectionGlyphVisibility = Visibility.Collapsed;

    public TrainingDataSeedProfile Profile { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconPath { get; init; } = string.Empty;
    public IReadOnlyList<string> FeatureChips { get; init; } = [];
    public IReadOnlyList<string> StatLines { get; init; } = [];

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public Brush BorderBrush
    {
        get => _borderBrush;
        set => SetProperty(ref _borderBrush, value);
    }

    public Brush BackgroundBrush
    {
        get => _backgroundBrush;
        set => SetProperty(ref _backgroundBrush, value);
    }

    public Visibility SelectionGlyphVisibility
    {
        get => _selectionGlyphVisibility;
        set => SetProperty(ref _selectionGlyphVisibility, value);
    }
}

public sealed class PreviewStatCardItem
{
    public string Name { get; init; } = string.Empty;
    public int EstimatedCount { get; init; }
    public string IconGlyph { get; init; } = "\uE81E";
}

public sealed class SafetyChecklistItem
{
    public SafetyChecklistItem(string label, bool isPassed)
    {
        Label = label;
        IsPassed = isPassed;
        Glyph = isPassed ? "\uE73E" : "\uE711";
        GlyphBrush = isPassed
            ? new SolidColorBrush(Color.FromRgb(22, 163, 74))
            : new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }

    public string Label { get; }
    public bool IsPassed { get; }
    public string Glyph { get; }
    public Brush GlyphBrush { get; }
}

public sealed class AlertItem
{
    public string Code { get; init; } = string.Empty;
    public string Severity { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime Timestamp { get; init; }
    public Brush SeverityBrush { get; init; } = new SolidColorBrush(Color.FromRgb(217, 119, 6));
}

public sealed class StatusBadgeItem
{
    public StatusBadgeItem(string text, string kind)
    {
        Text = text;
        Kind = kind;
    }

    public string Text { get; }
    public string Kind { get; }
    public Brush BackgroundBrush { get; init; } = new SolidColorBrush(Color.FromRgb(241, 245, 249));
    public Brush ForegroundBrush { get; init; } = new SolidColorBrush(Color.FromRgb(51, 65, 85));
    public Brush BorderBrush { get; init; } = new SolidColorBrush(Color.FromRgb(203, 213, 225));
}

public sealed class CompletionSummaryItem
{
    public CompletionSummaryItem(string label, int value)
    {
        Label = label;
        Value = value;
    }

    public string Label { get; }
    public int Value { get; }
}
