using System;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using PoliceMP.Core.Server.Commands.Interfaces;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;

namespace PoliceMP.Server.Controllers.Commands
{
    public class RoleplayCommands : Controller
    {
        private readonly ILogger<RoleplayCommands> _logger;
        private readonly ICommandManager _commands;
        private readonly IServerCommunicationsManager _comms;
        private readonly INotificationService _notifications;
        private readonly PlayerList _players;

        private readonly float ChatDistance = 10f;

        public RoleplayCommands(ILogger<RoleplayCommands> logger,
            ICommandManager commands,
            IServerCommunicationsManager comms,
            INotificationService notifications,
            PlayerList players)
        {
            _logger = logger;
            _commands = commands;
            _comms = comms;
            _notifications = notifications;
            _players = players;
        }

        public override Task Started()
        {
            _comms.On(ServerEvents.SendMeCommandToServer, (Player player, string message) =>
            {
                RoleplayMeCommand(player, message);
            });
            _comms.On(ServerEvents.SendDoCommandToServer, (Player player, string message) =>
            {
                RoleplayDoCommand(player, message);
            });

            return Task.FromResult(0);
        }

        private void RoleplayMeCommand(Player player, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message)) return;
                string trimmedMessage = message.Trim();

                Vector3 playerPosition = player.Character.Position;

                string formattedString = $"^6* {player.Name} {trimmedMessage}";

                foreach (var ped in _players.ToArray())
                {
                    if (ped == null || ped.Character == null) continue;

                    Vector3 pedPosition = ped.Character.Position;

                    if (pedPosition == default(Vector3)) continue;

                    if (pedPosition.Distance(playerPosition) > ChatDistance) continue;

                    ped.TriggerEvent("chat:addMessage", formattedString);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        private void RoleplayDoCommand(Player player, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message)) return;
                string trimmedMessage = message.Trim();

                Vector3 playerPosition = player.Character.Position;

                string formattedString = $"^6* {trimmedMessage} (( {player.Name} ))";

                foreach (var ped in _players.ToArray())
                {
                    if (ped == null || ped.Character == null) continue;

                    Vector3 pedPosition = ped.Character.Position;
                    if (pedPosition == default(Vector3)) continue;
                    if (pedPosition.Distance(playerPosition) > ChatDistance) continue;

                    ped.TriggerEvent("chat:addMessage", formattedString);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }
    }
}