using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Server.Commands.Interfaces;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Enums;
using Debug = CitizenFX.Core.Debug;

namespace PoliceMP.Server.Services.Callouts
{
    public interface ICalloutManager
    {
        Task<int> SpawnCallout();
        Task<int> SpawnCallout(string calloutName);
        Task<bool> AddPlayerToCallout(Player player, int calloutId);
        Task<bool> RemovePlayerFromCallout(Player player, int calloutId);
    }

    public class CalloutManager : ICalloutManager
    {
        private readonly Random _random = new Random();
        private readonly IList<Type> _calloutTypes;
        private readonly IDictionary<Type, CalloutOptions> _calloutList;
        private readonly int _probabilityMaxValue;
        private readonly IDictionary<int, ICallout> _activeCallouts = new Dictionary<int, ICallout>();
        private int _lastCalloutId = 1;
        private readonly INotificationService _notificationService;
        private readonly ILogger<CalloutManager> _log;
        private readonly IServiceProvider _serviceProvider;
        private readonly PlayerList _playerList;
        private readonly IPermissionService _permission;

        public CalloutManager(IServiceProvider serviceProvider, ILogger<CalloutManager> log, IDictionary<Type, CalloutOptions> calloutTypes, INotificationService notificationService, IServerCommunicationsManager comms, PlayerList playerList, IPermissionService permission)
        {
            _log = log;
            _serviceProvider = serviceProvider;
            _calloutList = calloutTypes;
            _probabilityMaxValue = calloutTypes.Values.Sum(v => v.Probability);
            _notificationService = notificationService;
            _playerList = playerList;
            _permission = permission;
            comms.On<Player, int>(ServerEvents.OnPlayerInteractWithEntity, OnPlayerInteractWithEntity);
        }

        private void OnPlayerInteractWithEntity(Player player, int entityNetworkId)
        {
            var entity = Entity.FromNetworkId(entityNetworkId);
            
            if (!API.DoesEntityExist(entity.Handle)) return;
            
            foreach (var activeCallout in _activeCallouts)
            {
                var callout = activeCallout.Value;
                if (entity.Type == 1)
                {
                    var ped = (Ped) entity;
                    callout.PlayerInteractWithPed(player, ped);
                }

                if (entity.Type == 2)
                {
                    var veh = (Vehicle) entity;
                    
                }
            }
        }

        public Task<int> SpawnCallout()
            => SpawnCallout(null);

        public async Task<int> SpawnCallout(string calloutName)
        {
            // Get Callout from _calloutList
            var calloutType = calloutName == null ? 
                GetProbableCalloutType() : 
                _calloutList.Keys.FirstOrDefault(c => c.Name.Equals(calloutName, StringComparison.InvariantCultureIgnoreCase));

            if (calloutType is null)
                return -1;

            var calloutId = _lastCalloutId;
            _log.Debug($"Spawning callout type: {calloutType.Name}");

            var callout = (ICallout)_serviceProvider.GetService(calloutType);

            callout.OnCalloutEnd += async (retry) =>
            {
                Debug.WriteLine($"Callout: {calloutId} has ended");

                if (callout is not null) await callout.DisposeAsync();
                if (retry)
                {
                    Debug.WriteLine("Retrying to Spawn Callout");
                    callout = (ICallout)_serviceProvider.GetService(calloutType);
                }
            };
            
            
            var sw = new Stopwatch();
            sw.Start();
            await callout.Setup();
            sw.Stop();
            _log.Debug($"Callout took: {sw.Elapsed.Seconds} seconds to setup!");
            _activeCallouts.Add(calloutId, callout);
            
            var calloutOptions = _calloutList.FirstOrDefault(c => c.Key == calloutType).Value;
            foreach (var player in _playerList)
            {
                _notificationService.Info(player, $"New 999 - Call ID: {_lastCalloutId}", $"{calloutOptions.Description} - Response Grade {(int)calloutOptions.CalloutGrade}");
            }
            
            _lastCalloutId++;
            
            return calloutId;
        }

        public async Task<bool> AddPlayerToCallout(Player player, int calloutId)
        {
            return _activeCallouts.TryGetValue(calloutId, out ICallout callout) && await callout.TryAddPlayerToCallout(player);
        }

        public async Task<bool> RemovePlayerFromCallout(Player player, int calloutId)
        {
            if (!_activeCallouts.TryGetValue(calloutId, out ICallout callout)) return false;
            
            await callout.RemovePlayerFromCallout(player);
            return true;
        }

        private Type GetProbableCalloutType()
        {
            var value = _random.Next(_probabilityMaxValue);

            _log.Debug("Callout value: ");

            var currentValue = 0;
            foreach(var callout in _calloutList)
            {
                currentValue += callout.Value.Probability;

                var appropriatePlayers = callout.Value.UserBranches.Sum(branch => _permission.GetUserCountByBranch(branch).Result);

                if (appropriatePlayers < callout.Value.MinimumAppropriatePlayers)
                {
                    return null;
                }

                _log.Debug($"{value} < {currentValue} ?: {value < currentValue}");
                if (value <= currentValue)
                {
                    return callout.Key;
                }
            }

            return null;
        }
    }
}