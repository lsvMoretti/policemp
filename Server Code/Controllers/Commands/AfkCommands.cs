using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Server.Commands.Interfaces;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;

namespace PoliceMP.Server.Controllers.Commands
{
    public class AfkCommands : Controller
    {
        private readonly IServerCommunicationsManager _comms;
        private readonly ILogger<AfkCommands> _logger;
        private readonly INotificationService _notification;
        private readonly ICommandManager _command;
        private readonly PlayerList _playerList;

        public AfkCommands(IServerCommunicationsManager comms,
            ILogger<AfkCommands> logger,
            INotificationService notification,
            ICommandManager command,
            PlayerList playerList)
        {
            _comms = comms;
            _logger = logger;
            _notification = notification;
            _command = command;
            _playerList = playerList;

            _command.Register("checktime").WithHandler(CheckPlayerPlayTime);
            _command.Register("playtime").WithHandler(CheckOwnPlayTime);
        }

        private async void CheckOwnPlayTime(Player player)
        {
            int[] timeData = await _comms.Request<int[]>(player, ClientEvents.FetchAfkTimeData, "");

            _notification.Success(player, $"Times for: {player.Name}", $"Total Play Time: {timeData[0]:D2}:{timeData[1]:D2}\n" +
                                                                       $"Total AFK Time: {timeData[2]:D2}:{timeData[3]:D2}");
        }

        private async void CheckPlayerPlayTime(Player player, string playerIdString)
        {
            if (!API.IsPlayerAceAllowed(player.Handle, "Police.modAuth")) return;

            Player targetPlayer = null;

            bool tryIdParse = int.TryParse(playerIdString, out int playerId);

            if (!tryIdParse)
            {
                _notification.Error(player, "Error", "Unable to get player by ID. /checktime [ID]");
                return;
            }

            foreach (var target in _playerList)
            {
                if (target.Handle != playerIdString) continue;
                targetPlayer = target;
                break;
            }

            if (targetPlayer == null)
            {
                _notification.Error(player, "Error", "Unable to find target player");
                return;
            }

            int[] timeData = await _comms.Request<int[]>(targetPlayer, ClientEvents.FetchAfkTimeData, "");

            _notification.Success(player, $"Times for: {targetPlayer.Name}", $"Total Play Time: {timeData[0]:D2}:{timeData[1]:D2}\n" +
                                                   $"Total AFK Time: {timeData[2]:D2}:{timeData[3]:D2}");
        }
    }
}