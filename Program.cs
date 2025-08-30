using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using GasFlowCalculator.Data;
using GasFlowCalculator.Models;
using GasFlowCalculator.Utilities;
using NLog;
using NLog.Extensions.Logging;

namespace GasFlowCalculator
{
    internal static class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        static async Task Main(string[] args)
        {
            try
            {
                // Configure NLog
                LogManager.Setup();

                // Build configuration
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Setup dependency injection
                var services = new ServiceCollection();
                ConfigureServices(services, configuration);
                var serviceProvider = services.BuildServiceProvider();

                // Ensure database is created
                using (var scope = serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    await dbContext.Database.EnsureCreatedAsync();
                    logger.Info("Database created and seeded successfully");
                }

                // Run the console application
                await RunConsoleApplication(serviceProvider);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Application startup failed");
                Console.WriteLine($"Application failed to start: {ex.Message}");
            }
        }

        private static async Task RunConsoleApplication(IServiceProvider serviceProvider)
        {
            Console.WriteLine("=== Gas Flow Calculator ===");
            Console.WriteLine();

            while (true)
            {
                try
                {
                    // Show menu
                    Console.WriteLine("1. Calculate Gas Flows");
                    Console.WriteLine("2. View Existing Flows");
                    Console.WriteLine("3. View Networks");
                    Console.WriteLine("4. View Points");
                    Console.WriteLine("5. View Segments");
                    Console.WriteLine("6. Exit");
                    Console.Write("Select an option (1-6): ");

                    var choice = Console.ReadLine();
                    Console.WriteLine();

                    switch (choice)
                    {
                        case "1":
                            await CalculateFlows(serviceProvider);
                            break;
                        case "2":
                            await ViewFlows(serviceProvider);
                            break;
                        case "3":
                            await ViewNetworks(serviceProvider);
                            break;
                        case "4":
                            await ViewPoints(serviceProvider);
                            break;
                        case "5":
                            await ViewSegments(serviceProvider);
                            break;
                        case "6":
                            Console.WriteLine("Goodbye!");
                            return;
                        default:
                            Console.WriteLine("Invalid choice. Please try again.");
                            break;
                    }
                    
                    Console.WriteLine();
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey();
                    Console.Clear();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error in menu operation");
                    Console.WriteLine($"Error: {ex.Message}");
                    Console.WriteLine();
                }
            }
        }

        private static async Task CalculateFlows(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Show available networks
            var networks = await dbContext.Networks.Where(n => n.IsActive).ToListAsync();
            if (!networks.Any())
            {
                Console.WriteLine("No networks found.");
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
                Console.WriteLine("Invalid network selection.");
                return;
            }

            var selectedNetwork = networks[networkChoice - 1];
            
            Console.Write("Enter start date (yyyy-mm-dd): ");
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime startDate))
            {
                Console.WriteLine("Invalid date format.");
                return;
            }

            Console.Write("Enter end date (yyyy-mm-dd): ");
            if (!DateTime.TryParse(Console.ReadLine(), out DateTime endDate))
            {
                Console.WriteLine("Invalid date format.");
                return;
            }

            Console.WriteLine("Calculating flows...");
            
            var bltNetwork = new BLT_Network(serviceProvider);
            var flowCount = bltNetwork.CalcFlow(selectedNetwork.Id, startDate, endDate);
            
            Console.WriteLine($"Flow calculation completed. {flowCount} flows calculated for network '{selectedNetwork.Name}'.");
        }

        private static async Task ViewFlows(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var flows = await dbContext.SegmentFlows
                .Include(f => f.Segment)
                .Include(f => f.StartPoint)
                .Include(f => f.EndPoint)
                .OrderBy(f => f.FlowDate)
                .ThenBy(f => f.Segment!.Name)
                .ToListAsync();

            if (!flows.Any())
            {
                Console.WriteLine("No flows found. Calculate flows first.");
                return;
            }

            Console.WriteLine("Flow Data:");
            Console.WriteLine($"{"Segment",-20} {"From Point",-15} {"To Point",-15} {"Volume From Prev",-18} {"Volume Change",-15} {"Pass-Thru",-12} {"Date",-12}");
            Console.WriteLine(new string('-', 110));

            foreach (var flow in flows)
            {
                Console.WriteLine($"{flow.Segment?.Name ?? "N/A",-20} " +
                                $"{flow.StartPoint?.Name ?? "N/A",-15} " +
                                $"{flow.EndPoint?.Name ?? "N/A",-15} " +
                                $"{flow.VolumeFromPrevPoint,18:F6} " +
                                $"{flow.VolumeChange,15:F6} " +
                                $"{flow.VolumePassThru,12:F6} " +
                                $"{flow.FlowDate,12:yyyy-MM-dd}");
            }
        }

        private static async Task ViewNetworks(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var networks = await dbContext.Networks.ToListAsync();
            
            if (!networks.Any())
            {
                Console.WriteLine("No networks found.");
                return;
            }

            Console.WriteLine("Networks:");
            Console.WriteLine($"{"ID",-5} {"Name",-25} {"Description",-30} {"Active",-8}");
            Console.WriteLine(new string('-', 70));

            foreach (var network in networks)
            {
                Console.WriteLine($"{network.Id,-5} {network.Name,-25} {network.Description ?? "N/A",-30} {network.IsActive,-8}");
            }
        }

        private static async Task ViewPoints(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var points = await dbContext.Points
                .Include(p => p.Network)
                .OrderBy(p => p.NetworkId)
                .ThenBy(p => p.PointType)
                .ToListAsync();
            
            if (!points.Any())
            {
                Console.WriteLine("No points found.");
                return;
            }

            Console.WriteLine("Points:");
            Console.WriteLine($"{"ID",-5} {"Name",-20} {"Type",-18} {"Network",-20} {"Active",-8}");
            Console.WriteLine(new string('-', 75));

            foreach (var point in points)
            {
                Console.WriteLine($"{point.Id,-5} {point.Name,-20} {point.PointType,-18} {point.Network?.Name ?? "N/A",-20} {point.IsActive,-8}");
            }
        }

        private static async Task ViewSegments(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var segments = await dbContext.Segments
                .Include(s => s.Network)
                .Include(s => s.StartPoint)
                .Include(s => s.EndPoint)
                .OrderBy(s => s.NetworkId)
                .ThenBy(s => s.Name)
                .ToListAsync();
            
            if (!segments.Any())
            {
                Console.WriteLine("No segments found.");
                return;
            }

            Console.WriteLine("Segments:");
            Console.WriteLine($"{"ID",-5} {"Name",-20} {"From Point",-15} {"To Point",-15} {"Network",-20} {"Active",-8}");
            Console.WriteLine(new string('-', 85));

            foreach (var segment in segments)
            {
                Console.WriteLine($"{segment.Id,-5} {segment.Name,-20} " +
                                $"{segment.StartPoint?.Name ?? "N/A",-15} " +
                                $"{segment.EndPoint?.Name ?? "N/A",-15} " +
                                $"{segment.Network?.Name ?? "N/A",-20} {segment.IsActive,-8}");
            }
        }

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Add DbContext
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
            });

            // Add data factories
            services.AddTransient<CA_SegmentFactory>();
            services.AddTransient<CA_PointFactory>();
        }
    }
}
