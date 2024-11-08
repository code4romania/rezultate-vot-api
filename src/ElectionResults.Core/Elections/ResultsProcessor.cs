﻿using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Infrastructure;

namespace ElectionResults.Core.Elections
{
    public class ResultsProcessor
    {
        public static ElectionResultsResponse PopulateElectionResults(Turnout electionTurnout, Ballot ballot, List<CandidateResult> candidates, List<Party> parties)
        {
            ElectionResultsResponse results = new ElectionResultsResponse();
            if (candidates == null)
            {
                results.Candidates = new List<CandidateResponse>();
                results.TotalVotes = 0;
                results.EligibleVoters = 0;
                results.NullVotes = 0;
                results.ValidVotes = 0;
                return results;
            }
            results.NullVotes = electionTurnout.NullVotes;
            results.CountedVotes = electionTurnout.ValidVotes + electionTurnout.NullVotes;
            results.TotalVotes = electionTurnout.TotalVotes;
            results.ValidVotes = electionTurnout.ValidVotes;
            results.EligibleVoters = electionTurnout.EligibleVoters;
            results.TotalSeats = candidates.Sum(c => c.Seats1 + c.Seats2);
            results.VotesByMail = electionTurnout.VotesByMail != 0 ? electionTurnout.VotesByMail : (int?)null;
            if (ballot.BallotType == BallotType.Referendum)
            {
                if (results.ValidVotes == 0)
                {
                    results.ValidVotes = results.TotalVotes - results.NullVotes;
                }

                results.Candidates = new List<CandidateResponse>
                {
                    new CandidateResponse
                    {
                        Name = "DA",
                        ShortName = "DA",
                        Votes = candidates.FirstOrDefault().YesVotes,
                    },
                    new CandidateResponse
                    {
                        Name = "NU",
                        ShortName = "NU",
                        Votes = candidates.FirstOrDefault().NoVotes,
                    },
                    new CandidateResponse
                    {
                        Name = "NU AU VOTAT",
                        ShortName = "NU AU VOTAT",
                        Votes = (results.EligibleVoters - results.TotalVotes).GetValueOrDefault(),
                    }
                };
            }
            else
            {
                var colors = new List<string>();
                var logos = new List<string>();
                results.Candidates = [];
                foreach (var candidate in candidates)
                {
                    var matchingParty = parties.GetMatchingParty(candidate.ShortName)
                                        ?? parties.FirstOrDefault(p => p.Alias.GenerateSlug().Equals(candidate.Name.GenerateSlug()))
                                        ?? parties.FirstOrDefault(p => p.Name.GenerateSlug().Equals(candidate.Name.GenerateSlug()))
                                        ?? parties.FirstOrDefault(p => p.Alias.GenerateSlug().ContainsString(candidate.Name.GenerateSlug()))
                                        ?? parties.FirstOrDefault(p => p.Name.GenerateSlug().ContainsString(candidate.Name.GenerateSlug()));
                    
                    var name = candidate.GetCandidateName(ballot);
                    var shortName = candidate.GetCandidateShortName(ballot);

                    if (matchingParty != null)
                    {
                        colors.Add(matchingParty.Color);
                        logos.Add(matchingParty.LogoUrl);

                        name = matchingParty.Name;
                        shortName = matchingParty.ShortName;
                    }
                    else
                    {
                        colors.Add(null);
                        logos.Add(null);
                    }

                    results.Candidates.Add(new CandidateResponse
                    {
                        ShortName = shortName,
                        Name = name,
                        Votes = candidate.Votes,
                        PartyColor = candidate.GetPartyColor(),
                        PartyName = candidate.Party?.Name,
                        PartyLogo = candidate.Party?.LogoUrl,
                        Seats = candidate.TotalSeats != 0 ? candidate.TotalSeats : candidate.Seats1 + candidate.Seats2,
                        TotalSeats = candidate.TotalSeats != 0 ? candidate.TotalSeats : candidate.Seats1 + candidate.Seats2
                    });
                }
                
                for (var i = 0; i < results.Candidates.Count; i++)
                {
                    var candidate = results.Candidates[i];
                    if (candidate.PartyColor.IsEmpty())
                    {
                        candidate.PartyColor = colors[i] ?? Consts.IndependentCandidateColor;
                    }
                    if (candidate.PartyLogo.IsEmpty())
                    {
                        candidate.PartyLogo = logos[i];
                    }
                }
            }

            results.Candidates = results.Candidates.OrderByDescending(c => c.Votes).ToList();
            if (ballot.BallotType == BallotType.Referendum)
            {
                results.Candidates = results.Candidates.OrderForReferendum(ballot.Election);
            }

            return results;
        }

    }
}
