using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using GasFlowCalculator.Models;
using GasFlowCalculator.Services;
using System.Globalization;

class Program
{
    private static ILogger<Program>? _logger;
    private static DataService? _dataService;

    static async Task Main(string[] args)
    {
        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        _logger = loggerFactory.CreateLogger<Program>();

        _dataService = new DataService();

        _logger.LogInformation("Gas Flow Calculator System Starting...");

        await RunMainMenu();
    }

    static async Task RunMainMenu()
    {
        bool running = true;
        while (running)
        {
            Console.Clear();
            Console.WriteLine("Gas Flow Calculator System");
            Console.WriteLine("=============================");
            Console.WriteLine("1. Calculate Gas Flows");
            Console.WriteLine("2. View Existing Flows");
            Console.WriteLine("3. View Networks");
            Console.WriteLine("4. View Points");
            Console.WriteLine("5. View Segments");
            Console.WriteLine("6. View Point Volumes");
            Console.WriteLine("7. Exit");
            Console.Write("Select an option (1-7): ");

            string? input = Console.ReadLine();
            if (int.TryParse(input, out int choice))
            {
                switch (choice)
                {
                    case 1:
                        await CalculateFlows();
                        break;
                    case 2:
                        await ViewExistingFlows();
                        break;
                    case 3:
                        await ViewNetworks();
                        break;
                    case 4:
                        await ViewPoints();
                        break;
                    case 5:
                        await ViewSegments();
                        break;
                    case 6:
                        await ViewPointVolumes();
                        break;
                    case 7:
                        running = false;
                        break;
                    default:
                        Console.WriteLine("Invalid option. Press any key to continue...");
                        Console.ReadKey();
                        break;
                }
            }
            else
            {
                Console.WriteLine("Invalid input. Press any key to continue...");
                Console.ReadKey();
            }
        }
    }

