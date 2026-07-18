namespace ASMILE.BO.App.Models;

public sealed class DepartmentPeopleImportPayload
{
    public List<DepartmentImportRow> Departments { get; } = [];
    public List<PersonImportRow> People { get; } = [];
}
