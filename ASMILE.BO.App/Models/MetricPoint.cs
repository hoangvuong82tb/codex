namespace ASMILE.BO.App.Models;

public sealed class MetricPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public double Percent { get; set; }
}
