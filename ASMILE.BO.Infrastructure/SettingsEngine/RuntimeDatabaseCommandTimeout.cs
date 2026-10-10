namespace ASMILE.BO.Infrastructure.SettingsEngine;

/// <summary>
/// Runtime value consumed by the EF Core command interceptor. The settings
/// monitor updates it after startup hydration and each persisted change.
/// </summary>
public sealed class RuntimeDatabaseCommandTimeout
{
    public const string SettingKey = "Database.CommandTimeout";
    public const int DefaultSeconds = 30;
    public const int MinimumSeconds = 5;
    public const int MaximumSeconds = 600;

    private int _seconds = DefaultSeconds;

    public int Seconds => Volatile.Read(ref _seconds);

    public bool TrySet(string? value)
    {
        if (!int.TryParse(value?.Trim(), out var seconds)
            || seconds is < MinimumSeconds or > MaximumSeconds)
        {
            return false;
        }

        Volatile.Write(ref _seconds, seconds);
        return true;
    }
}
