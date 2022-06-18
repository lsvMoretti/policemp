using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;
using System.Linq;
using System.Threading.Tasks;

namespace PoliceMP.Client.Scripts.Weapons
{
    public class FlashBang : Script
    {
        private readonly IClientCommunicationsManager _comms;
        private readonly INotificationService _notification;
        private readonly ILogger<FlashBang> _logger;
        private readonly ITickManager _ticks;

        private const string AnimDict = "core";
        private const string AnimName = "ent_anim_paparazzi_flash";
        private bool FlashBangEquipped = false;
        private const string WeaponModel = "w_ex_flashbang";
        private string[] Animation = new string[] { "anim@heists@ornate_bank@thermal_charge", "cover_eyes_intro" };

        public FlashBang(IClientCommunicationsManager comms, INotificationService notification,
            ILogger<FlashBang> logger,
            ITickManager ticks)
        {
            _comms = comms;
            _notification = notification;
            _logger = logger;
            _ticks = ticks;
        }

        protected override Task OnStartAsync()
        {
            LoadWeaponInfo();
            _comms.On<float, float, float, int, int, float, int>(ClientEvents.SendFlashBangEventToClient, OnReceiveFlashBangEvent);
            _ticks.On(FlashBangTick);
            return Task.FromResult(0);
        }

        private async Task FlashBangTick()
        {
            if (!FlashBangEquipped)
            {
                if (Game.Player.Character.Weapons.Current.Hash == (WeaponHash)API.GetHashKey("WEAPON_FLASHBANG"))
                {
                    FlashBangEquipped = true;
                }
            }
            else
            {
                if (Game.Player.Character.IsShooting)
                {
                    FlashBangEquipped = false;

                    await BaseScript.Delay(100);

                    Vector3 pos = Game.Player.Character.Position;
                    int handle = API.GetClosestObjectOfType(pos.X, pos.Y, pos.Z, 50f,
                        (uint)API.GetHashKey(WeaponModel), false, false, false);
                    if (handle != 0)
                    {
                        FlashBangThrown(handle);
                    }
                }
            }
        }

        private async void OnReceiveFlashBangEvent(float posX, float posY, float posZ, int stunTime, int afterTime,
            float radius, int flashBangNetworkId)
        {
            int stunRefTime = stunTime * 1000;
            int afterRefTime = afterTime * 1000;
            int finishTime = 0;
            Ped ped = Game.Player.Character;
            Vector3 pos = new Vector3(posX, posY, posZ);

            PlayParticles(pos);

            HandleNearbyPeds(pos, stunTime, afterTime);

            float distance = World.GetDistance(ped.Position, pos);

            int handle = API.GetClosestObjectOfType(pos.X, pos.Y, pos.Z, 50f,
                (uint)API.GetHashKey(WeaponModel), false, false, false);

            _logger.Debug($"Flashbang Handle: {handle}");

            if (distance <= radius)
            {
                if (handle != 0)
                {
                    _logger.Debug("Prop Not Null");
                    if (API.HasEntityClearLosToEntityInFront(Game.PlayerPed.Handle, handle))
                    {
                        _logger.Debug("Has clear LOS");
                        Screen.Effects.Start(ScreenEffect.DontTazemeBro, 0, true);
                        GameplayCamera.Shake(CameraShake.Hand, 15f);
                        await ped.Task.PlayAnimation(Animation[0], Animation[1], -8f, -8f, -1, AnimationFlags.StayInEndFrame | AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation, 8f);
                        finishTime = Game.GameTime + stunRefTime;
                        while (Game.GameTime < finishTime)
                        {
                            Game.Player.DisableFiringThisFrame();
                            await Delay(0);
                        }
                        ped.Task.ClearAnimation(Animation[0], Animation[1]);
                        GameplayCamera.ShakeAmplitude = 10f;
                        finishTime = Game.GameTime + afterRefTime;
                        while (Game.GameTime < finishTime)
                        {
                            await Delay(0);
                        }
                        GameplayCamera.StopShaking();
                        Screen.Effects.Stop(ScreenEffect.DontTazemeBro);
                    }
                }
            }
        }

        private async void HandleNearbyPeds(Vector3 pos, int stunTime, int afterTime)
        {
            int stunRefTime = stunTime * 1000;

            int targetPedCount = 0;

            foreach (Ped targetPed in World.GetAllPeds().ToList())
            {
                if (API.IsPedAPlayer(targetPed.Handle)) continue;
                if (!API.NetworkHasControlOfEntity(targetPed.Handle)) continue;

                float distance = World.GetDistance(targetPed.Position, pos);

                if (distance <= 15)
                {
                    targetPedCount++;
                    API.TaskSetBlockingOfNonTemporaryEvents(targetPed.Handle, true);
                    API.SetPedFleeAttributes(targetPed.Handle, 0, false);
                    API.SetPedCombatAttributes(targetPed.Handle, 17, true);

                    await targetPed.Task.PlayAnimation("random@homelandsecurity", "knees_loop_girl", -8f, -8f, stunRefTime,
                        AnimationFlags.Loop | AnimationFlags.AllowRotation,
                        8f);

                    /*await targetPed.Task.PlayAnimation("random@domestic", "f_distressed_loop", -8f, -8f, stunRefTime,
                    AnimationFlags.StayInEndFrame | AnimationFlags.UpperBodyOnly | AnimationFlags.AllowRotation,
                    8f);*/
                }
            }

            _logger.Debug($"Total peds affected: {targetPedCount}");

            await BaseScript.Delay(stunRefTime);

            foreach (Ped targetPed in World.GetAllPeds().ToList())
            {
                if (API.IsPedAPlayer(targetPed.Handle)) continue;
                if (!API.NetworkHasControlOfEntity(targetPed.Handle)) continue;

                float distance = World.GetDistance(targetPed.Position, pos);

                if (distance <= 15)
                {
                    API.TaskSetBlockingOfNonTemporaryEvents(targetPed.Handle, false);
                    targetPed.Task.ClearAnimation("random@homelandsecurity", "knees_loop_girl");
                }
            }
        }

        private async void PlayParticles(Vector3 pos)
        {
            API.RequestNamedPtfxAsset(AnimDict);
            while (!API.HasNamedPtfxAssetLoaded(AnimDict))
            {
                await Delay(0);
            }
            API.UseParticleFxAssetNextCall(AnimDict);
            API.StartParticleFxLoopedAtCoord(AnimName, pos.X, pos.Y, pos.Z, 0f, 0f, 0f, 25f, false, false, false, false);
        }

        private async void FlashBangThrown(int prop)
        {
            Prop flashBang = (Prop)Entity.FromHandle(prop);
            await Delay(2500);
            Vector3 flashBangPosition = flashBang.Position;
            World.AddExplosion(flashBangPosition, ExplosionType.ProgramAR, 0f, 1f, null, true, true);
            _comms.ToServer(ServerEvents.SendFlashBangEventToServer, flashBangPosition.X, flashBangPosition.Y, flashBangPosition.Z, flashBang.NetworkId);
            flashBang.Delete();
        }

        private void LoadWeaponInfo()
        {
            if (API.IsWeaponValid(4221696920))
            {
                API.AddTextEntry("WT_GNADE_FLSH", "Flashbang");
            }
        }
    }
}