using System.Text.RegularExpressions;

namespace FamilyPhotos.Services;

public partial class DateParsingService
{
    // Match YYYYMMDD (with optional separators)
    [GeneratedRegex(@"(?<!\d)(\d{4})([-_]?)(\d{2})\2(\d{2})(?!\d)")]
    private static partial Regex FullDateRegex();

    // Match YYYYMM (with optional separator)
    [GeneratedRegex(@"(?<!\d)(\d{4})([-_]?)(\d{2})(?!\d)")]
    private static partial Regex YearMonthRegex();

    // Match YYYY alone
    [GeneratedRegex(@"(?<!\d)(\d{4})(?!\d)")]
    private static partial Regex YearOnlyRegex();

    public string? ExtractDateFromFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) return null;

        // Remove extension for matching
        var nameOnly = Path.GetFileNameWithoutExtension(fileName);

        // Try YYYYMMDD first
        var match = FullDateRegex().Match(nameOnly);
        if (match.Success)
        {
            var year = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[3].Value);
            var day = int.Parse(match.Groups[4].Value);

            if (year >= 1800 && year <= 2100 && month >= 1 && month <= 12 && day >= 1 && day <= 31)
            {
                return $"{year:D4}-{month:D2}-{day:D2}";
            }
        }

        // Try YYYYMM
        var ymMatch = YearMonthRegex().Match(nameOnly);
        if (ymMatch.Success)
        {
            var year = int.Parse(ymMatch.Groups[1].Value);
            var month = int.Parse(ymMatch.Groups[3].Value);

            if (year >= 1800 && year <= 2100 && month >= 1 && month <= 12)
            {
                return $"{year:D4}-{month:D2}";
            }
        }

        // Try YYYY only
        var yMatch = YearOnlyRegex().Match(nameOnly);
        if (yMatch.Success)
        {
            var year = int.Parse(yMatch.Groups[1].Value);
            if (year >= 1800 && year <= 2100)
            {
                return $"{year:D4}";
            }
        }

        return null;
    }

    public int? ExtractYearFromDate(string? date)
    {
        if (string.IsNullOrEmpty(date) || date.Length < 4) return null;
        if (int.TryParse(date[..4], out var year)) return year;
        return null;
    }

    public int? ExtractMonthFromDate(string? date)
    {
        if (string.IsNullOrEmpty(date) || date.Length < 7) return null;
        if (int.TryParse(date.AsSpan(5, 2), out var month)) return month;
        return null;
    }
}
