namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a pipeline segment between two points in the network
    /// </summary>
    public class BLT_Segment
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int NetworkId { get; set; }
        public int StartPointId { get; set; }
        public int EndPointId { get; set; }
        public decimal Capacity { get; set; }
        public string CapacityUnit { get; set; } = "MCF"; // Million Cubic Feet
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
        public string? Description { get; set; }
    }
}
