using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using PoliceMP.Client.Scripts.Dsu;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PoliceMP.Shared.Enums;

namespace PoliceMP.Client.Scripts.Boot
{
    public class BootSystem : Script, IBootSystem
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<BootSystem> _logger;
        private readonly INotificationService _notifications;
        private readonly ITickManager _ticks;
        private readonly IPermissionService _permissionService;
        private readonly IDog _dog;
        private UserAces _userAces;
        private Menu _bootMenu = new Menu("Vehicle Boot");
        private bool _inputDisabled = false;
        private bool _afoStatus = false;
        private bool _dogStatus = false;
        private int _helmetId = -1;
        private int _helmetTexture = 0;
        private int _afoHelmet = 38;

        #region Boot & Bonnet Opening

        // This is used for the new boot AFO locker that opens on the bonnet
        private List<string> _afoBoots = new List<string>
        {
            {"addpolbmw5arv"},

        };



        #endregion

        #region Weapon Vehicle Network Ids

        private int _batonVehicle = -1;
        private int _g36CVehicle = -1;
        private int _l115A3Vehicle = -1;
        private int _mp5Vehicle = -1;
        private int _flashBangVehicle = -1;
        private int _combatPistolVehicle = -1;
        private int _pistolVehicle = -1;
        private int _marskmanVehicle = -1;

        #endregion

        public static event EventHandler<string> DeployDog;

        public static event EventHandler ReturnDog;

        /// <summary>
        /// Fetches if user is DSU
        /// </summary>
        /// <returns>True = Is DSU / Dog Section</returns>
        public bool FetchDsuStatus()
        {
            return _dogStatus;
        }

        public BootSystem(IClientCommunicationsManager comms, ILogger<BootSystem> logger,
            INotificationService notifications, ITickManager ticks, IPermissionService permissionService, IDog dog)
        {
            _comms = comms;
            _logger = logger;
            _notifications = notifications;
            _ticks = ticks;
            _permissionService = permissionService;
            _dog = dog;
            
            MenuController.EnableMenuToggleKeyOnController = false;
            MenuController.MenuToggleKey = (Control)(-1);
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;

            MenuController.AddMenu(_bootMenu);
        }

        protected override async Task OnStartAsync()
        {
            updateAces();
            
            _ticks.On(BootSystemTick);
        }

        private async void updateAces()
        {
            _userAces = await _permissionService.GetUserAces();

            _afoStatus = _userAces.IsAfoTrained || _userAces.IsAdmin || _userAces.IsDeveloper;
            _dogStatus = _userAces.IsDsuTrained || _userAces.IsAdmin || _userAces.IsDeveloper;
        }

        private Task BootSystemTick()
        {
            if (MenuController.DisableBackButton
                && !Game.IsDisabledControlPressed(32, Control.Aim))
            {
                MenuController.DisableBackButton = false;
            }

            if (_inputDisabled)
            {
                API.DisableAllControlActions(0);
                API.DisableAllControlActions(1);
            }

            return Task.FromResult(0);
        }

