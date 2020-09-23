using System;
using System.Collections.Generic;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ArticleResponse
    {
        public int Id { get; set; }
     
        public List<ArticlePicture> Images { get; set; }

        public Author Author { get; set; }

        public DateTime Timestamp { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string Link { get; set; }

        public string Embed { get; set; }
    }
}