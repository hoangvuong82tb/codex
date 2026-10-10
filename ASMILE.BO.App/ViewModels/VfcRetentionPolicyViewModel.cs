using System.Collections.ObjectModel;
using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Core.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class VfcRetentionPolicyViewModel(
    IArchiveService archiveService,
    IAuthorizationService authorizationService) : ObservableObject
{
    public ObservableCollection<DataRetentionPolicyDto> Policies { get; } = [];

    [ObservableProperty] private DataRetentionPolicyDto? selectedPolicy;
    [ObservableProperty] private string retentionDays = string.Empty;
    [ObservableProperty] private string archiveAfterDays = string.Empty;
    [ObservableProperty] private string deleteAfterDays = string.Empty;
    [ObservableProperty] private bool compressArchivedPayload;
    [ObservableProperty] private bool isEnabled;
    [ObservableProperty] private bool isLoading;
    [ObservableProperty] private bool isSaving;
    [ObservableProperty] private string infoMessage = string.Empty;
    [ObservableProperty] private string errorMessage = string.Empty;
    private bool _initialized;

    public bool HasSelection => SelectedPolicy is not null;
    public bool CanEdit => authorizationService.HasPermission(Permission.EditSystemSettings);

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
            var selectedId = SelectedPolicy?.Id;
            var policies = await archiveService.GetRetentionPoliciesAsync(cancellationToken);
            Policies.Clear();
            foreach (var policy in policies)
                Policies.Add(policy);

            SelectedPolicy = Policies.FirstOrDefault(x => x.Id == selectedId) ?? Policies.FirstOrDefault();
            InfoMessage = $"Đã tải {Policies.Count} retention policy từ database.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không tải được retention policy: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
            RaiseState();
        }
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
        if (SelectedPolicy is null)
        {
            ErrorMessage = "Hãy chọn một policy để chỉnh sửa.";
            RaiseState();
            return;
        }

        if (!int.TryParse(RetentionDays, out var retention)
            || !int.TryParse(ArchiveAfterDays, out var archiveAfter)
            || !int.TryParse(DeleteAfterDays, out var deleteAfter))
        {
            ErrorMessage = "Số ngày phải là số nguyên dương.";
            RaiseState();
            return;
        }

        IsSaving = true;
        try
        {
            var policy = new DataRetentionPolicyDto
            {
                Id = SelectedPolicy.Id,
                RetentionDays = retention,
                ArchiveAfterDays = archiveAfter,
                DeleteAfterDays = deleteAfter,
                CompressArchivedPayload = CompressArchivedPayload,
                IsEnabled = IsEnabled
            };
            var saved = await archiveService.SaveRetentionPolicyAsync(policy, cancellationToken);
            var index = Policies.IndexOf(SelectedPolicy);
            if (index >= 0)
                Policies[index] = saved;
            SelectedPolicy = saved;
            InfoMessage = "Đã lưu retention policy vào database và ghi audit.";
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Không lưu được retention policy: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
            RaiseState();
        }
    }

    partial void OnSelectedPolicyChanged(DataRetentionPolicyDto? value)
    {
        RetentionDays = value?.RetentionDays.ToString() ?? string.Empty;
        ArchiveAfterDays = value?.ArchiveAfterDays.ToString() ?? string.Empty;
        DeleteAfterDays = value?.DeleteAfterDays.ToString() ?? string.Empty;
        CompressArchivedPayload = value?.CompressArchivedPayload ?? false;
        IsEnabled = value?.IsEnabled ?? false;
        RaiseState();
    }

    private void RaiseState() => OnPropertyChanged(nameof(HasSelection));
}
