using System.Collections.Generic;
using PoliceMP.Shared.Models;
using System.Threading.Tasks;

namespace PoliceMP.Client.Services.Interfaces
{
    public interface IVehicleInfoService
    {
        Task<VehicleInfo> GetByNetworkId(int networkId, string plate, int ownerPedNetworkId = -1);
        Task<bool> UpdateMarkers(int networkId, string plate, List<string> markers);
        Task<bool> UpdateExpiredMarkers(int networkId, string plate, bool taxExpired, bool motExpired,
            bool insuranceExpired);
    }
}