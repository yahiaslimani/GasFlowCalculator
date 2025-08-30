using Microsoft.EntityFrameworkCore;
using GasFlowCalculator.Models;

namespace GasFlowCalculator.Data
{
    /// <summary>
    /// Data access factory for CA_Point entities
    /// </summary>
    public class CA_PointFactory
    {
        private readonly ApplicationDbContext _context;

        public CA_PointFactory(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets all point objects with optional ordering
        /// </summary>
        /// <param name="orderBy">Order by clause (e.g., "Name", "Id DESC")</param>
        /// <returns>List of points</returns>
        public List<CA_Point> GetAllObjects(string orderBy = "")
        {
            try
            {
                var query = _context.Points
                    .Include(p => p.Network)
                    .AsQueryable();

                // Apply ordering if specified
                if (!string.IsNullOrEmpty(orderBy))
                {
                    switch (orderBy.ToLower())
                    {
                        case "name":
                            query = query.OrderBy(p => p.Name);
                            break;
                        case "name desc":
                            query = query.OrderByDescending(p => p.Name);
                            break;
                        case "pointtype":
                            query = query.OrderBy(p => p.PointType);
                            break;
                        case "id":
                            query = query.OrderBy(p => p.Id);
                            break;
                        case "id desc":
                            query = query.OrderByDescending(p => p.Id);
                            break;
                        default:
                            query = query.OrderBy(p => p.Id);
                            break;
                    }
                }
                else
                {
                    query = query.OrderBy(p => p.Id);
                }

                return query.ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving points: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets points by network ID
        /// </summary>
        /// <param name="networkId">Network ID to filter by</param>
        /// <returns>List of points for the specified network</returns>
        public List<CA_Point> GetByNetworkId(int networkId)
        {
            try
            {
                return _context.Points
                    .Include(p => p.Network)
                    .Where(p => p.NetworkId == networkId && p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving points for network {networkId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets points by type
        /// </summary>
        /// <param name="pointType">Point type to filter by</param>
        /// <returns>List of points of the specified type</returns>
        public List<CA_Point> GetByType(PointType pointType)
        {
            try
            {
                return _context.Points
                    .Include(p => p.Network)
                    .Where(p => p.PointType == pointType && p.IsActive)
                    .OrderBy(p => p.Name)
                    .ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving points of type {pointType}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets a point by ID
        /// </summary>
        /// <param name="id">Point ID</param>
        /// <returns>Point or null if not found</returns>
        public CA_Point? GetById(int id)
        {
            try
            {
                return _context.Points
                    .Include(p => p.Network)
                    .FirstOrDefault(p => p.Id == id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error retrieving point {id}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Saves a point to the database
        /// </summary>
        /// <param name="point">Point to save</param>
        /// <returns>Saved point with updated ID</returns>
        public CA_Point Save(CA_Point point)
        {
            try
            {
                if (point.Id == 0)
                {
                    _context.Points.Add(point);
                }
                else
                {
                    point.ModifiedDate = DateTime.Now;
                    _context.Points.Update(point);
                }

                _context.SaveChanges();
                return point;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error saving point: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deletes a point from the database
        /// </summary>
        /// <param name="id">Point ID to delete</param>
        /// <returns>True if deleted successfully</returns>
        public bool Delete(int id)
        {
            try
            {
                var point = _context.Points.Find(id);
                if (point != null)
                {
                    _context.Points.Remove(point);
                    _context.SaveChanges();
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error deleting point {id}: {ex.Message}", ex);
            }
        }
    }
}
