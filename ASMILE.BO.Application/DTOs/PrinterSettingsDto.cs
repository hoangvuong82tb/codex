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
    public string Principal { get; set; } = string.Empty;
    public string Rights { get; set; } = string.Empty;
    public string AccessType { get; set; } = string.Empty;
    public bool IsInherited { get; set; }
    public string ReadStatus { get; set; } = "Read";
}
