using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.EntityFrameworkCore.Internal;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Core.Shared.Communications.Interfaces;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;

namespace PoliceMP.Server.Controllers
{
    public class CustomCharacterController : Controller
    {
        private readonly IServerCommunicationsManager _comms;
        private readonly ILogger<CustomCharacterController> _logger;
        private readonly IFiveEventManager _fiveEvent;
        private PlayerList _playerList;
        
        public CustomCharacterController(IServerCommunicationsManager comms, IFiveEventManager fiveEvent, ILogger<CustomCharacterController> logger, PlayerList playerList)
        {
            _logger = logger;
            _comms = comms;
            _fiveEvent = fiveEvent;
            _playerList = playerList;
            _comms.On<Player,int>(ServerEvents.StartCharacterCustomisation, OnStartCustomisation);
            _comms.On<Player,string>(ServerEvents.SetPlayerCharacterCustomisation, OnSetAppearance);
            
            fiveEvent.On<Player,string>("charcreator:charactercreated", OnCharacterCreated);
            fiveEvent.On<Player, string>("charcreator:appearance:applied", OnCharacterApplied);
        }
        
        public override Task Started()
        {
            return Task.FromResult(0);
        }

        private async Task OnCharacterApplied([FromSource] Player player, string json)
        {
            _comms.ToClient(player, ServerEvents.OnCharacterAppearanceApplied);
        }
        
        private async Task OnCharacterCreated([FromSource]Player player, string json)
        {
            _logger.Debug($"Custom Character for: {player.Name} is {json}");
            _comms.ToClient(player, ServerEvents.OnFinishCharacterCustomisation, json);
        }


        private async void OnStartCustomisation(Player player, int gender)
        {
            Exports["character-creator"].startCreation(player.Name, gender);
        }

        private void OnSetAppearance(Player player, string appearance)
        {
            Exports["character-creator"].applyAppearanace(player.Name, appearance);
        }
        
        
    }
}