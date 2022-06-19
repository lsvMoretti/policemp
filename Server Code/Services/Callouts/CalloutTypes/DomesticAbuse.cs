using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Server.Extensions;
using PoliceMP.Server.Factories;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Enums;
using Stateless;
using Vector3 = PoliceMP.Core.Shared.Models.Vector3;

namespace PoliceMP.Server.Services.Callouts.CalloutTypes
{
    public class DomesticAbuse : Callout
    {
        private Ped _attackerPed;
        private Ped _victimPed;
        private List<Vector3> _calloutPos;

        #region Services

        private readonly IRandomEntityService _randomEntityService;
        private readonly IPositionsFactory _positionsFactory;
        private readonly ILogger<DomesticAbuse> _logger;
        private readonly ITickService _tick;
        private readonly IServerCommunicationsManager _comms;
        
        #endregion
        
        #region Trigger & States

        private readonly StateMachine<State, Trigger> _calloutState;

        enum State
        {
            Waiting,
            Starting,
            VerbalAbuse,
            AttackingPed,
            Interacted
        }

        enum Trigger
        {
            Setup,
            StartVerbalAbuse,
            AttackVictim,
            Interacted
        }

        #endregion

        public DomesticAbuse(IRandomEntityService randomEntityService, IPositionsFactory positionsFactory, ILogger<DomesticAbuse> logger,
            ITickService tick, IServerCommunicationsManager comms)
        {
            _randomEntityService = randomEntityService;
            _positionsFactory = positionsFactory;
            _logger = logger;
            _tick = tick;
            _comms = comms;
            
            _calloutState = new StateMachine<State, Trigger>(State.Waiting);
            _tick.On(OnTick);
            
            OnPlayerJoined += OnPlayerJoinedCallout;
            OnPlayerInteractWithPed += OnOnPlayerInteractWithPed;
        }

        private async Task OnOnPlayerInteractWithPed(Player player, Ped ped)
        {
            if (_calloutState.IsInState(State.Interacted)) return;
            if (ped == _attackerPed || ped == _victimPed)
            {
                await _calloutState.FireAsync(Trigger.Interacted);
            }
        }

        private Task OnPlayerJoinedCallout(Player player)
        {
            player.Character.Position = _calloutPos[0].ConvertToCitizen();
            return Task.CompletedTask;
        }

        public override async Task Setup()
        {
            _calloutState.Configure(State.Waiting)
                .Permit(Trigger.Setup, State.Starting);
            _calloutState.Configure(State.Starting)
                .OnEntryAsync(OnCalloutInit)
                .Permit(Trigger.Interacted, State.Interacted)
                .Permit(Trigger.StartVerbalAbuse, State.VerbalAbuse);
            _calloutState.Configure(State.VerbalAbuse)
                .OnEntryAsync(OnStartVerbalAbuse)
                .Permit(Trigger.Interacted, State.Interacted)
                .Permit(Trigger.AttackVictim, State.AttackingPed);

            _calloutState.Configure(State.AttackingPed)
                .OnEntryAsync(OnStartAttackingPed)
                .Permit(Trigger.Interacted, State.Interacted);

            _calloutState.Configure(State.Interacted)
                .OnEntryAsync(OnInteractedState);

            await _calloutState.FireAsync(Trigger.Setup);
        }

        public async Task OnInteractedState()
        {
            await StopVictimScreaming();
        }
        
        public async Task StopVictimScreaming()
        {
            _comms.ToClient(_victimPed.Owner, CalloutEvents.DomesticAbuseVictimScreamStop);
        }

        private async Task OnStartAttackingPed()
        {
            if (_calloutState.IsInState(State.Interacted)) return;
            API.TaskCombatPed(_attackerPed.Handle, _victimPed.Handle, 0, 16);
            await StopVictimScreaming();
            var rnd = new Random();

            var chanceOfFlee = rnd.Next(100);

            _logger.Debug($"Chance of Fleeing: {chanceOfFlee}");
            if (chanceOfFlee < 30)
            {
                API.TaskReactAndFleePed(_victimPed.Handle, _attackerPed.Handle);
            }
            else
            {
                _comms.ToClient(_victimPed.Owner, CalloutEvents.DomesticAbuseVictimCower, _victimPed.NetworkId);
            }
        }

        private async Task OnStartVerbalAbuse()
        {
            if (_calloutState.IsInState(State.Interacted)) return;
            var rnd = new Random();

            var chanceOfWander = false;
            
            _comms.ToClient(_victimPed.Owner, CalloutEvents.DomesticAbuseVictimSpeechStart, _victimPed.NetworkId, _attackerPed.NetworkId, chanceOfWander);
            _comms.ToClient(_attackerPed.Owner, CalloutEvents.DomesticAbuseAttackerSpeechStart, _attackerPed.NetworkId, _victimPed.NetworkId, chanceOfWander);
            

            var chanceOfFight = rnd.Next(100);

            _logger.Debug($"Chance of Attack: {chanceOfFight}");
            
            if (chanceOfFight < 15)
            {
                var delayTime = rnd.Next(1000, 3000);
                await BaseScript.Delay(delayTime);
                await _calloutState.FireAsync(Trigger.AttackVictim);
                return;
            }
        }

        private async Task OnCalloutInit()
        {
            _logger.Debug($"Init DA Callout");
            
            _calloutPos = await _positionsFactory.GetMultiplePositionsNearOneByType(LocationType.RandomPositionOnStreet, 2);

            if (_calloutPos == null)
            {
                _logger.Debug($"Unable to fetch Callout Positions");
                OnCalloutEnded(true);
                return;
            }
            
            _logger.Debug($"Found {_calloutPos.Count} Positions");
            
            _attackerPed = await _randomEntityService.GenerateRandomPed(_calloutPos[0].ConvertToCitizen(), 0f, RandomPedType.Male);

            var attackerSpawnCount = 0;
            var pedTrySpawnCount = 3;

            while (_attackerPed == null && attackerSpawnCount < pedTrySpawnCount)
            {
                _attackerPed = await _randomEntityService.GenerateRandomPed(_calloutPos[0].ConvertToCitizen(), 0f, RandomPedType.Male);
                attackerSpawnCount++;
                await BaseScript.Delay(1000);
            }

            if (_attackerPed == null)
            {
                _logger.Error("Unable to spawn AttackerPed");
                OnCalloutEnded(true);
                return;
            }
            
            _victimPed =
                await _randomEntityService.GenerateRandomPed(_calloutPos[1].ConvertToCitizen(), 0f,
                    RandomPedType.Random);

            var victimPedSpawnCount = 0;
            while (_victimPed == null && victimPedSpawnCount < pedTrySpawnCount)
            {
                _victimPed =
                    await _randomEntityService.GenerateRandomPed(_calloutPos[1].ConvertToCitizen(), 0f,
                        RandomPedType.Random);
                victimPedSpawnCount++;
                await BaseScript.Delay(1000);
            }
            

            if (_victimPed == null)
            {
                _logger.Error("Unable to spawn VictimPed");
                OnCalloutEnded(true);
                return;
            }

            await _calloutState.FireAsync(Trigger.StartVerbalAbuse);
        }

        public override async Task Start()
        {
            return;
        }

        public override async Task End()
        {
            await BaseScript.Delay(10000);
            API.DeleteEntity(_attackerPed.Handle);
            API.DeleteEntity(_victimPed.Handle);
            return;
        }

        private async Task OnTick()
        {
        }
    }
}