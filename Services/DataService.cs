using Newtonsoft.Json;
using GasFlowCalculator.Models;

namespace GasFlowCalculator.Services
{
    /// <summary>
    /// Service for loading and saving data from JSON files
    /// </summary>
    public class DataService
    {
        private readonly string _dataPath;
        private List<CA_Network>? _networks;
        private List<CA_Point>? _points;
        private List<BLT_Segment>? _segments;
        private List<PointVolume>? _volumes;
        private List<CA_SegmentFlow>? _flows;

        public DataService()
        {
            _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            Directory.CreateDirectory(_dataPath);
        }

        public async Task<List<CA_Network>> GetNetworksAsync()
        {
            if (_networks == null)
            {
                var filePath = Path.Combine(_dataPath, "networks.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    _networks = JsonConvert.DeserializeObject<List<CA_Network>>(json) ?? new List<CA_Network>();
                }
                else
                {
                    _networks = new List<CA_Network>();
                }
            }
            return _networks;
        }

        public async Task<List<CA_Point>> GetPointsAsync()
        {
            if (_points == null)
            {
                var filePath = Path.Combine(_dataPath, "points.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    _points = JsonConvert.DeserializeObject<List<CA_Point>>(json) ?? new List<CA_Point>();
                }
                else
                {
                    _points = new List<CA_Point>();
                }
            }
            return _points;
        }

        public async Task<List<BLT_Segment>> GetSegmentsAsync()
        {
            if (_segments == null)
            {
                var filePath = Path.Combine(_dataPath, "segments.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    _segments = JsonConvert.DeserializeObject<List<BLT_Segment>>(json) ?? new List<BLT_Segment>();
                }
                else
                {
                    _segments = new List<BLT_Segment>();
                }
            }
            return _segments;
        }

        public async Task<List<PointVolume>> GetVolumesAsync()
        {
            if (_volumes == null)
            {
                var filePath = Path.Combine(_dataPath, "volumes.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    _volumes = JsonConvert.DeserializeObject<List<PointVolume>>(json) ?? new List<PointVolume>();
                }
                else
                {
                    _volumes = new List<PointVolume>();
                }
            }
            return _volumes;
        }

        public async Task<List<CA_SegmentFlow>> GetFlowsAsync()
        {
            if (_flows == null)
            {
                var filePath = Path.Combine(_dataPath, "flows.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    _flows = JsonConvert.DeserializeObject<List<CA_SegmentFlow>>(json) ?? new List<CA_SegmentFlow>();
                }
                else
                {
                    _flows = new List<CA_SegmentFlow>();
                }
            }
            return _flows;
        }

        public async Task SaveFlowsAsync(List<CA_SegmentFlow> flows)
        {
            _flows = flows;
            var filePath = Path.Combine(_dataPath, "flows.json");
            var json = JsonConvert.SerializeObject(flows, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task<List<CA_Point>> GetPointsByNetworkIdAsync(int networkId)
        {
            var points = await GetPointsAsync();
            return points.Where(p => p.NetworkId == networkId && p.IsActive).ToList();
        }

        public async Task<List<BLT_Segment>> GetSegmentsByNetworkIdAsync(int networkId)
        {
            var segments = await GetSegmentsAsync();
            return segments.Where(s => s.NetworkId == networkId && s.IsActive).ToList();
        }

        public async Task<List<PointVolume>> GetVolumesByDateAsync(DateTime date)
        {
            var volumes = await GetVolumesAsync();
            return volumes.Where(v => v.Date.Date == date.Date).ToList();
        }

        public async Task<decimal> GetPointVolumeAsync(int pointId, DateTime date, string volumeType)
        {
            var volumes = await GetVolumesAsync();
            var volume = volumes.FirstOrDefault(v => v.PointId == pointId && 
                                                    v.Date.Date == date.Date && 
                                                    v.VolumeType == volumeType);
            return volume?.Volume ?? 0;
        }

        public async Task<CA_Network?> GetNetworkByIdAsync(int id)
        {
            var networks = await GetNetworksAsync();
            return networks.FirstOrDefault(n => n.Id == id);
        }

        public async Task<CA_Point?> GetPointByIdAsync(int id)
        {
            var points = await GetPointsAsync();
            return points.FirstOrDefault(p => p.Id == id);
        }

        public async Task<BLT_Segment?> GetSegmentByIdAsync(int id)
        {
            var segments = await GetSegmentsAsync();
            return segments.FirstOrDefault(s => s.Id == id);
        }

        public void ClearCache()
        {
            _networks = null;
            _points = null;
            _segments = null;
            _volumes = null;
            _flows = null;
        }
    }
}