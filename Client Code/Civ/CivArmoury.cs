using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Shared;

namespace PoliceMP.Client.Scripts.Civ
{
    public interface ICivArmoury
    {
        Task ShowCivArmouryMenu();
    }

    public class CivArmoury : Script, ICivArmoury
    {
        #region Services

        private readonly ILogger<CivArmoury> _logger;
        private readonly IPermissionService _permissionService;
        
        #endregion

        #region Variables

        private Menu _menu;

        #endregion
        
        public CivArmoury(ILogger<CivArmoury> logger, IPermissionService permissionService)
        {
            _logger = logger;
            _permissionService = permissionService;
        }

        public async Task ShowCivArmouryMenu()
        {
            _logger.Debug("Showing Civ Armoury Menu");

            _menu?.ClearMenuItems();

            var userAces = await _permissionService.GetUserAces();

            _menu = new Menu("Civ Armoury", "Get Guns Bro");
            
            MenuController.AddMenu(_menu);

            
            #region Meele

            _menu.AddMenuItem(new MenuItem("Baseball Bat") { ItemData = WeaponHash.Bat });

            _menu.AddMenuItem(new MenuItem("Broken Bottle"){ ItemData = WeaponHash.Bottle });

            _menu.AddMenuItem(new MenuItem("Crowbar"){ ItemData = WeaponHash.Crowbar });

            _menu.AddMenuItem(new MenuItem("Hammer"){ ItemData = WeaponHash.Hammer });
            
            _menu.AddMenuItem(new MenuItem("Switch Blade") { ItemData = WeaponHash.SwitchBlade});
            
            _menu.AddMenuItem(new MenuItem("Knife") { ItemData = WeaponHash.Knife });

            _menu.AddMenuItem(new MenuItem("Wrench"){ ItemData = WeaponHash.Wrench });
            
            _menu.AddMenuItem(new MenuItem("Pool Cue") { ItemData = WeaponHash.PoolCue });
            
            _menu.AddMenuItem(new MenuItem("Machete") { ItemData = WeaponHash.Machete });
            

            #endregion

            #region Throwables

            _menu.AddMenuItem(new MenuItem("Flare") { ItemData = WeaponHash.Flare });

            #endregion

            _menu.AddMenuItem(new MenuItem("Petrol Can"){ ItemData = WeaponHash.PetrolCan });

            #region Weapons

            if (userAces.IsCivGunTrained || userAces.IsAdmin || userAces.IsDeveloper)
            {
                _menu.AddMenuItem(new MenuItem("Fake Gun") { ItemData = (WeaponHash)(uint)API.GetHashKey("WEAPON_GADGETPISTOL")});
                _menu.AddMenuItem(new MenuItem("Flare Gun"){ItemData = WeaponHash.FlareGun});
                _menu.AddMenuItem(new MenuItem("Pistol"){ItemData = WeaponHash.Pistol});
                _menu.AddMenuItem(new MenuItem("Molotov") {ItemData = WeaponHash.Molotov});
                _menu.AddMenuItem(new MenuItem("Compact Rifle") { ItemData = WeaponHash.CompactRifle});
                _menu.AddMenuItem(new MenuItem("Old Revolver") { ItemData = (WeaponHash)(uint)API.GetHashKey("WEAPON_NAVYREVOLVER")});
                _menu.AddMenuItem(new MenuItem("Tommy Gun") {ItemData = WeaponHash.Gusenberg});
            }

            #endregion

            _menu.OpenMenu();

            _menu.OnItemSelect += (menu, item, index) =>
            {
                if (menu != _menu) return;

                var weaponData = (WeaponHash) item.ItemData;

                Game.PlayerPed.Weapons.Give(weaponData, 100, true, true);
                
                menu.CloseMenu();
            };
        }
    }
}