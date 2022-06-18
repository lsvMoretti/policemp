using System.Threading.Tasks;
using CitizenFX.Core;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Commands.Interfaces;

namespace PoliceMP.Client.Scripts.Commands
{
    public class OtherCommands : Script
    {
        private readonly ICommandManager _commandManager;

        public OtherCommands(ICommandManager commandManager)
        {
            _commandManager = commandManager;
        }
        
        protected override async Task OnStartAsync()
        {
            _commandManager.Register("shuff").WithHandler(OnShuffCommand);
        }

        private void OnShuffCommand()
        {
            var player = Game.PlayerPed;

            if (player.CurrentVehicle == null) return;

            if (player.SeatIndex == VehicleSeat.Passenger && player.CurrentVehicle.IsSeatFree(VehicleSeat.Driver))
            {
                player.Task.ShuffleToNextVehicleSeat(player.CurrentVehicle);
            }
        }
    }
}