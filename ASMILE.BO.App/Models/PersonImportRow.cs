namespace ASMILE.BO.App.Models;

public sealed class PersonImportRow
{
    public string PersonCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string IsActiveRaw { get; set; } = string.Empty;
}
