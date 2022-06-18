using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Server.Services;
using PoliceMP.Server.Services.Interfaces;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Constants.Decors;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;

namespace PoliceMP.Server.Controllers
{
    public class BackupController : Controller
    {
        #region Services

        private readonly ILogger<BackupController> _logger;
        private readonly IServerCommunicationsManager _comms;
        private readonly ISoundService _sound;
        private readonly ISonoranService _sonoran;
        private readonly INotificationService _notification;

        #endregion
        
        #region Variables

        private DateTime _lastMobilizeSound = DateTime.Now.AddSeconds(-20);
        private readonly List<Vector3> _fireStations = new List<Vector3>
        {
            new Vector3(205.0975f, -1650.512f, 29.8031f) // Strawberry
        };

        #endregion
        
        public BackupController(ILogger<BackupController> logger, IServerCommunicationsManager comms, ISoundService soundService, ISonoranService sonoran, INotificationService notification)
        {
            _logger = logger;
            _comms = comms;
            _sound = soundService;
            _sonoran = sonoran;
            _notification = notification;
        }

        public override Task Started()
        {
            _comms.On<BackupRequest>(ServerEvents.SendBackupRequestToServer, OnReceiveBackupRequest);
            return Task.CompletedTask;
        }
        
        private async void OnReceiveBackupRequest(Player player, BackupRequest backupRequest)
        {
            var backupType = backupRequest.Type;
            
            _logger.Debug($"Incoming Backup Request from {player.Name} - Type: {backupType.ToString()}");

            _comms.ToClient(ClientEvents.ReceiveBackupRequest, backupRequest, player.Character.NetworkId);
            
            if (backupRequest.Type == BackupType.Panic)
            {
                await _sonoran.SendPanicEvent(player);
            }

            if (backupType == BackupType.Lfb)
            {
                var timeNow = DateTime.Now;
                var delayTime = timeNow.AddSeconds(20);
                _logger.Debug($"Delay Time: {delayTime} - LastMobilize: {_lastMobilizeSound}");
                var timeCompare = DateTime.Compare(_lastMobilizeSound, delayTime);
                _logger.Debug($"Time Compare: {timeCompare}");
                if (timeCompare > 0)
                {
                    _logger.Debug($"Less than 20 seconds since last immob!");
                    return;
                }
                _lastMobilizeSound = timeNow;
                foreach (var station in _fireStations)
                {
                    _logger.Debug($"Sending sound to Station");
                    _sound.SoundToCoord(station.X, station.Y, station.Z, 30f, "mobilise.wav", 0.75f);
                }
                
                _comms.ToClient(ServerEvents.SendLFBMDTSoundToPlayers, "");
                return;
            }
        }
        
    }
}