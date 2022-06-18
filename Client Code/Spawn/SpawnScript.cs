using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using MenuAPI;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Client.Extensions;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;
using PoliceMP.Shared.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PoliceMP.Client.Scripts.Weapons;
using PoliceMP.Client.Services;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client.Commands.Interfaces;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Constants.States;

namespace PoliceMP.Client.Scripts.Spawn
{

    public class SpawnScript : Script, ISpawnScript
    {
        private const string HasSpawnedState = "HasSpawned";
        private const int TimeoutMs = 15000;
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<SpawnScript> _logger;
        private readonly ITickManager _ticks;
        private readonly IPermissionService _permissionService;
        private readonly ICustomCharacterService _customCharacterService;
        private readonly ICommandManager _command;
        private readonly INotificationService _notification;
        private SpawnOptions _options;

        private static List<SpawnLocation> _spawnLocations;
        public static Vector3 LastSpawnPosition { get; private set; }

        private UserAces _userAces;
        public SpawnScript(IClientCommunicationsManager comms,
            ILogger<SpawnScript> logger,
            ITickManager ticks,
            IPermissionService permissionService,
            ICustomCharacterService customCharacterService, ICommandManager command, INotificationService notification)
        {
            _comms = comms;
            _logger = logger;
            _ticks = ticks;
            _permissionService = permissionService;
            _customCharacterService = customCharacterService;
            _command = command;
            _notification = notification;
        }

        protected override async Task OnStartAsync()
        {
            _userAces = await _permissionService.GetUserAces();
            
            #region Spawn Menu

            _spawnLocations = new List<SpawnLocation>
            {
                new("Vespucci", new Core.Shared.Models.Vector3(-1110, -847, 19), UserBranch.Police, UserDivision.Cid),
                new("Vinewood", new Core.Shared.Models.Vector3(647, -8, 84), UserBranch.Police, UserDivision.Rpu),
                new("Davis", new Core.Shared.Models.Vector3(358, -1582, 29), UserBranch.Police, UserDivision.Dsu),
                new("La Mesa", new Core.Shared.Models.Vector3(817, -1290, 26), UserBranch.Police),
                new("Sandy Shores", new Core.Shared.Models.Vector3(1858, 3678, 33), UserBranch.Police),
                new("Paleto Bay", new Core.Shared.Models.Vector3(-440, 6019, 31), UserBranch.Police),
                new("Rockford", new Core.Shared.Models.Vector3(-562.522f, -141.654f, 39.214f), UserBranch.Police),
                new("Mission Row", new Core.Shared.Models.Vector3(427, -980, 31), UserBranch.Police),
                new("St Thomas' Hospital", new Core.Shared.Models.Vector3(353.757f, -589.301f, 28.79f), UserBranch.Nhs),
                new("Mount Zonah Hospital", new Core.Shared.Models.Vector3(-449.91345214844f, -340.67440795898f, 34.501735687256f), UserBranch.Nhs),
                new("Davis Fire Station", new Core.Shared.Models.Vector3(200.567f, -1632.419f, 29.786f), UserBranch.Fire),
                new("El Burro Heights Fire Station", new Core.Shared.Models.Vector3(1165.299f, -1458.514f, 34.897f), UserBranch.Fire),
                new("Burton Civ Garage", new Core.Shared.Models.Vector3(-356.3898f, -130.3878f, 39.43875f), UserBranch.Civ),
                new("National Highways Depot", new Core.Shared.Models.Vector3(1527.562f, 816.1177f, 77.43f), UserBranch.Highways),
                new("Sandy Shores Hospital", new Core.Shared.Models.Vector3(1839.821f,3671.9978f,34.27668f), UserBranch.Nhs),
                new("Sandy Shores Fire Station", new Core.Shared.Models.Vector3(1695.102f, 3580.917f, 35.57851f), UserBranch.Fire),
                new("Paleto Bay Fire Station", new Core.Shared.Models.Vector3(-383.9563f,6122.702f,31.47955f), UserBranch.Fire),
                new("Paleto Bay Hospital", new Core.Shared.Models.Vector3(-233.2601f, 6317.174f, 31.48956f), UserBranch.Nhs),
                new("Sandy Shores Garage", new Core.Shared.Models.Vector3(1189.422f, 2648.254f,37.83511f), UserBranch.Civ),
                new("Heathrow Station", new Core.Shared.Models.Vector3(-894.52496337891f,-2403.7014160156f,14.02429485321f), UserBranch.Police),
                new("South Central Ambulance Station", new Core.Shared.Models.Vector3(-1413.4091796875f,-269.92266845703f,46.47974395752f), UserBranch.Nhs)
            };

            _logger.Debug($"Received {_spawnLocations.Count} spawn locations");
            
            #endregion Spawn Menu

            LastSpawnPosition = Vector3.Zero;

            _options = await _comms.Request<SpawnOptions>(ServerEvents.SpawnGetOptions);

            _logger.Debug(_options.ToString());

            _command.Register("respawn").WithHandler(Respawn);
            
            _command.Register("reviveself").WithHandler(() =>
            {
                if (_userAces.IsDeveloper || _userAces.IsAdmin)
                {
                    Revive();
                    return;
                }
                _notification.Error("Developer", "Dev things, begone!");
            });
            
            _command.Register("revive").WithHandler(() =>
            {
                if (_userAces.IsDeveloper || _userAces.IsAdmin || _userAces.IsNhsParamedic)
                {
                    foreach (var ped in World.GetAllPeds())
                    {
                        var playerPos = Game.PlayerPed.Position;
                        if(API.GetDistanceBetweenCoords(ped.Position.X, ped.Position.Y, ped.Position.Z, playerPos.X, playerPos.Y, playerPos.Z, true) >1f)
                        {
                            continue;
                        }

                        if (ped.IsPlayer)
                        {
                            _comms.ToServer(ServerEvents.SpawnNotifyPlayerToRevive, ped.NetworkId);
                        }
                    }
                }
                else
                {
                    _notification.Error("Revive", "You are not NHS.");
                }
            });
            
            _comms.On(ClientEvents.SpawnToldToRevive, Revive);
            
            await CreateSpawnSelectionMenu(true);

            _ticks.On(SpawnScriptTick);
            _ticks.On(ModelSuppressionTick);
        }

