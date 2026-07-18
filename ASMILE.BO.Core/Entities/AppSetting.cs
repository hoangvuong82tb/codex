using System.ComponentModel.DataAnnotations;

namespace ASMILE.BO.Core.Entities;

public sealed class AppSetting
{
    public int Id { get; set; }

    [MaxLength(100)]
    public string Key { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Value { get; set; } = string.Empty;
}

