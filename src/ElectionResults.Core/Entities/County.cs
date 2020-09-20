using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("counties")]
    public class County
    {
        [Key]
        public int CountyId { get; set; }

        public string Name { get; set; }

        public List<Locality> Localities { get; set; }
    }
}