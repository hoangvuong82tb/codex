using System.Text.RegularExpressions;

namespace ASMILE.BO.Shared.Security;

public static class ValidationConstants
{
    public const int MaxNameLength = 250;
    public const int MaxCodeLength = 50;
    public const int MaxPhoneLength = 30;
    public const int MaxEmailLength = 254;
    public const int MaxPathLength = 500;
    public const int MaxSearchLength = 200;

    public static readonly Regex ControlCharsRegex = new("[\\u0000-\\u001F\\u007F]", RegexOptions.Compiled);
}
