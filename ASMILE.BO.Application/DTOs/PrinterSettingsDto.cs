namespace ASMILE.BO.Application.DTOs;

public sealed class PrinterDeviceDto
{
    public string Name { get; set; } = string.Empty;
    public string SharePath { get; set; } = string.Empty;
    public string Status { get; set; } = "Online";
}

public sealed class PrinterPermissionDto
{
    public string PrinterName { get; set; } = string.Empty;
    public string AllowedDocuments { get; set; } = string.Empty;
    public string Profile { get; set; } = string.Empty;
}
