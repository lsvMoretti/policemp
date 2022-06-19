using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Microsoft.Extensions.Options;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Main.Core.Server.Enums;
using PoliceMP.Main.Core.Shared;
using PoliceMP.Server.Controllers;
using PoliceMP.Server.Extensions;
using PoliceMP.Server.Factories;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;
using PoliceMP.Shared.Options;
using Stateless;
using Prop = CitizenFX.Core.Prop;

namespace PoliceMP.Server.Services.Callouts.CalloutTypes
{
    public class Stabbing : Callout
    {

        private Ped _attackerPed;
        private Ped _victimPed;
        private Prop _knifeProp;
        private List<Core.Shared.Models.Vector3> _spawnLocations;
        private int _retrySpawnPed = 3;

        private bool _attackerInteracted = false;
        private bool _victimInteracted = false;

        private Item _knifeItem;

        enum Trigger
        {
            Setup,
            WaitingAttack,
            AttackPed,
            AttackedPed,
            AttackerFlee,
            AttackerDropKnife,
            AttackerWander,
            AttackerPanic
        }

        enum State
        {
            Waiting,
            Starting,
            WaitingAttack,
            AttackingPed,
            PedAttacked,
            AttackerFleeing,
            AttackerBlendIn,
            AttackerPanicking
        }

        private readonly StateMachine<State, Trigger> _calloutState;
        private readonly IServerCommunicationsManager _comms;
        private readonly IRandomEntityService _randomEntityService;
        private readonly IPositionsFactory _positionsFactory;
        private readonly ITickService _tickService;
        private readonly IPedInfoService _pedInfoService;
        private readonly IOptions<ItemOptions> _itemOptions;
        
        
        public Stabbing(ITickService tickService, IServerCommunicationsManager comms, IRandomEntityService randomEntityService, IPositionsFactory positionsFactory, IPedInfoService pedInfoService, IOptions<ItemOptions> itemOptions)
        {
            _comms = comms;
            _randomEntityService = randomEntityService;
            _positionsFactory = positionsFactory;
            _tickService = tickService;
            _pedInfoService = pedInfoService;
            _itemOptions = itemOptions;
            _tickService.On(Tick);
            
            _calloutState = new StateMachine<State, Trigger>(State.Waiting);
        }

        public override async Task Setup()
        {
            Debug.WriteLine("Setting Up");
            _calloutState.Configure(State.Waiting)
                .Permit(Trigger.Setup, State.Starting);
            _calloutState.Configure(State.Starting)
                .OnEntry(OnCallStarting)
                .Permit(Trigger.WaitingAttack, State.WaitingAttack);
            _calloutState.Configure(State.WaitingAttack)
                .OnEntryAsync(HandleStabbing)
                .Permit(Trigger.AttackPed, State.AttackingPed);
            _calloutState.Configure(State.AttackingPed)
                .OnEntry(OnAttackPed)
                .Permit(Trigger.AttackedPed, State.PedAttacked);
            _calloutState.Configure(State.PedAttacked)
                .InternalTransitionAsync(Trigger.AttackerDropKnife, AttackerDropKnife)
                .Permit(Trigger.AttackerFlee, State.AttackerFleeing)
                .Permit(Trigger.AttackerWander, State.AttackerBlendIn)
                .Permit(Trigger.AttackerPanic, State.AttackerPanicking);
            _calloutState.Configure(State.AttackerFleeing)
                .OnEntryAsync(OnAttackerFlee)
                .Permit(Trigger.AttackerPanic, State.AttackerPanicking)
                .Permit(Trigger.AttackerWander, State.AttackerBlendIn);
            _calloutState.Configure(State.AttackerBlendIn)
                .OnEntry(OnAttackerBlendIn)
                .Permit(Trigger.AttackerPanic, State.AttackerPanicking);
            _calloutState.Configure(State.AttackerPanicking)
                .OnEntry(OnAttackerPanic);
            
            Debug.WriteLine("State Configured");
            Debug.WriteLine("Requesting Positions");

            _spawnLocations = new List<Core.Shared.Models.Vector3>();

            var suitableLocations = new List<LocationType>
            {
                LocationType.Park,
                LocationType.TrainStationPlatform,
                LocationType.CityAttraction
            };

            var randomLocationType = suitableLocations[PoliceMpRandom.Next(suitableLocations.Count)];
            
            var firstSpawn = await _positionsFactory.GetPositionByType(randomLocationType);
            _spawnLocations.Add(firstSpawn);
            var secondSpawn = firstSpawn.Around(5f);
            _spawnLocations.Add(secondSpawn);
            
            Debug.WriteLine(_spawnLocations.Count().ToString());
            
            OnPlayerJoined += PlayerJoined;
            OnPlayerInteractWithPed += PlayerInteractedWithPed;
        }

        private async Task PlayerInteractedWithPed(Player player, Ped ped)
        {
            if (ped == _attackerPed && !_attackerInteracted)
            {
                _attackerInteracted = true;
            }

            if (ped == _victimPed && !_victimInteracted)
            {
                _victimInteracted = true;
            }

            return;
        }

