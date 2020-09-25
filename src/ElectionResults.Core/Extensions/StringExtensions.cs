using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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

        public static string Or(this string first, string second)
        {
            if (first.IsEmpty() == false)
                return first;
            return second;
        }

        public static bool EqualsIgnoringAccent(this string first, string second)
        {
            return string.Compare(first?.ToLower(), second?.ToLower(), CultureInfo.CurrentCulture, CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreCase) == 0;
        }

        public static bool ContainsString(this string value, string substring)
        {
            return value.IsNotEmpty() && value.ToLower().Contains(substring.ToLower());
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
            var enumerable = str.Select((x, i) => i > 0 && char.IsUpper(x) && str.IndexOf(x) > 0 && str[str.IndexOf(x)-1] != '_' ? "_" + x : x.ToString());
            return string.Concat(enumerable).ToLower();
        }

        public static string ConvertEnumToString(this Enum type)
        {
            return type
                .GetType()
                .GetMember(type.ToString())
                .FirstOrDefault()
                ?.GetCustomAttribute<DescriptionAttribute>()
                ?.Description ?? type.ToString();
        }
    }
}