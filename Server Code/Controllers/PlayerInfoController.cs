using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.EntityFrameworkCore.Internal;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Server.Commands.Interfaces;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Core.Shared.Models;
using PoliceMP.Server.Services.Interfaces;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Constants.States;
using PoliceMP.Shared.Models;
using Debug = System.Diagnostics.Debug;
using Vector3 = PoliceMP.Core.Shared.Models.Vector3;

namespace PoliceMP.Server.Controllers
{
    public class PlayerInfoController : Controller
    {
        private readonly IServerCommunicationsManager _comms;
        private readonly PlayerList _playerList;
        private readonly ICommandManager _command;
        private readonly ILogger<PlayerInfoController> _logger;
        private readonly IBucketService _buckets;
        private readonly IPermissionService _permissionService;

        private readonly Timer _timer = new Timer(10000)
        {
            AutoReset = true,
            Enabled = true
        };
        
        private List<PlayerInfo> _cachedPlayerInfo = new List<PlayerInfo>();
        
        public PlayerInfoController(IServerCommunicationsManager comms, PlayerList playerList, ICommandManager command, ILogger<PlayerInfoController> logger, IBucketService buckets, IPermissionService permissionService)
        {
            _comms = comms;
            _playerList = playerList;
            _command = command;
            _logger = logger;
            _buckets = buckets;
            _permissionService = permissionService;
            
            _timer.Elapsed += TimerElapsed;
        }
        
        /// <summary>
        /// Updates the cached player list every 10 seconds to stop spamming it everytime it is requested. When requested they are sent the cache. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            if (!_playerList.Any()) return;
            await UpdateCachedPlayers();
            _comms.ToClient(ServerEvents.SendAllPlayersToClient, _cachedPlayerInfo);
        }

        public async Task UpdateCachedPlayers()
        {
            try
            {
                var playerList = new List<PlayerInfo>();
                foreach (var targetPlayer in _playerList)
                {
                    
                    var userRole = await _permissionService.GetUserRole(targetPlayer);
                    if(targetPlayer.Character == null) continue;
                    lock (playerList)
                    {
                        var tryParse = int.TryParse(targetPlayer.Handle, out int targetHandle);

                        if (!tryParse) continue;
                        

                        var playerInfo = new PlayerInfo
                        {
                            Name = targetPlayer.Name,
                            Index = _playerList.IndexOf(targetPlayer),
                            RoutingBucket = _buckets.GetPlayerBucket(targetPlayer),
                            VehicleNetworkId = 0,
                            CallSign = (string)targetPlayer.State.Get("PMPCallsign"),
                            ActiveBranch = userRole.Branch,
                            ActiveDivision = userRole.Division
                        };

                        playerInfo.ServerHandle = targetHandle;

                        var ped = targetPlayer.Character;
                        if (ped != null)
                        {
                            playerInfo.NetworkId = ped.NetworkId;
                            if (ped.Position != default)
                            {
                                playerInfo.Position = new Vector3(ped.Position.X, ped.Position.Y, ped.Position.Z);
                            }

                            if (ped.Rotation != default)
                            {
                                playerInfo.Rotation = new Vector3(ped.Rotation.X, ped.Rotation.Y, ped.Rotation.Z);
                            }
                        }

                        playerList.Add(playerInfo);
                    }
                }

                lock (_cachedPlayerInfo)
                {
                    _cachedPlayerInfo = playerList;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return;
            }
        }
        
        public override Task Started()
        {
            _comms.OnRequest<int, PlayerInfo>(ServerEvents.RequestPlayerInfo, FetchPlayerInfoFromNetworkId);
            _comms.OnRequest(ServerEvents.FetchAllPlayersFromServer, FetchAllPlayersFromServer);
            _comms.On<string>(ServerEvents.OnReceiveCallSign, (player, callsign) =>
            {
                player.State.Set("PMPCallsign", callsign, true);
            });
            return Task.FromResult(0);
        }

        private async Task<List<PlayerInfo>> FetchAllPlayersFromServer(Player player)
        {
            return _cachedPlayerInfo;
        }
        
        private async Task<PlayerInfo> FetchPlayerInfoFromNetworkId(Player player, int networkId)
        {
            var targetPlayer = _playerList.FirstOrDefault(x => x.Character?.NetworkId == networkId);
            
            if (targetPlayer == null || targetPlayer.Character == null) return null;
            
            var ped = targetPlayer.Character;

            var userRole = await _permissionService.GetUserRole(player);
            
            var playerInfo = new PlayerInfo
            {
                Name = targetPlayer.Name,
                Index = _playerList.IndexOf(targetPlayer),
                RoutingBucket = _buckets.GetPlayerBucket(targetPlayer),
                NetworkId = ped.NetworkId,
                Position = new Vector3(ped.Position.X, ped.Position.Y, ped.Position.Z),
                Rotation = new Vector3(ped.Rotation.X, ped.Rotation.Y, ped.Rotation.Z),
                VehicleNetworkId = 0,
                CallSign = targetPlayer.State.Get("PMPCallsign"),
                ActiveBranch = userRole.Branch,
                ActiveDivision = userRole.Division
            };

            var tryParse = int.TryParse(targetPlayer.Handle, out int targetHandle);
            
            if (tryParse)
            {
                playerInfo.ServerHandle = targetHandle;
            }

            var pedVehicleId = API.GetVehiclePedIsIn(ped.Handle, false);

            if (pedVehicleId == 0) return playerInfo;
            
            var pedVehicle = (Vehicle)Entity.FromHandle(pedVehicleId);
            if (pedVehicle != null)
            {
                playerInfo.VehicleNetworkId = pedVehicle.NetworkId;
            }

            return playerInfo;
        }
        
    }
}