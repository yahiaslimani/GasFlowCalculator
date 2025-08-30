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

        await _dataService!.SaveFlowsAsync(flows);
        _logger!.LogInformation($"Saved {flows.Count} flow calculations");

        return flows;
    }

    static async Task ViewExistingFlows()
    {
        Console.Clear();
        Console.WriteLine("Existing Gas Flows");
        Console.WriteLine("==================");

        var flows = await _dataService!.GetFlowsAsync();
        if (!flows.Any())
        {
            Console.WriteLine("No flows found.");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            return;
        }

        Console.WriteLine($"{"ID",-5} {"Segment",-15} {"Date",-12} {"From Prev",-12} {"Change",-12} {"Pass Thru",-12}");
        Console.WriteLine(new string('-', 80));

        foreach (var flow in flows.OrderBy(f => f.FlowDate).ThenBy(f => f.SegmentId))
        {
            Console.WriteLine($"{flow.Id,-5} {flow.SegmentId,-15} {flow.FlowDate:yyyy-MM-dd,-12} " +
                            $"{flow.VolumeFromPrevPoint,-12:F6} {flow.VolumeChange,-12:F6} {flow.VolumePassThru,-12:F6}");
        }

        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
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