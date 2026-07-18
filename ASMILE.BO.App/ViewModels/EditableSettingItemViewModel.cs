using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Core.Entities.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.ViewModels.Settings;

public sealed partial class EditableSettingItemViewModel : ObservableValidator
{
    private readonly SettingDefinitionDto _definition;
    private readonly string? _originalValue;

    public EditableSettingItemViewModel(SettingDefinitionDto definition, string? value)
    {
        _definition = definition;
        _originalValue = value;
        Key = definition.Key;
        DisplayName = definition.DisplayName;
        Description = definition.Description;
        ValueType = definition.ValueType;
        IsSensitive = definition.IsSensitive;
        IsEncrypted = definition.IsSensitive;
        IsReadOnly = definition.IsReadOnly;
        IsRequired = definition.IsRequired;
        RequiresRestart = definition.RequiresRestart;
        RequiredPermission = definition.RequiredPermission;
        Value = value;
        OriginalValue = value;
    }

    public string Key { get; }
    public string DisplayName { get; }
    public string? Description { get; }
    public string? OriginalValue { get; }
    public SettingValueType ValueType { get; }
    public ObservableCollection<string> Options { get; } = [];
    public bool IsSensitive { get; }
    public bool IsEncrypted { get; }
    public bool IsReadOnly { get; }
    public bool IsRequired { get; }
    public bool RequiresRestart { get; }
    public string? RequiredPermission { get; }

    [ObservableProperty] private string? value;
    [ObservableProperty] private bool isRevealed;
    [ObservableProperty] private bool hasError;
    [ObservableProperty] private string? errorMessage;

    public bool IsDirty => !string.Equals(_originalValue ?? string.Empty, Value ?? string.Empty, StringComparison.Ordinal);
    public bool IsBoolean => ValueType == SettingValueType.Boolean;
    public bool IsText => ValueType is SettingValueType.String or SettingValueType.ConnectionString;
    public bool IsNumber => ValueType == SettingValueType.Number;
    public bool IsEnum => ValueType == SettingValueType.Enum;
    public bool IsPath => ValueType == SettingValueType.Path;
    public bool IsPassword => ValueType == SettingValueType.Password;
    public bool IsMasked => IsSensitive && !IsRevealed;
    public string? DisplayValue => IsMasked ? "********" : Value;
    public bool BoolValue { get => string.Equals(Value, "true", StringComparison.OrdinalIgnoreCase); set => Value = value ? "true" : "false"; }

    partial void OnValueChanged(string? value)
    {
        HasError = false;
        ErrorMessage = null;
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(DisplayValue));
    }

    partial void OnIsRevealedChanged(bool value) => OnPropertyChanged(nameof(DisplayValue));

    [RelayCommand]
    private void Toggle()
    {
        if (ValueType == SettingValueType.Boolean)
        {
            Value = string.Equals(Value, "true", StringComparison.OrdinalIgnoreCase) ? "false" : "true";
            OnPropertyChanged(nameof(IsDirty));
        }
    }

    [RelayCommand]
    private void Browse()
    {
    }

    [RelayCommand]
    private void ToggleReveal()
    {
        if (IsSensitive)
        {
            IsRevealed = !IsRevealed;
        }
    }
}