        private bool _bootMenuLoading = false;
        private async void HandleBootMenu(int vehicleId)
        {
            if (_bootMenuLoading) return;

            _bootMenuLoading = true;

            updateAces();

            Vehicle vehicle = (Vehicle)Entity.FromHandle(vehicleId);
            Ped playerPed = Game.PlayerPed;

            _bootMenu.ClearMenuItems();

            
            
            MenuItem firstAid = new MenuItem("First Aid");
            _bootMenu.AddMenuItem(firstAid);

            
            
            MenuItem reloadTaser = new MenuItem("Reload Taser");
            _bootMenu.AddMenuItem(reloadTaser);

            #region PoliceBoot

            if (_permissionService.CurrentUserRole.Branch == UserBranch.Police)
            {
                MenuItem armour = new MenuItem("Body Armour");
            _bootMenu.AddMenuItem(armour);

            #region Speed Gun

            bool hasSpeedGun = playerPed.Weapons.HasWeapon(WeaponHash.VintagePistol);
            string speedGunLabel = hasSpeedGun ? "Return Speed Gun" : "Take Speed Gun";
            MenuItem speedGun = new MenuItem(speedGunLabel);
            _bootMenu.AddMenuItem(speedGun);

            #endregion

            #region Enforcer

            if (playerPed.Weapons.HasWeapon(WeaponHash.GolfClub) && _afoStatus)
            {
                // Has Enforcer
                _bootMenu.AddMenuItem(new MenuItem("Rack Enforcer"));
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.GolfClub) && _afoStatus)
            {
                _bootMenu.AddMenuItem(new MenuItem("Un Rack Enforcer"));
            }

            #endregion

            #region Baton Gun

            if (playerPed.Weapons.HasWeapon(WeaponHash.SawnOffShotgun))
            {
                // Has Baton Gun
                MenuItem rackBatonGun = new MenuItem("Rack Baton Gun");
                _bootMenu.AddMenuItem(rackBatonGun);
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.SawnOffShotgun) && _afoStatus)
            {
                if (_batonVehicle == vehicle.NetworkId)
                {
                    MenuItem unRackBatonGun = new MenuItem("Un Rack Baton Gun");
                    _bootMenu.AddMenuItem(unRackBatonGun);
                }
            }

            #endregion Baton Gun

            #region Combat Pistol

