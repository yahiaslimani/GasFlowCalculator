using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a pipeline network configuration
    /// </summary>
    [Table("CA_Networks")]
    public class CA_Network
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        public virtual ICollection<BLT_Segment> Segments { get; set; } = new List<BLT_Segment>();
        public virtual ICollection<CA_Point> Points { get; set; } = new List<CA_Point>();

        public override string ToString()
        {
            return Name;
        }
    }
}
