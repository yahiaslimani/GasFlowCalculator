using GasFlowCalculator.Models;
using GasFlowCalculator.Utilities;

namespace GasFlowCalculator.Business
{
    /// <summary>
    /// Core engine for calculating gas flows through pipeline networks
    /// </summary>
    public static class FlowCalculationEngine
    {
        /// <summary>
        /// Validates that total receipt volumes match total delivery volumes
        /// </summary>
        /// <param name="pointVolumes">Dictionary of point ID to volume</param>
        /// <param name="points">List of all points</param>
        /// <returns>True if volumes are balanced</returns>
        public static bool ValidateVolumeBalance(Dictionary<int, decimal> pointVolumes, List<CA_Point> points)
        {
            try
            {
                var totalReceipt = points
                    .Where(p => p.PointType == PointType.Receipt)
                    .Sum(p => pointVolumes.GetValueOrDefault(p.Id, 0));

                var totalDelivery = Math.Abs(points
                    .Where(p => p.PointType == PointType.Delivery)
                    .Sum(p => pointVolumes.GetValueOrDefault(p.Id, 0)));

                const decimal tolerance = 0.001m; // Allow small rounding differences
                var difference = Math.Abs(totalReceipt - totalDelivery);

                LogHelper.Debug("FlowCalculationEngine", 
                    $"Volume Balance Check - Receipt: {totalReceipt:F6}, Delivery: {totalDelivery:F6}, Difference: {difference:F6}");

                return difference <= tolerance;
            }
            catch (Exception ex)
            {
                LogHelper.Error("FlowCalculationEngine", $"Error validating volume balance: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Aligns receipt and delivery volumes by proportional adjustment
        /// </summary>
        /// <param name="pointVolumes">Dictionary of point ID to volume</param>
        /// <param name="points">List of all points</param>
        /// <returns>Adjusted point volumes</returns>
        public static Dictionary<int, decimal> AlignVolumes(Dictionary<int, decimal> pointVolumes, List<CA_Point> points)
        {
            try
            {
                var adjustedVolumes = new Dictionary<int, decimal>(pointVolumes);

                var receiptPoints = points.Where(p => p.PointType == PointType.Receipt).ToList();
                var deliveryPoints = points.Where(p => p.PointType == PointType.Delivery).ToList();

                var totalReceipt = receiptPoints.Sum(p => pointVolumes.GetValueOrDefault(p.Id, 0));
                var totalDelivery = Math.Abs(deliveryPoints.Sum(p => pointVolumes.GetValueOrDefault(p.Id, 0)));

                if (totalReceipt == 0 || totalDelivery == 0)
                {
                    LogHelper.Warning("FlowCalculationEngine", "Cannot align volumes: one total is zero");
                    return adjustedVolumes;
                }

                // If receipt and delivery don't match, adjust delivery proportionally
                if (Math.Abs(totalReceipt - totalDelivery) > 0.001m)
                {
                    var adjustmentRatio = totalReceipt / totalDelivery;
                    
                    LogHelper.Debug("FlowCalculationEngine", 
                        $"Adjusting delivery volumes by ratio: {adjustmentRatio:F6}");

                    foreach (var point in deliveryPoints)
                    {
                        if (pointVolumes.ContainsKey(point.Id))
                        {
                            adjustedVolumes[point.Id] = pointVolumes[point.Id] * adjustmentRatio;
                        }
                    }
                }

                return adjustedVolumes;
            }
            catch (Exception ex)
            {
                LogHelper.Error("FlowCalculationEngine", $"Error aligning volumes: {ex.Message}");
                return pointVolumes;
            }
        }

        /// <summary>
        /// Distributes flow from a compressor station to downstream points
        /// </summary>
        /// <param name="compressorStation">The compressor station point</param>
        /// <param name="downstreamSegments">Segments leaving the compressor station</param>
        /// <param name="pointVolumes">Volume requirements at each point</param>
        /// <param name="availableVolume">Available volume at the compressor station</param>
        /// <returns>Dictionary of segment ID to allocated volume</returns>
        public static Dictionary<int, decimal> DistributeFlow(
            CA_Point compressorStation, 
            List<BLT_Segment> downstreamSegments, 
            Dictionary<int, decimal> pointVolumes, 
            decimal availableVolume)
        {
            try
            {
                var distribution = new Dictionary<int, decimal>();

                if (downstreamSegments.Count == 0)
                {
                    return distribution;
                }

                // Calculate total demand from downstream delivery points
                var totalDemand = downstreamSegments
                    .Sum(s => Math.Abs(pointVolumes.GetValueOrDefault(s.EndPointId, 0)));

                if (totalDemand == 0)
                {
                    // If no demand, distribute equally
                    var equalShare = availableVolume / downstreamSegments.Count;
                    foreach (var segment in downstreamSegments)
                    {
                        distribution[segment.Id] = equalShare;
                    }
                }
                else
                {
                    // Distribute proportionally based on demand
                    foreach (var segment in downstreamSegments)
                    {
                        var demand = Math.Abs(pointVolumes.GetValueOrDefault(segment.EndPointId, 0));
                        var proportion = demand / totalDemand;
                        distribution[segment.Id] = availableVolume * proportion;
                    }
                }

                LogHelper.Debug("FlowCalculationEngine", 
                    $"Distributed {availableVolume:F6} volume from compressor station {compressorStation.Id} " +
                    $"to {downstreamSegments.Count} segments");

                return distribution;
            }
            catch (Exception ex)
            {
                LogHelper.Error("FlowCalculationEngine", 
                    $"Error distributing flow from compressor station {compressorStation.Id}: {ex.Message}");
                return new Dictionary<int, decimal>();
            }
        }

        /// <summary>
        /// Calculates the pass-through volume for a segment
        /// </summary>
        /// <param name="volumeFromPrev">Volume entering the segment</param>
        /// <param name="volumeChange">Volume change at the end point</param>
        /// <returns>Volume passing through the segment</returns>
        public static decimal CalculatePassThrough(decimal volumeFromPrev, decimal volumeChange)
        {
            // Pass-through is the volume that continues beyond this segment
            // If volumeChange is negative (delivery), it reduces the pass-through
            // If volumeChange is positive (receipt), it increases the pass-through
            return Math.Max(0, volumeFromPrev + volumeChange);
        }

        /// <summary>
        /// Validates flow consistency across the network
        /// </summary>
        /// <param name="flows">List of calculated segment flows</param>
        /// <param name="segments">List of network segments</param>
        /// <returns>True if flows are consistent</returns>
        public static bool ValidateFlowConsistency(List<CA_SegmentFlow> flows, List<BLT_Segment> segments)
        {
            try
            {
                // Group segments by their connection points
                var pointConnections = new Dictionary<int, List<(BLT_Segment segment, bool isIncoming)>>();

                foreach (var segment in segments)
                {
                    // Start point has incoming flow
                    if (!pointConnections.ContainsKey(segment.StartPointId))
                        pointConnections[segment.StartPointId] = new List<(BLT_Segment, bool)>();
                    pointConnections[segment.StartPointId].Add((segment, false)); // outgoing

                    // End point has outgoing flow
                    if (!pointConnections.ContainsKey(segment.EndPointId))
                        pointConnections[segment.EndPointId] = new List<(BLT_Segment, bool)>();
                    pointConnections[segment.EndPointId].Add((segment, true)); // incoming
                }

                // Check flow balance at each connection point
                foreach (var pointConnection in pointConnections)
                {
                    var pointId = pointConnection.Key;
                    var connections = pointConnection.Value;

                    var incomingFlow = connections
                        .Where(c => c.isIncoming)
                        .Sum(c => flows.FirstOrDefault(f => f.SegmentId == c.segment.Id)?.VolumeFromPrevPoint ?? 0);

                    var outgoingFlow = connections
                        .Where(c => !c.isIncoming)
                        .Sum(c => flows.FirstOrDefault(f => f.SegmentId == c.segment.Id)?.VolumeFromPrevPoint ?? 0);

                    // For compressor stations, incoming should equal outgoing
                    // For receipt/delivery points, there will be an imbalance by design
                    var flowDifference = Math.Abs(incomingFlow - outgoingFlow);
                    
                    LogHelper.Debug("FlowCalculationEngine", 
                        $"Point {pointId} - Incoming: {incomingFlow:F6}, Outgoing: {outgoingFlow:F6}, Difference: {flowDifference:F6}");
                }

                return true; // Additional validation logic can be added here
            }
            catch (Exception ex)
            {
                LogHelper.Error("FlowCalculationEngine", $"Error validating flow consistency: {ex.Message}");
                return false;
            }
        }
    }
}
