using Diacritics.Extensions;
using System.Globalization;

namespace ElectionResults.Hangfire.Extensions;

public static class StringExtensions
{
    public static bool InvariantEquals(this string first, string second)
    {
        return string.Compare(first.RemoveDiacritics(), second.RemoveDiacritics(), CultureInfo.InvariantCulture,
             CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0;
    }
}