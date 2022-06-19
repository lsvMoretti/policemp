using PoliceMP.Shared.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PoliceMP.Core.Server.Interfaces.Factories;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Core.Shared.Extensions;

namespace PoliceMP.Server.Services
{
    public class VehicleInfoService : IVehicleInfoService
    {
        private readonly IVehicleInfoFactory _vehicleInfoFactory;
        private readonly IPedInfoService _pedInfoService;
        private readonly ConcurrentDictionary<int, VehicleInfo> _vehicles;

        public VehicleInfoService(IVehicleInfoFactory vehicleInfoFactory, IPedInfoService pedInfoService)
        {
            _vehicleInfoFactory = vehicleInfoFactory;
            _pedInfoService = pedInfoService;
            _vehicles = new ConcurrentDictionary<int, VehicleInfo>();
        }

        public VehicleInfo GetByNetworkId(int networkId, string plate, int ownerPedNetworkId = -1)
        {
            VehicleInfo vehicleInfo;
            if (networkId < 1)
            {
                vehicleInfo = _vehicles
                    .FirstOrDefault(x => x.Value.Plate.EqualsIgnoreCase(plate))
                    .Value;

                if (vehicleInfo != null) return vehicleInfo;
            } 
            else if (_vehicles.TryGetValue(networkId, out vehicleInfo))
            {
                if (vehicleInfo.Plate.EqualsIgnoreCase(plate))
                    return vehicleInfo;

                _vehicles.TryRemove(networkId, out _);
            }

            string ownerName = null;
            if (ownerPedNetworkId > 0)
            {
                var pedInfo = _pedInfoService.GetByNetworkId(ownerPedNetworkId);
                ownerName = pedInfo?.FullName;
            }

            vehicleInfo = _vehicleInfoFactory.Random(networkId, plate, ownerName);
            return _vehicles.TryAdd(networkId, vehicleInfo) ? vehicleInfo : new VehicleInfo { NetworkId = -1 };
        }

        public bool UpdateMarkers(int networkId, List<string> markers)
        {
            var tryGet = _vehicles.TryGetValue(networkId, out VehicleInfo vehicleInfo);
            if (!tryGet) return false;

            vehicleInfo.Markers = markers;
            return true;
        }

        public bool UpdateExpiredMarkers(int networkId, List<bool> expiredMarkers)
        {
            var tryGet = _vehicles.TryGetValue(networkId, out VehicleInfo vehicleInfo);
            if (!tryGet) return false;

            var rnd = new Random();

            var taxExpired = expiredMarkers[0];
            var motExpired = expiredMarkers[1];
            var insuranceExpired = expiredMarkers[2];
            
            if (taxExpired != vehicleInfo.IsTaxExpired)
            {
                if (taxExpired)
                {
                    var taxExpiredDays = rnd.Next(1, 366);
                    vehicleInfo.TaxExpiryDate = DateTime.Now.AddDays(-taxExpiredDays);
                }
                else
                {
                    var taxDays = rnd.Next(1, 366);
                    vehicleInfo.TaxExpiryDate = DateTime.Now.AddDays(taxDays);
                }
            }

            if (motExpired != vehicleInfo.IsMotExpired)
            {
                if (motExpired)
                {
                    var motExpiredDays = rnd.Next(1, 366);
                    vehicleInfo.MotExpiryDate = DateTime.Now.AddDays(-motExpiredDays);
                }
                else
                {
                    var motDays = rnd.Next(1, 366);
                    vehicleInfo.MotExpiryDate = DateTime.Now.AddDays(motDays);
                }
            }
            
            if (insuranceExpired != vehicleInfo.IsInsuranceExpired)
            {
                if (insuranceExpired)
                {
                    var insuranceExpiredDays = rnd.Next(1, 366);
                    vehicleInfo.InsuranceExpiryDate = DateTime.Now.AddDays(-insuranceExpiredDays);
                }
                else
                {
                    var insuranceDays = rnd.Next(1, 366);
                    vehicleInfo.InsuranceExpiryDate = DateTime.Now.AddDays(insuranceDays);
                }
            }

            return true;
        }
    }
}