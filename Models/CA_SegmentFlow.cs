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

        // Calculated properties for capacity validation
        public decimal ActualFlow => Math.Abs(VolumePassThru);
        public decimal Capacity => Segment?.Capacity ?? 0;
        public decimal UsagePercentage => Capacity > 0 ? (ActualFlow / Capacity) * 100 : 0;
        public bool IsOverCapacity => ActualFlow > Capacity;
        public decimal AvailableCapacity => Math.Max(0, Capacity - ActualFlow);
        public string CapacityStatus => IsOverCapacity ? "OVER CAPACITY" : 
                                       UsagePercentage > 90 ? "HIGH USAGE" : 
                                       UsagePercentage > 75 ? "MODERATE USAGE" : "NORMAL";
    }
}
