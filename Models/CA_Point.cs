namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a point in the pipeline network (receipt, compressor station, or delivery)
    /// </summary>
    public class CA_Point
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public PointType PointType { get; set; }
        public int NetworkId { get; set; }
        public bool IsActive { get; set; } = true;
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }
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
