using Microsoft.EntityFrameworkCore;
using GasFlowCalculator.Models;

namespace GasFlowCalculator.Data
{
    /// <summary>
    /// Entity Framework DbContext for the gas flow calculator application
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=gasflow.db");
            }
        }

        public DbSet<CA_Network> Networks { get; set; }
        public DbSet<BLT_Segment> Segments { get; set; }
        public DbSet<CA_Point> Points { get; set; }
        public DbSet<CA_SegmentFlow> SegmentFlows { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<BLT_Segment>()
                .HasOne(s => s.StartPoint)
                .WithMany(p => p.StartSegments)
                .HasForeignKey(s => s.StartPointId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<BLT_Segment>()
                .HasOne(s => s.EndPoint)
                .WithMany(p => p.EndSegments)
                .HasForeignKey(s => s.EndPointId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CA_SegmentFlow>()
                .HasOne(sf => sf.Segment)
                .WithMany(s => s.SegmentFlows)
                .HasForeignKey(sf => sf.SegmentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed some sample data for testing
            modelBuilder.Entity<CA_Network>().HasData(
                new CA_Network { Id = 1, Name = "Main Pipeline Network", Description = "Primary gas transmission network", IsActive = true },
                new CA_Network { Id = 2, Name = "Distribution Network", Description = "Local distribution network", IsActive = true }
            );

            modelBuilder.Entity<CA_Point>().HasData(
                new CA_Point { Id = 1, Name = "Receipt Point A", PointType = PointType.Receipt, NetworkId = 1, IsActive = true },
                new CA_Point { Id = 2, Name = "Compressor Station 1", PointType = PointType.CompressorStation, NetworkId = 1, IsActive = true },
                new CA_Point { Id = 3, Name = "Delivery Point B", PointType = PointType.Delivery, NetworkId = 1, IsActive = true },
                new CA_Point { Id = 4, Name = "Delivery Point C", PointType = PointType.Delivery, NetworkId = 1, IsActive = true }
            );

            modelBuilder.Entity<BLT_Segment>().HasData(
                new BLT_Segment { Id = 1, Name = "Segment A-CS1", NetworkId = 1, StartPointId = 1, EndPointId = 2, IsActive = true },
                new BLT_Segment { Id = 2, Name = "Segment CS1-B", NetworkId = 1, StartPointId = 2, EndPointId = 3, IsActive = true },
                new BLT_Segment { Id = 3, Name = "Segment CS1-C", NetworkId = 1, StartPointId = 2, EndPointId = 4, IsActive = true }
            );

            // Configure indexes for performance
            modelBuilder.Entity<CA_SegmentFlow>()
                .HasIndex(sf => new { sf.SegmentId, sf.FlowDate })
                .HasDatabaseName("IX_SegmentFlow_SegmentId_FlowDate");

            modelBuilder.Entity<BLT_Segment>()
                .HasIndex(s => s.NetworkId)
                .HasDatabaseName("IX_Segment_NetworkId");

            modelBuilder.Entity<CA_Point>()
                .HasIndex(p => p.NetworkId)
                .HasDatabaseName("IX_Point_NetworkId");
        }
    }
}
