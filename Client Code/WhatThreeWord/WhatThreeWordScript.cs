using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Commands.Interfaces;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Shared.Constants;
using Vector3 = PoliceMP.Core.Shared.Models.Vector3;

namespace PoliceMP.Client.Scripts.WhatThreeWord
{
    public class WhatThreeWordScript : Script
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ICommandManager _command;

        public WhatThreeWordScript(IClientCommunicationsManager comms, ICommandManager command)
        {
            _comms = comms;
            _command = command;
            _comms.On<string>(ServerEvents.SendWhatThreeWordToClient, word =>
            {
                BaseScript.TriggerEvent("chat:addMessage", $"^3 ^* [W3W] Your W3W Location: {word}");
            });
            _comms.On<Vector3>(ServerEvents.SendWhatThreeWordPosToClient, (position) =>
            {
                API.SetNewWaypoint(position.X, position.Y);
            });
        }

        protected override async Task OnStartAsync()
        {
            _command.Register("gotow3w").HasGreedyArgs().WithHandler(whatWord =>
            {
                _comms.ToServer(ClientEvents.GoToWhatThreeWord, whatWord);
            });
        }
    }
}