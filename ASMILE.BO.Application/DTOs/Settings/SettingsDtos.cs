using ASMILE.BO.Core.Entities.Settings;

namespace ASMILE.BO.Application.DTOs.Settings;

public sealed class SettingDefinitionDto
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Group { get; set; } = string.Empty;
    public SettingScope Scope { get; set; }
    public SettingValueType ValueType { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSensitive { get; set; }
    public bool IsReadOnly { get; set; }
    public bool RequiresRestart { get; set; }
    public string? RequiredPermission { get; set; }
    public int SortOrder { get; set; }
}

public sealed class SettingValueDto
{
    public string Key { get; set; } = string.Empty;
    public SettingScope Scope { get; set; }
    public string? ScopeTarget { get; set; }
    public string? Value { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

public sealed class EditableSettingItemDto
{
    public SettingDefinitionDto Definition { get; set; } = new();
    public SettingValueDto? Value { get; set; }
    public string? DisplayValue { get; set; }
}

public sealed class SaveSettingValueRequest
{
    public string Key { get; set; } = string.Empty;
    public SettingScope Scope { get; set; }
    public string? ScopeTarget { get; set; }
    public string? Value { get; set; }
    public string? Reason { get; set; }
}

public sealed class AppModuleDto
{
    public string AppKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Version { get; set; }
    public bool IsEnabled { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool PinToTaskbar { get; set; }
    public bool AllowClientAccess { get; set; }
    public string? RequiredPermission { get; set; }
    public int SortOrder { get; set; }
}

public sealed class UpdateAppModuleRequest
{
    public string AppKey { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public bool IsEnabled { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool PinToTaskbar { get; set; }
    public bool AllowClientAccess { get; set; }
    public string? RequiredPermission { get; set; }
    public string? Reason { get; set; }
}

public sealed class SettingChangeLogDto
{
    public string SettingKey { get; set; } = string.Empty;
    public SettingScope Scope { get; set; }
    public string? ScopeTarget { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}

public sealed class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Fail(string error) => new() { IsValid = false, ErrorMessage = error };
}
