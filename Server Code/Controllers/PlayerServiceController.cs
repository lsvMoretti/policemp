using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;

namespace PoliceMP.Server.Controllers
{
    public class PlayerServiceController : Controller
    {
        private readonly IServerCommunicationsManager _comms;
        private readonly ILogger<PlayerServiceController> _logger;
        private readonly PlayerList _playerList;

        public PlayerServiceController(IServerCommunicationsManager comms, ILogger<PlayerServiceController> logger,
            PlayerList playerList)
        {
            _comms = comms;
            _logger = logger;
            _playerList = playerList;
        }

        public override Task Started()
        {
            _comms.OnRequest(ServerEvents.FetchAllNetworkIdsFromServer, SendPlayersToClient);
            _comms.OnRequest<int, string>(ServerEvents.FetchPlayerNameFromNetworkId, FetchNameFromNetworkId);
            return Task.FromResult(0);
        }


        private async Task<List<int>> SendPlayersToClient(Player player)
        {
            List<int> networkIds = new List<int>();

            foreach (var targetPlayer in _playerList)
            {
                if (targetPlayer == null) continue;

                if (targetPlayer.Character == null) continue;
                
                networkIds.Add(targetPlayer.Character.NetworkId);
            }

            _logger.Debug($"Found {networkIds.Count()} Players");
            
            return networkIds;
        }

        private async Task<string> FetchNameFromNetworkId(Player player, int networkId)
        {
            var targetPlayer = _playerList.FirstOrDefault(x => x.Character?.NetworkId == networkId);

            return targetPlayer == null ? string.Empty : targetPlayer.Name;
        }
    }
}