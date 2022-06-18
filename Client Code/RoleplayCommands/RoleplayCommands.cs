using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Commands.Interfaces;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using System.Threading.Tasks;

namespace PoliceMP.Client.Scripts
{
    public class RoleplayCommands : Script
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<RoleplayCommands> _logger;
        private readonly ICommandManager _commands;
        private readonly INotificationService _notifications;

        public RoleplayCommands(ILogger<RoleplayCommands> logger, ICommandManager commands, IClientCommunicationsManager comms, INotificationService notifications)
        {
            _logger = logger;
            _commands = commands;
            _comms = comms;
            _notifications = notifications;
        }

        protected override Task OnStartAsync()
        {
            _commands.Register("me").HasGreedyArgs().WithHandler((message) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(message) || message == "" || message == string.Empty)
                    {
                        _notifications.Error("Command Error", "/me [Emote Text]");
                        return;
                    }
                    _comms.ToServer(ServerEvents.SendMeCommandToServer, message);
                }
                catch
                {
                    return;
                }
            });

            _commands.Register("do").HasGreedyArgs().WithHandler((message) =>
            {
                try
                {
                    if (string.IsNullOrEmpty(message) || message == "" || message == string.Empty)
                    {
                        _notifications.Error("Command Error", "/do [Emote Text]");
                        return;
                    }
                    _comms.ToServer(ServerEvents.SendDoCommandToServer, message);
                }
                catch
                {
                    return;
                }
            });

            return Task.FromResult(0);
        }
    }
}