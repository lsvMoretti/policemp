using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.EntityFrameworkCore.Internal;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Main.Core.Server.Extensions;
using PoliceMP.Main.Core.Shared;
using PoliceMP.Server.Controllers;
using PoliceMP.Server.Extensions;
using PoliceMP.Server.Factories;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Enums;
using Stateless;
using Debug = CitizenFX.Core.Debug;
using Vector3 = PoliceMP.Core.Shared.Models.Vector3;

namespace PoliceMP.Server.Services.Callouts.CalloutTypes
{
    public class VehicleBreakIn : Callout
    {
        #region States

        enum Trigger
        {
            Initialize,
            WalkToVehicle,
            SmashWindow,
            WindowSmashed,
            ReturnedToSpawnPos,
            InteractedWithPed
        }

        enum State
        {
            NotStarted,
            WalkToVehicle,
            SmashingWindow,
            WindowSmashed,
            ReturnedToSpawnPoint,
            HasBeenInteracted
        }

        #endregion

        #region Variables
        
        private readonly List<Vector3> _spawnLocations = new();
        private Vehicle _vehicle;
        private Vector3 _doorPosition;
        private readonly List<Ped> _peds = new();
        private int _targetVehicleNetId;
        
        #endregion

        #region Services

        private readonly ILogger<VehicleBreakIn> _logger;
        private readonly ITickService _tick;
        private readonly IRandomEntityService _entityService;
        private readonly IPositionsFactory _positionsFactory;
        private readonly IServerCommunicationsManager _comms;
        private readonly StateMachine<State, Trigger> _calloutState = new(State.NotStarted);
        
        #endregion

        public VehicleBreakIn(ILogger<VehicleBreakIn> logger, ITickService tick, IRandomEntityService entityService,
            IPositionsFactory positionsFactory, IServerCommunicationsManager comms)
        {
            _logger = logger;
            _tick = tick;
            _entityService = entityService;
            _positionsFactory = positionsFactory;
            _comms = comms;
            
            OnPlayerJoined += PlayerJoined;
            
            _calloutState.Configure(State.NotStarted)
                .InternalTransitionAsync(Trigger.Initialize, InitializeCallout)
                .Permit(Trigger.WalkToVehicle, State.WalkToVehicle)
                .Permit(Trigger.InteractedWithPed, State.HasBeenInteracted);

            _calloutState.Configure(State.WalkToVehicle)
                .OnEntryAsync(WalkToVehicle)
                .Permit(Trigger.SmashWindow, State.SmashingWindow)
                .Permit(Trigger.InteractedWithPed, State.HasBeenInteracted);

            _calloutState.Configure(State.SmashingWindow)
                .OnEntryAsync(SmashVehicleWindow)
                .Permit(Trigger.WindowSmashed, State.WindowSmashed)
                .Permit(Trigger.InteractedWithPed, State.HasBeenInteracted);

            _calloutState.Configure(State.WindowSmashed)
                .OnEntryAsync(OnTargetReturnToSpawnPosition)
                .Permit(Trigger.ReturnedToSpawnPos, State.ReturnedToSpawnPoint)
                .Permit(Trigger.InteractedWithPed, State.HasBeenInteracted);

            _calloutState.Configure(State.HasBeenInteracted);
            
            OnCalloutEnd += CalloutEnded; 
            OnPlayerInteractWithPed += PlayerInteractWithPed;

            _comms.On<Player, int>(CalloutEvents.VehicleBreakInVehicleBrokenInto, OnVehicleWindowSmashed);
        }

        private async Task PlayerInteractWithPed(Player player, Ped ped)
        {
            if (ped != _peds.FirstOrDefault()) return;

            await _calloutState.FireAsync(Trigger.InteractedWithPed);
        }

