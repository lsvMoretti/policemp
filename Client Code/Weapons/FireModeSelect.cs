using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using PoliceMP.Core.Client;

namespace PoliceMP.Client.Scripts.Weapons
{
    public enum FireMode
    {
        Safety,
        Single,
        Burst,
        Auto
    }
    
    public class FireModeSelect : Script
    {
        #region Variables

        private FireMode _fireMode = FireMode.Safety;
        private int _switchFiringModeKey = 7;
        private int _safetyToggleKey = 311;
        private WeaponHash _lastWeapon = WeaponHash.Unarmed;

        private Dictionary<WeaponHash, List<FireMode>> _weapons = new Dictionary<WeaponHash, List<FireMode>>
        {
            {WeaponHash.CombatPistol, new List<FireMode> {FireMode.Safety, FireMode.Single}}, //SIG Sauer P226
            {WeaponHash.Pistol, new List<FireMode> {FireMode.Safety, FireMode.Single}}, // GLock 17
            {WeaponHash.SNSPistol, new List<FireMode> {FireMode.Safety, FireMode.Single}}, // Chimano 88
            {WeaponHash.SpecialCarbine, new List<FireMode> {FireMode.Safety, FireMode.Single, FireMode.Burst, FireMode.Auto}}, // G36C
            {(WeaponHash) API.GetHashKey("WEAPON_LIVEMP5SEMI"), new List<FireMode> {FireMode.Safety, FireMode.Single}}, // Live MP5
            {WeaponHash.SniperRifle, new List<FireMode> {FireMode.Safety, FireMode.Single}}, // L115A1
            {(WeaponHash) API.GetHashKey("WEAPON_SIGMCX"), new List<FireMode> {FireMode.Safety, FireMode.Single}}, // MCX
            {(WeaponHash) API.GetHashKey("WEAPON_TRAININGMP5SEMI"), new List<FireMode> {FireMode.Safety, FireMode.Single}}, // Training MP5
        };
        
        #endregion
        
        public FireModeSelect()
        {
            Tick += FireModeOnTick;
            Tick += ShowCurrentMode;
        }

