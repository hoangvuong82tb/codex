using System.Collections.ObjectModel;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class ExportPipelineSettingsViewModel(
    IExportPipelineService exportPipelineService,
    IAuthorizationService authorizationService) : ObservableObject
{
    public ObservableCollection<ExportRetentionPolicyDto> Policies { get; } = [];
    public ObservableCollection<ExportExecutionDto> Executions { get; } = [];

    [ObservableProperty] private ExportRetentionPolicyDto? selectedPolicy;
    [ObservableProperty] private string exportCode = string.Empty;
    [ObservableProperty] private string retentionDays = "30";
    [ObservableProperty] private bool isActive = true;
    [ObservableProperty] private bool isNewPolicy;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    private bool _initialized;

    public bool CanEdit => authorizationService.HasPermission(Permission.EditSystemSettings);
    public bool HasSelection => SelectedPolicy is not null || IsNewPolicy;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_initialized)
            return;
        _initialized = true;
        await LoadAsync(cancellationToken);
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        try
        {
            var keepId = SelectedPolicy?.Id;
            var policies = await exportPipelineService.GetRetentionPoliciesAsync(cancellationToken);
            var executions = await exportPipelineService.GetExecutionsAsync(100, cancellationToken);
            Policies.Clear();
            foreach (var policy in policies)
                Policies.Add(policy);
            Executions.Clear();
            foreach (var execution in executions)
                Executions.Add(execution);
            IsNewPolicy = false;
            SelectedPolicy = Policies.FirstOrDefault(x => x.Id == keepId) ?? Policies.FirstOrDefault();
            InfoMessage = $"Đã tải {Policies.Count} policy và {Executions.Count} export execution từ database.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không tải được cấu hình export: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            RaiseState();
        }
    }

    [RelayCommand]
    private void NewPolicy()
    {
        SelectedPolicy = null;
        IsNewPolicy = true;
        ExportCode = string.Empty;
        RetentionDays = "30";
        IsActive = true;
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        RaiseState();
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        ErrorMessage = string.Empty;
        InfoMessage = string.Empty;
        if (!CanEdit)
        {
            ErrorMessage = "Bạn không có quyền sửa System Settings.";
            RaiseState();
            return;
        }
        if (!int.TryParse(RetentionDays, out var days))
        {
            ErrorMessage = "Thời hạn lưu phải là số nguyên.";
            RaiseState();
            return;
        }

        IsSaving = true;
        try
        {
            var saved = await exportPipelineService.SaveRetentionPolicyAsync(new ExportRetentionPolicyDto
            {
                Id = SelectedPolicy?.Id ?? Guid.Empty,
                ExportCode = ExportCode,
                RetentionDays = days,
                IsActive = IsActive
            }, cancellationToken);
            IsNewPolicy = false;
            await LoadAsync(cancellationToken);
            SelectedPolicy = Policies.FirstOrDefault(x => x.Id == saved.Id);
            InfoMessage = "Đã lưu retention policy cho export execution.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không lưu được policy: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
            RaiseState();
        }
    }

    partial void OnSelectedPolicyChanged(ExportRetentionPolicyDto? value)
    {
        if (value is not null)
        {
            IsNewPolicy = false;
            ExportCode = value.ExportCode;
            RetentionDays = value.RetentionDays.ToString();
            IsActive = value.IsActive;
        }
        RaiseState();
    }

    private void RaiseState()
    {
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(CanEdit));
    }
}
