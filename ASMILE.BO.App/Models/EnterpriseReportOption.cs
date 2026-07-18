namespace ASMILE.BO.App.Models;

public sealed record EnterpriseReportOption(string Code, string Label)
{
    public override string ToString() => Label;
}
