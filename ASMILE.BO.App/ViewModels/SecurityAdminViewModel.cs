using ASMILE.BO.Application.DTOs;
using ASMILE.BO.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Threading;

namespace ASMILE.BO.App.ViewModels;

public sealed partial class SecurityAdminViewModel : ViewModelBase, IDisposable
{
    private readonly IPermissionService _permissionService;
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private readonly DispatcherTimer _filterTimer;
    private CancellationTokenSource? _loadCancellation;
    private bool _suppressRoleChangedReload;
    private long _loadRequestId;
    private bool _disposed;

    public ObservableCollection<EnterpriseRoleDto> Roles { get; } = new();
    public ObservableCollection<PermissionSelectionRow> Permissions { get; } = new();
    public ObservableCollection<UserSelectionRow> Users { get; } = new();
    public ObservableCollection<PermissionDiffDto> PermissionDiffs { get; } = new();

    [ObservableProperty]
    private EnterpriseRoleDto? selectedRole;

    [ObservableProperty]
    private EnterpriseRoleDto? compareRole;

    [ObservableProperty]
    private string permissionFilter = string.Empty;

    [ObservableProperty]
    private string roleCode = string.Empty;

    [ObservableProperty]
    private string roleName = string.Empty;

    [ObservableProperty]
    private string roleDescription = string.Empty;

    [ObservableProperty]
    private string statusMessage = "Sẵn sàng";

    [ObservableProperty]
    private bool isLoadingData;

    public SecurityAdminViewModel(IPermissionService permissionService)
    {
        _permissionService = permissionService;
        _filterTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _filterTimer.Tick += (_, _) =>
        {
            _filterTimer.Stop();
            QueueLoad();
        };
        QueueLoad();
    }

    partial void OnSelectedRoleChanged(EnterpriseRoleDto? value)
    {
        if (value is not null)
        {
            RoleCode = value.Code;
            RoleName = value.Name;
            RoleDescription = value.Description ?? string.Empty;
        }

        if (_suppressRoleChangedReload)
        {
            return;
        }

        QueueLoad();
    }

    partial void OnCompareRoleChanged(EnterpriseRoleDto? value)
    {
        _ = RefreshDiffAsync();
    }

    partial void OnPermissionFilterChanged(string value)
    {
        _filterTimer.Stop();
        _filterTimer.Start();
    }

    [RelayCommand]
    private Task Refresh()
    {
        return ForceLoadAsync();
    }

