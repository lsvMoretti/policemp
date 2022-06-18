using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Services
{
    public class PlayerService : Script, IPlayerService
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<PlayerService> _logger;
        private readonly ITickManager _tickManager;
        private List<PlayerInfo> _playerInfos = new List<PlayerInfo>();

        public PlayerService(IClientCommunicationsManager comms, ILogger<PlayerService> logger, ITickManager tickManager)
        {
            _comms = comms;
            _logger = logger;
            _tickManager = tickManager;
            
            _comms.On<List<PlayerInfo>>(ServerEvents.SendAllPlayersToClient, list =>
            {
                _playerInfos = list;
            });
            
            tickManager.On(FetchAllPlayerInfoTick);
        }

        private async Task FetchAllPlayerInfoTick()
        {
            _playerInfos = await _comms.Request<List<PlayerInfo>>(ServerEvents.FetchAllPlayersFromServer);
            await Delay(10000);
        }

        /// <summary>
        /// Returns a list of Character Network IDs from the server
        /// </summary>
        /// <returns></returns>
        public async Task<List<int>> FetchAllPlayerNetworkIds()
        {
            return await _comms.Request<List<int>>(ServerEvents.FetchAllNetworkIdsFromServer);
        }

        /// <summary>
        /// Returns a players name from the Network ID given
        /// </summary>
        /// <param name="networkId"></param>
        /// <returns></returns>
        public async Task<string> FetchPlayerNameFromNetworkId(int networkId)
        {
            return await _comms.Request<string>(ServerEvents.FetchPlayerNameFromNetworkId, networkId);
        }

        public async Task<List<PlayerInfo>> FetchAllRecentPlayerInfo()
        {
            return _playerInfos;
        } 

        public async Task<PlayerInfo> FetchPlayerInfoFromNetworkId(int networkId)
        {
            return await _comms.Request<PlayerInfo>(ServerEvents.RequestPlayerInfo, networkId);
        }
        
    }
}