        private async Task OnTargetReturnToSpawnPosition()
        {
            var randomAnimList = new Dictionary<string, string>
            {
                {"amb@world_human_bum_standing@twitchy@idle_a", "idle_c"},
                {"mp_missheist_countrybank@nervous", "nervous_idle"},
                {"rcmme_tracey1", "nervous_loop"}
            };

            var ped = _peds.FirstOrDefault();

            if (ped == null)
            {
                OnCalloutEnded(false);
                return;
            }
            
            if (_calloutState.IsInState(State.HasBeenInteracted)) return;
            
            
            API.TaskReactAndFleePed(ped.Handle, _vehicle.Handle);

            await BaseScript.Delay(PoliceMpRandom.Next(15000));
            
            if (_calloutState.IsInState(State.HasBeenInteracted)) return;

            if (!_calloutState.IsInState(State.ReturnedToSpawnPoint)) return;

            var rnd = new Random();

            var wanderAreaChance = rnd.Next(101) < 30;

            if (wanderAreaChance)
            {
                _comms.ToClient(ped.Owner, CalloutEvents.VehicleBreakInWanderArea, ped.NetworkId);
                return;
            }
            
            var randomAnimIndex = PoliceMpRandom.Next(randomAnimList.Count - 1);
            var randomAnim = randomAnimList.ElementAt(randomAnimIndex);

            _comms.ToClient(ped.Owner, CalloutEvents.VehicleBreakInPlayPedAnim, ped.NetworkId, randomAnim.Key, randomAnim.Value);
            
            _logger.Debug($"Played Anim {randomAnim.Key}");
        }

        private async void OnVehicleWindowSmashed(Player player, int vehicleNetworkHandle)
        {
            if (vehicleNetworkHandle != _targetVehicleNetId) return;
            var targetPed = _peds.FirstOrDefault();

            if (targetPed == null)
            {
                OnCalloutEnded(false);
                return;
            }

            if (_calloutState.IsInState(State.HasBeenInteracted)) return;
            
            var targetPedSpawnPosition = _spawnLocations.Skip(1).FirstOrDefault().ConvertToCitizen();
            
            API.TaskGoToCoordAnyMeans(targetPed.Handle, targetPedSpawnPosition.X, targetPedSpawnPosition.Y, targetPedSpawnPosition.Z, 1.5f, 0, false, 786603, 0f);

            await _calloutState.FireAsync(Trigger.WindowSmashed);
        }

        private async Task CalloutEnded(bool retry)
        {
            CleanupCallout();
        }

        public override async Task Setup()
        {
            _tick.On(OnTick);
            await _calloutState.FireAsync(Trigger.Initialize);
            return;
        }

        private async Task SmashVehicleWindow()
        {
            var vehicleOwner = _vehicle.Owner;

            var ownerTryCount = 0;
            
            while (vehicleOwner == null && ownerTryCount < 5)
            {
                ownerTryCount++;
                await BaseScript.Delay(1000);
            }

            if (vehicleOwner == null)
            {
                OnCalloutEnded(true);
                return;
            }

            var attackerPed = _peds.FirstOrDefault();
            
            if (_calloutState.IsInState(State.HasBeenInteracted)) return;
            _comms.ToClient(vehicleOwner, CalloutEvents.VehicleBreakInSetPedToBreakIn, _vehicle.NetworkId, attackerPed?.NetworkId);
        }

        private async Task WalkToVehicle()
        {
            var vehicleOwner = _vehicle.Owner;

            var ownerTryCount = 0;
            
            while (vehicleOwner == null && ownerTryCount < 5)
            {
                ownerTryCount++;
                await BaseScript.Delay(1000);
            }

            if (vehicleOwner == null)
            {
                OnCalloutEnded(true);
                return;
            }

            ownerTryCount = 0;

            _doorPosition = await _comms.Request<Vector3>(vehicleOwner, CalloutEvents.VehicleBreakInGetDoorPosition, _vehicle.NetworkId);

            var targetPed = _peds.FirstOrDefault();

            if (targetPed == null)
            {
                OnCalloutEnded(true);
                return;
            }
            
            while (targetPed.Owner == null && ownerTryCount < 5)
            {
                ownerTryCount++;
                await BaseScript.Delay(1000);
            }

            if (targetPed.Owner == null)
            {
                OnCalloutEnded(true);
                return;
            }
            
            if (_calloutState.IsInState(State.HasBeenInteracted)) return;
            API.TaskGoToCoordAnyMeans(targetPed.Handle, _doorPosition.X, _doorPosition.Y, _doorPosition.Z, 1f, 0, false, 786603, 0f);
        }
        