        private async void OnCallStarting()
        {
            var enemySpawnPos = _spawnLocations[0];
            
            _attackerPed = await _randomEntityService.GenerateRandomPed(
                new Vector3(enemySpawnPos.X, enemySpawnPos.Y, enemySpawnPos.Z), 180f, RandomPedType.Random);

            int attackerTryCount = 0;
            
            while (_attackerPed == null && attackerTryCount < _retrySpawnPed)
            {
                _attackerPed = await _randomEntityService.GenerateRandomPed(
                    new Vector3(enemySpawnPos.X, enemySpawnPos.Y, enemySpawnPos.Z), 180f, RandomPedType.Random);
                attackerTryCount++;
                await BaseScript.Delay(1000);
            }

            if (_attackerPed == null)
            {
                OnCalloutEnded(true);
                return;
            }
            
            Debug.WriteLine($"Attacker Ped handle: {_attackerPed.Handle}");
            
            API.GiveWeaponToPed(_attackerPed.Handle, (uint)WeaponHash.Knife, 1, false, true);
            API.SetCurrentPedWeapon(_attackerPed.Handle, (uint)WeaponHash.Knife, true);

            var friendlySpawnPos = _spawnLocations[1];
            
            _victimPed = await _randomEntityService.GenerateRandomPed(
                new Vector3(friendlySpawnPos.X, friendlySpawnPos.Y, friendlySpawnPos.Z), 180f, RandomPedType.Random);

            await BaseScript.Delay(100);

            _comms.ToClient(_victimPed.Owner, CalloutEvents.OnStabbingVictimSpawn, _victimPed.NetworkId);
            _comms.ToClient(_attackerPed.Owner, CalloutEvents.OnStabbingAttackerSpawn, _attackerPed.NetworkId, _victimPed.NetworkId);

            var knifeItem = _itemOptions.Value.Items.FirstOrDefault(i => i.Id == 48);
            if (knifeItem != null)
            {
                _knifeItem = knifeItem;
                var attackerInfo = _pedInfoService.GetByNetworkId(_attackerPed.NetworkId);
                if (attackerInfo != null)
                {
                    _pedInfoService.AddItem(attackerInfo, knifeItem);
                }
            }

            await _calloutState.FireAsync(Trigger.WaitingAttack);
        }

        private bool HasAttackerBeenInteracted()
        {
            return _attackerInteracted;
        }

        private async Task HandleStabbing()
        {
            while (!API.DoesEntityExist(_attackerPed.Handle))
            {
                await BaseScript.Delay(10);
            }
            
            var random = new Random();
            var interval = random.Next(15000, 60000);
            
            await BaseScript.Delay(interval);

            if (HasAttackerBeenInteracted()) return;
            
            await _calloutState.FireAsync(Trigger.AttackPed);
        }

        private async void OnAttackPed()
        {
            API.TaskCombatPed(_attackerPed.Handle, _victimPed.Handle, 0, 16);
            
            while (API.GetEntityHealth(_victimPed.Handle) > 0)
            {
                await BaseScript.Delay(1);
            }
            
            await _calloutState.FireAsync(Trigger.AttackedPed);

            _comms.ToClient(_victimPed.Owner, CalloutEvents.OnStabbingVictimAttacked, _victimPed.NetworkId, _attackerPed.NetworkId);

            var random = new Random();
            var dropKnifeChance = random.Next(11);
            if (dropKnifeChance <= 4)
            {
                if (HasAttackerBeenInteracted()) return;
                await _calloutState.FireAsync(Trigger.AttackerDropKnife);
            }
            
            var fleeChance = random.Next(11);
            if (fleeChance <= 3)
            {
                if (HasAttackerBeenInteracted()) return;
                await _calloutState.FireAsync(Trigger.AttackerFlee);
                return;
            }

            var panicChance = random.Next(11);
            if (panicChance <= 2)
            {
                if (HasAttackerBeenInteracted()) return;
                await _calloutState.FireAsync(Trigger.AttackerPanic);
                return;
            }
            
            if (HasAttackerBeenInteracted()) return;
            await _calloutState.FireAsync(Trigger.AttackerWander);
        }

        private async Task AttackerDropKnife()
        {
            var random = new Random();
            var dropTime = random.Next(0, 10000);
            await BaseScript.Delay(dropTime);
            
            _comms.ToClient(_attackerPed.Owner, CalloutEvents.OnStabbingAttackerDropKnife, _attackerPed.NetworkId);
            
            var knifePropHandle = API.CreateObject(API.GetHashKey("prop_w_me_knife_01"), _attackerPed.Position.X, _attackerPed.Position.Y,
                _attackerPed.Position.Z, true, true, false);

            while (!API.DoesEntityExist(knifePropHandle))
            {
                await BaseScript.Delay(10);
            }

            _knifeProp = (Prop) Entity.FromHandle(knifePropHandle);
            var knifeOwner = _knifeProp.Owner;

            while (knifeOwner == null)
            {
                knifeOwner = _knifeProp.Owner;
                await BaseScript.Delay(10);
            }
            
            _knifeProp.SetPropOnGroundProperly();

            var attackerPedInfo = _pedInfoService.GetByNetworkId(_attackerPed.NetworkId);
            if (attackerPedInfo != null)
            {
                var containsKnife = attackerPedInfo.Items.Contains(_knifeItem);
                if (containsKnife)
                { 
                    _pedInfoService.RemoveItem(attackerPedInfo, _knifeItem);
                }
            }

            _comms.ToClient(_knifeProp.Owner, CalloutEvents.OnStabbingAttackerKnifeSpawned, 
                API.NetworkGetNetworkIdFromEntity(knifePropHandle));
        }
        
