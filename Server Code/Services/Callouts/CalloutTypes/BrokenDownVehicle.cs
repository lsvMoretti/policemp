using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Server.Abstraction;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Server.Extensions;
using PoliceMP.Server.Factories;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Enums;
using Stateless;
namespace PoliceMP.Server.Services.Callouts.CalloutTypes
{
    public class BrokenDownVehicle : Callout
    {
        #region States
        enum Trigger
        {
            Initialize,
            SetIntoVehicle,
            WanderVehicle,
            BreakVehicle,
            InteractedWithVehicle,
        }
        enum State
        {
            NotStarted,
            SettingIntoVehicle,
            VehicleWandering,
            VehicleBreaking,
            VehicleHasBeenInteracted
        }
        #endregion
        #region Variables
        private Vehicle _brokenVehicle;
        private Ped _ped;
        private Vector3 _calloutPosition;
        #endregion
        #region Services
        private readonly ILogger<BrokenDownVehicle> _logger;
        private readonly IServerCommunicationsManager _comms;
        private readonly IRandomEntityService _entityService;
        private readonly IPositionsFactory _positionsFactory;
        private readonly ITickService _tick;
        private readonly StateMachine<State, Trigger> _calloutState = new(State.NotStarted);
        #endregion
        
        public BrokenDownVehicle(ILogger<BrokenDownVehicle> logger, IServerCommunicationsManager comms, IRandomEntityService randomEntityService, IPositionsFactory positionsFactory, ITickService tickService)
        {
            _logger = logger;
            _comms = comms;
            _entityService = randomEntityService;
            _positionsFactory = positionsFactory;
            _tick = tickService;
            
            OnPlayerInteractWithPed += PlayerInteractWithPed;
            OnPlayerInteractWithVehicle += PlayerInteractWithVehicle;
            OnPlayerJoined += OnOnPlayerJoined;
        }
        
        public override async Task Setup()
        {
            _calloutState.Configure(State.NotStarted)
                .InternalTransition(Trigger.Initialize, InitCallout)
                .Permit(Trigger.SetIntoVehicle, State.SettingIntoVehicle)
                .Permit(Trigger.InteractedWithVehicle, State.VehicleHasBeenInteracted);
            _calloutState.Configure(State.SettingIntoVehicle)
                .OnEntry(HandleSettingIntoVehicle)
                .Permit(Trigger.InteractedWithVehicle, State.VehicleHasBeenInteracted)
                .Permit(Trigger.WanderVehicle, State.VehicleWandering);
            _calloutState.Configure(State.VehicleWandering)
                .OnEntryAsync(OnVehicleWandering)
                .Permit(Trigger.BreakVehicle, State.VehicleBreaking)
                .Permit(Trigger.InteractedWithVehicle, State.VehicleHasBeenInteracted);
            _calloutState.Configure(State.VehicleBreaking)
                .OnEntryAsync(OnVehicleRandomBreak)
                .Permit(Trigger.InteractedWithVehicle, State.VehicleHasBeenInteracted);
            
            _calloutState.Configure(State.VehicleHasBeenInteracted);
            await _calloutState.FireAsync(Trigger.Initialize);
        }
        
        public override async Task Start()
        {
        }
        
        public override Task End()
        {
            if (_brokenVehicle != null && API.DoesEntityExist(_brokenVehicle.Handle))
            {
                API.DeleteEntity(_brokenVehicle.Handle);
            }
            if (_ped != null && API.DoesEntityExist(_ped.Handle))
            {
                API.DeleteEntity(_ped.Handle);
            }
            return Task.CompletedTask;
        }

        private async Task HandlePedInVehicle()
        {
            if (!_calloutState.IsInState(State.SettingIntoVehicle))
            {
                _tick.Off(HandlePedInVehicle);
                return;
            }

            if (API.GetPedInVehicleSeat(_brokenVehicle.Handle, (int)VehicleSeat.Driver) == _ped.Handle)
            {
                await _calloutState.FireAsync(Trigger.WanderVehicle);
            }
            
            if (API.GetPedInVehicleSeat(_brokenVehicle.Handle, (int)VehicleSeat.Driver) != _ped.Handle)
            {
                API.SetPedIntoVehicle(_ped.Handle, _brokenVehicle.Handle, (int)VehicleSeat.Driver);
            }

            await BaseScript.Delay(1000);
        }

        private Task OnOnPlayerJoined(Player player)
        {
            player.Character.Position = _calloutPosition;
            return Task.CompletedTask;
        }

        private async Task OnVehicleRandomBreak()
        {
            var rnd = new Random();
            var randomTime = rnd.Next(10000, 15000);
            _logger.Debug($"Random Break Time: {randomTime}");
            await BaseScript.Delay(randomTime);
            if (_calloutState.IsInState(State.VehicleHasBeenInteracted)) return;
            _comms.ToClient(_brokenVehicle.Owner, CalloutEvents.BrokenDownVehicleBreakDown, _brokenVehicle.Handle);
        }
        private async Task OnVehicleWandering()
        {
            _logger.Debug($"Vehicle Wandering");
            if (!API.DoesEntityExist(_brokenVehicle.Handle)) return;
            _comms.ToClient(_brokenVehicle.Owner, CalloutEvents.BrokenDownVehicleWander, _brokenVehicle.NetworkId);
            await _calloutState.FireAsync(Trigger.BreakVehicle);
        }
        
        private async void InitCallout()
        {
            var rnd = new Random();
            
            var randomPedHash = _entityService.FetchRandomPed();
            var randomPositions = await _positionsFactory.GetMultiplePositionsNearOneByType(LocationType.RandomPositionOnStreet, 1);
            var randomPosition = randomPositions.FirstOrDefault();
            _calloutPosition = randomPosition.ConvertToCitizen();
            var randomHeading = rnd.Next(180);
            var calloutEntities = await _entityService.GenerateRandomVehicleWithPed(randomPosition.ConvertToCitizen(), randomHeading, true,
                VehicleDataClass.Coupe, (uint)randomPedHash);

            var randomVehicle = (Vehicle)calloutEntities[0];
            var randomPed = (Ped) calloutEntities[1];

            if (randomVehicle == null)
            {
                _logger.Error("RandomVehicle is null!");
                OnCalloutEnded(true);
                return;
            }
            _brokenVehicle = randomVehicle;
            _ped = randomPed;

            await _calloutState.FireAsync(Trigger.SetIntoVehicle);
        }

        private void HandleSettingIntoVehicle()
        {
            _tick.On(HandlePedInVehicle);
        }
        
        private async Task PlayerInteractWithVehicle(Player player, Vehicle vehicle)
        {
            if (vehicle != _brokenVehicle) return;
            await _calloutState.FireAsync(Trigger.InteractedWithVehicle);
        }
        private async Task PlayerInteractWithPed(Player player, Ped ped)
        {
            if (_ped == null) return;
            if (ped != _ped) return;
            await _calloutState.FireAsync(Trigger.InteractedWithVehicle);
        }
    }
}