        private async Task InitializeCallout()
        {
            //TODO: CALLOUTS: Change this when CarPark location type is added!
            //_spawnLocations = await _positionsFactory.GetMultiplePositionsNearOneByType(LocationType.RandomPositionOnStreet, _pedCount);

            var spawn = new Vector3(1152.404296875f, -472.55081176758f, 66.548828125f);
            _spawnLocations.Add(spawn);
            
            var newSpawnPos = spawn.Around(10f);
            _spawnLocations.Add(newSpawnPos);

            _logger.Debug($"Found {_spawnLocations.Count} spawn positions");

            var vehicleSpawn =_spawnLocations.FirstOrDefault().ConvertToCitizen();
            
            _vehicle = await _entityService.GenerateRandomVehicle(vehicleSpawn, 0f, true, VehicleDataClass.Muscle);

            if (_vehicle == null)
            {
                OnCalloutEnded(true);
                return;
            }

            _targetVehicleNetId = _vehicle.NetworkId;

            var pedSpawns = _spawnLocations.Skip(1);

            var randomIdleList = new Dictionary<string, string>
            {
                {"anim@heists@heist_corona@team_idles@male_a", "idle"},
                {"amb@world_human_hang_out_street@male_b@idle_a", "idle_b"},
                {"friends@fra@ig_1", "base_idle"},
                {"random@countrysiderobbery", "idle_a"},
                {"anim@mp_corona_idles@male_d@idle_a", "idle_a"},
                {"anim@mp_corona_idles@male_c@idle_a", "idle_a"}
            };

            foreach (var pedSpawn in pedSpawns)
            {
                var spawnLocation = pedSpawn.ConvertToCitizen();

                var ped = await _entityService.GenerateRandomPed(spawnLocation, 0f, RandomPedType.Random);
                if (ped == null)
                {
                    OnCalloutEnded(true);
                    return;
                }

                var randomIdleIndex = PoliceMpRandom.Next(randomIdleList.Count - 1);
                var randomIdleAnim = randomIdleList.ElementAt(randomIdleIndex);
                _comms.ToClient(ped.Owner, CalloutEvents.VehicleBreakInPlayPedAnim, ped.NetworkId, randomIdleAnim.Key, randomIdleAnim.Value);
                
                _peds.Add(ped);
            }

            await _calloutState.FireAsync(Trigger.WalkToVehicle);
        }

        public override async Task Start()
        {
            return;
        }
        
        private async Task PlayerJoined(Player player)
        {
            if (player.Character == null) return;

            var firstSpawnLoc = _spawnLocations.FirstOrDefault().ConvertToCitizen();

            player.Character.Position = firstSpawnLoc + new CitizenFX.Core.Vector3(0f, 0f, 2f);
        }

        private async Task OnTick()
        {
            _logger.Debug(_calloutState.State.ToString());
            
            var targetPed = _peds.FirstOrDefault();
            if (_calloutState.IsInState(State.WalkToVehicle))
            {
                if (targetPed == null) return;
                var doorPos = _doorPosition.ConvertToCitizen();

                if (targetPed.Position.Distance(doorPos) < 3f)
                {
                    await _calloutState.FireAsync(Trigger.SmashWindow);
                    return;
                }
                return;
            }

            if (_calloutState.IsInState(State.WindowSmashed))
            {
                var spawnPosition = _spawnLocations.FirstOrDefault().ConvertToCitizen();
                var pedDistanceToSpawnPoint = targetPed?.Position.Distance(spawnPosition);
                if (pedDistanceToSpawnPoint > 2f) return;
                await _calloutState.FireAsync(Trigger.ReturnedToSpawnPos);
            }
            
            await BaseScript.Delay(1000);
        }

        public override async Task End()
        {
            CleanupCallout();
            return;
        }

        private void CleanupCallout()
        {
            foreach (var ped in _peds)
            {
                if(!API.DoesEntityExist(ped.Handle)) continue;
                
                API.DeleteEntity(ped.Handle);
            }

            if (API.DoesEntityExist(_vehicle.Handle))
            {
                API.DeleteEntity(_vehicle.Handle);
            }
            
            _tick.Off(OnTick);
            return;
        }
    }
}