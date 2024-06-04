﻿using System.Collections.Generic;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionResponse
    {
        public string Id { get; set; }

        public ElectionScope Scope { get; set; }

        public ElectionMeta Meta { get; set; }

        public ElectionTurnout Turnout { get; set; }

        public ElectionResultsResponse Results { get; set; }

        public Observation Observation { get; set; }
        
        public List<ArticleResponse> ElectionNews { get; set; }
        public bool Aggregated { get; set; }
    }
}
