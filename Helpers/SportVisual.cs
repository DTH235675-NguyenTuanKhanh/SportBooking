using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SportBooking.Helpers;

/// <summary>Presentation assets are keyed by sport name, never by database IDs.</summary>
public static class SportVisual
{
    private static readonly Regex FootballSize = new(@"\b([57])\s*(?:nguoi|(?:vs?|x)\s*\1)\b");

    public static string Key(string? name)
    {
        var normalized = Normalize(name);

        if (normalized.Contains("pickleball")) return "pickleball";
        if (normalized.Contains("cau long") || normalized.Contains("badminton")) return "badminton";
        if (normalized.Contains("tennis")) return "tennis";
        if (normalized.Contains("bong ro") || normalized.Contains("basketball")) return "basketball";
        if (normalized.Contains("bong chuyen") || normalized.Contains("volleyball")) return "volleyball";
        if (normalized.Contains("bong da") || normalized.Contains("football") || normalized.Contains("soccer")) return "football";
        return "all";
    }

    public static string IconUrl(string? name)
    {
        var key = Key(name);
        var extension = key == "all" ? "svg" : "webp";
        return $"/images/sports/icons/{key}.{extension}";
    }

    public static string? CourtIllustrationUrl(string? sportName, string? courtName, string? courtDescription)
    {
        const string directory = "~/images/courts/defaults";
        return Key(sportName) switch
        {
            "football" => (GetFootballSize(courtName) ?? GetFootballSize(sportName) ?? GetFootballSize(courtDescription)) switch
            {
                "5" => $"{directory}/bong-da-5-nguoi.webp",
                "7" => $"{directory}/bong-da-7-nguoi.webp",
                _ => $"{directory}/bong-da.webp"
            },
            "badminton" => $"{directory}/cau-long.webp",
            "tennis" => $"{directory}/tennis.webp",
            "pickleball" => $"{directory}/pickleball.webp",
            "basketball" => $"{directory}/bong-ro.webp",
            "volleyball" => $"{directory}/bong-chuyen.webp",
            _ => null
        };
    }

    private static string? GetFootballSize(string? value)
    {
        var sizes = FootballSize.Matches(Normalize(value))
            .Select(match => match.Groups[1].Value).Distinct().ToArray();
        return sizes.Length == 1 ? sizes[0] : null;
    }

    private static string Normalize(string? value) => new string((value ?? string.Empty).Normalize(NormalizationForm.FormD)
        .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
        .ToArray()).ToLowerInvariant().Replace('đ', 'd');
}