        private List<string> vehicleSuppressionList = new List<string>
        {
            "blimp",
            "blimp2",
            "blimp3",
            "duster",
            "stunt",
            "mammatus",
            "Dodo",
            "frogger",
            "frogger2",
            "cargoplane",
        };
        
        private Task ModelSuppressionTick()
        {
            foreach (var vehicleSuppression in vehicleSuppressionList)
            {
                var model = new Model(vehicleSuppression);
                if(!model.IsValid) return Task.FromResult(0);

                var hash = (uint) model.Hash;
                API.SetVehicleModelIsSuppressed(hash, true);
            }
            return Task.FromResult(0);
        }

        private bool spawnAttempting = false;
        private int notifyCooldown = 0;

        private async Task SpawnScriptTick()
        {
            if (Game.Player.IsDead)
            {
                if (notifyCooldown > 0)
                {
                    notifyCooldown--;
                    await Delay(1);
                    return;
                }

                notifyCooldown = 2000;
                var builder = new StringBuilder();
                builder.Append(
                    "You have <b>DIED</b> NHS can revive you. Or /respawn.");
                
                _notification.Info("Dead", builder.ToString());
            }
        }

        private async void Respawn()
        {
            if (spawnAttempting) return;
            if (!Game.PlayerPed.IsDead)
            {
                _notification.Error("Respawn","You must be dead to use this.");
            }
            
            spawnAttempting = true;
            _logger.Debug("Player died, about to respawn...");
            Game.PlayerPed.Resurrect();
            await SpawnPlayerAsync(LastSpawnPosition, false);
            spawnAttempting = false;
            notifyCooldown = 0;
            _logger.Debug("Player should be spawned");
        }
        
        private async void Revive()
        {
            if (spawnAttempting) return;
            
            spawnAttempting = true;
            Game.PlayerPed.Resurrect();
            spawnAttempting = false;
            notifyCooldown = 0;
            _logger.Debug("Player should be respawned");
        }

