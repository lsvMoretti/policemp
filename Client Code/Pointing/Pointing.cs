using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Interface;
using PoliceMP.Core.Shared;

namespace PoliceMP.Client.Scripts.Pointing
{
    public class Pointing : Script
    {
        private readonly ILogger<Pointing> _logger;
        private readonly ITickManager _tickManager;
        private int _lastPressedPoint = 0;

        public Pointing(ILogger<Pointing> logger, ITickManager tickManager)
        {
            _logger = logger;
            _tickManager = tickManager;
        }
        
        protected override Task OnStartAsync()
        {
            _tickManager.On(OnPointTick);
            return Task.FromResult(0);
        }

        private async Task OnPointTick()
        {
            // Double Press right analogue stick
            if (Game.CurrentInputMode == InputMode.GamePad)
            {
                if (Game.IsControlJustReleased(0, Control.SpecialAbilitySecondary) && !Game.PlayerPed.IsInVehicle())
                {
                    if (API.GetGameTimer() - _lastPressedPoint < 300)
                    {
                        _lastPressedPoint = API.GetGameTimer();
                        await TogglePointing();
                    }
                    else
                    {
                        _lastPressedPoint = API.GetGameTimer();
                    }
                }
            }
            // Press the B button to toggle
            else
            {
                if (Game.IsControlJustReleased(0, Control.SpecialAbilitySecondary) && !Game.PlayerPed.IsInVehicle())
                {
                    await TogglePointing();
                }
            }

            var playerHandle = Game.PlayerPed.Handle;

            if (IsPedPointing(playerHandle))
            {
                if (Game.PlayerPed.IsInVehicle())
                {
                    Game.PlayerPed.Task.ClearSecondary();
                }
                else
                {
                    API.SetTaskMoveNetworkSignalFloat(playerHandle, "Pitch", GetPointingPitch());
                    API.SetTaskMoveNetworkSignalFloat(playerHandle, "Heading", GetPointingHeading());
                    API.SetTaskMoveNetworkSignalBool(playerHandle, "isBlocked", GetPointingIsBlocked());
                    API.SetTaskMoveNetworkSignalBool(playerHandle, "isFirstPerson", API.GetFollowPedCamViewMode() == 4);
                    API.SetTaskMoveNetworkSignalFloat(playerHandle, "Speed", 0.25f);
                }
            }
        }

        private float GetPointingPitch()
        {
            var pitch = API.GetGameplayCamRelativePitch();
            if (pitch < -70f)
            {
                pitch = -70f;
            }
            if (pitch > 42f)
            {
                pitch = 42f;
            }
            pitch += 70f;
            pitch /= 112f;

            return pitch;
        }
        private float GetPointingHeading()
        {
            var heading = API.GetGameplayCamRelativeHeading();
            if (heading < -180f)
            {
                heading = -180f;
            }
            if (heading > 180f)
            {
                heading = 180f;
            }
            heading += 180f;
            heading /= 360f;
            heading *= -1f;
            heading += 1f;

            return heading;
        }
        private bool GetPointingIsBlocked()
        {
            var hit = false;
            var rawHeading = API.GetGameplayCamRelativeHeading() / 90f;
            var heading = (float)MathUtil.Clamp(rawHeading, -180.0f, 180.0f);
            heading += 180.0f;
            heading /= 360.0f;
            var v1 = ((0.7f - 0.3f) * heading) + 0.3f;
            var pos0 = new Vector3(-0.2f, v1, 0.6f);
            var rot = new Vector3(0f, 0f, rawHeading);
            var vec1 = Vector3.Zero;

            // pos0, rot
            // ----
            var f0 = (float)Math.Cos(rot.X);
            var f1 = (float)Math.Sin(rot.X);
            vec1.X = pos0.X;
            vec1.Y = (f0 * pos0.Y) - (f1 * pos0.Z);
            vec1.Z = (f1 * pos0.Y) + (f0 * pos0.Z);
            pos0 = vec1;

            // ----
            f0 = (float)Math.Cos(rot.Y);
            f1 = (float)Math.Sin(rot.Y);
            vec1.X = (f0 * pos0.X) + (f1 * pos0.Z);
            vec1.Y = pos0.Y;
            vec1.Z = (f0 * pos0.Z) - (f1 * pos0.X);
            pos0 = vec1;

            // ----
            f0 = (float)Math.Cos(rot.Z);
            f1 = (float)Math.Sin(rot.Z);
            vec1.X = (f0 * pos0.X) - (f1 * pos0.Y);
            vec1.Y = (f1 * pos0.X) + (f0 * pos0.Y);
            vec1.Z = pos0.Z;
            pos0 = vec1;

            var pos1 = API.GetOffsetFromEntityInWorldCoords(Game.PlayerPed.Handle, pos0.X, pos0.Y, pos0.Z);
            var handle = API.StartShapeTestCapsule(pos1.X, pos1.Y, (pos1.Z - 0.2f), pos1.X, pos1.Y, (pos1.Z + 0.2f), 0.4f, 95, Game.PlayerPed.Handle, 7);
            var outPos = Vector3.Zero;
            var surfaceNormal = Vector3.Zero;
            var entityHit = 0;
            API.GetShapeTestResult(handle, ref hit, ref outPos, ref surfaceNormal, ref entityHit);
            return hit;
        }
        
        private async Task TogglePointing()
        {
            var handle = Game.PlayerPed.Handle;
            var task = "task_mp_pointing";
            var animDict = "anim@mp_point";
            if (IsPedPointing(handle))
            {
                Game.PlayerPed.Task.ClearSecondary();
                return;
            }

            if (!API.HasAnimDictLoaded(animDict))
            {
                API.RequestAnimDict(animDict);
            }

            while (!API.HasAnimDictLoaded(animDict))
            {
                await Delay(0);
            }
            API.TaskMoveNetwork(handle, task, 0.5f, false, animDict, 24);
            API.RemoveAnimDict(animDict);
        }
        
        private bool IsPedPointing(int handle)
        {
            return API.IsTaskMoveNetworkActive(handle);
        }
    }
}