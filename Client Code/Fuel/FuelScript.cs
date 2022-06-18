using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Commands.Interfaces;
using PoliceMP.Core.Client.Extensions;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Shared.Constants;

namespace PoliceMP.Client.Scripts.Fuel
{
    public class  FuelScript : Script
    {
        private readonly ITickManager _ticks;
        private readonly IFeatureService _featureService;
        private readonly ICommandManager _command;
        private bool _featureEnabled = false;

        public FuelScript(ITickManager ticks, IFeatureService featureService, ICommandManager command)
        {
            _command = command;
            _ticks = ticks;
            _featureService = featureService;
            _command.Register("engine").WithHandler(ToggleEngineCommand);
        }
        protected override Task OnStartAsync()
        {
            _ticks.On(InfiniteFuelTick);
            _ticks.On(FuelFeatureCheck);
            _ticks.On(FuelTimer);
            
            API.RegisterKeyMapping("engine", "Toggle Vehicle Engine", "keyboard", "u");
            
            return Task.FromResult(0);
        }

        private void ToggleEngineCommand()
        {
            var currentVehicle = Game.PlayerPed.CurrentVehicle;

            if (currentVehicle == null) return;

            if (currentVehicle.Driver == null) return;

            if (currentVehicle.Driver != Game.PlayerPed) return;
            
            currentVehicle.SetEngineState(!currentVehicle.IsEngineRunning, false, true);
        }

        private async Task FuelTimer()
        {
            var currentVehicle = Game.PlayerPed.CurrentVehicle;
            if (currentVehicle == null) return;

            if (currentVehicle.ClassType == VehicleClass.Cycles) return;
            
            if(currentVehicle.Driver == null) return;
            if (currentVehicle.Driver != Game.PlayerPed) return;

            if (!currentVehicle.IsEngineRunning) return;
            
            if(currentVehicle.ClassType is VehicleClass.Helicopters or VehicleClass.Boats or VehicleClass.Planes) return;
            
            currentVehicle.FuelLevel -= 1;

            var delayTime = 27000;

            await Delay(delayTime);
        }

        private async Task FuelFeatureCheck()
        {
            _featureEnabled = _featureService.IsFeatureEnabled(FeatureToggle.InfiniteFuel);
            await Delay(30000);
        }


        private async Task InfiniteFuelTick()
        {
            if (!_featureEnabled)
            {
                await Delay(1000);
                return;
            }
            
            if(Game.PlayerPed.CurrentVehicle != null && Game.PlayerPed.CurrentVehicle.ClassType == VehicleClass.Emergency)
                Game.PlayerPed.CurrentVehicle.FuelLevel = 101;

            await Delay(1000);
        }
    }
}