        public async Task CreateSpawnSelectionMenu(bool firstLoadIn)
        {
            _logger.Debug("Creating Spawn Selection Menu");

            MenuController.CloseAllMenus();

            Menu spawnMenu = new Menu("Spawn", "Select your spawn");

            MenuController.DisableBackButton = true;
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuToggleKey = (Control)(-1);
            MenuController.AddMenu(spawnMenu);

            var branchNames = new Dictionary<string, UserBranch>
            {
                {"Police", UserBranch.Police},
                {"NHS", UserBranch.Nhs}
            };
            
            /*if(_userAces.IsNhsClinical)
                branchNames.Add("NHS", UserBranch.Nhs);*/
            
            if(_userAces.IsFireTrained)
                branchNames.Add("Fire", UserBranch.Fire);
            if(_userAces.IsCivTrained)
                branchNames.Add("Civilian", UserBranch.Civ);
            if (_userAces.IsHighwaysTrained)
                branchNames.Add("Highways", UserBranch.Highways);
            if (_userAces.IsControl)
                branchNames.Add("Control", UserBranch.Control);

            var branchSelection = new MenuListItem("Role", branchNames.Keys.ToList(), 0);
            
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            
            spawnMenu.AddMenuItem(branchSelection);

            var userRole = PermissionService.DefaultRole;

            #region Police

            
            var policeDivisionList = new Dictionary<string, UserDivision>
            {
                { "Emergency Response Team", UserDivision.Ert}
            };

            if (_userAces.IsAfoTrained)
            {
                policeDivisionList.Add("Authorized Firearms Officer", UserDivision.Afo);
                _logger.Debug("Is AFO");
            }

            if(_userAces.IsCidTrained)
            {
                policeDivisionList.Add("CID", UserDivision.Cid);
                _logger.Debug("Is CID");
            }

            if(_userAces.IsDsuTrained)
            {
                policeDivisionList.Add("Dog Support Unit", UserDivision.Dsu);
                _logger.Debug("Is DSU");
            }

            if(_userAces.IsNpasTrained)
            {
                policeDivisionList.Add("National Police Air Service", UserDivision.Npas);
                _logger.Debug("Is NPAS");
            }

            if(_userAces.IsRpuTrained)
            {
                policeDivisionList.Add("Roads Policing Unit", UserDivision.Rpu);
                _logger.Debug("Is RPU");
            }
            
            var policeDivisionMenuItem = new MenuListItem("Division", policeDivisionList.Keys.ToList(), 0);

            var policeSpawnList = _spawnLocations.Where(o => o.Branch == UserBranch.Police).Select(spawnLocation => spawnLocation.Name).ToList();
            
            var policeSpawnItem = new MenuListItem("Location", policeSpawnList, 0);


            #endregion

            #region NHS
            

            var nhsDivisionList = new Dictionary<string, UserDivision>
            {
                {"St John's", UserDivision.StJohn}
            };

            if (_userAces.IsNhsParamedic)
            {
                nhsDivisionList.Add("NHS Clinical", UserDivision.Clinical);
            }
            if (_userAces.IsNhsHems)
            {
                nhsDivisionList.Add("NHS HEMS", UserDivision.Hems);
            }

            var nhsDivisionMenuItem = new MenuListItem("Division", nhsDivisionList.Keys.ToList(), 0);
            
            var nhsSpawnList = _spawnLocations.Where(o => o.Branch == UserBranch.Nhs)
                .Select(spawnLocation => spawnLocation.Name).ToList();
            
            var nhsSpawnItem = new MenuListItem("Location", nhsSpawnList, 0);
            
            #endregion
            

            var fireSpawnList = _spawnLocations.Where(o => o.Branch == UserBranch.Fire)
                .Select(spawnLocation => spawnLocation.Name).ToList();
            var fireSpawnItem = new MenuListItem("Location", fireSpawnList, 0);

            var civSpawnList = _spawnLocations.Where(o => o.Branch == UserBranch.Civ)
                .Select(sL => sL.Name).ToList();
            var civSpawnItem = new MenuListItem("Location", civSpawnList, 0);
            
            var highwaysSpawnList = _spawnLocations.Where(o => o.Branch == UserBranch.Highways)
                .Select(sL => sL.Name).ToList();
            var highwaysSpawnItem = new MenuListItem("Location", highwaysSpawnList, 0);
            
            var spawnMenuItem = new MenuItem("Spawn")
            {
                LeftIcon = MenuItem.Icon.STAR
            };
            
            spawnMenu.AddMenuItem(policeDivisionMenuItem);
            spawnMenu.AddMenuItem(policeSpawnItem);
            spawnMenu.AddMenuItem(spawnMenuItem);

            spawnMenu.OpenMenu();

            var itemSelected = false;

            spawnMenu.OnListIndexChange += (menu, item, index, selectionIndex, itemIndex) =>
            {
                if (item == branchSelection)
                {
                    switch (branchNames[item.GetCurrentSelection()])
                    {
                        case UserBranch.Police:
                            menu.ClearMenuItems();
                            menu.AddMenuItem(branchSelection);
                            menu.AddMenuItem(policeDivisionMenuItem);
                            menu.AddMenuItem(policeSpawnItem);
                            menu.AddMenuItem(spawnMenuItem);
                            break;
                        case UserBranch.Nhs:
                            menu.ClearMenuItems();
                            menu.AddMenuItem(branchSelection);
                            menu.AddMenuItem(nhsSpawnItem);
                            menu.AddMenuItem(nhsDivisionMenuItem);
                            menu.AddMenuItem(spawnMenuItem);
                            break;
                        case UserBranch.Fire:
                            menu.ClearMenuItems();
                            menu.AddMenuItem(branchSelection);
                            menu.AddMenuItem(fireSpawnItem);
                            menu.AddMenuItem(spawnMenuItem);
                            break;
                        case UserBranch.Civ:
                            menu.ClearMenuItems();
                            menu.AddMenuItem(branchSelection);
                            menu.AddMenuItem(civSpawnItem);
                            menu.AddMenuItem(spawnMenuItem);
                            break;
                        case UserBranch.Highways:
                            userRole.Branch = UserBranch.Highways;
                            userRole.Division = UserDivision.None;
                            menu.ClearMenuItems();
                            menu.AddMenuItem(branchSelection);
                            menu.AddMenuItem(highwaysSpawnItem);
                            menu.AddMenuItem(spawnMenuItem);
                            break;
                        case UserBranch.Control:
                            userRole.Branch = UserBranch.Control;
                            userRole.Division = UserDivision.None;
                            menu.ClearMenuItems();
                            menu.AddMenuItem(branchSelection);
                            menu.AddMenuItem(policeSpawnItem);
                            menu.AddMenuItem(spawnMenuItem);
                            break;
                        default:
                            menu.ClearMenuItems();
                            menu.AddMenuItem(branchSelection);
                            menu.AddMenuItem(spawnMenuItem);
                            break;
                    }
                }
                else if (item == policeDivisionMenuItem) 
                {
                    
                    Debug.WriteLine($"Division Changed: {item.GetCurrentSelection()}");
                    
                    policeSpawnItem.ListIndex = policeDivisionList[item.GetCurrentSelection()] switch
                    {
                        UserDivision.Ert => policeSpawnList.IndexOf("Vespucci"),
                        UserDivision.Afo => policeSpawnList.IndexOf("Heathrow Station"),
                        UserDivision.Cid => policeSpawnList.IndexOf("Vespucci"),
                        UserDivision.Dsu => policeSpawnList.IndexOf("Davis"),
                        UserDivision.Npas => policeSpawnList.IndexOf("Heathrow Station"),
                        UserDivision.Rpu => policeSpawnList.IndexOf("La Mesa"),
                        _ => policeSpawnItem.ListIndex
                    };
                }
            };

            spawnMenu.OnItemSelect += async (menu, item, index) =>
            {
                if (item != spawnMenuItem) return;

                if (!item.Selected) return;

                itemSelected = true;
                
                userRole.Branch = branchNames[branchSelection.GetCurrentSelection()];
                userRole.Division = UserDivision.None;
                
                if (userRole.Branch == UserBranch.Police)
                {
                    userRole.Division = policeDivisionList[policeDivisionMenuItem.GetCurrentSelection()];
                }
                
                if (userRole.Branch == UserBranch.Nhs)
                {
                    userRole.Division = nhsDivisionList[nhsDivisionMenuItem.GetCurrentSelection()];
                }
                
                Debug.WriteLine($"Spawn selected: ({userRole.Branch.ToString()}, {userRole.Division.ToString()})");
                    
                _permissionService.SetUserRole(userRole);

                if (menu.GetMenuItems().Contains(policeSpawnItem) && userRole.Branch == UserBranch.Police)
                {
                    var policeSpawnLocation = _spawnLocations.Where(o => o.Branch == UserBranch.Police).ToList()[policeSpawnItem.ListIndex];
                    _logger.Debug($"Selected: {policeSpawnLocation.Name}");
                    await SpawnPlayerAsync(policeSpawnLocation.Position.ToCitizenVector3());
                    API.SetPedAsCop(API.PlayerId(), true);
                }

                if (menu.GetMenuItems().Contains(nhsSpawnItem) && userRole.Branch == UserBranch.Nhs)
                {
                    var nhsSpawnLocation = _spawnLocations.Where(o => o.Branch == UserBranch.Nhs).ToList()[nhsSpawnItem.ListIndex];
                    _logger.Debug($"Selected: {nhsSpawnLocation.Name}");
                    await SpawnPlayerAsync(nhsSpawnLocation.Position.ToCitizenVector3());
                    API.SetPedAsCop(API.PlayerId(), true);
                }
                    
                if (menu.GetMenuItems().Contains(fireSpawnItem) && userRole.Branch == UserBranch.Fire)
                {
                    var fireSpawnLocation = _spawnLocations.Where(o => o.Branch == UserBranch.Fire).ToList()[fireSpawnItem.ListIndex];
                    _logger.Debug($"Selected: {fireSpawnLocation.Name}");
                    await SpawnPlayerAsync(fireSpawnLocation.Position.ToCitizenVector3());
                    API.SetPedAsCop(API.PlayerId(), true);
                }
                    
                if (menu.GetMenuItems().Contains(civSpawnItem) && userRole.Branch == UserBranch.Civ)
                {
                    var civSpawnLocation = _spawnLocations.Where(o => o.Branch == UserBranch.Civ).ToList()[civSpawnItem.ListIndex];
                    _logger.Debug($"Selected: {civSpawnLocation.Name}");
                    await SpawnPlayerAsync(civSpawnLocation.Position.ToCitizenVector3());
                    API.SetPedAsCop(API.PlayerId(), false);
                }
                if (menu.GetMenuItems().Contains(highwaysSpawnItem) && userRole.Branch == UserBranch.Highways)
                {
                    var highwaysSpawnLocation = _spawnLocations.Where(o => o.Branch == UserBranch.Highways).ToList()[highwaysSpawnItem.ListIndex];
                    _logger.Debug($"Selected: {highwaysSpawnLocation.Name}");
                    await SpawnPlayerAsync(highwaysSpawnLocation.Position.ToCitizenVector3());
                    API.SetPedAsCop(API.PlayerId(), true);
                }
                if (menu.GetMenuItems().Contains(policeSpawnItem) && userRole.Branch == UserBranch.Control)
                {
                    var controlSpawnLocation = _spawnLocations.Where(o => o.Branch == UserBranch.Police).ToList()[policeSpawnItem.ListIndex];
                    _logger.Debug($"Selected: {controlSpawnLocation.Name}");
                    await SpawnPlayerAsync(controlSpawnLocation.Position.ToCitizenVector3());
                    API.SetPedAsCop(API.PlayerId(), true);
                }
                menu.CloseMenu();
                API.NetworkSetFriendlyFireOption(true);
                API.SetCanAttackFriendly(API.PlayerPedId(), true, false);

            };

            spawnMenu.OnMenuOpen += menu =>
            {
                if (userRole.Branch == UserBranch.Police && !menu.GetMenuItems().Contains(policeDivisionMenuItem))
                {
                    menu.AddMenuItem(policeDivisionMenuItem);
                }
            };

            spawnMenu.OnMenuClose += async menu =>
            {
                if (menu == spawnMenu)
                {
                    MenuController.DisableBackButton = false;
                    if (!itemSelected && firstLoadIn)
                    {
                        await CreateSpawnSelectionMenu(true);
                    }
                }
            };
        }

