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
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Scripts.Anticheat
{
    public class SkinCheck : Script
    {
        #region Services

        private readonly IClientCommunicationsManager _comms;
        private readonly INotificationService _notification;
        private readonly ILogger<SkinCheck> _logger;
        private readonly ITickManager _ticks;
        private readonly IPlayerService _playerService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Variables
        
        private UserAces _userAces = null;
        
        private DateTime _lastSkinCheck = DateTime.Now;

        private bool _isMod = false;

        private bool _isSpawned = false;

        #endregion
        
        public SkinCheck(IClientCommunicationsManager comms, INotificationService notification,
            ILogger<SkinCheck> logger, ITickManager ticks, IPlayerService playerService, IPermissionService permissionService)
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
            _comms.On<bool>(ClientEvents.PlayerSpawned, (isFirstSpawn) =>
            {
                _isSpawned = true;
            });
            
            _userAces = await _permissionService.GetUserAces();
            _isMod = _userAces.IsModerator || _userAces.IsAdmin;
            
            _ticks.On(CheckPlayerSkin);
        }
        
        private async Task CheckPlayerSkin()
        {
            if (!_isSpawned) return;

            if (Game.PlayerPed.IsDead) return;

            if (_permissionService.CurrentUserRole.Branch == UserBranch.Civ) return;

            if (DateTime.Compare(DateTime.Now, _lastSkinCheck.AddMinutes(1)) <= 0) return;

            Player player = Game.Player;

            if (player == null || player.Character == null) return;

            if (player.Character.Model.Hash == API.GetHashKey("mp_m_freemode_01") ||
                player.Character.Model.Hash == API.GetHashKey("mp_f_freemode_01")) return;

            SendMessageToMods($"Non-MP Skin Detected for {player.Name}.");

            _lastSkinCheck = DateTime.Now;
        }
        
        private void SendMessageToMods(string message)
        {
            _comms.ToServer(ServerEvents.SendMessageToMods, message);
        }
    }
}