        private async Task OnAttackerFlee()
        {
            var random = new Random();
            var blendInTime = random.Next(15000, 60000);
            var panicChance = random.Next(11);
            API.TaskReactAndFleePed(_attackerPed.Handle, _victimPed.Handle);
            if (panicChance <= 3)
            {
                if (HasAttackerBeenInteracted()) return;
                await _calloutState.FireAsync(Trigger.AttackerPanic);
                return;
            }
            await BaseScript.Delay(blendInTime);
            if (HasAttackerBeenInteracted()) return;
            await _calloutState.FireAsync(Trigger.AttackerWander);
        }

        private void OnAttackerBlendIn()
        {
            if (_attackerPed != null && _attackerPed.Owner != null)
            {
                _comms.ToClient(_attackerPed.Owner, CalloutEvents.OnStabbingAttackerBlendIn, _attackerPed.NetworkId);
            }
        }
        
        private void OnAttackerPanic()
        {
            if (_attackerPed != null && _attackerPed.Owner != null)
            {
                _comms.ToClient(_attackerPed.Owner, CalloutEvents.OnStabbingAttackerPanic, _attackerPed.NetworkId);
            }
        }

        private async Task PlayerJoined(Player player)
        {
            Debug.WriteLine($"Sending {_spawnLocations[0].X} to {player.Name}");
            _comms.ToClient(player, CalloutEvents.SetUserWaypointPosition, _spawnLocations[0]);
        }
        
        private async Task Tick()
        {
            if (_attackerPed == null || !API.DoesEntityExist(_attackerPed.Handle)) return;

            if (_attackerInteracted) return;

            var currentTask = API.GetPedScriptTaskCommand(_attackerPed.Handle);
            
            Debug.WriteLine($"Current Task: {Enum.GetName(typeof(ScriptTaskHash), currentTask)}");

            Debug.WriteLine($"Current State: {_calloutState.State.ToString()}");
            
            if (_calloutState.IsInState(State.WaitingAttack))
            {
                if ((uint) currentTask !=
                    (uint) ScriptTaskHash.SCRIPT_TASK_FOLLOW_TO_OFFSET_OF_ENTITY)
                {
                    if (API.GetEntityHealth(_attackerPed.Handle) <= 0) return;
                    _comms.ToClient(_attackerPed.Owner, CalloutEvents.OnStabbingAttackerReFollow, _victimPed.NetworkId,
                        _attackerPed.NetworkId);
                }
            }

            if (_calloutState.IsInState(State.AttackerFleeing))
            {
                if ((uint) currentTask != (uint) ScriptTaskHash.SCRIPT_TASK_REACT_AND_FLEE_PED)
                {
                    if (API.GetEntityHealth(_attackerPed.Handle) <= 0) return;
                    API.TaskReactAndFleePed(_attackerPed.Handle, _victimPed.Handle);
                }
            }

            if (_calloutState.IsInState(State.AttackerBlendIn))
            {
                if ((uint) currentTask != (uint) ScriptTaskHash.SCRIPT_TASK_WANDER_STANDARD)
                {
                    if (API.GetEntityHealth(_attackerPed.Handle) <= 0) return;
                    OnAttackerBlendIn();
                }
            }
            
            if (_calloutState.IsInState(State.AttackerPanicking))
            {
                
                if ((uint) currentTask != (uint) ScriptTaskHash.SCRIPT_TASK_COWER)
                {
                    if (API.GetEntityHealth(_attackerPed.Handle) <= 0) return;
                    OnAttackerPanic();
                }
            }

            await BaseScript.Delay(1000);
        }
        
        public override async Task Start()
        {
            await _calloutState.FireAsync(Trigger.Setup);
        }

        public override async Task End()
        {
            if(_attackerPed != null && API.DoesEntityExist(_attackerPed.Handle))
               _attackerPed.MarkEntityAsNoLongerRequired();
            
            if(_victimPed != null && API.DoesEntityExist(_victimPed.Handle))
                _victimPed.MarkEntityAsNoLongerRequired();
            
            if(_knifeProp != null && API.DoesEntityExist(_knifeProp.Handle))
                _knifeProp.MarkEntityAsNoLongerRequired();
            
            OnCalloutEnded(false);
        }
    }
}