        private async Task SpawnPlayerAsync(Vector3 position, bool isFirstSpawn = true)
        {
            Debug.WriteLine("Spawning...");
            
            var customCharacterString = API.GetResourceKvpString(ResourceKvp.LastUsedCharacter);
            
            Game.PlayerPed.IsInvincible = true;
            Screen.Hud.IsVisible = false;
            Game.Player.Freeze();
            if (!API.IsPlayerSwitchInProgress())
            {
                _logger.Trace("Player switch is in progress. Switching out player now...");
                API.SwitchOutPlayer(API.PlayerPedId(), 0, 1);
            }
            var timeOut = Game.GameTime + TimeoutMs;
            while (API.GetPlayerSwitchState() != 5)
            {
                _logger.Trace($"Waiting for SwitchState 5. Current: {API.GetPlayerSwitchState()}");
                await Delay(10);

                if (Game.GameTime > timeOut)
                    break;
            }
            
            Game.Player.State.Set("PMPCallsign", "null");

            if (isFirstSpawn)
            {
                API.ShutdownLoadingScreen();
                API.ShutdownLoadingScreenNui();
                Game.PlayerPed.Position = position;
                while (!await Game.Player.ChangeModel(new Model(API.GetHashKey(_options.Model))))
                {
                    _logger.Trace("Waiting for Player model change...");
                    await Delay(10);
                }
                API.SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                _comms.ToClient(ClientEvents.SetSpawnClothes);
                BaseScript.TriggerEvent("PoliceMP:SetPlayerSpawnLocation", position.X, position.Y, position.Z);
            }
            else
            {
                Game.Player.Freeze();
                Game.PlayerPed.Position = position;

                API.SetPedDefaultComponentVariation(Game.PlayerPed.Handle);
                _comms.ToClient(ClientEvents.SetSpawnClothes);


                await Delay(500);

                Game.PlayerPed.Resurrect();
            }

            timeOut = Game.GameTime + TimeoutMs;
            while (API.IsEntityWaitingForWorldCollision(Game.PlayerPed.Handle))
            {
                _logger.Trace($"Waiting for SwitchState 8. Current: {API.GetPlayerSwitchState()}");
                await Delay(10);

                if (Game.GameTime > timeOut)
                    break;
            }

            API.SwitchInPlayer(API.PlayerPedId());

            while (API.GetPlayerSwitchState() < 8)
                await Delay(1);

            Game.Player.Freeze(false);
            API.PlaceObjectOnGroundProperly(Game.PlayerPed.Handle);
            Screen.Hud.IsVisible = true;
            Game.Player.Character.IsVisible = true;
            Game.PlayerPed.IsInvincible = false;

            _logger.Trace("Spawning player... Done!");

            _comms.ToServer(ServerEvents.PlayerSpawned, isFirstSpawn);
            _comms.ToClient(ClientEvents.PlayerSpawned, isFirstSpawn);


            if (!(_userAces.IsDeveloper && _userAces.IsAdmin))
            {
                Game.PlayerPed.Weapons.RemoveAll();
            }

            Game.PlayerPed.GiveDefaultEquipment(_permissionService.CurrentUserRole, _userAces);

            if (_userAces.IsAdmin || _userAces.IsDeveloper || _userAces.IsProDonator)
            {
                if (!string.IsNullOrEmpty(customCharacterString))
                {
                    var customCharacter = JsonConvert.DeserializeObject<CustomCharacter>(customCharacterString);

                    if(_permissionService.CurrentUserRole.Branch != UserBranch.Civ)
                    {
                        customCharacter.PedOutfit = JsonConvert.SerializeObject(Game.PlayerPed.FetchCurrentPedOutfit());
                    }

                    _customCharacterService.SetCharacterAppearance(customCharacter);
                }
            }
            
            LastSpawnPosition = position;

        }
    }
}