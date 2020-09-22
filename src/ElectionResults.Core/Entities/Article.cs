using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("articles")]
    public class Article
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Election))]
        public int ElectionId { get; set; }

        [ForeignKey(nameof(Ballot))]
        public int BallotId { get; set; }

        public Ballot Ballot { get; set; }

        public Election Election { get; set; }

        public Author Author { get; set; }

        public DateTime Timestamp { get; set; }

        [ForeignKey(nameof(Author))]
        public int AuthorId { get; set; }

        public string Title { get; set; }

        public string Body { get; set; }

        public string Link { get; set; }
        public List<ArticlePicture> Pictures { get; set; }
    }

    [Table("authors")]
    public class Author
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }
    }

    [Table("articlepictures")]
    public class ArticlePicture
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Article))]
        public int ArticleId { get; set; }
        
        public string Url { get; set; }
    }
}
