using PoliceMP.Core.Shared.Models;

namespace PoliceMP.Shared.Models
{
    public class RoadManagementItem
    {
        public uint ObjectHash { get; set; }
        public int NetworkId { get; set; }
        public Vector3 Position { get; set; }
        public bool HasSpeedZone { get; set; }
        public int SpeedZone { get; set; }
        public int ObjectHandle { get; set; }
        public int BlipId { get; set; }
        public int RatNetworkId { get; set; }
        public int PlacedByNetworkId { get; set; }
    }
}