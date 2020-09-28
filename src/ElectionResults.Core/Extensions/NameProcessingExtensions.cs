using System;
using System.Collections.Generic;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;

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
            var fullName = shortName.Replace("CANDIDAT INDEPENDENT - ", "");
            var firstInitial = fullName[0].ToString().ToUpper() + ". ";
            string firstname = string.Empty;
            if (fullName.Contains("-"))
            {
                firstname = fullName.Split("-")[0] + "-";
            }
            else firstname = fullName.Split(" ")[0] + " ";

            var candidateName = firstInitial + fullName.Replace(firstname, "");
            return candidateName;
        }

        public static string GetPartyColor(this CandidateResult c)
        {
            if (c.Party != null && c.Party.Name?.ToLower() == "independent" || c.Name.ToLower() == "independent")
                return null;
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
