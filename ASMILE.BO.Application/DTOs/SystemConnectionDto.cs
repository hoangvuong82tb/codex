namespace ASMILE.BO.Application.DTOs;

public sealed class SystemConnectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string DatabaseName { get; set; } = string.Empty;
    public string Status { get; set; } = "Unknown";
    public DateTime? LastCheckedAt { get; set; }
}
