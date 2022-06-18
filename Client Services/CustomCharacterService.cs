using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Communications;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Client.Extensions;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;
using PoliceMP.Core.Shared.Communications.Interfaces;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Constants.States;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Services
{
    public class CustomCharacterService : Script, ICustomCharacterService
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly IInputService _inputService;
        private readonly ILogger<CustomCharacterService> _logger;
        private readonly IPermissionService _permissionService;
        private Vector3 _playerPosition = Vector3.Zero;
        private PedOutfit _currentOutfit = null;
        private bool _inCustomisation = false;
        private UserAces _userAces;
        
        
        public CustomCharacterService(IClientCommunicationsManager comms, IInputService inputService, ILogger<CustomCharacterService> logger, ITickManager tickManager, IPermissionService permissionService)
        {
            _comms = comms;
            _inputService = inputService;
            _logger = logger;
            _permissionService = permissionService;
            _comms.On<string>(ServerEvents.OnFinishCharacterCustomisation, OnFinishCustomisation);
            _comms.On(ServerEvents.OnCharacterAppearanceApplied, OnCharacterAppearanceApplied);
            tickManager.On(CustomisationTick);
        }

        private async Task CustomisationTick()
        {
            if (_userAces == null)
            {
                _userAces = await _permissionService.GetUserAces();
                while (_userAces == null)
                {
                    _userAces = await _permissionService.GetUserAces();
                    await Delay(50);
                }
            }
            
            if (!_inCustomisation) return;
            for (int i = 0; i <= 256; i++)
            {
                if (API.PlayerId() == i) { continue; }
                API.NetworkConcealPlayer(i, true, true);
            }
        }

        private void OnCharacterAppearanceApplied()
        {
            Game.PlayerPed.SetPedOutfit(_currentOutfit);
            
            Game.PlayerPed.GiveDefaultEquipment(_permissionService.CurrentUserRole, _userAces);
        }

        private async void OnFinishCustomisation(string json)
        {
            _inCustomisation = false;
            
            for (int i = 0; i < 500; i++)
            {
                API.NetworkConcealPlayer(i, false, false);
            }
            API.SwitchOutPlayer(API.PlayerPedId(), 0, 1);
            var characterName = await _inputService.ShowKeyboardInput("Character Name", "", 25);
            if (characterName == null)
            {
                API.SwitchInPlayer(API.PlayerPedId());
            }
            var characterData = API.GetResourceKvpString(ResourceKvp.CustomCharacters);
            var customCharacters = new List<CustomCharacter>();
            if (!string.IsNullOrEmpty(characterData))
            {
                customCharacters = JsonConvert.DeserializeObject<List<CustomCharacter>>(characterData);
            }
            var newCharacter = new CustomCharacter
            {
                Name = characterName,
                Appearance = json
            };

            customCharacters.Add(newCharacter);

            var output = JsonConvert.SerializeObject(customCharacters);
            API.SetResourceKvp(ResourceKvp.CustomCharacters, output);

            await Delay(500);

            Game.PlayerPed.SetPedOutfit(_currentOutfit);
            Game.PlayerPed.Position = _playerPosition;
            while (API.IsEntityWaitingForWorldCollision(Game.PlayerPed.Handle) && API.GetPlayerSwitchState() != 5)
            {
                await Delay(10);
            }
            API.SwitchInPlayer(API.PlayerPedId());
            
            Game.PlayerPed.GiveDefaultEquipment(_permissionService.CurrentUserRole, _userAces);

        }

        public void SetCharacterAppearance(CustomCharacter customCharacter)
        {
            if (customCharacter == null) return;

            _comms.ToServer(ServerEvents.SetPlayerCharacterCustomisation, customCharacter.Appearance);

            _currentOutfit = !string.IsNullOrEmpty(customCharacter.PedOutfit) ? JsonConvert.DeserializeObject<PedOutfit>(customCharacter.PedOutfit) : Game.PlayerPed.FetchCurrentPedOutfit();

            customCharacter.PedOutfit = JsonConvert.SerializeObject(_currentOutfit);

            
            API.SetResourceKvp(ResourceKvp.LastUsedCharacter, JsonConvert.SerializeObject(customCharacter));
        }
        
        public void SetCharacterAppearance(string name, string appearanceJson)
        {
            var characterString = API.GetResourceKvpString(ResourceKvp.CustomCharacters);
            if (string.IsNullOrEmpty(characterString)) return;

            var customCharacters = JsonConvert.DeserializeObject<List<CustomCharacter>>(characterString);

            var customCharacter = customCharacters.FirstOrDefault(x => x.Name == name && x.Appearance == appearanceJson);

            if (customCharacter == null) return;

            _comms.ToServer(ServerEvents.SetPlayerCharacterCustomisation, customCharacter.Appearance);
            
            _currentOutfit = !string.IsNullOrEmpty(customCharacter.PedOutfit) ? JsonConvert.DeserializeObject<PedOutfit>(customCharacter.PedOutfit) : Game.PlayerPed.FetchCurrentPedOutfit();

            customCharacter.PedOutfit = JsonConvert.SerializeObject(_currentOutfit);
            
            API.SetResourceKvp(ResourceKvp.LastUsedCharacter, JsonConvert.SerializeObject(customCharacter));
        }

        public void SaveOutfitToCharacter(string name, string appearanceJson)
        {
            var characterString = API.GetResourceKvpString(ResourceKvp.CustomCharacters);
            if (string.IsNullOrEmpty(characterString)) return;

            var customCharacters = JsonConvert.DeserializeObject<List<CustomCharacter>>(characterString);

            var customCharacter = customCharacters.FirstOrDefault(x => x.Name == name && x.Appearance == appearanceJson);

            if (customCharacter == null) return;

            customCharacters.Remove(customCharacter);

            var newCharacter = customCharacter;

            newCharacter.PedOutfit = JsonConvert.SerializeObject(Game.PlayerPed.FetchCurrentPedOutfit());
            
            customCharacters.Add(newCharacter);
            
            var json = JsonConvert.SerializeObject(customCharacters);
            
            API.SetResourceKvp(ResourceKvp.CustomCharacters, json);
        }

        public List<CustomCharacter> FetchCustomCharacters()
        {
            var characterData = API.GetResourceKvpString(ResourceKvp.CustomCharacters);
            return !string.IsNullOrEmpty(characterData) ? JsonConvert.DeserializeObject<List<CustomCharacter>>(characterData) : new List<CustomCharacter>();
        }
        
        /// <summary>
        /// Show the Character Creator
        /// </summary>
        /// <param name="playerName">Players In game Name</param>
        /// <param name="gender">-1 Select, 0 Male, 1 Female</param>
        public async void ShowCharacterCreator(int gender)
        {
            _inCustomisation = true;
            _playerPosition = Game.PlayerPed.Position;
            _currentOutfit = Game.PlayerPed.FetchCurrentPedOutfit();
            await Delay(500);
            _logger.Debug(JsonConvert.SerializeObject(_currentOutfit));
            _comms.ToServer(ServerEvents.StartCharacterCustomisation, gender);
        }

        public void DeleteCustomCharacter(string name, string appearance)
        {
            var characterString = API.GetResourceKvpString(ResourceKvp.CustomCharacters);
            if (string.IsNullOrEmpty(characterString)) return;

            var customCharacters = JsonConvert.DeserializeObject<List<CustomCharacter>>(characterString);

            var customCharacter = customCharacters.FirstOrDefault(x => x.Name == name && x.Appearance == appearance);

            if (customCharacter == null) return;

            customCharacters.Remove(customCharacter);

            var json = JsonConvert.SerializeObject(customCharacters);
            
            API.SetResourceKvp(ResourceKvp.CustomCharacters, json);
        }
    }
}