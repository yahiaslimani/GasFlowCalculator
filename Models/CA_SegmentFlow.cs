using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents the calculated gas flow through a segment for a specific time period
    /// </summary>
    [Table("CA_SegmentFlows")]
    public class CA_SegmentFlow
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int SegmentId { get; set; }

        [Required]
        public int StartPointId { get; set; }

        [Required]
        public int EndPointId { get; set; }

        [Required]
        public DateTime FlowDate { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal VolumeFromPrevPoint { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal VolumeChange { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal VolumePassThru { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey("SegmentId")]
        public virtual BLT_Segment? Segment { get; set; }

        [ForeignKey("StartPointId")]
        public virtual CA_Point? StartPoint { get; set; }

        [ForeignKey("EndPointId")]
        public virtual CA_Point? EndPoint { get; set; }

        /// <summary>
        /// Saves or updates the segment flow record in the database
        /// </summary>
        public void Save()
        {
            ModifiedDate = DateTime.Now;
            // This would typically be handled by the DbContext
            // Implementation depends on the data access pattern used
        }
    }
}
