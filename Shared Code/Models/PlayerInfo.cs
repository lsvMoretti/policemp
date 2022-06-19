using PoliceMP.Core.Shared.Models;
using PoliceMP.Shared.Enums;

namespace PoliceMP.Shared.Models
{
    public class PlayerInfo
    {
        /// <summary>
        /// Player Name
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Serverside PlayerList Index
        /// </summary>
        public int Index { get; set; }
        public int ServerHandle { get; set; }
        public RoutingBucket RoutingBucket { get; set; }
        /// <summary>
        /// Ped NetworkId
        /// </summary>
        public int NetworkId { get; set; }
        /// <summary>
        /// Ped Position
        /// </summary>
        public Vector3 Position { get; set; }
        /// <summary>
        /// Ped Rotation
        /// </summary>
        public Vector3 Rotation { get; set; }
        /// <summary>
        /// Ped NetworkID - 0 if not in a vehicle
        /// </summary>
        public int VehicleNetworkId { get; set; }
        /// <summary>
        /// Assigned /callsign
        /// </summary>
        public string CallSign { get; set; }
        /// <summary>
        /// Selected Branch / Faction
        /// </summary>
        public UserBranch ActiveBranch { get; set; }
        /// <summary>
        /// Selected Sub Division
        /// </summary>
        public UserDivision ActiveDivision { get; set; }
    }
}