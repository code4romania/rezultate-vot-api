using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Diacritics.Extensions;

namespace ElectionResults.Core.Extensions
{
    public static class StringExtensions
    {
        public static string Sha256(this string input)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = sha.ComputeHash(bytes);

                return Convert.ToBase64String(hash);
            }
        }
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

        public static string GetPartyShortName(this string name, string shortName)
        {
            if (shortName == null)
            {
                var partyName = name.ToLower();
                if (partyName.StartsWith("partidul ") || partyName.StartsWith("conventia ")
                                                      || partyName.StartsWith("uniunea ")
                                                      || partyName.StartsWith("asociatia ")
                                                      || partyName.StartsWith("blocul ")
                                                      || partyName.StartsWith("alianta "))
                {
                    return GetInitials(name);
                }

                return name;
            }

            if (shortName.ToLower() == "ci")
                return name;
            return name?.ToUpper();
        }

        private static string GetInitials(this string party)
        {
            var words = party.Split(' ');
            string partyName = "";
            if (party.StartsWith("ALIANTA ELECTORALA ("))
            {
                var strings = party.Split("ALIANTA ELECTORALA (");
                return strings[1].Trim(')', ' ');
            }
            if (words.Count() > 5)
                return party.ToUpper();
            foreach (var word in words)
            {
                if (word.Length > 3)
                    partyName += word.Trim('+', '(', ')', '.', '"').ToUpper()[0];
            }

            return partyName;
        }
        public static string ToSnakeCase(this string str)
        {
            Regex pattern = new Regex(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");
            return string.Join("_", pattern.Matches(str)).ToLower();
        }

        public static string NormalizeCountryName(this string countryName)
        {
            return countryName.Replace("REPUBLICA ALBANIA", "Albania")
                .Replace("REGATUL ARABIEI SAUDITE", "Arabia Saudita")
                .Replace("REPUBLICA BELARUS", "Belarus")
                .Replace("BOSNIA SI HERTEGOVINA", "Bosnia")
                .Replace("REPUBLICA CEHA", "Cehia")
                .Replace("REPUBLICA COREEA", "Coreea De Sud")
                .Replace("REPUBLICA ELENA", "Grecia")
                .Replace("REPUBLICA INDIA", "India")
                .Replace("REPUBLICA INDONEZIA", "Indonezia")
                .Replace("REGATUL HASEMIT AL IORDANIEI", "Iordania")
                .Replace("REPUBLICA MACEDONIA DE NORD", "Macedonia")
                .Replace("REGATUL UNIT AL MARII BRITANII SI IRLANDEI DE NORD", "Marea Britanie")
                .Replace("REGATUL MAROC", "Maroc")
                .Replace("REPUBLICA ISLAMICA PAKISTAN", "Pakistan")
                .Replace("REPUBLICA PERU", "Peru")
                .Replace("FEDERATIA RUSA", "Rusia")
                .Replace("REPUBLICA SINGAPORE", "Singapore")
                .Replace("REPUBLICA ARABA SIRIANA", "Siria")
                .Replace("REPUBLICA SLOVACA", "Slovacia")
                .Replace("REGATUL THAILANDEI", "Thailanda")
                .Replace("REPUBLICA ORIENTALA A URUGUAYULUI", "Uruguay")
                .Replace("REPUBLICA SOCIALISTA VIETNAM", "Vietnam");
        }

    }
}