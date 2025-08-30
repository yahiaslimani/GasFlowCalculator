namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a pipeline network configuration
    /// </summary>
    public class CA_Network
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ModifiedDate { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
