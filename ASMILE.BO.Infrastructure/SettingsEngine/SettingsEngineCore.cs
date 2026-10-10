using ASMILE.BO.Application.DTOs.Settings;
using ASMILE.BO.Application.Interfaces;
using ASMILE.BO.Application.Interfaces.Settings;
using ASMILE.BO.Core.Entities.Settings;
using ASMILE.BO.Shared.Security;
using ASMILE.BO.Infrastructure.Services;

namespace ASMILE.BO.Infrastructure.SettingsEngine;

internal static class SettingsMapper
{
    public static SettingDefinitionDto ToDto(this SettingDefinition x) => new()
    {
        Key = x.Key,
        DisplayName = x.DisplayName,
        Description = x.Description,
        Category = x.Category,
        Group = x.Group,
        Scope = x.Scope,
        ValueType = x.ValueType,
        DefaultValue = x.DefaultValue,
        IsRequired = x.IsRequired,
        IsSensitive = x.IsSensitive,
        IsReadOnly = x.IsReadOnly,
        RequiresRestart = x.RequiresRestart,
        RequiredPermission = x.RequiredPermission,
        SortOrder = x.SortOrder
    };

    public static SettingValueDto ToDto(this SettingValue x, string? valueOverride = null) => new()
    {
        Key = x.DefinitionKey,
        Scope = x.Scope,
        ScopeTarget = x.ScopeTarget,
        Value = valueOverride ?? x.Value,
        IsPersisted = true,
        IsEncrypted = x.IsEncrypted,
        UpdatedAt = x.UpdatedAt,
        UpdatedBy = x.UpdatedBy
    };
}

public sealed class SettingsChangeNotifier : ISettingsChangeNotifier
{
    public event EventHandler<SettingChangedEventArgs>? SettingChanged;
    public void NotifyChanged(SettingValueDto value, bool requiresRestart) => SettingChanged?.Invoke(this, new SettingChangedEventArgs(value, requiresRestart));
}

public sealed class SettingsAuditService : ISettingsAuditService
{
    private readonly ISettingChangeLogRepository _repo;

    public SettingsAuditService(ISettingChangeLogRepository repo) => _repo = repo;

    public Task LogChangeAsync(SettingChangeLogDto log)
    {
        return _repo.AddAsync(new SettingChangeLog
        {
            SettingKey = log.SettingKey,
            Scope = log.Scope,
            ScopeTarget = log.ScopeTarget,
            OldValue = log.OldValue,
            NewValue = log.NewValue,
            ChangedBy = log.ChangedBy,
            ChangedAt = log.ChangedAt,
            Reason = log.Reason
        });
    }
}

