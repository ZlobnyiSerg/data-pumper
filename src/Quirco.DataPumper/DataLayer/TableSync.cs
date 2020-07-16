using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quirco.DataPumper.DataLayer
{
    [Table("TableSync", Schema = "dp")]
    public class TableSync
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public string TableName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ActualDate { get; set; }

        public DateTime? PreviousActualDate { get; set; }
    }
}
