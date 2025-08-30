using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GasFlowCalculator.Business;
using GasFlowCalculator.Data;
using GasFlowCalculator.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace GasFlowCalculator.Models
{
    /// <summary>
    /// Represents a pipeline network and contains the main flow calculation logic
    /// </summary>
    public class BLT_Network
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly List<CA_SegmentFlow> _segmentFlows = new();

        public BLT_Network()
        {
            // For backwards compatibility when created without DI
            _serviceProvider = null!;
        }

        public BLT_Network(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Main flow calculation method
        /// </summary>
        /// <param name="networkId">Network ID to calculate flows for</param>
        /// <param name="dStart">Start date for calculation period</param>
        /// <param name="dEnd">End date for calculation period</param>
        /// <returns>Number of flows calculated</returns>
        public int CalcFlow(int networkId, DateTime dStart, DateTime dEnd)
        {
            LogHelper.Debug("CalcFlow", "Start...");

            try
            {
                // Step 1: Initialize the network
                var segments = GetSegments(networkId, dStart);
                var points = GetPoints(segments);

                LogHelper.Debug("CalcFlow", $"Found {segments.Count} segments and {points.Count} points");

                // Step 2: Calculate total volumes
                var pointVolumes = GetPointVolumes(points, dStart, dEnd);

                // Step 3: Calculate segment flows
                _segmentFlows.Clear();
                foreach (var segment in segments)
                {
                    GetSegmentFlows(segment, pointVolumes, dStart);
                }

                // Step 4: Totalize flows
                var flows = TotalizeFlows(segments, _segmentFlows);

                LogHelper.Debug("CalcFlow", "Finish: TotalizeFlows");

                // Step 5: Save flows
                using var scope = _serviceProvider?.CreateScope();
                var dbContext = scope?.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                if (dbContext != null)
                {
                    foreach (var flow in flows)
                    {
                        var existingFlow = dbContext.SegmentFlows
                            .FirstOrDefault(f => f.SegmentId == flow.SegmentId && 
                                                f.FlowDate.Date == dStart.Date);

                        if (existingFlow != null)
                        {
                            existingFlow.VolumeFromPrevPoint = flow.VolumeFromPrevPoint;
                            existingFlow.VolumeChange = flow.VolumeChange;
                            existingFlow.VolumePassThru = flow.VolumePassThru;
                            existingFlow.ModifiedDate = DateTime.Now;
                        }
                        else
                        {
                            dbContext.SegmentFlows.Add(flow);
                        }
                    }

                    dbContext.SaveChanges();
                }

                LogHelper.Debug("CalcFlow", $"Saved {flows.Count} flows to database");

                return flows.Count;
            }
            catch (Exception ex)
            {
                LogHelper.Error("CalcFlow", $"Error during flow calculation: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Gets all active segments for the specified network
        /// </summary>
        public List<BLT_Segment> GetSegments(int networkId, DateTime dStart)
        {
            using var scope = _serviceProvider?.CreateScope();
            var segmentFactory = scope?.ServiceProvider.GetRequiredService<CA_SegmentFactory>();
            
            if (segmentFactory != null)
            {
                return segmentFactory.GetAllObjects()
                    .Where(s => s.NetworkId == networkId && s.IsActive)
                    .ToList();
            }

            // Fallback for when DI is not available
            return new List<BLT_Segment>();
        }

        /// <summary>
        /// Gets all points associated with the given segments
        /// </summary>
        public List<CA_Point> GetPoints(List<BLT_Segment> segments)
        {
            using var scope = _serviceProvider?.CreateScope();
            var pointFactory = scope?.ServiceProvider.GetRequiredService<CA_PointFactory>();
            
            if (pointFactory != null)
            {
                var pointIds = segments.SelectMany(s => new[] { s.StartPointId, s.EndPointId }).Distinct();
                return pointFactory.GetAllObjects()
                    .Where(p => pointIds.Contains(p.Id) && p.IsActive)
                    .ToList();
            }

            return new List<CA_Point>();
        }

        /// <summary>
        /// Gets volume data for points within the specified date range
        /// </summary>
        private Dictionary<int, decimal> GetPointVolumes(List<CA_Point> points, DateTime dStart, DateTime dEnd)
        {
            var volumes = new Dictionary<int, decimal>();

            // For receipt points, get positive volumes (gas entering)
            // For delivery points, get negative volumes (gas exiting)
            // For compressor stations, volume is typically 0 (pass-through)

            using var scope = _serviceProvider?.CreateScope();
            var dbContext = scope?.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (dbContext != null)
            {
                // This would typically query a daily balance or volume table
                // For now, we'll simulate some basic logic
                foreach (var point in points)
                {
                    decimal volume = 0;

                    switch (point.PointType)
                    {
                        case PointType.Receipt:
                            // Receipt points have positive volumes
                            volume = GetReceiptVolume(point.Id, dStart, dEnd);
                            break;
                        case PointType.Delivery:
                            // Delivery points have negative volumes
                            volume = -GetDeliveryVolume(point.Id, dStart, dEnd);
                            break;
                        case PointType.CompressorStation:
                            // Compressor stations typically have zero net volume
                            volume = 0;
                            break;
                    }

                    volumes[point.Id] = volume;
                }
            }

            return volumes;
        }

        /// <summary>
        /// Gets receipt volume for a point (placeholder - would query actual data)
        /// </summary>
        private decimal GetReceiptVolume(int pointId, DateTime dStart, DateTime dEnd)
        {
            // This would query actual receipt data from the database
            // For demonstration, returning a sample value
            return pointId * 10; // Placeholder
        }

        /// <summary>
        /// Gets delivery volume for a point (placeholder - would query actual data)
        /// </summary>
        private decimal GetDeliveryVolume(int pointId, DateTime dStart, DateTime dEnd)
        {
            // This would query actual delivery data from the database
            // For demonstration, returning a sample value
            return pointId * 5; // Placeholder
        }

        /// <summary>
        /// Calculates flows for a specific segment
        /// </summary>
        private void GetSegmentFlows(BLT_Segment segment, Dictionary<int, decimal> volumes, DateTime flowDate)
        {
            var startVolume = volumes.GetValueOrDefault(segment.StartPointId, 0);
            var endVolume = volumes.GetValueOrDefault(segment.EndPointId, 0);

            var flow = new CA_SegmentFlow
            {
                SegmentId = segment.Id,
                StartPointId = segment.StartPointId,
                EndPointId = segment.EndPointId,
                FlowDate = flowDate,
                VolumeFromPrevPoint = Math.Abs(startVolume),
                VolumeChange = endVolume,
                VolumePassThru = Math.Max(0, Math.Abs(startVolume) + endVolume)
            };

            _segmentFlows.Add(flow);
        }

        /// <summary>
        /// Totalizes flows across all segments to ensure consistency
        /// </summary>
        private List<CA_SegmentFlow> TotalizeFlows(List<BLT_Segment> segments, List<CA_SegmentFlow> flows)
        {
            var totalFlows = new List<CA_SegmentFlow>();

            foreach (var segment in segments)
            {
                var segmentFlows = flows.Where(f => f.SegmentId == segment.Id).ToList();
                
                if (segmentFlows.Any())
                {
                    var totalFlow = new CA_SegmentFlow
                    {
                        SegmentId = segment.Id,
                        StartPointId = segment.StartPointId,
                        EndPointId = segment.EndPointId,
                        FlowDate = segmentFlows.First().FlowDate,
                        VolumeFromPrevPoint = segmentFlows.Sum(f => f.VolumeFromPrevPoint),
                        VolumeChange = segmentFlows.Sum(f => f.VolumeChange),
                        VolumePassThru = segmentFlows.Sum(f => f.VolumePassThru)
                    };
                    
                    totalFlows.Add(totalFlow);
                }
            }

            return totalFlows;
        }
    }
}
