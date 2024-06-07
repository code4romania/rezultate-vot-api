using System.Text.Json;
using System.Text.RegularExpressions;

namespace ElectionResults.API.Converters
{
    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public static SnakeCaseNamingPolicy Instance { get; } = new();
        private static readonly Regex Pattern = new Regex(@"[A-Z]{2,}(?=[A-Z][a-z]+[0-9]*|\b)|[A-Z]?[a-z]+[0-9]*|[A-Z]|[0-9]+");

        private static string ToSnakeCase(string str)
        {
            return string.Join("_", Pattern.Matches(str)).ToLower();
        }

        public override string ConvertName(string name)
        {
            return ToSnakeCase(name);
        }
    }
}