using Diacritics.Extensions;
using Humanizer;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ElectionResults.Hangfire.Extensions;

public static class StringExtensions
{
    public static bool InvariantEquals(this string first, string second)
    {
        return first.GenerateSlug() == second.GenerateSlug();
    }

    public static string GenerateSlug(this string phrase)
    { 
        string str = phrase.ToLowerInvariant();

        // Remove invalid characters
        str = RemoveDiacritics(str).Dehumanize();


        return str;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }


    public static bool ContainsString(this string value, string substring)
    {
        var s1 = value?.GenerateSlug();
        var s2 = substring?.GenerateSlug();
        return s1.IsNotEmpty() && s2.IsNotEmpty() && (s1 == s2 || s1.Contains(s2 ?? string.Empty));
    }

    public static bool IsNotEmpty(this string value)
    {
        return string.IsNullOrWhiteSpace(value) == false;
    }
}