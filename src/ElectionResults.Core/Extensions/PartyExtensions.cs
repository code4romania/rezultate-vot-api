using System.Collections.Generic;
using System.Linq;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Extensions
{
    public static class PartyExtensions
    {
        public static Party GetMatchingParty(this List<Party> parties, string shortName)
        {
            if (shortName.ContainsString("-"))
            {
                string[] members = shortName.Split("-");
                foreach (var member in members)
                {
                    return parties.FirstOrDefault(p =>
                        string.Equals(p.ShortName, member.Trim()));
                }
            }

            if (shortName.ContainsString("+"))
            {
                string[] members = shortName.Split("+");
                foreach (var member in members)
                {
                    return parties.FirstOrDefault(p =>
                        string.Equals(p.ShortName, member.Trim()));
                }
            }

            return parties.FirstOrDefault(p =>
                shortName.ContainsString(p.ShortName + "-")
                || shortName.ContainsString(p.ShortName + "+")
                || shortName.ContainsString(p.ShortName + " +")
                || shortName.ContainsString("+" + p.ShortName)
                || shortName.ContainsString("+ " + p.ShortName)
                || shortName.ContainsString(" " + p.ShortName)
                || shortName.ContainsString(p.ShortName + " "));
        }
    }
}
