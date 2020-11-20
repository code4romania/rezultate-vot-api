using System.Collections.Generic;

namespace ElectionResults.Core.Endpoints.Response
{
    public class PartyList
    {
        public string Name { get; set; }

        public List<BasicCandidateInfo> Candidates { get; set; }
    }
}