    static async Task CalculateFlows()
    {
        try
        {
            Console.Clear();
            Console.WriteLine("Calculate Gas Flows");
            Console.WriteLine("==================");

            var networks = await _dataService!.GetNetworksAsync();
            if (!networks.Any())
            {
                Console.WriteLine("No networks found.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Available Networks:");
            for (int i = 0; i < networks.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {networks[i].Name}");
            }

            Console.Write("Select network (number): ");
            if (!int.TryParse(Console.ReadLine(), out int networkChoice) || 
                networkChoice < 1 || networkChoice > networks.Count)
            {
                Console.WriteLine("Invalid selection.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            var selectedNetwork = networks[networkChoice - 1];

            Console.Write("Enter date for calculation (yyyy-mm-dd): ");
            if (!DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime flowDate))
            {
                Console.WriteLine("Invalid date format. Please use yyyy-mm-dd.");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return;
            }

            _logger!.LogInformation($"Starting flow calculation for network '{selectedNetwork.Name}' on {flowDate:yyyy-MM-dd}");

            var flows = await CalculateNetworkFlows(selectedNetwork.Id, flowDate);

            Console.WriteLine($"\nFlow calculation completed for {flows.Count} segments.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            _logger!.LogError(ex, "Error calculating flows");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }

    static async Task<List<CA_SegmentFlow>> CalculateNetworkFlows(int networkId, DateTime flowDate)
    {
        var points = await _dataService!.GetPointsByNetworkIdAsync(networkId);
        var segments = await _dataService!.GetSegmentsByNetworkIdAsync(networkId);
        var volumes = await _dataService!.GetVolumesByDateAsync(flowDate);

        // Group points by type
        var receiptPoints = points.Where(p => p.PointType == PointType.Receipt).ToList();
        var deliveryPoints = points.Where(p => p.PointType == PointType.Delivery).ToList();

        // Calculate total receipt and delivery volumes
        decimal totalReceipt = 0;
        decimal totalDelivery = 0;

        foreach (var point in receiptPoints)
        {
            var volume = await _dataService!.GetPointVolumeAsync(point.Id, flowDate, "Receipt");
            totalReceipt += volume;
        }

        foreach (var point in deliveryPoints)
        {
            var volume = await _dataService!.GetPointVolumeAsync(point.Id, flowDate, "Delivery");
            totalDelivery += volume;
        }

        _logger!.LogInformation($"Total Receipt: {totalReceipt}, Total Delivery: {totalDelivery}");

        // Balance adjustment if needed
        if (totalReceipt != totalDelivery && totalDelivery > 0)
        {
            decimal adjustmentFactor = totalReceipt / totalDelivery;
            _logger!.LogInformation($"Applying balance adjustment factor: {adjustmentFactor}");
        }

        // Calculate flows for each segment
        var flows = new List<CA_SegmentFlow>();
        int flowId = 1;

        foreach (var segment in segments)
        {
            var startPoint = await _dataService!.GetPointByIdAsync(segment.StartPointId);
            var endPoint = await _dataService!.GetPointByIdAsync(segment.EndPointId);

            decimal volumeFromPrev = 0;
            decimal volumeChange = 0;
            decimal volumePassThru = 0;

            // Calculate flow based on point types
            if (startPoint?.PointType == PointType.Receipt)
            {
                volumeFromPrev = await _dataService!.GetPointVolumeAsync(startPoint.Id, flowDate, "Receipt");
            }

            if (endPoint?.PointType == PointType.Delivery)
            {
                volumeChange = -(await _dataService!.GetPointVolumeAsync(endPoint.Id, flowDate, "Delivery"));
            }

            volumePassThru = volumeFromPrev + volumeChange;

            var flow = new CA_SegmentFlow
            {
                Id = flowId++,
                SegmentId = segment.Id,
                StartPointId = segment.StartPointId,
                EndPointId = segment.EndPointId,
                FlowDate = flowDate,
                VolumeFromPrevPoint = volumeFromPrev,
                VolumeChange = volumeChange,
                VolumePassThru = volumePassThru,
                CreatedDate = DateTime.Now,
                Segment = segment,
                StartPoint = startPoint,
                EndPoint = endPoint
            };

            flows.Add(flow);
        }

        // Perform capacity validation
        var validationResults = ValidateFlowCapacities(flows);
        LogCapacityValidation(validationResults);

        await _dataService!.SaveFlowsAsync(flows);
        _logger!.LogInformation($"Saved {flows.Count} flow calculations");

        return flows;
    }

    static async Task ViewExistingFlows()
    {
        Console.Clear();
        Console.WriteLine("Gas Flow Analysis - Comprehensive Metrics");
        Console.WriteLine("========================================");

        var flows = await _dataService!.GetFlowsAsync();
        if (!flows.Any())
        {
            Console.WriteLine("No flows found.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        // Header
        Console.WriteLine($"{"ID",-4} {"Segment",-12} {"Date",-12} {"Start Point",-15} {"End Point",-15} " +
                         $"{"Flow",-10} {"Capacity",-10} {"Usage%",-8} {"Available",-10} {"Status",-15}");
        Console.WriteLine(new string('-', 132));

        // Flow data with capacity metrics
        var sortedFlows = flows.OrderBy(f => f.FlowDate).ThenBy(f => f.SegmentId);
        int overCapacityCount = 0;
        int highUsageCount = 0;

        foreach (var flow in sortedFlows)
        {
            var startPointName = flow.StartPoint?.Name ?? "Unknown";
            var endPointName = flow.EndPoint?.Name ?? "Unknown";
            var segmentName = flow.Segment?.Name ?? $"Seg-{flow.SegmentId}";
            
            if (startPointName.Length > 14) startPointName = startPointName.Substring(0, 14);
            if (endPointName.Length > 14) endPointName = endPointName.Substring(0, 14);
            if (segmentName.Length > 11) segmentName = segmentName.Substring(0, 11);

            var statusColor = flow.IsOverCapacity ? "⚠️" : flow.UsagePercentage > 90 ? "🔶" : 
                             flow.UsagePercentage > 75 ? "🔷" : "✅";
            
            Console.WriteLine($"{flow.Id,-4} {segmentName,-12} {flow.FlowDate:yyyy-MM-dd,-12} " +
                            $"{startPointName,-15} {endPointName,-15} " +
                            $"{flow.ActualFlow,-10:F2} {flow.Capacity,-10:F2} {flow.UsagePercentage,-8:F1} " +
                            $"{flow.AvailableCapacity,-10:F2} {statusColor + flow.CapacityStatus,-15}");

            if (flow.IsOverCapacity) overCapacityCount++;
            else if (flow.UsagePercentage > 75) highUsageCount++;
        }

        // Summary statistics
        Console.WriteLine(new string('-', 132));
        Console.WriteLine("CAPACITY ANALYSIS SUMMARY:");
        Console.WriteLine($"Total Segments: {flows.Count}");
        Console.WriteLine($"Over Capacity: {overCapacityCount} segments");
        Console.WriteLine($"High Usage (>75%): {highUsageCount} segments");
        Console.WriteLine($"Normal Operation: {flows.Count - overCapacityCount - highUsageCount} segments");

        if (overCapacityCount > 0)
        {
            Console.WriteLine("\n⚠️  CRITICAL: Segments operating over capacity require immediate attention!");
        }
        else if (highUsageCount > 0)
        {
            Console.WriteLine("\n🔶 ADVISORY: Monitor high usage segments for potential capacity expansion.");
        }
        else
        {
            Console.WriteLine("\n✅ GOOD: All segments operating within normal capacity limits.");
        }

        // Additional metrics
        var totalFlow = flows.Sum(f => f.ActualFlow);
        var totalCapacity = flows.Sum(f => f.Capacity);
        var overallUsage = totalCapacity > 0 ? (totalFlow / totalCapacity) * 100 : 0;

        Console.WriteLine($"\nNETWORK OVERVIEW:");
        Console.WriteLine($"Total Network Flow: {totalFlow:F2} MCF");
        Console.WriteLine($"Total Network Capacity: {totalCapacity:F2} MCF");
        Console.WriteLine($"Overall Network Usage: {overallUsage:F1}%");

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    static List<string> ValidateFlowCapacities(List<CA_SegmentFlow> flows)
    {
        var validationResults = new List<string>();
        var overCapacityFlows = flows.Where(f => f.IsOverCapacity).ToList();
        var highUsageFlows = flows.Where(f => f.UsagePercentage > 75 && !f.IsOverCapacity).ToList();

        if (overCapacityFlows.Any())
        {
            validationResults.Add($"⚠️  WARNING: {overCapacityFlows.Count} segments are OVER CAPACITY!");
            foreach (var flow in overCapacityFlows)
            {
                validationResults.Add($"   Segment {flow.Segment?.Name}: {flow.ActualFlow:F2} MCF exceeds capacity of {flow.Capacity:F2} MCF ({flow.UsagePercentage:F1}%)");
            }
        }

        if (highUsageFlows.Any())
        {
            validationResults.Add($"ℹ️  INFO: {highUsageFlows.Count} segments have high capacity usage (>75%):");
            foreach (var flow in highUsageFlows.Take(5)) // Show top 5
            {
                validationResults.Add($"   Segment {flow.Segment?.Name}: {flow.UsagePercentage:F1}% capacity usage");
            }
        }

        if (!overCapacityFlows.Any() && !highUsageFlows.Any())
        {
            validationResults.Add("✅ All segments are operating within normal capacity limits.");
        }

        return validationResults;
    }

    static void LogCapacityValidation(List<string> validationResults)
    {
        _logger!.LogInformation("Capacity Validation Results:");
        foreach (var result in validationResults)
        {
            if (result.Contains("WARNING"))
                _logger!.LogWarning(result);
            else if (result.Contains("INFO"))
                _logger!.LogInformation(result);
            else
                _logger!.LogInformation(result);
        }
    }

    static async Task ViewNetworks()
    {
        Console.Clear();
        Console.WriteLine("Networks");
        Console.WriteLine("========");

        var networks = await _dataService!.GetNetworksAsync();
        if (!networks.Any())
        {
            Console.WriteLine("No networks found.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"{"ID",-5} {"Name",-30} {"Active",-8} {"Description",-40}");
        Console.WriteLine(new string('-', 85));

        foreach (var network in networks)
        {
            Console.WriteLine($"{network.Id,-5} {network.Name,-30} {network.IsActive,-8} {network.Description,-40}");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task ViewPoints()
    {
        Console.Clear();
        Console.WriteLine("Points");
        Console.WriteLine("======");

        var networks = await _dataService!.GetNetworksAsync();
        if (!networks.Any())
        {
            Console.WriteLine("No networks found.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Available Networks:");
        for (int i = 0; i < networks.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {networks[i].Name}");
        }

        Console.Write("Select network (number): ");
        if (!int.TryParse(Console.ReadLine(), out int networkChoice) || 
            networkChoice < 1 || networkChoice > networks.Count)
        {
            Console.WriteLine("Invalid selection.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var selectedNetwork = networks[networkChoice - 1];
        var points = await _dataService!.GetPointsByNetworkIdAsync(selectedNetwork.Id);

        if (!points.Any())
        {
            Console.WriteLine($"No points found for network '{selectedNetwork.Name}'.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nPoints for Network '{selectedNetwork.Name}':");
        Console.WriteLine($"{"ID",-5} {"Name",-25} {"Type",-18} {"Active",-8} {"Description",-30}");
        Console.WriteLine(new string('-', 88));

        foreach (var point in points.OrderBy(p => p.PointType).ThenBy(p => p.Name))
        {
            Console.WriteLine($"{point.Id,-5} {point.Name,-25} {point.PointType,-18} {point.IsActive,-8} {point.Description,-30}");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task ViewSegments()
    {
        Console.Clear();
        Console.WriteLine("Segments");
        Console.WriteLine("========");

        var networks = await _dataService!.GetNetworksAsync();
        if (!networks.Any())
        {
            Console.WriteLine("No networks found.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Available Networks:");
        for (int i = 0; i < networks.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {networks[i].Name}");
        }

        Console.Write("Select network (number): ");
        if (!int.TryParse(Console.ReadLine(), out int networkChoice) || 
            networkChoice < 1 || networkChoice > networks.Count)
        {
            Console.WriteLine("Invalid selection.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var selectedNetwork = networks[networkChoice - 1];
        var segments = await _dataService!.GetSegmentsByNetworkIdAsync(selectedNetwork.Id);
        var points = await _dataService!.GetPointsAsync();

        if (!segments.Any())
        {
            Console.WriteLine($"No segments found for network '{selectedNetwork.Name}'.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"\nSegments for Network '{selectedNetwork.Name}':");
        Console.WriteLine($"{"ID",-5} {"Name",-20} {"Start Point",-20} {"End Point",-20} {"Description",-30}");
        Console.WriteLine(new string('-', 97));

        foreach (var segment in segments.OrderBy(s => s.Id))
        {
            var startPoint = points.FirstOrDefault(p => p.Id == segment.StartPointId);
            var endPoint = points.FirstOrDefault(p => p.Id == segment.EndPointId);

            Console.WriteLine($"{segment.Id,-5} {segment.Name,-20} {startPoint?.Name ?? "Unknown",-20} " +
                            $"{endPoint?.Name ?? "Unknown",-20} {segment.Description,-30}");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
    }

    static async Task ViewPointVolumes()
    {
        Console.Clear();
        Console.WriteLine("Point Volumes");
        Console.WriteLine("=============");

        var networks = await _dataService!.GetNetworksAsync();
        if (!networks.Any())
        {
            Console.WriteLine("No networks found.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine("Available Networks:");
        for (int i = 0; i < networks.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {networks[i].Name}");
        }

        Console.Write("Select network (number): ");
        if (!int.TryParse(Console.ReadLine(), out int networkChoice) || 
            networkChoice < 1 || networkChoice > networks.Count)
        {
            Console.WriteLine("Invalid selection.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var selectedNetwork = networks[networkChoice - 1];

        Console.Write("Enter date for volume data (yyyy-mm-dd): ");
        if (!DateTime.TryParseExact(Console.ReadLine(), "yyyy-MM-dd", 
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime volumeDate))
        {
            Console.WriteLine("Invalid date format. Please use yyyy-mm-dd.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        var points = await _dataService!.GetPointsByNetworkIdAsync(selectedNetwork.Id);
        var volumes = await _dataService!.GetVolumesByDateAsync(volumeDate);

        Console.WriteLine($"\nPoint Volumes for Network '{selectedNetwork.Name}' on {volumeDate:yyyy-MM-dd}:");
        Console.WriteLine($"{"ID",-5} {"Name",-25} {"Type",-18} {"Volume",-15} {"Description",-30}");
        Console.WriteLine(new string('-', 95));

        decimal totalReceipt = 0;
        decimal totalDelivery = 0;

        foreach (var point in points.OrderBy(p => p.PointType).ThenBy(p => p.Name))
        {
            var volume = volumes.FirstOrDefault(v => v.PointId == point.Id);
            decimal volumeValue = volume?.Volume ?? 0;
            string volumeType = volume?.VolumeType ?? "None";

            Console.WriteLine($"{point.Id,-5} {point.Name,-25} {point.PointType,-18} " +
                            $"{volumeValue,-15:F6} {point.Description,-30}");

            if (point.PointType == PointType.Receipt)
                totalReceipt += volumeValue;
            else if (point.PointType == PointType.Delivery)
                totalDelivery += volumeValue;
        }

        Console.WriteLine("\nVolume Balance Summary:");
        Console.WriteLine($"Total Receipt Volume:        {totalReceipt:F6}");
        Console.WriteLine($"Total Delivery Volume:       {totalDelivery:F6}");
        Console.WriteLine($"Net Balance:                {totalReceipt - totalDelivery:F6}");

        if (Math.Abs(totalReceipt - totalDelivery) > 0.001m)
        {
            Console.WriteLine("⚠️  Warning: Receipt and delivery volumes are not balanced!");
        }

        Console.WriteLine("Press any key to continue...");
        Console.ReadKey();
    }
}