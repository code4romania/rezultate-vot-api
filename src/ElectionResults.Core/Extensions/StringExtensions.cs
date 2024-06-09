using System.Globalization;
using System.Text;
using Diacritics.Extensions;
using Humanizer;

namespace ElectionResults.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool IsParty(this string partyName)
        {
            if (partyName.IsEmpty())
                return false;
            partyName = partyName?.ToLower();
            return partyName != null && partyName.StartsWith("partidul ") || partyName.StartsWith("conventia ")
                                                                          || partyName.StartsWith("uniunea ")
                                                                          || partyName.StartsWith("asociatia ")
                                                                          || partyName.StartsWith("blocul ")
                                                                          || partyName.StartsWith("federatia ")
                                                                          || partyName.StartsWith("forumul ")
                                                                          || partyName.StartsWith("comunitatea ")
                                                                          || partyName.StartsWith("alianta ");
        }
        public static string Or(this string first, string second)
        {
            if (first.IsEmpty() == false)
                return first;
            return second;
        }

        public static bool EqualsIgnoringAccent(this string first, string second)
        {
            return string.Compare(first, second, CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0;
        }

        public static bool ContainsString(this string value, string substring)
        {
            var s1 = value?.ToLower().RemoveDiacritics();
            var s2 = substring?.ToLower().RemoveDiacritics();
            return s1.IsNotEmpty() && s2.IsNotEmpty() && (s1 == s2 || s1.Contains(s2 ?? string.Empty));
        }

        public static bool IsNotEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value) == false;
        }

        public static bool IsEmpty(this string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        public static string GenerateSlug(this string phrase)
        {
            if (phrase.IsEmpty())
            {
                return string.Empty;
            }
            string str = phrase.ToLowerInvariant();

            // Remove invalid characters
            str = RemoveDiacritics(str).Dehumanize();


            return str;
        }
        public static bool InvariantEquals(this string first, string second)
        {
            return first.GenerateSlug() == second.GenerateSlug();
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



    }
}