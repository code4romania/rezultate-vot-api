using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("authors")]
    public class Author
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Avatar { get; set; }
    }
}