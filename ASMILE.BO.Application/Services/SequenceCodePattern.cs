using System.Text.RegularExpressions;

namespace ASMILE.BO.Application.Services;

/// <summary>Validates and renders configurable master-data and document code patterns.</summary>
public static class SequenceCodePattern
{
    private static readonly Regex TokenRegex = new(@"\{(?<token>yyyyMMdd|yyyy|MM|dd|0+)\}", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static bool IsValid(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        var matches = TokenRegex.Matches(pattern);
        var sequenceTokens = matches.Cast<Match>()
            .Count(match => match.Groups["token"].Value.All(character => character == '0'));
        var remaining = TokenRegex.Replace(pattern, string.Empty);
        var renderedLength = matches.Cast<Match>().Aggregate(pattern.Length, (length, match) =>
        {
            var token = match.Groups["token"].Value;
            var replacementLength = token.All(character => character == '0')
                ? token.Length
                : token.ToLowerInvariant() switch
                {
                    "yyyymmdd" => 8,
                    "yyyy" => 4,
                    "mm" or "dd" => 2,
                    _ => 0
                };
            return length - match.Length + replacementLength;
        });

        return sequenceTokens == 1
            && renderedLength <= 50
            && !remaining.Contains('{')
            && !remaining.Contains('}');
    }

    public static string Render(string pattern, DateTime date, long sequence)
    {
        if (!IsValid(pattern))
        {
            throw new ArgumentException("Quy tắc mã không hợp lệ.", nameof(pattern));
        }

        return TokenRegex.Replace(pattern, match =>
        {
            var token = match.Groups["token"].Value;
            return token.All(character => character == '0')
                ? sequence.ToString($"D{token.Length}")
                : RenderDateToken(token, date);
        });
    }

    public static string GetPrefix(string pattern, DateTime date)
    {
        var sequenceToken = FindSequenceToken(pattern);
        return RenderDateTokens(pattern[..sequenceToken.Index], date);
    }

    public static long GetNextSequence(string pattern, DateTime date, IEnumerable<string?> existingCodes)
    {
        if (!IsValid(pattern))
        {
            throw new ArgumentException("Quy tắc mã không hợp lệ.", nameof(pattern));
        }

        var sequenceToken = FindSequenceToken(pattern);
        var prefix = RenderDateTokens(pattern[..sequenceToken.Index], date);
        var suffix = RenderDateTokens(pattern[(sequenceToken.Index + sequenceToken.Length)..], date);
        var sequenceWidth = sequenceToken.Groups["token"].Value.Length;
        var matcher = new Regex(
            $"^{Regex.Escape(prefix)}(?<sequence>\\d{{{sequenceWidth},}}){Regex.Escape(suffix)}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        long max = 0;
        foreach (var code in existingCodes)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            var match = matcher.Match(code.Trim());
            if (match.Success
                && long.TryParse(match.Groups["sequence"].Value, out var sequence)
                && sequence > max)
            {
                max = sequence;
            }
        }

        return checked(max + 1);
    }

    private static Match FindSequenceToken(string pattern)
        => TokenRegex.Matches(pattern).Cast<Match>()
            .Single(match => match.Groups["token"].Value.All(character => character == '0'));

    private static string RenderDateTokens(string value, DateTime date)
        => TokenRegex.Replace(value, match => RenderDateToken(match.Groups["token"].Value, date));

    private static string RenderDateToken(string token, DateTime date)
        => token.ToLowerInvariant() switch
        {
            "yyyymmdd" => date.ToString("yyyyMMdd"),
            "yyyy" => date.ToString("yyyy"),
            "mm" => date.ToString("MM"),
            "dd" => date.ToString("dd"),
            _ => throw new ArgumentException("Không thể render token số thứ tự trong phần ngày.", nameof(token))
        };
}
