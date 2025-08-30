using Microsoft.EntityFrameworkCore;
using GasFlowCalculator.Models;

namespace GasFlowCalculator.Data
{
    /// <summary>
    /// Data access factory for BLT_Segment entities
    /// </summary>
    public class CA_SegmentFactory
    {
        private readonly ApplicationDbContext _context;

        public CA_SegmentFactory(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all segment objects with optional ordering
        /// </summary>
        /// <param name="orderBy">Order by clause (e.g., "Name", "Id DESC")</param>
        /// <returns>List of segments</returns>
        public List<BLT_Segment> GetAllObjects(string orderBy = "")
        {
            try
            {
                var query = _context.Segments
                    .Include(s => s.StartPoint)
                    .Include(s => s.EndPoint)
                    .Include(s => s.Network)
                    .AsQueryable();

                // Apply ordering if specified
                if (!string.IsNullOrEmpty(orderBy))
                {
                    switch (orderBy.ToLower())
                    {
                        case "name":
                            query = query.OrderBy(s => s.Name);
                            break;
                        case "name desc":
                            query = query.OrderByDescending(s => s.Name);
                            break;
                        case "id":
                            query = query.OrderBy(s => s.Id);
                            break;
                        case "id desc":
                            query = query.OrderByDescending(s => s.Id);
                            break;
                        default:
                            query = query.OrderBy(s => s.Id);
                            break;
                    }
                }
                else
                {
                    query = query.OrderBy(s => s.Id);
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving segments: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets segments by network ID
        /// </summary>
        /// <param name="networkId">Network ID to filter by</param>
        /// <returns>List of segments for the specified network</returns>
        public List<BLT_Segment> GetByNetworkId(int networkId)
        {
            try
            {
                return _context.Segments
                    .Include(s => s.StartPoint)
                    .Include(s => s.EndPoint)
                    .Include(s => s.Network)
                    .Where(s => s.NetworkId == networkId && s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving segments for network {networkId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a segment by ID
        /// </summary>
        /// <param name="id">Segment ID</param>
        /// <returns>Segment or null if not found</returns>
        public BLT_Segment? GetById(int id)
        {
            try
            {
                return _context.Segments
                    .Include(s => s.StartPoint)
                    .Include(s => s.EndPoint)
                    .Include(s => s.Network)
                    .FirstOrDefault(s => s.Id == id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving segment {id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves a segment to the database
        /// </summary>
        /// <param name="segment">Segment to save</param>
        /// <returns>Saved segment with updated ID</returns>
        public BLT_Segment Save(BLT_Segment segment)
        {
            try
            {
                if (segment.Id == 0)
                {
                    _context.Segments.Add(segment);
                }
                else
                {
                    segment.ModifiedDate = DateTime.Now;
                    _context.Segments.Update(segment);
                }

                _context.SaveChanges();
                return segment;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving segment: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a segment from the database
        /// </summary>
        /// <param name="id">Segment ID to delete</param>
        /// <returns>True if deleted successfully</returns>
        public bool Delete(int id)
        {
            try
            {
                var segment = _context.Segments.Find(id);
                if (segment != null)
                {
                    _context.Segments.Remove(segment);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting segment {id}: {ex.Message}", ex);
            }
        }
    }
}
