using System.Text;

namespace ASMILE.BO.Shared.Security;

public static class InputSanitizer
{
    public static string NormalizeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Normalize(NormalizationForm.FormKC).Trim();
        return RemoveControlCharacters(normalized);
    }

    public static string TrimAndLimit(string? value, int maxLength)
    {
        var normalized = NormalizeText(value);
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..maxLength];
    }

    public static string SanitizeSearchKeyword(string? keyword)
    {
        var value = TrimAndLimit(keyword, ValidationConstants.MaxSearchLength);
        return value.Replace("\"", string.Empty).Replace("'", string.Empty);
    }

    public static string RemoveControlCharacters(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return ValidationConstants.ControlCharsRegex.Replace(value, string.Empty);
    }
}