        private async Task FireModeOnTick()
        {
            if (!API.HasStreamedTextureDictLoaded("mpweaponsgang0"))
            {
                API.RequestStreamedTextureDict("mpweaponsgang0", true);
                while (!API.HasStreamedTextureDictLoaded("mpweaponsgang0"))
                {
                    await Delay(10);
                }
            }

            var currentSelectedWeapon = Game.PlayerPed.Weapons.Current;

            if (currentSelectedWeapon != _lastWeapon && _fireMode != FireMode.Safety)
            {
                _fireMode = FireMode.Safety;
            }

            _lastWeapon = currentSelectedWeapon;
            
            if (currentSelectedWeapon.Hash == WeaponHash.Unarmed) return;

            if (Game.PlayerPed.CurrentVehicle != null) return;

            var isFireModeWeapon = _weapons.TryGetValue(currentSelectedWeapon.Hash, out List<FireMode> fireModes);

            if (!isFireModeWeapon) return;

            if (_fireMode == FireMode.Safety)
            {
                API.DisablePlayerFiring(API.PlayerId(), true);
                if (API.IsDisabledControlJustPressed(0, 24))
                {
                    Screen.ShowNotification("~r~Weapon safety mode is enabled!~n~~w~Press ~y~K ~w~to switch it off.",
                        true);
                    API.PlaySoundFrontend(-1, "Place_Prop_Fail", "DLC_Dmod_Prop_Editor_Sounds", false);
                }
            }
            
            // If the player pressed L (7/Slowmotion Cinematic Camera Button) ON KEYBOARD ONLY(!) then switch to the next firing mode.
            if (API.IsInputDisabled(2) && API.IsControlJustPressed(0, _switchFiringModeKey))
            {
                switch (_fireMode)
                {
                    case FireMode.Safety:
                        if (fireModes.Contains(FireMode.Single))
                        {
                            _fireMode = FireMode.Single;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~single shot~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        if (fireModes.Contains(FireMode.Burst))
                        {
                            _fireMode = FireMode.Burst;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~burst fire~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        if (fireModes.Contains(FireMode.Auto))
                        {
                            _fireMode = FireMode.Auto;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~full auto fire~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        break;
                    case FireMode.Single:
                        if (fireModes.Contains(FireMode.Burst))
                        {
                            _fireMode = FireMode.Burst;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~burst fire~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        if (fireModes.Contains(FireMode.Auto))
                        {
                            _fireMode = FireMode.Auto;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~full auto fire~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        break;
                    case FireMode.Burst:
                        if (fireModes.Contains(FireMode.Auto))
                        {
                            _fireMode = FireMode.Auto;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~full auto fire~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        if (fireModes.Contains(FireMode.Single))
                        {
                            _fireMode = FireMode.Single;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~single shot~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        break;
                    case FireMode.Auto:
                        if (fireModes.Contains(FireMode.Single))
                        {
                            _fireMode = FireMode.Single;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~single shot~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        if (fireModes.Contains(FireMode.Burst))
                        {
                            _fireMode = FireMode.Burst;
                            Screen.ShowSubtitle("Weapon firing mode switched to ~b~burst fire~w~.", 3000);
                            API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                            break;
                        }
                        break;
                }
            }
            if (API.IsInputDisabled(2) && API.IsControlJustPressed(0, _safetyToggleKey))
            {
                if (_fireMode == FireMode.Safety)
                {
                    if (fireModes.Contains(FireMode.Single))
                    {
                        _fireMode = FireMode.Single;
                        Screen.ShowSubtitle("Weapon firing mode switched to ~b~single shot~w~.", 3000);
                        API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                    }
                    else if (fireModes.Contains(FireMode.Burst))
                    {
                        _fireMode = FireMode.Burst;
                        Screen.ShowSubtitle("Weapon firing mode switched to ~b~burst fire~w~.", 3000);
                        API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                    }
                    else if (fireModes.Contains(FireMode.Auto))
                    {
                        _fireMode = FireMode.Auto;
                        Screen.ShowSubtitle("Weapon firing mode switched to ~b~full auto fire~w~.", 3000);
                        API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
                    }
                }
                else
                {
                    _fireMode = FireMode.Safety;
                }
                Screen.ShowSubtitle("~y~Weapon safety mode ~g~" + (_fireMode == FireMode.Safety ? "~g~enabled" : "~r~disabled") + "~y~.", 3000);
                API.PlaySoundFrontend(-1, "Place_Prop_Success", "DLC_Dmod_Prop_Editor_Sounds", false);
            }
            
            // Handling Fire Modes
            if (_fireMode == FireMode.Single)
            {
                if (API.IsControlJustPressed(0, 24))
                {
                    // ...disable the weapon after the first shot and keep it disabled as long as the trigger is being pulled.
                    // once the player lets go of the trigger, the loop will stop and they can pull it again.
                    while (API.IsControlPressed(0, 24) || API.IsDisabledControlPressed(0, 24))
                    {
                        API.DisablePlayerFiring(API.PlayerId(), true);

                        // Because we're now in a while loop, we need to add a delay to prevent the game from freezing up/crashing.
                        await Delay(0);
                    }
                }
            }
            else if (_fireMode == FireMode.Burst)
            {
                // If the player starts shooting... 
                if (API.IsControlJustPressed(0, 24))
                {
                    // ...wait 300ms(for most guns this allows about 3 bullets to be shot when holding down the trigger)
                    await Delay(300);

                    // After that, if the user is still pulling the trigger, disable shooting for the player while still allowing them to aim.
                    // As soon as the user lets go of the trigger, this while loop will be stopped and the user can pull the trigger again.
                    while (API.IsControlPressed(0, 24) || API.IsDisabledControlPressed(0, 24))
                    {
                        API.DisablePlayerFiring(API.PlayerId(), true);

                        // Because we're now in a while loop, we need to add a delay to prevent the game from freezing up/crashing.
                        await Delay(0);
                    }
                }
            }
        }
        
        /// <summary>
        /// Used to draw text ont the screen on the specified x,y
        /// </summary>
        /// <param name="text"></param>
        /// <param name="posx"></param>
        /// <param name="posy"></param>
        private void ShowText(string text, float posx, float posy)
        {
            API.SetTextFont(4);
            API.SetTextScale(0.0f, 0.31f);
            API.SetTextJustification(1);
            API.SetTextColour(250, 250, 120, 255);
            API.SetTextDropshadow(1, 255, 255, 255, 255);
            API.SetTextEdge(1, 0, 0, 0, 205);
            API.BeginTextCommandDisplayText("STRING");
            API.AddTextComponentSubstringPlayerName(text);
            API.EndTextCommandDisplayText(posx, posy);
        }
        
        
        /// <summary>
        /// Show the current firing mode visually just below the ammo count.
        /// Called every frame.
        /// </summary>
        private async Task ShowCurrentMode()
        {
            if (Game.PlayerPed.CurrentVehicle != null) return;
            
            var currentWeaponHash = Game.PlayerPed.Weapons.Current.Hash;

            // Just add a wait in here when it's not being displayed, to remove the async warnings. 
            if (!_weapons.ContainsKey(currentWeaponHash))
            {
                await Delay(0);
            }
            // If the weapon is a valid weapon that has different firing modes, then this will be shown.
            else
            {
                switch (_fireMode)
                {
                    case FireMode.Safety:
                        ShowText(" ~r~X", 0.975f, 0.065f);
                        break;
                    case FireMode.Single:
                        ShowText("|", 0.975f, 0.065f);
                        break;
                    case FireMode.Burst:
                        ShowText("||", 0.975f, 0.065f);
                        break;
                    default:
                        ShowText("|||", 0.975f, 0.065f);
                        break;
                }
                API.DrawSprite("mpweaponsgang0", "w_ar_carbinerifle_mag1", 0.975f, 0.06f, 0.099f, 0.099f, 0.0f, 200, 200, 200, 255);
            }
        }

        protected override async Task OnStartAsync()
        {
            
        }
    }
}