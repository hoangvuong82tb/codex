using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace ASMILE.BO.App.Models;

public enum ModuleKey
{
    TrainingData,
    Dashboard,
    VirtualFileCenter,
    Inventory,
    Receivable,
    Payable,
    Customer,
    Supplier,
    Product,
    Warehouse,
    DepartmentHR,
    ReceiptVoucher,
    PaymentVoucher,
    StockInVoucher,
    StockOutVoucher
}

public enum ModuleSettingKind
{
    Toggle,
    Select,
    Text,
    Number,
    Permission
}

public sealed record ModuleSettingDefinition(
    string Key,
    string Title,
    string Description,
    ModuleSettingKind Kind,
    string DefaultValue,
    IReadOnlyList<string>? Options = null,
    bool IsRequired = false,
    string? DependsOnModuleKey = null,
    string? DisabledDescription = null);

public sealed record ModuleSettingSectionDefinition(
    string Key,
    string Title,
    string Description,
    string IconGlyph,
    IReadOnlyList<ModuleSettingDefinition> Settings);

public sealed record ModuleSettingsDefinition(
    ModuleKey ModuleKey,
    string AppKey,
    string Title,
    string Description,
    string IconGlyph,
    IReadOnlyList<ModuleSettingSectionDefinition> Sections);

public sealed class ModuleSettingsState
{
    public string AppKey { get; set; } = string.Empty;
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public sealed partial class ModuleSettingItemViewModel : ObservableObject
{
    public ModuleSettingItemViewModel(ModuleSettingDefinition definition, string value)
    {
        Definition = definition;
        key = definition.Key;
        title = definition.Title;
        description = definition.Description;
        kind = definition.Kind;
        value = string.IsNullOrWhiteSpace(value) ? definition.DefaultValue : value;
        this.value = value;
        originalValue = value;
        boolValue = ToBoolean(value);
        isEnabled = string.IsNullOrWhiteSpace(definition.DisabledDescription);
        disabledReason = definition.DisabledDescription ?? string.Empty;
        foreach (var option in definition.Options ?? Array.Empty<string>()) Options.Add(option);
    }

    public ModuleSettingDefinition Definition { get; }
    public ObservableCollection<string> Options { get; } = [];

    [ObservableProperty] private string key;
    [ObservableProperty] private string title;
    [ObservableProperty] private string description;
    [ObservableProperty] private ModuleSettingKind kind;
    [ObservableProperty] private string value;
    [ObservableProperty] private bool boolValue;
    [ObservableProperty] private string originalValue;
    [ObservableProperty] private bool isEnabled = true;
    [ObservableProperty] private string disabledReason = string.Empty;
    [ObservableProperty] private string validationError = string.Empty;

    public bool IsToggle => Kind == ModuleSettingKind.Toggle || Kind == ModuleSettingKind.Permission;
    public bool IsSelect => Kind == ModuleSettingKind.Select;
    public bool IsText => Kind == ModuleSettingKind.Text;
    public bool IsNumber => Kind == ModuleSettingKind.Number;
    public bool HasError => !string.IsNullOrWhiteSpace(ValidationError);
    public bool HasDisabledReason => !string.IsNullOrWhiteSpace(DisabledReason);
    public bool IsDirty => !string.Equals(Value ?? string.Empty, OriginalValue ?? string.Empty, StringComparison.Ordinal);

    partial void OnKindChanged(ModuleSettingKind value)
    {
        OnPropertyChanged(nameof(IsToggle));
        OnPropertyChanged(nameof(IsSelect));
        OnPropertyChanged(nameof(IsText));
        OnPropertyChanged(nameof(IsNumber));
    }

    partial void OnValueChanged(string value)
    {
        if (IsToggle && BoolValue != ToBoolean(value))
        {
            BoolValue = ToBoolean(value);
        }
        OnPropertyChanged(nameof(IsDirty));
    }

    partial void OnBoolValueChanged(bool value)
    {
        var next = value ? "true" : "false";
        if (!string.Equals(Value, next, StringComparison.OrdinalIgnoreCase))
        {
            Value = next;
        }
    }

    partial void OnOriginalValueChanged(string value) => OnPropertyChanged(nameof(IsDirty));
    partial void OnValidationErrorChanged(string value) => OnPropertyChanged(nameof(HasError));
    partial void OnDisabledReasonChanged(string value) => OnPropertyChanged(nameof(HasDisabledReason));

    public void AcceptChanges()
    {
        OriginalValue = Value;
        ValidationError = string.Empty;
    }

    public void ResetToDefault()
    {
        Value = Definition.DefaultValue;
        BoolValue = ToBoolean(Value);
        ValidationError = string.Empty;
    }

    private static bool ToBoolean(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "bật", StringComparison.OrdinalIgnoreCase);
}

public sealed class ModuleSettingSectionViewModel
{
    public ModuleSettingSectionViewModel(ModuleSettingSectionDefinition definition)
    {
        Key = definition.Key;
        Title = definition.Title;
        Description = definition.Description;
        IconGlyph = definition.IconGlyph;
    }

    public string Key { get; }
    public string Title { get; }
    public string Description { get; }
    public string IconGlyph { get; }
    public ObservableCollection<ModuleSettingItemViewModel> Items { get; } = [];
}
