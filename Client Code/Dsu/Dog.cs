using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Printing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using PoliceMP.Client.Scripts.Afk;
using PoliceMP.Client.Scripts.Boot;
using PoliceMP.Client.Scripts.RoadManagement;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Commands.Interfaces;
using PoliceMP.Core.Client.Communications;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Client.Extensions;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;
using PoliceMP.Core.Shared.Communications.Interfaces;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Scripts.Dsu
{
    public interface IDog
    {
        Task Deploy();
        Task ReturnDog();
        Task PlaceInVehicle(Vehicle vehicle);
        Task TakeFromVehicle();
        Task PlayDogSound(DogSound dogSound);
        Task PlayDogAnim(DogAnim dogAnim);
        Ped FetchDogPed();
        void StopSitting();
        bool HasTakenFromKennel();

        Task SearchVehicle(Vehicle vehicle, VehicleDoorIndex vehicleDoorIndex);
    }

    public class Dog : Script, IDog
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<Dog> _logger;
        private readonly INotificationService _notifications;
        private readonly ITickManager _ticks;
        private readonly ISpeechService _speech;
        private readonly IPermissionService _permissionService;
        private UserAces _userAces;
        public static Vehicle inVehicle;
        private bool _decampMode = false;
        private Vehicle _lastVehicle = null;

        private Menu _dogMenu = new Menu("Dog Interactions", "LShift + X for send the dog.");
        private Menu _dogKennelMenu = new Menu("Dog Kennel", "Select the dog you wish to take out on Patrol!");

        private const string _dogModel = "a_c_shepherd"; //a_c_shepherd
        public static bool DogSpawned = false;

        private Ped _dog;
        private Blip _blip;

        private bool _shouldSit = false;

        private bool _chasingSuspect = false;
        private Ped _suspectEntity;

        private DateTime _lastDeployTime = DateTime.Now;


        private Vector3 _dogKennelPosition = new(371.71f, -1612.19f, 29.29f);
        private float _dogSpawnRotation = 321.59f;
        private bool _hasTakenDogFromKennel = false;
        private int _selectedDogTexture = -1;
        private DateTime _lastMenuTime = DateTime.Now;

        private readonly Dictionary<int, string> _shepherdDogTypes = new Dictionary<int, string>
        {
            {0, "Black & Tan German Shepherd"},
            {1, "Red & Tan German Shepherd"},
            {2, "Tan Sable German Shepherd"},
            {3, "Black German Shepherd"}
        };

        public Dog(IClientCommunicationsManager comms,
            ILogger<Dog> logger,
            INotificationService notifications,
            ITickManager ticks,
            ISpeechService speech,
            IPermissionService permissionService,
            ICommandManager commandManager,
            IFiveEventManager fiveEventManager)
        {
            _comms = comms;
            _logger = logger;
            _notifications = notifications;
            _ticks = ticks;
            _speech = speech;
            _permissionService = permissionService;
            
            commandManager.Register("dogmenu").WithHandler(() =>
            {
                if (_dogMenu.Visible)
                {
                    _dogMenu.CloseMenu();
                    return;
                }

                if (MenuController.IsAnyMenuOpen()) return;
                if (_dog == null) return;
                if (!DogSpawned) return;
                LoadDogMenuOptions();
            });
            
            API.RegisterKeyMapping("dogmenu", "Dog Menu", "keyboard", "F4");

            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuToggleKey = (Control) (-1);
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.AddMenu(_dogMenu);
            MenuController.AddMenu(_dogKennelMenu);

            //LoadDogMenuOptions();
            LoadDogKennelOptions();

            BootSystem.DeployDog += DeployDog;
            BootSystem.ReturnDog += async (sender, args) => { await ReturnDog(); };

            _ticks.On(DogTick);
            _ticks.On(DogKennelTick);
            _ticks.On(HandleDecamp);

            _comms.On<int, DogSound>(ClientEvents.SendDogSoundEventToClient, OnReceiveSoundEventFromServer);
            _comms.On<int, DogFx>(ClientEvents.SendDogParticleFxEventToClient, OnReceiveDogFxEventFromServer);
        }
        protected override async Task OnStartAsync()
        {
            _userAces = await _permissionService.GetUserAces();
        }


        #region Dog Kennel Handling

        public bool HasTakenFromKennel()
        {
            return _hasTakenDogFromKennel;
        }

        private async Task DogKennelTick()
        {
            while (_userAces == null)
            {
                await Delay(0);
            }

            if (MenuController.IsAnyMenuOpen()) return;
            
            
            if (!_userAces.IsAdmin || !_userAces.IsDeveloper)
            {
                if (_permissionService.CurrentUserRole.Division != UserDivision.Dsu) return;
            }


            var playerPosition = Game.PlayerPed.Position;

            var distance = World.GetDistance(playerPosition, _dogKennelPosition);

            if (distance > 10) return;

            float scale = 0.1F * API.GetGameplayCamFov();

            API.DrawMarker(1, _dogKennelPosition.X, _dogKennelPosition.Y, _dogKennelPosition.Z - 1, 0, 0, 0, 0, 0, 0,
                1F, 1F, 2F, 232, 232, 0, 50,
                false, true, 2, false, null, null, false);
            API.SetTextScale(0.1F * scale, 0.1F * scale);
            API.SetTextFont(4);
            API.SetTextProportional(true);
            API.SetTextColour(250, 250, 250, 255);
            API.SetTextDropshadow(1, 1, 1, 1, 255);
            API.SetTextEdge(2, 0, 0, 0, 255);
            API.SetTextDropShadow();
            API.SetTextOutline();
            API.SetTextEntry("STRING");
            API.SetTextCentre(true);
            API.AddTextComponentString($"Police Kennel");
            API.SetDrawOrigin(_dogKennelPosition.X, _dogKennelPosition.Y, _dogKennelPosition.Z + 1F, 0);
            API.DrawText(0, 0);
            API.ClearDrawOrigin();

            if (!(distance < 1.0f)) return;
            if (DateTime.Compare(DateTime.Now, _lastMenuTime.AddSeconds(5)) <= 0) return;

            _lastMenuTime = DateTime.Now;

            if (_hasTakenDogFromKennel)
            {
                if (_dog.Exists())
                {
                    await ReturnDog();
                }

                _hasTakenDogFromKennel = false;
                return;
            }

            _dogKennelMenu.OpenMenu();
        }

        private void LoadDogKennelOptions()
        {
            var blackTanShepherd = new MenuItem("Black & Tan German Shepherd");
            var redTanShepherd = new MenuItem("Red & Tan German Shepherd");
            var tanShepherd = new MenuItem("Tan Sable German Shepherd");
            var blackShepherd = new MenuItem("Black German Shepherd");

            _dogKennelMenu.AddMenuItem(blackTanShepherd);
            _dogKennelMenu.AddMenuItem(redTanShepherd);
            _dogKennelMenu.AddMenuItem(tanShepherd);
            _dogKennelMenu.AddMenuItem(blackShepherd);

            _dogKennelMenu.OnItemSelect += (menu, item, index) =>
            {
                if (item == blackTanShepherd)
                {
                    _selectedDogTexture = 0;
                    DeployDog(this, DsuNaming.FetchDogName());
                }

                if (item == redTanShepherd)
                {
                    _selectedDogTexture = 1;
                    DeployDog(this, DsuNaming.FetchDogName());
                }

                if (item == tanShepherd)
                {
                    _selectedDogTexture = 2;
                    DeployDog(this, DsuNaming.FetchDogName());
                }

                if (item == blackShepherd)
                {
                    _selectedDogTexture = 3;
                    DeployDog(this, DsuNaming.FetchDogName());
                }

                _hasTakenDogFromKennel = true;
            };
        }

        #endregion

        private async void OnReceiveDogFxEventFromServer(int dogNetworkId, DogFx dogFx)
        {
            var dogEntity = Entity.FromNetworkId(dogNetworkId);

            // Dog doesn't exist
            if (!dogEntity.Exists()) return;

            if (dogFx == DogFx.Pee)
            {
                API.RequestNamedPtfxAsset("scr_amb_chop");

                while (!API.HasNamedPtfxAssetLoaded("scr_amb_chop"))
                {
                    API.RequestNamedPtfxAsset("scr_amb_chop");
                    await BaseScript.Delay(0);
                }

                API.UseParticleFxAssetNextCall("scr_amb_chop");

                var particleFx = API.StartNetworkedParticleFxLoopedOnEntity("ent_anim_dog_peeing", dogEntity.Handle, 0.1f,
                    -0.32f, -0.04f,
                    0f, 0f, 30f,
                    1f, false, false, false);

                await PlayDogSound(DogSound.Whine);

                await BaseScript.Delay(9000);

                API.StopParticleFxLooped(particleFx, false);
                return;
            }

            if (dogFx == DogFx.Poo)
            {
                API.RequestNamedPtfxAsset("scr_amb_chop");

                while (!API.HasNamedPtfxAssetLoaded("scr_amb_chop"))
                {
                    API.RequestNamedPtfxAsset("scr_amb_chop");
                    await BaseScript.Delay(0);
                }

                API.UseParticleFxAssetNextCall("scr_amb_chop");

                var particleFx = API.StartNetworkedParticleFxLoopedOnEntity("ent_anim_dog_poo", dogEntity.Handle, 0f, -0.15f,
                    -0.2f,
                    0f, 0f, 0f,
                    1f, false, false, false);

                await BaseScript.Delay(6000);

                API.StopParticleFxLooped(particleFx, false);
            }
        }

        private void OnReceiveSoundEventFromServer(int dogNetworkId, DogSound soundType)
        {
            var dogEntity = Entity.FromNetworkId(dogNetworkId);

            // Dog doesn't exist
            if (!dogEntity.Exists()) return;

            switch (soundType)
            {
                case DogSound.Bark:
                    API.PlayAnimalVocalization(dogEntity.Handle, 3, "BARK");
                    break;
                case DogSound.Sniff:
                    API.PlayAnimalVocalization(dogEntity.Handle, 3, "SNIFF");
                    break;
                case DogSound.Playful:
                    API.PlayAnimalVocalization(dogEntity.Handle, 3, "PLAYFUL");
                    break;
                case DogSound.Agitated:
                    API.PlayAnimalVocalization(dogEntity.Handle, 3, "AGITATED");
                    break;
                case DogSound.Whine:
                    API.PlayAnimalVocalization(dogEntity.Handle, 3, "WHINE");
                    break;
                default:
                    break;
            }
        }

        public async Task PlaceInVehicle(Vehicle vehicle)
        {
            if (_dog == null) return;
            if (!DogSpawned) return;
            
            var vehCoords = vehicle.Position;
            float forwardX = vehicle.ForwardVector.X * 2.0f;
            float forwardY = vehicle.ForwardVector.Y * 2.0f;

            var boot = new Vector3(vehCoords.X - forwardX, vehCoords.Y - forwardY, World.GetGroundHeight(vehCoords));
            
            vehicle.Doors[VehicleDoorIndex.Trunk].Open();
            
            _dog.Task.RunTo(boot, true);

            await Delay(3000);
            _dog.Task.AchieveHeading(vehicle.Heading, -1);
            API.RequestAnimDict("creatures@rottweiler@in_vehicle@van");
            API.RequestAnimDict("creatures@rottweiler@amb@world_dog_sitting@base");
            while (!API.HasAnimDictLoaded("creatures@rottweiler@in_vehicle@van") || !API.HasAnimDictLoaded("creatures@rottweiler@amb@world_dog_sitting@base"))
            {
                await Delay(5);
            }
            
            await _dog.Task.PlayAnimation("creatures@rottweiler@in_vehicle@van", "get_in", 8.0f, -4.0f, -1, AnimationFlags.StayInEndFrame, 0.0f);

            await Delay(2000);

            _dog.Task.ClearAll();

            _dog.Task.AchieveHeading(vehicle.Heading - 180, -1);
            _dog.AttachTo(vehicle.Bones["seat_pside_r"], new Vector3(0.0f, 0.0f, 0.25f));
            

            // Sit anim
            //await _dog.Task.PlayAnimation("creatures@rottweiler@amb@world_dog_sitting@base", "base", 8.0f, -4.0f, -1, AnimationFlags.StayInEndFrame, 0.0f);

            _dog.Task.PlayAnimation("creatures@rottweiler@amb@sleep_in_kennel@", "sleep_in_kennel");

            await Delay(1000);

            vehicle.Doors[VehicleDoorIndex.Trunk].Close();
            inVehicle = vehicle;
        }

        public async Task SearchVehicle(Vehicle vehicle, VehicleDoorIndex lookingAtDoor)
        {
            if (_dog == null) return;
            if (!DogSpawned) return;
            if (inVehicle != null) return;
            
            var door = lookingAtDoor switch
            {
                VehicleDoorIndex.FrontLeftDoor => "door_dside_f",
                VehicleDoorIndex.FrontRightDoor => "door_pside_f",
                VehicleDoorIndex.BackLeftDoor => "door_dside_r",
                VehicleDoorIndex.BackRightDoor => "door_pside_f",
                VehicleDoorIndex.Hood => string.Empty,
                VehicleDoorIndex.Trunk => "boot",
                _ => string.Empty
            };


            if (string.IsNullOrEmpty(door))
            {
                _notifications.Error("DSU", "Unable to search this part!");
                return;
            }
            
            var heading = lookingAtDoor switch
            {
                VehicleDoorIndex.FrontLeftDoor => 90,
                VehicleDoorIndex.FrontRightDoor => 270,
                VehicleDoorIndex.BackLeftDoor => 90,
                VehicleDoorIndex.BackRightDoor => 270,
                VehicleDoorIndex.Hood => 180,
                _ => 0
            };
            
            _notifications.Info("DSU", $"{DsuNaming.FetchDogName()} is searching!");

            var boneIndex = API.GetEntityBoneIndexByName(vehicle.Handle, door);
            var pos = API.GetWorldPositionOfEntityBone(vehicle.Handle, boneIndex);
            
            API.TaskGoToCoordAnyMeans(_dog.Handle, pos.X, pos.Y, pos.Z, 5f, 0, false, 786603,0xbf800000);
            
            vehicle.Doors[lookingAtDoor].Open();
            
            await Delay(1000);

            _dog.Task.AchieveHeading(vehicle.Heading - heading, -1);

            for (int i = 0; i < 5; i++)
            {
                await PlayDogSound(DogSound.Bark);
                await Delay(1000);
            }

            vehicle.Doors[lookingAtDoor].Close();
            
            _notifications.Success("DSU", $"{DsuNaming.FetchDogName()} has searched the vehicle!");
        }

        public async Task TakeFromVehicle()
        {
            if (_dog == null) return;
            if (!DogSpawned) return;
            if (inVehicle == null) return;

            Vector3 vehCoords = inVehicle.Position;

            float forwardX = inVehicle.ForwardVector.X * 3.7f;
            
            float forwardY = inVehicle.ForwardVector.Y * 3.7f;
            
            _dog.Task.ClearAll();
            
            inVehicle.Doors[VehicleDoorIndex.Trunk].Open();

            await Delay(300);
            
            _dog.Detach();

            _dog.Position = new Vector3(vehCoords.X - forwardX, vehCoords.Y - forwardY, World.GetGroundHeight(vehCoords));

            await Delay(500);
            
            inVehicle.Doors[VehicleDoorIndex.Trunk].Close();

            inVehicle = null;
            
        }
        
        public void StopSitting()
        {
            _shouldSit = false;
        }

        public Ped FetchDogPed()
        {
            return _dog;
        }

        private async Task DogTick()
        {
            if (!DogSpawned) return;

            CheckSitAnim();

            if (_chasingSuspect)
            {
                CheckSuspect();
            }

            if (API.IsControlPressed(0, (int) Control.VehicleDropProjectile) &&
                API.IsControlPressed(0, (int) Control.Sprint))
            {
                if (_dogMenu.Visible) return;

                int suspectEntityHandle = 0;
                API.GetEntityPlayerIsFreeAimingAt(Game.Player.Handle, ref suspectEntityHandle);

                if (suspectEntityHandle == 0)
                {
                    // Do a ray cast?

                    bool hit = false;
                    int entity = 0;
                    Vector3 endCoords = Vector3.Zero;
                    Vector3 surfaceNormal = Vector3.Zero;
                    
                    var pos = Game.PlayerPed.Position;
                    var offset = API.GetOffsetFromEntityInWorldCoords(Game.PlayerPed.Handle, 0.0f, 100f, 0f);
                    var rayHandle = API.StartShapeTestRay(pos.X, pos.Y, pos.Z, offset.X, offset.Y, offset.Z, 4,_dog.Handle, 4);
                    var result = API.GetShapeTestResult(rayHandle, ref hit, ref endCoords, ref surfaceNormal, ref entity);
                    _logger.Debug($"Entity Hit? {hit}: {entity}");
                    suspectEntityHandle = entity;
                }

                if (API.IsPedDeadOrDying(suspectEntityHandle, true)) return;

                var suspectEntity = Entity.FromHandle(suspectEntityHandle);

                if (!suspectEntity.Exists()) return;

                if (API.IsEntityAPed(suspectEntityHandle))
                {
                    _suspectEntity = (Ped) suspectEntity;
                    if (!_suspectEntity.IsPlayer)
                    {
                        await _suspectEntity.TryRequestNetworkEntityControl();

                        if (!_suspectEntity.IsFleeing && !_suspectEntity.IsPlayer)
                        {
                            //_suspectEntity.Task.WanderAround();
                            //_suspectEntity.Task.FleeFrom(_dog);
                        }
                        //API.SetEntityAsMissionEntity(_suspectEntity.Handle, true, true);
                    }

                    ClearDogTasks();
                    API.TaskFollowToOffsetOfEntity(_dog.Handle, _suspectEntity.Handle, 0f, 0f, 0f, 180f, -1, -1, true);
                    API.SetPedSeeingRange(_dog.Handle, 100f);
                    API.SetPedHearingRange(_dog.Handle, 100f);
                    API.SetPedCombatAttributes(_dog.Handle, 5, true);
                    API.SetPedCombatAttributes(_dog.Handle, 0, false);
                    API.SetPedCombatAttributes(_dog.Handle, 46, true);
                    API.SetPedFleeAttributes(_dog.Handle, 512, true);
                    API.SetCanAttackFriendly(_dog.Handle, true, true);
                    _chasingSuspect = true;
                    _speech.Say(_dog, "Woof Woof!");
                    API.RequestAnimDict("random@arrests@busted");
                }
            }

            return;
        }

        private async void CheckSuspect()
        {
            if (!_suspectEntity.Exists())
            {
                ClearDogTasks();
                _logger.Error("Suspect Entity Lost!");
                _chasingSuspect = false;
                return;
            }

            if (!_dog.HasNetworkControl())
            {
                _logger.Error("Lost network control of dog. Trying to regain");
                if (!await _dog.TryRequestNetworkEntityControl())
                {
                    _logger.Error("Unable to get control of dog!");
                    _chasingSuspect = false;
                    return;
                }
            }

            if (!_chasingSuspect) return;
            
            if (API.GetDistanceBetweenCoords(_dog.Position.X, _dog.Position.Y, _dog.Position.Z,
                _suspectEntity.Position.X, _suspectEntity.Position.Y, _suspectEntity.Position.Z, true) <= 5f)
            {
                if (!_suspectEntity.IsPlayer)
                {
                    await _suspectEntity.TryRequestNetworkEntityControl();
                    _suspectEntity.Task.Wait(3000);
                    //_dog.Task.FightAgainst(_suspectEntity);
                }
            }
            /*
            if (API.GetDistanceBetweenCoords(_dog.Position.X, _dog.Position.Y, _dog.Position.Z,
                _suspectEntity.Position.X, _suspectEntity.Position.Y, _suspectEntity.Position.Z, true) <= 1f)
            {
                if (!_suspectEntity.IsPlayer)
                {
                    await _suspectEntity.TryRequestNetworkEntityControl(canMigrate: true);
                }

                _suspectEntity.Task.Cower(6000);
                _chasingSuspect = false;
                ClearDogTasks();
                _shouldSit = true;
                SuspectBark();
            }
            
             */

            if (API.GetDistanceBetweenCoords(_dog.Position.X, _dog.Position.Y, _dog.Position.Z,
                _suspectEntity.Position.X, _suspectEntity.Position.Y, _suspectEntity.Position.Z, true) <= 3f)
            {
                _chasingSuspect = false;
                if (!_suspectEntity.IsPlayer)
                {
                    _suspectEntity.Task.Cower(3000);
                }

                //await Delay(7000);
               //if (_suspectEntity is null) return;
               await _suspectEntity.Task.PlayAnimation("random@arrests@busted", "enter", 8f, 1f, -1, AnimationFlags.StayInEndFrame, 1f);
               await Delay(1000); 
               await _suspectEntity.Task.PlayAnimation("random@arrests@busted", "idle_a", 8f, 1f, -1, AnimationFlags.StayInEndFrame, 1f);
               await Delay(2000);
               _shouldSit = true; 
               SuspectBark(); 
               _suspectEntity = null;
            }
        }
        private async void SuspectBark()
        {
            for (int i = 0; i < 10; i++)
            {
                await PlayDogSound(DogSound.Bark);
                await BaseScript.Delay(1000);
            }
        }

        private void CheckSitAnim()
        {
            if (!_shouldSit) return;
            _dog.Task.PlayAnimation("creatures@rottweiler@amb@world_dog_sitting@base", "base");
        }

        private async void DeployDog(object sender, string dogName)
        {
            // Prevents dog spam?
            if (DateTime.Compare(DateTime.Now, _lastDeployTime.AddSeconds(3)) <= 0) return;

            _logger.Debug($"Requesting Deployment of {dogName}");

            if (dogName == "none")
            {
                _notifications.Error("Error", "You've not named your dog yet!");
                return;
            }

            if (DogSpawned)
            {
                await ReturnDog();
            }

            _lastDeployTime = DateTime.Now;

            Model dogModel = new Model(_dogModel);

            if (!dogModel.IsValid)
            {
                _logger.Error("Dog Model is Invalid!");
                return;
            }
            dogModel.Request();
            
            if (!dogModel.IsLoaded)
            {
                _logger.Error("Unable to load the dog model!");
                return;
            }

            _dog = await World.CreatePed(dogModel, Game.PlayerPed.Position, 0F);
            _dog.CanBeTargetted = false;
            _dog.RelationshipGroup = new RelationshipGroup(API.GetHashKey("policedog"));
            _dog.RelationshipGroup.SetRelationshipBetweenGroups(Game.PlayerPed.RelationshipGroup,
                Relationship.Companion, true);
            _dog.BlockPermanentEvents = true;
            API.SetCanAttackFriendly(_dog.Handle, false, false);
            API.SetPedFleeAttributes(_dog.Handle, 0, false);

            API.SetPedComponentVariation(_dog.Handle, 0, 0, _selectedDogTexture, 0);

            if (!API.IsPedInGroup(_dog.Handle))
            {
                API.SetPedAsGroupMember(_dog.Handle, API.GetPedGroupIndex(API.PlayerPedId()));
            }

            if (API.IsPedInGroup(_dog.Handle))
            {
                API.SetPedNeverLeavesGroup(_dog.Handle, true);
                API.SetGroupFormationSpacing(API.GetPlayerGroup(API.PlayerPedId()), 1f, 0.9f, 3f);
                API.SetPedCanTeleportToGroupLeader(_dog.Handle, API.GetPedGroupIndex(API.PlayerPedId()), true);
            }

            _notifications.Success("Deployment", $"{dogName} has been deployed!");

            _blip = _dog.AttachBlip();
            _blip.Sprite = (BlipSprite) 442;
            _blip.Color = BlipColor.MichaelBlue;
            _blip.Name = dogName;

            API.SetEntityAsMissionEntity(_dog.Handle, true, true);
            API.SetPedAsCop(_dog.Handle, true);

            uint hash = (uint) API.GetPedRelationshipGroupHash(Game.PlayerPed.Handle);
            API.SetPedRelationshipGroupHash(Game.PlayerPed.Handle, hash);
            API.SetNetworkIdCanMigrate(_dog.NetworkId, false);
            
            SyncDogToAllClients();

            DogSpawned = true;

            await PlayDogAnim(DogAnim.Follow);
        }


        private void SyncDogToAllClients()
        {
            for (int i = 0; i <= 256; i++)
            {
                if (API.NetworkIsPlayerActive(i))
                {
                    API.SetNetworkIdSyncToPlayer(_dog.NetworkId, i, true);
                }
            }
        }

        private void LoadDogMenuOptions()
        {
            _dogMenu.ClearMenuItems();
            
            var prepareDecamp = new MenuItem("Prepare Decamp");
            var follow = new MenuItem("Follow");
            var sit = new MenuItem("Sit");
            var search = new MenuItem("Search Area");
            var hs = new MenuItem("Hide and Seek");
            var lie = new MenuItem("Lie Down");
            var bark = new MenuItem("Bark");
            var sniff = new MenuItem("Sniff");

            
            //_dogMenu.AddMenuItem(prepareDecamp);
            _dogMenu.AddMenuItem(follow);
            _dogMenu.AddMenuItem(sit);
            _dogMenu.AddMenuItem(search);
            _dogMenu.AddMenuItem(hs);
            _dogMenu.AddMenuItem(lie);
            _dogMenu.AddMenuItem(bark);
            _dogMenu.AddMenuItem(sniff);

            _dogMenu.OnItemSelect += async (menu, item, index) =>
            {
                if (item == prepareDecamp) await PrepareDecamp();
                if (item == follow) await PlayDogAnim(DogAnim.Follow);
                if (item == sit) await  PlayDogAnim(DogAnim.Sit);
                if (item == search) await  PlayDogAnim(DogAnim.SearchArea);
                if (item == hs)  await PlayDogAnim(DogAnim.HideSeek);
                if (item == bark) await  PlayDogSound(DogSound.Bark);
                if (item == lie)  await PlayDogAnim(DogAnim.LieDown);
                if (item == sniff)  await PlayDogSound(DogSound.Sniff);
            };
            
            _dogMenu.OpenMenu();
            
        }

        public async Task PrepareDecamp()
        {
            if (_dog == null) return;
            if (inVehicle == null) return;
            _dog.Task.PlayAnimation("creatures@rottweiler@amb@world_dog_sitting@base", "base");
            _dog.AttachTo(inVehicle.Bones["seat_pside_f"], new Vector3(0.0f, 0.0f, 0.25f));
            _decampMode = true;
        }
        
        private async Task HandleDecamp()
        {
            if (_dog == null) return;
            if (inVehicle == null) return;
            if (!_decampMode) return;

            var currentVehicle = Game.PlayerPed.CurrentVehicle;

            if (currentVehicle != null && _lastVehicle == null || currentVehicle != null && _lastVehicle != currentVehicle)
            {
                _lastVehicle = currentVehicle;
                return;
            }

            if (currentVehicle != null && _lastVehicle == currentVehicle) return;
            
            // Left Vehicle?
            _logger.Debug("Left Vehicle?");

            var doorIndex = API.GetEntityBoneIndexByName(_lastVehicle.Handle, "door_dside_f");
            var doorWorldPosition = API.GetWorldPositionOfEntityBone(_lastVehicle.Handle, doorIndex);
            
            Vector3 vehCoords = _lastVehicle.Position;

            float forwardX = _lastVehicle.ForwardVector.X * 3.7f;
            
            float forwardY = _lastVehicle.ForwardVector.Y * 3.7f;
            
            _dog.Task.ClearAll();

            _dog.Detach();

            _dog.Position = new Vector3(doorWorldPosition.X - forwardX, doorWorldPosition.Y - forwardY, World.GetGroundHeight(doorWorldPosition));

            inVehicle = null;

            _decampMode = false;

            await PlayDogAnim(DogAnim.Follow);
        }
        
        public async Task PlayDogAnim(DogAnim animType)
        {
            if (!_dog.Exists())
            {
                _logger.Error("Unable to find the dog!");
                return;
            }

            if (!_dog.HasNetworkControl())
            {
                if (!await _dog.TryRequestNetworkEntityControl())
                {
                    _logger.Error("Unable to get control of dog from Wait!");
                    return;
                }
            }

            ClearDogTasks();

            switch (animType)
            {
                case DogAnim.Follow:
                    API.TaskFollowToOffsetOfEntity(_dog.Handle, Game.PlayerPed.Handle, 0.5f, 0.5f, 0f, 25f, -1, -1,
                        true);
                    break;
                case DogAnim.Sit:
                    _shouldSit = true;
                    break;
                case DogAnim.SearchArea:
                    Vector3 position = Game.PlayerPed.Position;
                    API.TaskWanderInArea(_dog.Handle, position.X, position.Y, position.Z, 60f, 0, 0);
                    break;
                case DogAnim.HideSeek:
                    API.TaskSeekCoverFromPed(_dog.Handle, Game.PlayerPed.Handle, 60, true);
                    break;
                case DogAnim.LieDown:
                    _dog.Task.PlayAnimation("creatures@rottweiler@amb@sleep_in_kennel@", "sleep_in_kennel");
                    break;
                default:
                    break;
            }
        }

        public async Task PlayDogSound(DogSound soundType)
        {
            if (!_dog.Exists())
            {
                _logger.Error("Unable to find the dog!");
                return;
            }

            if (!_dog.HasNetworkControl())
            {
                if (!await _dog.TryRequestNetworkEntityControl())
                {
                    _logger.Error("Unable to get control of dog from Bark!");
                    return;
                }
            }

            _comms.ToServer(ServerEvents.SendDogSoundEventToServer, _dog.NetworkId, soundType);
        }

        public async Task ReturnDog()
        {
            if (!DogSpawned) return;

            if (_dog == null) return;

            if (!_dog.HasNetworkControl())
            {
                if (!await _dog.TryRequestNetworkEntityControl())
                {
                    _logger.Error("Unable to gain control of putting dog away!");
                    return;
                }
            }

            ClearDogTasks();

            await BaseScript.Delay(10);

            _blip.Delete();
            _dog.Delete();
            DogSpawned = false;
            _dog = null;
            _suspectEntity = null;
            _chasingSuspect = false;
        }

        private async void ClearDogTasks()
        {
            if (!_dog.HasNetworkControl())
            {
                if (!await _dog.TryRequestNetworkEntityControl())
                {
                    _logger.Error("Unable to gain control of stopping anims!");
                    return;
                }
            }

            _shouldSit = false;
            //chasingSuspect = false;
            API.ClearPedTasks(_dog.Handle);
        }

        public Task Deploy()
        {
            DeployDog(this, DsuNaming.FetchDogName());

            return Task.FromResult(0);
        }
    }
}