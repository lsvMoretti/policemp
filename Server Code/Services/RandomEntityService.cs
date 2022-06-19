using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Main.Core.Server.Enums;
using PoliceMP.Main.Core.Shared;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;
using PoliceMP.Shared.Options;
using Debug = CitizenFX.Core.Debug;

namespace PoliceMP.Server.Services
{
    public enum RandomPedType
    {
        Random,
        Male,
        Female
    }

    public interface IRandomEntityService
    {
        PedHash FetchRandomPed(RandomPedType pedType = RandomPedType.Random);
        Task<Ped> GenerateRandomPed(Vector3 spawnPosition, float heading, RandomPedType pedType);
        uint FetchRandomVehicle();
        uint FetchRandomVehicle(VehicleDataClass vehicleDataClass);

        Task<Vehicle> GenerateRandomVehicle(Vector3 spawnPosition, float heading, bool randomClass,
            VehicleDataClass vehicleDataClass);

        Task<List<Entity>> GenerateRandomVehicleWithPed(Vector3 spawnPosition, float heading, bool randomClass,
            VehicleDataClass vehicleDataClass, uint pedHash);
    }

    public class RandomEntityService : Controller , IRandomEntityService
    {
        private static List<PedData> _pedList;
        private static List<VehicleData> _vehicleList;
        
        public RandomEntityService()
        {

        }
        
        public override Task Started()
        {
            var pedFileContents = File.ReadAllText($"{AppContext.BaseDirectory}\\server\\Options\\Peds.conf.json");
            _pedList = JsonConvert.DeserializeObject<List<PedData>>(pedFileContents);

            var vehicleFileContents = File.ReadAllText($"{AppContext.BaseDirectory}\\server\\Options\\Vehicles.conf.json");
            _vehicleList = JsonConvert.DeserializeObject<List<VehicleData>>(vehicleFileContents);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Generates a Random Ped at a Position
        /// </summary>
        /// <param name="spawnPosition"></param>
        /// <param name="heading"></param>
        /// <param name="pedType"></param>
        /// <returns>Returns Ped if spawned and has owner, else Returns Null</returns>
        public async Task<Ped> GenerateRandomPed(Vector3 spawnPosition, float heading, RandomPedType pedType)
        {
            // Wait for main thread
            await Delay(0);

            var pedHash = FetchRandomPed(pedType);

            var pedHandle = API.CreatePed(1, (uint) pedHash, spawnPosition.X, spawnPosition.Y, spawnPosition.Z, heading,
                true, true);

            await BaseScript.Delay(1000);

            var sw = new Stopwatch();
            
            sw.Start();
            
            while (!API.DoesEntityExist(pedHandle))
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                await Delay(10);
            }

            var ped = (Ped) Entity.FromHandle(pedHandle);

            sw.Restart();
            
            while (ped == null)
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                ped = (Ped) Entity.FromHandle(pedHandle);
                await Delay(10);
            }

            sw.Restart();
            
            while (ped.NetworkId == 0)
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                await Delay(10);
            }

            sw.Restart();
            
            while (ped.Owner == null && sw.Elapsed.Minutes < 5)
            {
                await Delay(10);
            }

