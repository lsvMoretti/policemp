using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Debug = System.Diagnostics.Debug;

namespace PoliceMP.Server.Services.Callouts
{
    public delegate Task OnPlayerJoinedHandler(Player player);
    public delegate Task OnFirstPlayerJoinedHandler(Player player);
    public delegate Task OnPlayerInteractWithPed(Player player, Ped ped);
    public delegate Task OnPlayerInteractWithVehicle(Player player, Vehicle vehicle);
    public delegate Task OnCalloutEndHandler(bool retry);

    public interface ICallout : IAsyncDisposable
    {
        IEnumerable<Player> PlayersOnCallout { get; set; }
        Task Setup();
        Task Start();
        Task End();
        Task<bool> TryAddPlayerToCallout(Player player);
        Task<bool> RemovePlayerFromCallout(Player player);
        void PlayerInteractWithPed(Player player, Ped ped);
        void PlayerInteractWithVehicle(Player player, Vehicle vehicle);
        event OnPlayerJoinedHandler OnPlayerJoined;
        event OnFirstPlayerJoinedHandler OnFirstPlayerJoined;
        event OnPlayerInteractWithPed OnPlayerInteractWithPed;
        event OnCalloutEndHandler OnCalloutEnd;

    }
}