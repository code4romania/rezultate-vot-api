using System;
using System.Collections.Generic;
using System.Linq;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Infrastructure;

namespace ElectionResults.Core.Extensions
{
    public static class NameProcessingExtensions
    {
        public static List<CandidateResponse> OrderForReferendum(this List<CandidateResponse> candidates, Election election)
        {
            var yesCandidate = candidates.Find(c => c.Name == "DA");
            var noCandidate = candidates.Find(c => c.Name == "NU");
            var noneCandidate = candidates.Find(c => c.Name == "NU AU VOTAT");

            if (string.Equals(election.Subtitle, "Validat"))
            {
                if (yesCandidate.Votes > noCandidate.Votes)
                {
                    candidates.RemoveAll(c => c.Name == "DA");
                    candidates.Insert(0, yesCandidate);
                }
                else
                {
                    candidates.RemoveAll(c => c.Name == "NU");
                    candidates.Insert(0, noCandidate);
                }
            }
            else
            {
                candidates.RemoveAll(c => c.Name == "NU AU VOTAT");
                candidates.Insert(0, noneCandidate);
            }

            return candidates;
        }

        public static string GetCandidateShortName(this CandidateResult c, Ballot ballot)
        {
            try
            {
                if (ballot.BallotType == BallotType.EuropeanParliament || ballot.BallotType == BallotType.Senate ||
                    ballot.BallotType == BallotType.House)
                    return c.ShortName;
                if (c.Name.IsParty() || c.Name.IsEmpty())
                    return c.ShortName;
                var processedName = ParseShortName(c.Name);
                return processedName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return c.ShortName;
            }
        }

        public static string ParseShortName(this string shortName)
        {
            if (shortName.ToLower() == "independent")
                return shortName;

            var fullName = shortName.Replace("CANDIDAT INDEPENDENT - ", "").Trim();
            var nameParts = fullName.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (nameParts.Length < 2)
                return shortName; // Return original if not enough parts

            // Assuming the first part is the last name and the rest are first names
            var lastName = nameParts[0];
            var firstName = string.Join(" ", nameParts.Skip(1));

            // Handle cases where the first name might be hyphenated or have multiple parts
            var firstInitial = firstName[0].ToString().ToUpper() + ". ";

            return firstInitial + lastName;
        }


        public static string GetPartyColor(this CandidateResult c)
        {
            if (c.Party != null && c.Party.Name?.ToLower() == "independent" || c.Name.ToLower() == "independent")
                return Consts.IndependentCandidateColor;
            return c.Party?.Color;
        }

        public static string GetCandidateName(this CandidateResult c, Ballot ballot)
        {
            if (ballot.BallotType == BallotType.EuropeanParliament || ballot.BallotType == BallotType.Senate ||
                ballot.BallotType == BallotType.House)
                return c.Party?.Name.Or(c.PartyName).Or(c.Name) ?? c.Name.Or(c.PartyName);
            return c.Name.IsEmpty() ? c.PartyName : c.Name;
        }
    }
}