            if (playerPed.Weapons.HasWeapon(WeaponHash.CombatPistol))
            {
                MenuItem rackCombatPistol = new MenuItem("Rack SIG Sauer P226");
                _bootMenu.AddMenuItem(rackCombatPistol);
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.CombatPistol) && _afoStatus)
            {
                if (_combatPistolVehicle == vehicle.NetworkId)
                {
                    MenuItem unRackCombatPistol = new MenuItem("Un Rack SIG Sauer P226");
                    _bootMenu.AddMenuItem(unRackCombatPistol);
                }
            }

            #endregion

            #region Pistol

            if (playerPed.Weapons.HasWeapon(WeaponHash.Pistol))
            {
                MenuItem rackPistol = new MenuItem("Rack Glock G17");
                _bootMenu.AddMenuItem(rackPistol);
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.Pistol) && _afoStatus)
            {
                if (_pistolVehicle == vehicle.NetworkId)
                {
                    MenuItem unRackPistol = new MenuItem("Un Rack Glock G17");
                    _bootMenu.AddMenuItem(unRackPistol);
                }
            }

            #endregion

            #region G36C

            if (playerPed.Weapons.HasWeapon(WeaponHash.SpecialCarbine))
            {
                // Has G36C
                MenuItem rackG36C = new MenuItem("Rack G36C");
                _bootMenu.AddMenuItem(rackG36C);
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.SpecialCarbine) && _afoStatus)
            {
                if (_g36CVehicle == vehicle.NetworkId)
                {
                    MenuItem unRackG36C = new MenuItem("Un Rack G36C");
                    _bootMenu.AddMenuItem(unRackG36C);
                }
            }

            #endregion G36C

            #region L115A3

            if (playerPed.Weapons.HasWeapon(WeaponHash.SniperRifle))
            {
                _bootMenu.AddMenuItem(new MenuItem("Rack L115A3"));
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.SniperRifle) && _afoStatus)
            {
                if (_l115A3Vehicle == vehicle.NetworkId)
                {
                    _bootMenu.AddMenuItem(new MenuItem("Un Rack L115A3"));
                }
            }

            #endregion L115A3

            #region Marksman Rifle

            if (playerPed.Weapons.HasWeapon(WeaponHash.MarksmanRifle))
            {
                MenuItem rackMarksman = new MenuItem("Rack Scar DMR");
                _bootMenu.AddMenuItem(rackMarksman);
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.MarksmanRifle) && _afoStatus)
            {
                if (_marskmanVehicle == vehicle.NetworkId)
                {
                    MenuItem unRackMarksman = new MenuItem("Un Rack Scar DMR");
                    _bootMenu.AddMenuItem(unRackMarksman);
                }
            }

            #endregion

            #region MP5

            if (playerPed.Weapons.HasWeapon(WeaponHash.SMG))
            {
                _bootMenu.AddMenuItem(new MenuItem("Rack MP5"));
            }
            else if (!playerPed.Weapons.HasWeapon(WeaponHash.SMG) && _afoStatus)
            {
                if (_mp5Vehicle == vehicle.NetworkId)
                {
                    _bootMenu.AddMenuItem(new MenuItem("Un Rack MP5"));
                }
            }

            #endregion MP5

            #region Flashbangs

            WeaponHash flashBangHash = (WeaponHash)API.GetHashKey("WEAPON_FLASHBANG");

            if (playerPed.Weapons.HasWeapon(flashBangHash))
            {
                _bootMenu.AddMenuItem(new MenuItem("Store Flash Bangs"));
            }
            else if (!playerPed.Weapons.HasWeapon(flashBangHash) && _afoStatus)
            {
                if (_flashBangVehicle == vehicle.NetworkId)
                {
                    _bootMenu.AddMenuItem(new MenuItem("Get Flash Bangs"));
                }
            }

            #endregion

            #region DSU

            if (_dog.HasTakenFromKennel())
            {
                _bootMenu.AddMenuItem(!Dog.DogSpawned ? new MenuItem("Deploy Dog") : new MenuItem("Cage Dog"));
            }

            #endregion DSU

            #region Helmet

            var playerId = playerPed.Handle;

            bool hasHelmet = API.GetPedPropIndex(playerId, 0) == _afoHelmet;

            string helmetLabel = hasHelmet ? "Store Helmet" : "Take Helmet";

            MenuItem helmetItem = new MenuItem(helmetLabel);

            if (_afoStatus)
            {
                _bootMenu.AddMenuItem(helmetItem);
            }

            #endregion

            

            _bootMenu.OnItemSelect += (menu, item, index) =>
            {
                if (item == armour) playerPed.Armor = _afoStatus ? 100 : 50;
                if (item == reloadTaser)
                {
                    _comms.ToClient(ClientEvents.TaserReload);
                }
                if (item == helmetItem)
                {
                    if (hasHelmet)
                    {
                        API.SetPedPropIndex(playerId, 0, _helmetId, _helmetTexture, true);
                    }
                    else
                    {
                        _helmetId = API.GetPedPropIndex(playerId, 0);
                        _helmetTexture = API.GetPedPropTextureIndex(playerId, 0);
                        API.SetPedPropIndex(playerId, 0, _afoHelmet, 0, true);
                    }
                }

                if (item == speedGun)
                {
                    if (hasSpeedGun)
                    {
                        playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));
                        playerPed.Weapons.Remove(WeaponHash.VintagePistol);
                    }
                    else
                    {
                        playerPed.Weapons.Give(WeaponHash.VintagePistol, 1, true, true);
                    }
                }

                #region Enforcer

                if (item.Text == "Rack Enforcer")
                {
                    playerPed.Weapons.Remove(WeaponHash.GolfClub);
                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));
                }

                if (item.Text == "Un Rack Enforcer")
                {
                    if (!_afoStatus) return;
                    Game.PlayerPed.Weapons.Give(WeaponHash.GolfClub, 1, true, true);
                }

                #endregion

                #region Baton Gun

                if (item.Text == "Rack Baton Gun")
                {
                    playerPed.Weapons.Remove(WeaponHash.SawnOffShotgun);

                    _batonVehicle = vehicle.NetworkId;
                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));
                }

                if (item.Text == "Un Rack Baton Gun")
                {
                    if (!_afoStatus) return;

                    if (_batonVehicle != vehicle.NetworkId)
                    {
                        _notifications.Error("Error", "Unable to get a Baton Gun!");
                        return;
                    }

                    _batonVehicle = -1;

                    Game.PlayerPed.Weapons.Give(WeaponHash.SawnOffShotgun, 100, true, true);
                }

                #endregion Baton Gun

                #region G36C

                if (item.Text == "Rack G36C")
                {
                    playerPed.Weapons.Remove(WeaponHash.SpecialCarbine);

                    _g36CVehicle = vehicle.NetworkId;

                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));
                }

                if (item.Text == "Un Rack G36C")
                {
                    if (!_afoStatus) return;

                    if (_g36CVehicle != vehicle.NetworkId) return;

                    _g36CVehicle = -1;

                    Game.PlayerPed.Weapons.Give(WeaponHash.SpecialCarbine, 100, true, true);
                    API.GiveWeaponComponentToPed(playerPed.Handle, (uint)WeaponHash.SpecialCarbine, (uint)API.GetHashKey("COMPONENT_AT_AR_FLSH"));
                    API.GiveWeaponComponentToPed(playerPed.Handle, (uint)WeaponHash.SpecialCarbine, (uint)API.GetHashKey("COMPONENT_AT_SCOPE_MEDIUM"));
                }

                #endregion G36C

                #region L115A3

                if (item.Text == "Rack L115A3")
                {
                    playerPed.Weapons.Remove(WeaponHash.SniperRifle);

                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));

                    _l115A3Vehicle = vehicle.NetworkId;
                }

                if (item.Text == "Un Rack L115A3")
                {
                    if (!_afoStatus) return;

                    if (_l115A3Vehicle != vehicle.NetworkId) return;

                    _l115A3Vehicle = -1;
                    Game.PlayerPed.Weapons.Give(WeaponHash.SniperRifle, 50, true, true);
                    API.GiveWeaponComponentToPed(playerPed.Handle, (uint)WeaponHash.SniperRifle, (uint)API.GetHashKey("COMPONENT_AT_SCOPE_LARGE"));
                }

                #endregion L115A3

                #region MP5

                if (item.Text == "Rack MP5")
                {
                    playerPed.Weapons.Remove(WeaponHash.SMG);

                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));

                    _mp5Vehicle = vehicle.NetworkId;
                }

                if (item.Text == "Un Rack MP5")
                {
                    Game.PlayerPed.Weapons.Give(WeaponHash.SMG, 100, true, true);
                    API.GiveWeaponComponentToPed(playerPed.Handle, (uint)WeaponHash.SMG, (uint)API.GetHashKey("COMPONENT_AT_AR_FLSH"));
                    API.GiveWeaponComponentToPed(playerPed.Handle, (uint)WeaponHash.SMG, (uint)API.GetHashKey("COMPONENT_AT_SCOPE_MACRO_02"));
                }

                #endregion MP5

                #region FlashBangs

                if (item.Text == "Store Flash Bangs")
                {
                    playerPed.Weapons.Remove(flashBangHash);

                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));

                    _flashBangVehicle = vehicle.NetworkId;
                }

                if (item.Text == "Get Flash Bangs")
                {
                    Game.PlayerPed.Weapons.Give(flashBangHash, 5, true, true);
                }

                #endregion

                #region DSU

                string dogName = DsuNaming.FetchDogName();

                if (item.Text == "Deploy Dog")
                {
                    DeployDog?.Invoke(this, dogName);
                }

                if (item.Text == "Cage Dog")
                {
                    ReturnDog?.Invoke(this, EventArgs.Empty);
                }

                #endregion DSU

                #region Combat Pistol

                if (item.Text == "Rack SIG Sauer P226")
                {
                    playerPed.Weapons.Remove(WeaponHash.CombatPistol);
                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));
                    _combatPistolVehicle = vehicle.NetworkId;
                }

                if (item.Text == "Un Rack SIG Sauer P226")
                {
                    _combatPistolVehicle = -1;
                    Game.PlayerPed.Weapons.Give(WeaponHash.CombatPistol, 100, true, true);

                }

                #endregion

                #region Pistol

                if (item.Text == "Rack Glock G17")
                {
                    playerPed.Weapons.Remove(WeaponHash.Pistol);
                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));
                    _pistolVehicle = vehicle.NetworkId;
                }

                if (item.Text == "Un Rack Glock G17")
                {
                    _pistolVehicle = -1;
                    Game.PlayerPed.Weapons.Give(WeaponHash.Pistol, 100, true, true);

                }

                #endregion

                #region Scar DMR

                if (item.Text == "Rack Scar DMR")
                {
                    playerPed.Weapons.Remove(WeaponHash.MarksmanRifle);
                    playerPed.Weapons.Select((WeaponHash)API.GetHashKey("WEAPON_UNARMED"));
                    _marskmanVehicle = vehicle.NetworkId;
                }

                if (item.Text == "Un Rack Scar DMR")
                {
                    _marskmanVehicle = -1;
                    Game.PlayerPed.Weapons.Give(WeaponHash.MarksmanRifle, 50, true, true);

                }

                #endregion


                

                
            };
            }

            #endregion



            #region MedicalBoot

            if (_permissionService.CurrentUserRole.Branch == UserBranch.Nhs)
            {
                #region Defib

                if (API.HasPedGotWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ECG"), false))
                {
                    MenuItem rackDefib = new MenuItem("Rack Defib Kit");
                    _bootMenu.AddMenuItem(rackDefib);
                }
                else if (!API.HasPedGotWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ECG"), false))
                {
                    MenuItem unrackDefib = new MenuItem("Un Rack Defib Kit");
                    _bootMenu.AddMenuItem(unrackDefib);
                }

                #endregion

                #region MedBag

                if (API.HasPedGotWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ALS"), false))
                {
                    MenuItem rackMedic = new MenuItem("Rack Medic Kit");
                    _bootMenu.AddMenuItem(rackMedic);
                }
                else if (!API.HasPedGotWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ALS"), false))
                {
                    MenuItem unrackMedic = new MenuItem("Un Rack Medic Kit");
                    _bootMenu.AddMenuItem(unrackMedic);
                }

                #endregion


                _bootMenu.OnItemSelect += (menu, item, index) =>
                {
                    if (item.Text == "Rack Defib Kit")
                    {
                        API.RemoveWeaponFromPed(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ECG"));
                        playerPed.Weapons.Select((WeaponHash) API.GetHashKey("WEAPON_UNARMED"));
                    }

                    if (item.Text == "Un Rack Defib Kit")
                    {
                        API.GiveWeaponToPed(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ECG"), 1000, false,
                            true);
                        API.SetCurrentPedWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ECG"), true);
                    }

                    if (item.Text == "Rack Medic Kit")
                    {
                        API.RemoveWeaponFromPed(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ALS"));
                        playerPed.Weapons.Select((WeaponHash) API.GetHashKey("WEAPON_UNARMED"));
                    }

                    if (item.Text == "Un Rack Medic Kit")
                    {
                        API.GiveWeaponToPed(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ALS"), 1000, false,
                            true);
                        API.SetCurrentPedWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ALS"), true);
                    }
                };
            }

            #endregion
            _bootMenu.OnItemSelect += (menu, item, index) =>
            {
                if (item == firstAid) Game.PlayerPed.Health = 100;
                _bootMenu.CloseMenu();
            };

            _bootMenuLoading = false;
            
            _bootMenu.OnMenuClose += menu =>
            {
                _inputDisabled = false;
                Game.PlayerPed.Task.StandStill(1);
            };
        }

        public void OpenMenu(Vehicle vehicle)
        {
            _inputDisabled = true;
            Game.PlayerPed.Task.PlayAnimation("missexile3", "ex03_dingy_search_case_base_michael");

            _bootMenu.OpenMenu();
            HandleBootMenu(vehicle.Handle);
        }

        public bool IsMenuActive()
        {
            return _bootMenu?.Visible == true;
        }
    }
}