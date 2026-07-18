using ASMILE.BO.Application.DTOs.Security;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels.Settings;

public sealed partial class UserPermissionSettingsViewModel : ObservableObject
{
    public ObservableCollection<UserAccountDto> Users { get; } = [];
    public ObservableCollection<RoleDto> Roles { get; } = [];
    public ObservableCollection<PermissionDto> Permissions { get; } = [];
    [ObservableProperty] private UserAccountDto? selectedUser;
    [ObservableProperty] private RoleDto? selectedRole;
    public ObservableCollection<string> PermissionMatrix { get; } = [];
    public IRelayCommand? AddUserCommand { get; set; }
    public IRelayCommand? EditUserCommand { get; set; }
    public IRelayCommand? ResetPasswordCommand { get; set; }
    public IRelayCommand? LockUnlockCommand { get; set; }
    public IRelayCommand? SavePermissionsCommand { get; set; }
}

