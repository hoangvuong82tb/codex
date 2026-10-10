using System.Text;
using System.Text.RegularExpressions;

namespace ASMILE.BO.App.Services;

/// <summary>
/// Renders and sequences the persisted stock-voucher code pattern.
/// Supported tokens are date tokens ({yyyyMMdd}, {yyyy}, {MM}, {dd}) and one
/// numeric sequence token such as {0000}.
/// </summary>
public static class InventoryDocumentCodePattern
{
    private static readonly Regex TokenRegex = new(@"\{(yyyyMMdd|yyyy|MM|dd|0+)\}", RegexOptions.Compiled);

    public static string Normalize(string? pattern, string fallback)
    {
        var value = (pattern ?? string.Empty).Trim();
        var tokens = TokenRegex.Matches(value);
        var sequenceTokens = tokens.Count(x => x.Groups[1].Value.All(c => c == '0'));
        return TokenRegex.IsMatch(value) && sequenceTokens == 1
            ? value
            : fallback;
    }

    public static string Render(string pattern, DateTime date, int sequence)
    {
        return TokenRegex.Replace(pattern, match =>
        {
            var token = match.Groups[1].Value;
            return token switch
            {
                "yyyyMMdd" => date.ToString("yyyyMMdd"),
                "yyyy" => date.ToString("yyyy"),
                "MM" => date.ToString("MM"),
                "dd" => date.ToString("dd"),
                _ => sequence.ToString($"D{token.Length}")
            };
        });
    }

    public static int GetNextSequence(string pattern, DateTime date, IEnumerable<string?> existingCodes)
    {
        var matcher = BuildMatcher(pattern, date);
        var max = 0;
        foreach (var code in existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var match = matcher.Match(code.Trim());
            if (match.Success
                && int.TryParse(match.Groups["seq"].Value, out var sequence)
                && sequence > max)
            {
                max = sequence;
            }
        }

        return max + 1;
    }

    private static Regex BuildMatcher(string pattern, DateTime date)
    {
        var builder = new StringBuilder("^");
        var position = 0;
        foreach (Match tokenMatch in TokenRegex.Matches(pattern))
        {
            builder.Append(Regex.Escape(pattern[position..tokenMatch.Index]));
            var token = tokenMatch.Groups[1].Value;
            builder.Append(token.All(c => c == '0')
                ? $"(?<seq>\\d{{{token.Length},}})"
                : token switch
                {
                    "yyyyMMdd" => Regex.Escape(date.ToString("yyyyMMdd")),
                    "yyyy" => Regex.Escape(date.ToString("yyyy")),
                    "MM" => Regex.Escape(date.ToString("MM")),
                    "dd" => Regex.Escape(date.ToString("dd")),
                    _ => Regex.Escape(tokenMatch.Value)
                });
            position = tokenMatch.Index + tokenMatch.Length;
        }

        builder.Append(Regex.Escape(pattern[position..]));
        builder.Append("$");
        return new Regex(builder.ToString(), RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }
}
