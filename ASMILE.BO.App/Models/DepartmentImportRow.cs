namespace ASMILE.BO.App.Models;

public sealed class DepartmentImportRow
{
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string ParentDepartmentCode { get; set; } = string.Empty;
    public string LevelRaw { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string IsActiveRaw { get; set; } = string.Empty;
}
