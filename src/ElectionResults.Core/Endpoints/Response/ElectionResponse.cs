using System.Collections.Generic;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionResponse
    {
        public string Id { get; set; }

        public ElectionScope Scope { get; set; }

        public ElectionMeta Meta { get; set; }

        public ElectionTurnout Turnout { get; set; }

        public ElectionResultsResponse Results { get; set; }

        public ElectionObservation Observation { get; set; }
        
        public List<ArticleResponse> ElectionNews { get; set; }
    }
}
