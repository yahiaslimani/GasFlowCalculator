using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a point in the pipeline network (receipt, compressor station, or delivery)
    /// </summary>
    [Table("CA_Points")]
    public class CA_Point
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public PointType PointType { get; set; }

        [Required]
        public int NetworkId { get; set; }

        public bool IsActive { get; set; } = true;

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Latitude { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal? Longitude { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime? ModifiedDate { get; set; }

        // Navigation properties
        [ForeignKey("NetworkId")]
        public virtual CA_Network? Network { get; set; }

        public virtual ICollection<BLT_Segment> StartSegments { get; set; } = new List<BLT_Segment>();

        public virtual ICollection<BLT_Segment> EndSegments { get; set; } = new List<BLT_Segment>();
    }

    /// <summary>
    /// Enumeration for different types of points in the network
    /// </summary>
    public enum PointType
    {
        Receipt = 1,
        CompressorStation = 2,
        Delivery = 3
    }
}