public sealed class SettingsValidator : ISettingsValidator
{
    public ValidationResult Validate(SettingDefinitionDto definition, string? value)
    {
        if (definition.IsRequired && string.IsNullOrWhiteSpace(value)) return ValidationResult.Fail("Giá trị bắt buộc.");
        if (definition.Key == "Backup.RetentionCount" && (!int.TryParse(value, out var ret) || ret < 1)) return ValidationResult.Fail("Retention count phải >= 1.");
        if (definition.Key == "Logs.RetentionDays"
            && (!int.TryParse(value, out var logRetentionDays) || logRetentionDays is < 1 or > 3650))
        {
            return ValidationResult.Fail("Thời hạn giữ log phải từ 1 đến 3650 ngày.");
        }
        if (definition.Key == "Database.CommandTimeout"
            && (!int.TryParse(value, out var databaseTimeout) || databaseTimeout is < 5 or > 600))
        {
            return ValidationResult.Fail("Command timeout phải từ 5 đến 600 giây.");
        }
        if (definition.Key == "Warehouse.LowStockThreshold"
            && (!int.TryParse(value, out var lowStockThreshold) || lowStockThreshold < 0))
        {
            return ValidationResult.Fail("Ngưỡng tồn kho thấp phải là số nguyên không âm.");
        }
        if (definition.Key == "Maintenance.UpdateChannel"
            && (value is null || !new[] { "Stable", "Beta", "Internal" }.Contains(value, StringComparer.OrdinalIgnoreCase)))
        {
            return ValidationResult.Fail("Kênh cập nhật phải là Stable, Beta hoặc Internal.");
        }
        if (definition.Key == "Logs.LogLevel"
            && (value is null || !new[] { "Debug", "Information", "Warning", "Error" }.Contains(value, StringComparer.OrdinalIgnoreCase)))
        {
            return ValidationResult.Fail("Mức log phải là Debug, Information, Warning hoặc Error.");
        }
        if (definition.Key == "GeneralLedger.DefaultExportFormat"
            && (value is null || !new[] { "Excel", "PDF" }.Contains(value, StringComparer.OrdinalIgnoreCase)))
        {
            return ValidationResult.Fail("Định dạng xuất mặc định phải là Excel hoặc PDF.");
        }
        if (definition.Key == "GeneralLedger.ReportFooterNote"
            && (value?.Length > 500 || value?.Any(char.IsControl) == true))
        {
            return ValidationResult.Fail("Ghi chú cuối báo cáo tối đa 500 ký tự và không được chứa ký tự điều khiển.");
        }
        if (definition.Key == "Appearance.Theme"
            && (value is null || !new[] { "Light", "Dark", "System" }.Contains(value, StringComparer.OrdinalIgnoreCase)))
        {
            return ValidationResult.Fail("Giao diện phải là Light, Dark hoặc System.");
        }
        if (definition.Key == "Backup.Schedule"
            && (string.IsNullOrWhiteSpace(value)
                || BackgroundSchedulerService.ComputeNextRun(DateTime.UtcNow, intervalMinutes: null, cronExpression: value) is null))
        {
            return ValidationResult.Fail("Lịch backup phải là biểu thức cron 5 trường hợp lệ, ví dụ: 0 0 * * *.");
        }
        if (definition.Key == "Security.SessionTimeoutMinutes"
            && (!int.TryParse(value, out var timeout) || timeout is < 5 or > 1440))
        {
            return ValidationResult.Fail("Thời hạn phiên phải từ 5 đến 1.440 phút.");
        }
        if (definition.Key == "Security.MaxFailedLoginAttempts" && (!int.TryParse(value, out var attempt) || attempt is < 3 or > 10)) return ValidationResult.Fail("Max failed attempts phải từ 3 đến 10.");
        if (definition.Key == "Network.ApiPort" && (!int.TryParse(value, out var port) || port is < 1 or > 65535)) return ValidationResult.Fail("Port phải từ 1 đến 65535.");
        if (definition.Key is "Network.ApiBaseUrl" or "POS.ApiEndpoint" or "FO.ApiEndpoint"
            && !string.IsNullOrWhiteSpace(value))
        {
            if (!Uri.TryCreate(value.Trim(), UriKind.Absolute, out var endpoint)
                || (!string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                || string.IsNullOrWhiteSpace(endpoint.Host)
                || !string.IsNullOrEmpty(endpoint.UserInfo)
                || !string.IsNullOrEmpty(endpoint.Query)
                || !string.IsNullOrEmpty(endpoint.Fragment))
            {
                return ValidationResult.Fail("Endpoint phải là URL tuyệt đối HTTP/HTTPS, không chứa thông tin đăng nhập, query hoặc fragment.");
            }
        }
        if (definition.ValueType == SettingValueType.Number && !string.IsNullOrWhiteSpace(value) && !int.TryParse(value, out _)) return ValidationResult.Fail("Giá trị số không hợp lệ.");
        if (definition.ValueType == SettingValueType.Decimal && !string.IsNullOrWhiteSpace(value) && !decimal.TryParse(value, out _)) return ValidationResult.Fail("Giá trị decimal không hợp lệ.");
        if (definition.ValueType == SettingValueType.Boolean && !bool.TryParse(value?.Trim(), out _))
        {
            return ValidationResult.Fail("Giá trị Boolean phải là true hoặc false.");
        }
        if (definition.ValueType == SettingValueType.Path && !string.IsNullOrWhiteSpace(value))
        {
            var path = Environment.ExpandEnvironmentVariables(value.Trim());
            if (path.Contains("..") || path.Contains('*')) return ValidationResult.Fail("Path không hợp lệ.");
            if (!Path.IsPathFullyQualified(path)) return ValidationResult.Fail("Đường dẫn phải là đường dẫn tuyệt đối hoặc UNC.");
            try
            {
                _ = Path.GetFullPath(path);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                return ValidationResult.Fail("Đường dẫn không hợp lệ hoặc vượt quá giới hạn hệ thống.");
            }
        }
        return ValidationResult.Success();
    }
}

