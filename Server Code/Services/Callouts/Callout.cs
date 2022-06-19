using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;

namespace PoliceMP.Server.Services.Callouts
{
    public abstract class Callout : ICallout
    {
        public IEnumerable<Player> PlayersOnCallout { get; set; } = new List<Player>();

        public abstract Task Setup();
        public abstract Task Start();
        public abstract Task End();
        
        public async Task<bool> TryAddPlayerToCallout(Player player)
        {
            if (PlayersOnCallout.Contains(player)) return false;
            var playerList = (List<Player>) PlayersOnCallout;
            playerList.Add(player);
            
            if (playerList.Count == 1)
            {
                Debug.WriteLine("Starting Callout");
                await Start();
            } 
            
            OnPlayerJoined?.Invoke(player);
            return true;
        }

        public async Task<bool> RemovePlayerFromCallout(Player player)
        {
            if (!PlayersOnCallout.Contains(player)) return false;
            var playerList = (List<Player>) PlayersOnCallout;
            playerList.Remove(player);
            
            if (!playerList.Any())
            {
                await End();
            }

            return true;
        }

        public void PlayerInteractWithPed(Player player, Ped ped)
        {
            OnPlayerInteractWithPed?.Invoke(player, ped);
        }
        
        public void PlayerInteractWithVehicle(Player player, Vehicle veh)
        {
            OnPlayerInteractWithVehicle?.Invoke(player, veh);
        }

        public event OnPlayerJoinedHandler OnPlayerJoined;
        public event OnFirstPlayerJoinedHandler OnFirstPlayerJoined;
        public event OnPlayerInteractWithPed OnPlayerInteractWithPed;
        public event OnPlayerInteractWithVehicle OnPlayerInteractWithVehicle;
        public event OnCalloutEndHandler OnCalloutEnd;


        protected virtual void OnCalloutEnded(bool retry)
        {
            OnCalloutEnd?.Invoke(retry);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var onCallPlayer in PlayersOnCallout)
            {
                await RemovePlayerFromCallout(onCallPlayer);
            }
        }
    }
}