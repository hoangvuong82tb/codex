namespace ASMILE.BO.Infrastructure.Options;

public sealed class TrainingDataOptions
{
    public const string SectionName = "TrainingData";
    public bool AllowDestructiveReset { get; set; } = false;
    public bool AllowInProduction { get; set; } = false;
    public bool AllowResetWithRealDataRisk { get; set; } = false;
    public string RequireConfirmationText { get; set; } = "RESET DATA";
}
