using System;
using System.Linq;
using System.Threading.Tasks;
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
    public class DsuCommands : Controller
    {
        private readonly ILogger<DsuCommands> _logger;
        private readonly ICommandManager _commands;
        private readonly IServerCommunicationsManager _comms;
        private readonly INotificationService _notifications;

        //private readonly IUserService _userService;
        private readonly PlayerList _players;

        private readonly float ChatDistance = 50f;

        public DsuCommands(ILogger<DsuCommands> logger,
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
            //_userService = userService;
        }

        public override Task Started()
        {
            _comms.On(ServerEvents.SendDogMeCommand, (Player player, string dogName, string emoteText) =>
            {
                DogMeCommand(player, dogName, emoteText);
            });

            _comms.On(ServerEvents.SendDogDoCommand, (Player player, string dogName, string emoteText) =>
            {
                DogDoCommand(player, dogName, emoteText);
            });

            _comms.OnRequest<string, bool>(ServerEvents.FetchDsuAceAllowed, (player, s) => Task.FromResult(API.IsPlayerAceAllowed(player.Handle, "Police.dogTrained")));

            return Task.FromResult(0);
        }

        public void DogMeCommand(Player player, string dogName, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message)) return;

                string formattedString = $"^6* {dogName} {message}";

                Vector3 playerPosition = player.Character.Position;

                foreach (var target in _players.ToArray())
                {
                    if (target == null || target.Character == null) continue;

                    Vector3 targetPosition = target.Character.Position;

                    if (targetPosition == default(Vector3)) continue;

                    if (targetPosition.Distance(playerPosition) > ChatDistance) continue;

                    target.TriggerEvent("chat:addMessage", formattedString);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }

        public void DogDoCommand(Player player, string dogName, string message)
        {
            try
            {
                if (string.IsNullOrEmpty(message)) return;

                string formattedString = $"^6* {message} (( {dogName} ))";

                Vector3 playerPosition = player.Character.Position;

                foreach (var target in _players.ToArray())
                {
                    if (target == null || target.Character == null) continue;
                    Vector3 targetPosition = target.Character.Position;

                    if (targetPosition == default(Vector3)) continue;

                    if (targetPosition.Distance(playerPosition) > ChatDistance) continue;

                    target.TriggerEvent("chat:addMessage", formattedString);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return;
            }
        }
    }

    public static class Vector3Extension
    {
        public static float Distance(this Vector3 position, Vector3 targetPosition)
        {
            var diffX = position.X - targetPosition.X;
            var diffY = position.Y - targetPosition.Y;
            var diffZ = position.Z - targetPosition.Z;

            var sum = diffX * diffX + diffY * diffY + diffZ * diffZ;
            return (float)Math.Sqrt(sum);
        }
    }
}