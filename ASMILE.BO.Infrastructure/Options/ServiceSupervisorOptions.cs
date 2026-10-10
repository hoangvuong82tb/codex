namespace ASMILE.BO.Infrastructure.Options;

public sealed class ServiceSupervisorOptions
{
    public const string SectionName = "ServiceSupervisor";

    /// <summary>
    /// Restart is disabled by default because it changes host state and may
    /// require administrator privileges.
    /// </summary>
    public bool AllowRestart { get; set; }

    /// <summary>
    /// Restart is only permitted for explicitly listed Windows service names.
    /// </summary>
    public string[] AllowedServiceNames { get; set; } = [];

    public int RestartTimeoutSeconds { get; set; } = 30;
}
