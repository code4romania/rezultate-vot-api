using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("articlepictures")]
    public class ArticlePicture
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Article))]
        public int ArticleId { get; set; }
        
        public string Url { get; set; }
    }
}