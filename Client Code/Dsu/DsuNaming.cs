using CitizenFX.Core.Native;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Commands.Interfaces;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using System.Threading.Tasks;

namespace PoliceMP.Client.Scripts.Dsu
{
    public class DsuNaming : Script
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<DsuNaming> _logger;
        private readonly ICommandManager _commands;
        private readonly INotificationService _notifications;

        public DsuNaming(ILogger<DsuNaming> logger, ICommandManager commands, IClientCommunicationsManager comms, INotificationService notifications)
        {
            _logger = logger;
            _commands = commands;
            _comms = comms;
            _notifications = notifications;
        }

        protected override async Task OnStartAsync()
        {
            bool aceAllowed = await _comms.Request<bool>(ServerEvents.FetchDsuAceAllowed, "");

            if (aceAllowed)
            {
                _commands.Register("namedog").HasGreedyArgs().WithHandler(dogName =>
                {
                    if (string.IsNullOrEmpty(dogName))
                    {
                        _notifications.Error("K9 System", "Please enter a correct name for your dog.");
                        return;
                    }

                    SetDogName(dogName);
                });

                _commands.Register("kme").HasGreedyArgs().WithHandler(emoteText =>
                {
                    if (string.IsNullOrEmpty(emoteText))
                    {
                        _notifications.Error("K9 System", "Please enter a longer message!");
                        return;
                    }

                    string dogName = FetchDogName();

                    if (string.IsNullOrEmpty(dogName) || dogName == string.Empty || dogName == "none")
                    {
                        _notifications.Error("Error", "You haven't set a dog's name.");
                        return;
                    }

                    _comms.ToServer(ServerEvents.SendDogMeCommand, dogName, emoteText);
                });

                _commands.Register("kdo").HasGreedyArgs().WithHandler(emoteText =>
                {
                    if (string.IsNullOrEmpty(emoteText))
                    {
                        _notifications.Error("K9 System", "Please enter a longer message!");
                        return;
                    }

                    string dogName = FetchDogName();

                    if (string.IsNullOrEmpty(dogName) || dogName == string.Empty || dogName == "none")
                    {
                        _notifications.Error("Error", "You haven't set a dog's name.");
                        return;
                    }

                    _comms.ToServer(ServerEvents.SendDogDoCommand, dogName, emoteText);
                });
            }
        }

        private void SetDogName(string dogName)
        {
            _logger.Debug($"Setting Dog Name to: {dogName}");
            API.SetResourceKvp($"{ResourceKvp.DogName}", dogName);
        }

        public static string FetchDogName()
        {
            if (string.IsNullOrEmpty(API.GetResourceKvpString(ResourceKvp.DogName)))
            {
                API.SetResourceKvp(ResourceKvp.DogName, "none");
            }

            return API.GetResourceKvpString(ResourceKvp.DogName);
        }
    }
}