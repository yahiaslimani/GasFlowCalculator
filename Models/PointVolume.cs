namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents volume data for a point at a specific date
    /// </summary>
    public class PointVolume
    {
        public int PointId { get; set; }
        public DateTime Date { get; set; }
        public decimal Volume { get; set; }
        public string VolumeType { get; set; } = string.Empty; // "Receipt" or "Delivery"
        public string? Description { get; set; }
    }
}