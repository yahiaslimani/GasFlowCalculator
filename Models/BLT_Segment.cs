using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a pipeline segment between two points in the network
    /// </summary>
    [Table("BLT_Segments")]
    public class BLT_Segment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int NetworkId { get; set; }

        [Required]
        public int StartPointId { get; set; }

        [Required]
        public int EndPointId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation properties
        [ForeignKey("NetworkId")]
        public virtual CA_Network? Network { get; set; }

        [ForeignKey("StartPointId")]
        public virtual CA_Point? StartPoint { get; set; }

        [ForeignKey("EndPointId")]
        public virtual CA_Point? EndPoint { get; set; }

        public virtual ICollection<CA_SegmentFlow> SegmentFlows { get; set; } = new List<CA_SegmentFlow>();
    }
}
