using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("localities")]
    public class Locality
    {
        [Key]
        public int LocalityId { get; set; }

        [ForeignKey(nameof(County))]
        public int CountyId { get; set; }

        public string Name { get; set; }

        public County County { get; set; }

        public bool IsCountry { get; set; }

        public int CountryId { get; set; }
    }
}