            return ped.Owner == null ? null : ped;
        }

        public PedHash FetchRandomPed(RandomPedType pedType = RandomPedType.Random)
        {
            var random = new Random();
            
            if (pedType == RandomPedType.Random)
            {
                var probability = random.Next(101);
                pedType = probability <= 20 ? RandomPedType.Female : RandomPedType.Male;
            }
            
            if (pedType == RandomPedType.Male)
            {
                var maleList =
                    _pedList.Where(x => x.Pedtype.ToLower().Contains("civmale") && x.CanSpawnInCar).ToList();
                var rndMaleIndex = random.Next(maleList.Count -1);
                var randomMalePedData = maleList[rndMaleIndex];
                var malePedHashUint = Convert.ToUInt32(randomMalePedData.Hash);
                var malePedHash = (PedHash) malePedHashUint;
                return malePedHash;
            }
            var femaleList =
                _pedList.Where(x => x.Pedtype.ToLower().Contains("civfemale") && x.CanSpawnInCar).ToList();
            var rndFemaleIndex = random.Next(femaleList.Count - 1);
            var randomFemalePedData = femaleList[rndFemaleIndex];
            var femalePedHashUint = Convert.ToUInt32(randomFemalePedData.Hash);
            var femalePedHash = (PedHash) femalePedHashUint;
            return femalePedHash;
        }

        /// <summary>
        /// Gets a Random Vehicle and returns when spawned
        /// </summary>
        /// <param name="spawnPosition"></param>
        /// <param name="heading"></param>
        /// <param name="randomClass">Ignore vehicleDataClass</param>
        /// <param name="vehicleDataClass"></param>
        /// <returns>Vehicle if Spawned, Null if not!</returns>
        public async Task<Vehicle> GenerateRandomVehicle(Vector3 spawnPosition, float heading, bool randomClass,
            VehicleDataClass vehicleDataClass)
        {
            await Delay(0);

            var vehicleHash = randomClass ? FetchRandomVehicle() : FetchRandomVehicle(vehicleDataClass);

            var vehicleHandle = API.CreateVehicle(vehicleHash, spawnPosition.X, spawnPosition.Y, spawnPosition.Z,
                heading, true, true);
            
            Debug.WriteLine($"VehicleHandle: {vehicleHandle}");

            await BaseScript.Delay(1000);

            var sw = new Stopwatch();
            
            sw.Start();
            
            while (!API.DoesEntityExist(vehicleHandle))
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                
                Debug.WriteLine("Entity not existing");
                await Delay(10);
            }

            var vehicle = (Vehicle) Entity.FromHandle(vehicleHandle);

            sw.Restart();
            
            while (vehicle == null)
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                
                Debug.WriteLine("Vehicle is NULL");
                vehicle = (Vehicle) Entity.FromHandle(vehicleHandle);
                await Delay(10);
            }

            sw.Restart();
            
            while (vehicle.NetworkId == 0)
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                
                Debug.WriteLine("NetworkID 0");
                await Delay(10);
            }

            sw.Restart();
            
            while (vehicle.Owner == null && sw.Elapsed.Minutes < 5)
            {
                Debug.WriteLine("Vehicle Owner is null");
                await Delay(10);
            }

            return vehicle.Owner == null ? null : vehicle;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="spawnPosition"></param>
        /// <param name="heading"></param>
        /// <param name="randomClass"></param>
        /// <param name="vehicleDataClass"></param>
        /// <param name="pedHash"></param>
        /// <returns>Entity 1 = Vehicle, Entity 2 = Ped</returns>
        public async Task<List<Entity>> GenerateRandomVehicleWithPed(Vector3 spawnPosition, float heading, bool randomClass,
            VehicleDataClass vehicleDataClass, uint pedHash)
        {
            Debug.WriteLine("Generating Random Vehicle");

            var vehicle = await GenerateRandomVehicle(spawnPosition, heading, randomClass, vehicleDataClass);

            await BaseScript.Delay(1000);
            var sw = new Stopwatch();
            
            sw.Start();
            
            while (vehicle == null)
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                
                Debug.WriteLine("Vehicle is NULL");
                vehicle = await GenerateRandomVehicle(spawnPosition, heading, randomClass, vehicleDataClass);
                await Delay(10);
            }

            sw.Restart();
            
            while (vehicle.NetworkId == 0)
            {
                if (sw.Elapsed > TimeSpan.FromSeconds(3))
                {
                    return null;
                }
                
                Debug.WriteLine("NetworkID 0");
                await Delay(10);
            }

            sw.Restart();
            
            while (vehicle.Owner == null && sw.Elapsed.Minutes < 5)
            {
                Debug.WriteLine("Vehicle Owner is null");
                await Delay(10);
            }

            var ped = await GenerateRandomPed(spawnPosition, heading, RandomPedType.Random);

            sw.Restart();
            
            while (!API.DoesEntityExist(ped.Handle))
            {
                if (sw.Elapsed.Seconds > 5)
                {
                    return null;
                }
                Debug.WriteLine("Ped Doesn't Exist!");
                await Delay(10);
            }
            
            API.TaskWarpPedIntoVehicle(ped.Handle, vehicle.Handle, (int)VehicleSeat.Driver);
            
            Debug.WriteLine($"Spawned {ped.Handle} into {vehicle.Handle}");

            var returnList = new List<Entity>
            {
                vehicle,
                ped
            };
            return returnList;
        }

        public uint FetchRandomVehicle()
        {
            Debug.WriteLine($"Fetching Random Vehicle");
            var random = new Random();
            Debug.WriteLine(_vehicleList.Count.ToString());
            var vehicleList = _vehicleList.Where(v => v.Class != VehicleDataClass.Emergency && v.Class != VehicleDataClass.Military && v.Class != VehicleDataClass.Helicopter && v.Class != VehicleDataClass.Plane && v.Class != VehicleDataClass.Boat && v.Class != VehicleDataClass.Rail).ToList();
            var randomVehicleIndex = random.Next(vehicleList.Count - 1);
            var randomVehicleData = vehicleList[randomVehicleIndex];
            var vehicleHashUint = Convert.ToUInt32(randomVehicleData.Hash);
            return vehicleHashUint;
        }

        public uint FetchRandomVehicle(VehicleDataClass vehicleDataClass)
        {
            Debug.WriteLine($"Fetching Vehicle by class");
            var random = new Random();
            var vehicleList = _vehicleList.Where(v => v.Class == vehicleDataClass).ToList();
            Debug.WriteLine($"Found {vehicleList.Count} Vehicles");
            var randomIndex = random.Next(_vehicleList.Count - 1);
            var randomVehicle = vehicleList[randomIndex];
            var vehicleHashUint = Convert.ToUInt32(randomVehicle.Hash);
            return vehicleHashUint;
        }
    }
}