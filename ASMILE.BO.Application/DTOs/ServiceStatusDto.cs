namespace ASMILE.BO.Application.DTOs;

public sealed class ServiceStatusDto
{
    public int Id { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = "Stopped";
    public DateTime? LastHeartbeatAt { get; set; }
}
