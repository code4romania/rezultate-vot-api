using System.Collections.Generic;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ArticleResponse : Article
    {
        public List<ArticlePicture> Images { get; set; }
    }
}