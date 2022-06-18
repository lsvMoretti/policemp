using CitizenFX.Core;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using System.Linq;

namespace PoliceMP.Server.Controllers
{
    public class FlashBangController : Controller
    {
        private readonly IServerCommunicationsManager _comms;
        private readonly ILogger<FlashBangController> _logger;
        private readonly PlayerList _players;

        private int stunTime = 8;
        private int afterTime = 8;
        private float range = 8.0f;

        public FlashBangController(IServerCommunicationsManager comms, ILogger<FlashBangController> logger, PlayerList players)
        {
            _comms = comms;
            _logger = logger;
            _players = players;

            _comms.On<float, float, float, int>(ServerEvents.SendFlashBangEventToServer, OnReceiveFlashBangFromClient);
        }

        private void OnReceiveFlashBangFromClient(Player player, float posX, float posY, float posZ, int networkId)
        {
            foreach (var targetPlayer in _players.ToList())
            {
                _comms.ToClient(targetPlayer, ClientEvents.SendFlashBangEventToClient, posX, posY, posZ, stunTime,
                    afterTime, range, networkId);
            }
        }
    }
}