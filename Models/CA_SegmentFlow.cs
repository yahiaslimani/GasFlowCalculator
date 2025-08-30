namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents the calculated gas flow through a segment for a specific time period
    /// </summary>
    public class CA_SegmentFlow
    {
        public int Id { get; set; }
        public int SegmentId { get; set; }
        public int StartPointId { get; set; }
        public int EndPointId { get; set; }
        public DateTime FlowDate { get; set; }
        public decimal VolumeFromPrevPoint { get; set; }
        public decimal VolumeChange { get; set; }
        public decimal VolumePassThru { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public string? Notes { get; set; }

        // Navigation properties for display purposes
        public BLT_Segment? Segment { get; set; }
        public CA_Point? StartPoint { get; set; }
        public CA_Point? EndPoint { get; set; }
    }
}
