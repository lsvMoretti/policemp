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
    public class HealthCheck : Script
    {
        #region Services

        private readonly IClientCommunicationsManager _comms;
        private readonly INotificationService _notification;
        private readonly ILogger<HealthCheck> _logger;
        private readonly ITickManager _ticks;
        private readonly IPlayerService _playerService;
        private readonly IPermissionService _permissionService;

        #endregion

        #region Variables
        
        private UserAces _userAces = null;
        
        private DateTime _lastHealthCheck = DateTime.Now;

        private bool _isMod = false;

        private bool _isSpawned = false;

        #endregion
        
        public HealthCheck(IClientCommunicationsManager comms, INotificationService notification,
            ILogger<HealthCheck> logger, ITickManager ticks, IPlayerService playerService, IPermissionService permissionService)
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
            
            _ticks.On(CheckPlayerHealth);
        }
        
        
        private async Task CheckPlayerHealth()
        {
            if(!_isSpawned) return;
            
            if (_isMod) return;

            if (DateTime.Compare(DateTime.Now, _lastHealthCheck.AddMinutes(1)) <= 0) return;

            Random rnd = new Random();

            Player player = Game.Player;

            int currentHealth = player.Character.Health;

            int waitTime = rnd.Next(10, 150);

            player.Character.Health -= 10;

            await BaseScript.Delay(waitTime);

            if (player.Character.Health > 100)
            {
                SendMessageToMods($"Possible Health Hack for {player.Name}.");
            }
            else if (player.Character.Health == currentHealth)
            {
                SendMessageToMods($"Possible Health Hack for {player.Name}.");
            }

            player.Character.Health = currentHealth;
            _lastHealthCheck = DateTime.Now;
        }
        
        private void SendMessageToMods(string message)
        {
            _comms.ToServer(ServerEvents.SendMessageToMods, message);
        }
    }
}