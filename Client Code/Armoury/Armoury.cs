using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
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
using PoliceMP.Client.Scripts.Civ;
using PoliceMP.Shared.Enums;

namespace PoliceMP.Client.Scripts.Armoury
{
    public class Armoury : Script, IArmoury
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<Armoury> _logger;
        private readonly INotificationService _notifications;
        private readonly ITickManager _ticks;
        private readonly IPermissionService _permissionService;
        private bool _isInMenu = false;
        private Menu _armouryMenu = new("Armoury");
        private UserAces _userAces;
        private DateTime _lastMenuTime = DateTime.Now;

        private readonly List<Vector3> _armouryLocations = new()
        {
            // Mission Row Armory
            new Vector3(452.39f, -980.05f,30.68f),
            // Vespucci PD
            new Vector3(-1098.78f, -826.07f, 14.28f),
            // Vinewood PD
            new Vector3(621.36f, -18.84f, 82.78f),
            new Vector3(1862.6f, 3689.64f, 34.27f),
            // Heathrow
            new(-879.81481933594f,-2382.6013183594f,14.065853118896f)
        };

        public Armoury(IClientCommunicationsManager comms,
            ILogger<Armoury> logger,
            INotificationService notifications,
            ITickManager ticks,
            IPermissionService permissionService)
        {
            _comms = comms;
            _logger = logger;
            _notifications = notifications;
            _ticks = ticks;
            _permissionService = permissionService;
        }

        protected override async Task OnStartAsync()
        {
            _userAces = await _permissionService.GetUserAces();

            if (_userAces.IsAdmin || _userAces.IsDeveloper || _userAces.IsAfoTrained)
            {
                _logger.Debug("Able to access Armoury");

                MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
                MenuController.AddMenu(_armouryMenu);

                _armouryMenu.InstructionalButtons.Clear();

                MenuController.EnableMenuToggleKeyOnController = false;
                MenuController.MenuToggleKey = (Control)(-1);

                _armouryMenu.OnMenuOpen += menu =>
                {
                    if (menu == _armouryMenu)
                    {
                        _isInMenu = true;
                    }
                };

                _armouryMenu.OnMenuClose += menu =>
                {
                    if (menu == _armouryMenu)
                    {
                        _isInMenu = false;
                    }
                };
            }

            _ticks.On(ArmouryTick);
            _ticks.On(KeepInHandChecker);
        }

