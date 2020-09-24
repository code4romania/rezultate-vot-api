using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("countries")]
    public class Country
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

    }
}