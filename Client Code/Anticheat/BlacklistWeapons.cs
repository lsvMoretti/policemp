using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Client.Services;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Scripts.Anticheat
{
    public class BlacklistWeapons : Script
    {
        #region Services

        private readonly IClientCommunicationsManager _comms;
        private readonly INotificationService _notification;
        private readonly ILogger<BlacklistWeapons> _logger;
        private readonly ITickManager _ticks;
        private readonly IPlayerService _playerService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Variables

        private List<string> BlackListedWeapons = new List<string>
        {
            "WEAPON_RAILGUN",
            "WEAPON_GARBAGEBAG",
            "WEAPON_GRENADELAUNCHER",
            "WEAPON_RPG",
            "WEAPON_GRENADE",
            "WEAPON_HOMINGLAUNCHER",
            "WEAPON_COMPACTLAUNCHER",
            "WEAPON_STICKYBOMB",
            "WEAPON_PIPEBOMB",
            "WEAPON_MINIGUN",
            "WEAPON_RAYPISTOL",
            "WEAPON_RAYCARBINE",
            "WEAPON_RAYMINIGUN",
        };
        
        private UserAces _userAces = null;
        
        private DateTime _lastWeaponCheck = DateTime.Now;

        private bool _isMod = false;

        #endregion
        
        public BlacklistWeapons(IClientCommunicationsManager comms, INotificationService notification,
            ILogger<BlacklistWeapons> logger, ITickManager ticks, IPlayerService playerService, IPermissionService permissionService)
        {
            _comms = comms;
            _notification = notification;
            _logger = logger;
            _ticks = ticks;
            _playerService = playerService;
            _permissionService = permissionService;
        }
        
        protected override async Task OnStartAsync()
        {
            _userAces = await _permissionService.GetUserAces();
            _isMod = _userAces.IsModerator || _userAces.IsAdmin;
            _ticks.On(CheckWeaponHacks);
        }
        
        private async Task CheckWeaponHacks()
        {
            if (_isMod) return;

            if (DateTime.Compare(DateTime.Now, _lastWeaponCheck.AddSeconds(30)) <= 0) return;

            foreach (var blackListedWeapon in BlackListedWeapons)
            {
                int weaponHash = API.GetHashKey(blackListedWeapon);

                if (!API.HasPedGotWeapon(Game.PlayerPed.Handle, (uint) weaponHash, false)) continue;
                
                SendMessageToMods($"Banned Weapon Detected for {Game.Player.Name} ({blackListedWeapon}). Weapon has been removed.");
                API.RemoveWeaponFromPed(Game.PlayerPed.Handle, (uint)weaponHash);
            }
            
            _lastWeaponCheck = DateTime.Now;
        }
        
        private void SendMessageToMods(string message)
        {
            _comms.ToServer(ServerEvents.SendMessageToMods, message);
        }
    }
}