        private async Task KeepInHandChecker()
        {
            //Keep the medbag in hand
            if (API.HasPedGotWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ALS"), false))
            {
                API.SetCurrentPedWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ALS"), true);
            }
            //Keep the defib in hand
            else if (API.HasPedGotWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ECG"), false))
            {
                API.SetCurrentPedWeapon(Game.PlayerPed.Handle, (uint) API.GetHashKey("WEAPON_ECG"), true);
            }
        }

        private async Task ArmouryTick()
        {
            await HandleInteractionPoints();
        }

        public void ReloadArmouryMenu()
        {
            if (DateTime.Compare(DateTime.Now, _lastMenuTime.AddSeconds(5)) <= 0) return;

            var ped = Game.PlayerPed.Handle;

            _armouryMenu.ClearMenuItems();

            bool hasCombatPistol = Game.PlayerPed.Weapons.HasWeapon(WeaponHash.CombatPistol);
            string combatPistolLabel = hasCombatPistol ? "Return SIG Sauer P226" : "Take SIG Sauer P226";
            var combatPistol = new MenuItem(combatPistolLabel);

            bool hasPistol = Game.PlayerPed.Weapons.HasWeapon(WeaponHash.Pistol);
            string pistolLabel = hasPistol ? "Return Glock G17" : "Take Glock G17";
            MenuItem pistol = new MenuItem(pistolLabel); ;

            bool hasSnsPistol = Game.PlayerPed.Weapons.HasWeapon(WeaponHash.SNSPistol);
            string snsLabel = hasSnsPistol ? "Return Chimano 88" : "Take Chimano 88";
            MenuItem snsPistol = new MenuItem(snsLabel);

            if (!hasPistol && !hasSnsPistol)
            {
                _armouryMenu.AddMenuItem(combatPistol);
            }

            if (!hasCombatPistol && !hasSnsPistol)
            {
                _armouryMenu.AddMenuItem(pistol);
            }

            if (!hasPistol && !hasCombatPistol)
            {
                _armouryMenu.AddMenuItem(snsPistol);
            }

            bool hasBatonGun = Game.PlayerPed.Weapons.HasWeapon(WeaponHash.SawnOffShotgun);
            string batonGunLabel = hasBatonGun
                ? "Return Baton Gun"
                : "Take Baton Gun";
            var batonGun = new MenuItem(batonGunLabel);
            _armouryMenu.AddMenuItem(batonGun);
            
            bool hasG36C = Game.PlayerPed.Weapons.HasWeapon(WeaponHash.SpecialCarbine);
            string g36GunLabel = hasG36C
                ? "Return G36C"
                : "Take G36C";
            var g36C = new MenuItem(g36GunLabel);
            _armouryMenu.AddMenuItem(g36C);
            
            var hasMp5 = Game.PlayerPed.Weapons.HasWeapon((WeaponHash) API.GetHashKey("WEAPON_LIVEMP5SEMI"));
            string mp5Label = hasMp5 ? "Return H&K MP5" : "Take H&K MP5";
            var mp5 = new MenuItem(mp5Label);
            _armouryMenu.AddMenuItem(mp5);

            var hasL115A1 = Game.PlayerPed.Weapons.HasWeapon(WeaponHash.SniperRifle);

            string l115A1Label = hasL115A1 ? "Return L115A1" : "Take L115A1";

            var l115A1 = new MenuItem(l115A1Label);
            
            var hasScarDMR = Game.PlayerPed.Weapons.HasWeapon(WeaponHash.MarksmanRifle);
            string scarDmrLabel = hasScarDMR ? "Return Scar DMR" : "Take Scar DMR";
            var scarDmr = new MenuItem(scarDmrLabel);

            if (!hasScarDMR)
            {
                _armouryMenu.AddMenuItem(l115A1);
            }

            if (!hasL115A1)
            {
                _armouryMenu.AddMenuItem(scarDmr);
            }

            var hasMcx = Game.PlayerPed.Weapons.HasWeapon((WeaponHash) API.GetHashKey("WEAPON_SIGMCX"));
            string mcxLabel = hasMcx ? "Return Sig Sauer MCX" : "Take Sig Sauer MCX";
            var mcxItem = new MenuItem(mcxLabel);
            _armouryMenu.AddMenuItem(mcxItem);
            
            var hasFlashBang = Game.PlayerPed.Weapons.HasWeapon((WeaponHash)API.GetHashKey("WEAPON_FLASHBANG"));
            string flashBangLabel = hasFlashBang ? "Return Flash Bangs" : "Take Flash Bangs (x5)";
            var flashBangItem = new MenuItem(flashBangLabel);
            _armouryMenu.AddMenuItem(flashBangItem);
            
            var hasMp5Training = Game.PlayerPed.Weapons.HasWeapon((WeaponHash) API.GetHashKey("WEAPON_TRAININGMP5SEMI"));
            string mp5TrainingLabel = hasMp5Training ? "Return H&K MP5 Training Rifle" : "Take H&K MP5 Training Rifle";
            var mp5TrainingItem = new MenuItem(mp5TrainingLabel);
            _armouryMenu.AddMenuItem(mp5TrainingItem);
            
            _armouryMenu.OnItemSelect += (menu, item, index) =>
            {
                _armouryMenu.CloseMenu();

                if (item == combatPistol)
                {
                    if (hasCombatPistol)
                    {
                        Game.PlayerPed.Weapons.Remove(WeaponHash.CombatPistol);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(WeaponHash.CombatPistol, 100, true, true);
                    }
                }

                if (item == pistol)
                {
                    if (hasPistol)
                    {
                        Game.PlayerPed.Weapons.Remove(WeaponHash.Pistol);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(WeaponHash.Pistol, 100, true, true);
                    }
                }

                if (item == snsPistol)
                {
                    if (hasSnsPistol)
                    {
                        Game.PlayerPed.Weapons.Remove(WeaponHash.SNSPistol);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(WeaponHash.SNSPistol, 100, true, true);
                    }
                }


                if (item == batonGun)
                {
                    if (hasBatonGun)
                    {
                        Game.PlayerPed.Weapons.Remove(WeaponHash.SawnOffShotgun);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(WeaponHash.SawnOffShotgun, 100, true, true);
                    }
                }

                if (item == g36C)
                {
                    if (hasG36C)
                    {
                        Game.PlayerPed.Weapons.Remove(WeaponHash.SpecialCarbine);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(WeaponHash.SpecialCarbine, 100, true, true);
                        API.GiveWeaponComponentToPed(ped, (uint)WeaponHash.SpecialCarbine, (uint)API.GetHashKey("COMPONENT_AT_AR_FLSH"));
                        API.GiveWeaponComponentToPed(ped, (uint)WeaponHash.SpecialCarbine, (uint)API.GetHashKey("COMPONENT_AT_SCOPE_MEDIUM"));
                    }
                }

                if (item == mp5)
                {
                    var mp5Hash = (WeaponHash) API.GetHashKey("WEAPON_LIVEMP5SEMI");
                    if (hasMp5)
                    {
                        Game.PlayerPed.Weapons.Remove(mp5Hash);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(mp5Hash, 100, true, true);
                        API.GiveWeaponComponentToPed(ped, (uint)mp5Hash, (uint)API.GetHashKey("COMPONENT_MP5_LIVEAUTO_CLIP_02"));
                        API.GiveWeaponComponentToPed(ped, (uint)mp5Hash, (uint)API.GetHashKey("COMPONENT_AT_AR_FLSH"));
                        API.GiveWeaponComponentToPed(ped, (uint)mp5Hash, (uint)API.GetHashKey("COMPONENT_AT_SIGHT_MP5"));
                    }
                }
                
                if (item == l115A1)
                {
                    if (hasL115A1)
                    {
                        Game.PlayerPed.Weapons.Remove(WeaponHash.SniperRifle);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(WeaponHash.SniperRifle, 50, true, true);
                        API.GiveWeaponComponentToPed(ped, (uint)WeaponHash.SniperRifle, (uint)API.GetHashKey("COMPONENT_AT_SCOPE_LARGE"));
                    }
                }

                if (item == scarDmr)
                {
                    if (hasScarDMR)
                    {
                        Game.PlayerPed.Weapons.Remove(WeaponHash.MarksmanRifle);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(WeaponHash.MarksmanRifle, 50, true, true);

                    }
                }

                if (item == mcxItem)
                {
                    if (hasMcx)
                    {
                        Game.PlayerPed.Weapons.Remove((WeaponHash)API.GetHashKey("WEAPON_SIGMCX"));
                    }
                    else
                    {
                        var sigHash = (WeaponHash) API.GetHashKey("WEAPON_SIGMCX");
                        Game.PlayerPed.Weapons.Give(sigHash, 100, true, true);
                        API.GiveWeaponComponentToPed(ped, (uint)sigHash, (uint)API.GetHashKey("COMPONENT_SIGMCX_CLIP_01"));
                        API.GiveWeaponComponentToPed(ped, (uint)sigHash, (uint)API.GetHashKey("COMPONENT_AT_AR_SUREFIRE_MCX"));
                        API.GiveWeaponComponentToPed(ped, (uint)sigHash, (uint)API.GetHashKey("COMPONENT_AT_SCOPE_REDDOT"));
                        API.GiveWeaponComponentToPed(ped, (uint)sigHash, (uint)API.GetHashKey("COMPONENT_AT_AR_VERTGRIP"));
                    }
                }
                
                if (item == mp5TrainingItem)
                {
                    var mp5TrainingHash = (WeaponHash) API.GetHashKey("WEAPON_TRAININGMP5SEMI");
                    if (hasMp5Training)
                    {
                        Game.PlayerPed.Weapons.Remove(mp5TrainingHash);
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give(mp5TrainingHash, 100, true, true);
                        API.GiveWeaponComponentToPed(ped, (uint)mp5TrainingHash, (uint)API.GetHashKey("COMPONENT_MP5_TRAININGAUTO_CLIP_01"));
                        API.GiveWeaponComponentToPed(ped, (uint)mp5TrainingHash, (uint)API.GetHashKey("COMPONENT_AT_AR_FLSH"));
                        API.GiveWeaponComponentToPed(ped, (uint)mp5TrainingHash, (uint)API.GetHashKey("COMPONENT_AT_SIGHT_MP5"));
                    }
                }
                

                if (item == flashBangItem)
                {
                    if (hasFlashBang)
                    {
                        Game.PlayerPed.Weapons.Remove((WeaponHash)API.GetHashKey("WEAPON_FLASHBANG"));
                    }
                    else
                    {
                        Game.PlayerPed.Weapons.Give((WeaponHash)API.GetHashKey("WEAPON_FLASHBANG"), 5, true, true);
                    }
                }
            };

            _lastMenuTime = DateTime.Now;
            _armouryMenu.OpenMenu();
        }

        private async Task HandleInteractionPoints()
        {
            // if (!_userAces.IsAdmin || !_userAces.IsDeveloper)
            // {
            //     if (userRole.Division != UserDivision.Afo) return;
            // }

            if (MenuController.IsAnyMenuOpen()) return;

            Vector3 playerPos = Game.PlayerPed.Position;

            foreach (Vector3 armouryLocation in _armouryLocations)
            {
                float distance = API.GetDistanceBetweenCoords(playerPos.X, playerPos.Y, playerPos.Z, armouryLocation.X, armouryLocation.Y, armouryLocation.Z, true);
                float scale = 0.1F * API.GetGameplayCamFov();

                if (distance < 5.0f)
                {
                    API.DrawMarker(1, armouryLocation.X, armouryLocation.Y, armouryLocation.Z - 1, 0, 0, 0, 0, 0, 0, 1F, 1F, 2F, 232, 232, 0, 50,
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
                    API.AddTextComponentString($"Police Armoury");
                    API.SetDrawOrigin(armouryLocation.X, armouryLocation.Y, armouryLocation.Z + 1F, 0);
                    API.DrawText(0, 0);
                    API.ClearDrawOrigin();

                    if (!(distance < 1.0f)) continue;

                    ReloadArmouryMenu();
                }
            }

            return;
        }
    }
}