using CitizenFX.Core;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Enums;

namespace PoliceMP.Server.Controllers
{
    public class DogController : Controller
    {
        private readonly IServerCommunicationsManager _comms;

        public DogController(IServerCommunicationsManager comms)
        {
            _comms = comms;
            
            _comms.On<Player, int, DogSound>(ServerEvents.SendDogSoundEventToServer, OnReceiveSoundEventFromClient);
            _comms.On<Player, int, DogFx>(ServerEvents.SendDogParticleFxEventToServer, OnReceiveDogFxEventFromClient);
        }

        private void OnReceiveSoundEventFromClient(Player player, int dogNetworkId, DogSound soundType)
        {
            _comms.ToClient(ClientEvents.SendDogSoundEventToClient, dogNetworkId, soundType);
        }

        private void OnReceiveDogFxEventFromClient(Player player, int dogNetworkId, DogFx dogFx)
        {
            _comms.ToClient(ClientEvents.SendDogParticleFxEventToClient, dogNetworkId, dogFx);
        }
        
    }
}