    [RelayCommand]
    private async Task CreateRole()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(RoleCode) || string.IsNullOrWhiteSpace(RoleName))
            {
                StatusMessage = "Nhập mã và tên vai trò.";
                return;
            }

            SelectedRole = await _permissionService.CreateRoleAsync(RoleCode, RoleName, RoleDescription);
            await ForceLoadAsync();
            StatusMessage = "Đã tạo vai trò.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveRole()
    {
        try
        {
            if (SelectedRole is null)
            {
                return;
            }

            await _permissionService.RenameRoleAsync(SelectedRole.Id, RoleName, RoleDescription);
            await ForceLoadAsync();
            StatusMessage = "Đã cập nhật vai trò.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task CloneRole()
    {
        try
        {
            if (SelectedRole is null)
            {
                return;
            }

            var cloneCode = $"{SelectedRole.Code}-copy";
            var cloneName = $"{SelectedRole.Name} Bản sao";
            SelectedRole = await _permissionService.CloneRoleAsync(SelectedRole.Id, cloneCode, cloneName);
            await ForceLoadAsync();
            StatusMessage = "Đã nhân bản vai trò.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task ArchiveRole()
    {
        try
        {
            if (SelectedRole is null)
            {
                return;
            }

            await _permissionService.ArchiveRoleAsync(SelectedRole.Id);
            await ForceLoadAsync();
            StatusMessage = "Đã lưu trữ vai trò.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AssignSelectedPermissions()
    {
        try
        {
            if (SelectedRole is null)
            {
                return;
            }

            var codes = Permissions.Where(x => x.IsSelected && !x.IsAssigned).Select(x => x.Code).ToArray();
            await _permissionService.AssignPermissionsAsync(SelectedRole.Id, codes);
            await ForceLoadAsync();
            StatusMessage = $"Đã gán {codes.Length} quyền.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task RemoveSelectedPermissions()
    {
        try
        {
            if (SelectedRole is null)
            {
                return;
            }

            var codes = Permissions.Where(x => x.IsSelected && x.IsAssigned).Select(x => x.Code).ToArray();
            await _permissionService.RemovePermissionsAsync(SelectedRole.Id, codes);
            await ForceLoadAsync();
            StatusMessage = $"Đã gỡ {codes.Length} quyền.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private async Task AssignSelectedUsers()
    {
        try
        {
            if (SelectedRole is null)
            {
                return;
            }

            var userIds = Users.Where(x => x.IsSelected).Select(x => x.Id).ToArray();
            await _permissionService.AssignUsersToRoleAsync(SelectedRole.Id, userIds);
            await ForceLoadAsync();
            StatusMessage = $"Đã gán {userIds.Length} người dùng.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    private void QueueLoad()
    {
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadCancellation = new CancellationTokenSource();
        var requestId = Interlocked.Increment(ref _loadRequestId);
        _ = LoadAsync(requestId, _loadCancellation.Token);
    }

    private Task ForceLoadAsync(CancellationToken cancellationToken = default)
    {
        var requestId = Interlocked.Increment(ref _loadRequestId);
        return LoadAsync(requestId, cancellationToken);
    }

    private async Task LoadAsync(long requestId, CancellationToken cancellationToken = default)
    {
        var lockTaken = false;
        try
        {
            await _loadGate.WaitAsync(cancellationToken);
            lockTaken = true;
            if (requestId != Interlocked.Read(ref _loadRequestId))
            {
                return;
            }

            IsLoadingData = true;

            var selectedRoleId = SelectedRole?.Id;

            _suppressRoleChangedReload = true;
            try
            {
            var roles = await _permissionService.GetRolesAsync(cancellationToken: cancellationToken);
            Roles.Clear();
            foreach (var role in roles)
            {
                Roles.Add(role);
            }

                if (selectedRoleId.HasValue)
                {
                    SelectedRole = Roles.FirstOrDefault(x => x.Id == selectedRoleId.Value);
                }
                else
                {
                    SelectedRole ??= Roles.FirstOrDefault();
                }
            }
            finally
            {
                _suppressRoleChangedReload = false;
            }

            var permissions = await _permissionService.GetPermissionsAsync(SelectedRole?.Id, PermissionFilter, cancellationToken);
            Permissions.Clear();
            foreach (var permission in permissions)
            {
                Permissions.Add(new PermissionSelectionRow(permission));
            }

            var users = await _permissionService.GetUsersAsync(cancellationToken: cancellationToken);
            Users.Clear();
            foreach (var user in users)
            {
                Users.Add(new UserSelectionRow(user));
            }

            await RefreshDiffAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsLoadingData = false;
            if (lockTaken)
            {
                _loadGate.Release();
            }
        }
    }

    private async Task RefreshDiffAsync(CancellationToken cancellationToken = default)
    {
        PermissionDiffs.Clear();
        if (SelectedRole is null || CompareRole is null || SelectedRole.Id == CompareRole.Id)
        {
            return;
        }

        var rows = await _permissionService.CompareRolesAsync(SelectedRole.Id, CompareRole.Id, cancellationToken);
        foreach (var row in rows)
        {
            PermissionDiffs.Add(row);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _filterTimer.Stop();
        _loadCancellation?.Cancel();
        _loadCancellation?.Dispose();
        _loadGate.Dispose();
    }
}

public sealed partial class PermissionSelectionRow : ObservableObject
{
    public PermissionSelectionRow(EnterprisePermissionDto dto)
    {
        Code = dto.Code;
        Name = dto.Name;
        Group = dto.Group;
        Description = dto.Description ?? string.Empty;
        IsDangerous = dto.IsDangerous;
        IsAssigned = dto.IsAssigned;
    }

    public string Code { get; }
    public string Name { get; }
    public string Group { get; }
    public string Description { get; }
    public bool IsDangerous { get; }
    public bool IsAssigned { get; }

    [ObservableProperty]
    private bool isSelected;
}

public sealed partial class UserSelectionRow : ObservableObject
{
    public UserSelectionRow(EnterpriseUserDto dto)
    {
        Id = dto.Id;
        UserName = dto.UserName;
        DisplayName = dto.DisplayName;
        Roles = string.Join(", ", dto.Roles);
        IsActive = dto.IsActive;
    }

    public Guid Id { get; }
    public string UserName { get; }
    public string DisplayName { get; }
    public string Roles { get; }
    public bool IsActive { get; }

    [ObservableProperty]
    private bool isSelected;
}
