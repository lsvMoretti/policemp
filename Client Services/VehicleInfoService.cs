using System.Collections.Generic;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;
using System.Threading.Tasks;

namespace PoliceMP.Client.Services
{
    public class VehicleInfoService : IVehicleInfoService
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<VehicleInfo> _logger;
        private readonly IVehicleInfoCacheService _vehicleInfoCacheService;

        public VehicleInfoService(IClientCommunicationsManager comms,
            ILogger<VehicleInfo> logger,
            IVehicleInfoCacheService vehicleInfoCacheService)
        {
            _comms = comms;
            _logger = logger;
            _vehicleInfoCacheService = vehicleInfoCacheService;
        }

        public async Task<VehicleInfo> GetByNetworkId(int networkId, string plate, int ownerPedNetworkId = -1)
        {
            var cachedVehicleInfo = _vehicleInfoCacheService.GetByNetworkId(networkId);
            if (cachedVehicleInfo != null) return cachedVehicleInfo;

            _logger.Trace($"Retrieving VehicleInfo for NetworkId {networkId} and plate {plate}");
            var vehicleInfo = await _comms
                .Request<VehicleInfo>(ServerEvents.GetVehicleInfoByNetworkId,
                    networkId,
                    plate,
                    ownerPedNetworkId);

            if (vehicleInfo == null)
            {
                _logger.Trace($"Failed to get VehicleInfo for NetworkId {networkId}");
                return null;
            }

            _logger.Trace($"Successfully got VehicleInfo for NetworkId {networkId}");
            _vehicleInfoCacheService.Cache(vehicleInfo);

            return vehicleInfo;
        }

        public async Task<bool> UpdateMarkers(int networkId, string plate, List<string> markers)
        {
            if (!_vehicleInfoCacheService.RemoveCache(networkId))
            {
                _logger.Error("An error occurred trying to remove the cache.");
                return false;
            }

            var updatedVehicleMarkers =
                await _comms.Request<bool>(ServerEvents.UpdateVehicleInfoMarkers, networkId, markers);
            if (!updatedVehicleMarkers)
            {
                _logger.Error("An error occurred updating the vehicle markers.");
                return false;
            }

            var newVehicleInfo = await GetByNetworkId(networkId, plate);
            return newVehicleInfo != null;
        }

        public async Task<bool> UpdateExpiredMarkers(int networkId, string plate, bool taxExpired, bool motExpired,
            bool insuranceExpired)
        {
            if (!_vehicleInfoCacheService.RemoveCache(networkId))
            {
                _logger.Error("An error occurred trying to remove the cache.");
                return false;
            }

            _logger.Debug($"Requesting Update of Markers: {taxExpired} - {motExpired} - {insuranceExpired}");

            var boolList = new List<bool>
            {
                taxExpired,
                motExpired,
                insuranceExpired
            };
            
            var updatedExpiredMarkers = await _comms.Request<bool>(ServerEvents.UpdatedVehicleExpiredMarkers, networkId,
                boolList);
            if (!updatedExpiredMarkers)
            {
                _logger.Error("An error occurred updated the expired markers!");
                return false;  
            }

            var newVehicleInfo = await GetByNetworkId(networkId, plate);
            return newVehicleInfo != null;

        }
    }
}