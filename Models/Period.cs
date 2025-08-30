namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a time period for flow calculations
    /// </summary>
    public class Period
    {
        public string Name { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }

        public Period()
        {
        }

        public Period(string name, DateTime startTime, DateTime endTime)
        {
            Name = name;
            StartTime = startTime;
            EndTime = endTime;
        }

        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Creates common period options
        /// </summary>
        public static List<Period> GetCommonPeriods()
        {
            var today = DateTime.Today;
            var periods = new List<Period>
            {
                new("Today", today, today.AddDays(1)),
                new("Yesterday", today.AddDays(-1), today),
                new("Last 7 Days", today.AddDays(-7), today.AddDays(1)),
                new("Last 30 Days", today.AddDays(-30), today.AddDays(1)),
                new("This Month", new DateTime(today.Year, today.Month, 1), 
                    new DateTime(today.Year, today.Month, 1).AddMonths(1)),
                new("Last Month", new DateTime(today.Year, today.Month, 1).AddMonths(-1), 
                    new DateTime(today.Year, today.Month, 1))
            };

            return periods;
        }
    }
}
