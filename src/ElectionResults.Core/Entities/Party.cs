using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("parties")]
    public class Party
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public string LogoUrl { get; set; }

        public string Color { get; set; }
        
        public string Alias { get; set; }
    }
}