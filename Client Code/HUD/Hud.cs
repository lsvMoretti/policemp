using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Commands.Interfaces;
using PoliceMP.Core.Client.Interface;

namespace PoliceMP.Client.Scripts.HUD
{
    public class Hud : Script
    {
        private readonly ITickManager _ticks;
        private bool _seatbeltState = false;
        private float _vehicleSpeed = 0f;
        private Vector3 _vehicleVelocity = Vector3.Zero;
        private INotificationService _notification;
        private string _callSign = "";
        private int _lastVehicle = 0;

        private float _speedLimiter = -1;
        
        #region Zones

        private readonly List<string> zoneNameShort = new()
        {
            "AIRP", "ALAMO", "ALTA", "ARMYB", "BANHAMC", "BANNING", "BEACH",
            "BHAMCA", "BRADP", "BRADT", "BURTON", "CALAFB", "CANNY", "CCREAK", "CHAMH", "CHIL", "CHU", "CMSW", "CYPRE",
            "DAVIS", "DELBE", "DELPE", "DELSOL", "DESRT", "DOWNT", "DTVINE", "EAST_V", "EBURO", "ELGORL", "ELYSIAN",
            "GALFISH", "GOLF", "GRAPES", "GREATC", "HARMO", "HAWICK", "HORS", "HUMLAB", "JAIL", "KOREAT", "LACT",
            "LAGO",
            "LDAM", "LEGSQU", "LMESA", "LOSPUER", "MIRR", "MORN", "MOVIE", "MTCHIL", "MTGORDO", "MTJOSE", "MURRI",
            "NCHU",
            "NOOSE", "OCEANA", "PALCOV", "PALETO", "PALFOR", "PALHIGH", "PALMPOW", "PBLUFF", "PBOX", "PROCOB", "RANCHO",
            "RGLEN", "RICHM", "ROCKF", "RTRAK", "SANAND", "SANCHIA", "SANDY", "SKID", "SLAB", "STAD", "STRAW", "TATAMO",
            "TERMINA", "TEXTI", "TONGVAH", "TONGVAV", "VCANA", "VESP", "VINE", "WINDF", "WVINE", "ZANCUDO", "ZP_ORT",
            "ZQ_UAR"
        };

        private readonly List<string> zoneListComp = new()
        {
            "Los Santos International Airport", "Alamo Sea", "Alta", "Fort Zancudo", "Banham Canyon Dr", "Banning",
            "Vespucci Beach", "Banham Canyon", "Braddock Pass", "Braddock Tunnel", "Burton", "Calafia Bridge",
            "Raton Canyon", "Cassidy Creek", "Chamberlain Hills", "Vinewood Hills", "Chumash",
            "Chiliad Mountain State Wilderness", "Cypress Flats", "Davis", "Del Perro Beach", "Del Perro", "La Puerta",
            "Grand Senora Desert", "Downtown", "Downtown Vinewood", "East Vinewood", "El Burro Heights",
            "El Gordo Lighthouse", "Elysian Island", "Galilee", "GWC and Golfing Society", "Grapeseed",
            "Great Chaparral", "Harmony", "Hawick", "Vinewood Racetrack", "Humane Labs and Research",
            "Bolingbroke Penitentiary", "Little Seoul", "Land Act Reservoir", "Lago Zancudo", "Land Act Dam",
            "Legion Square", "La Mesa", "La Puerta", "Mirror Park", "Morningwood", "Richards Majestic", "Mount Chiliad",
            "Mount Gordo", "Mount Josiah", "Murrieta Heights", "North Chumash", "N.O.O.S.E", "Pacific Ocean",
            "Paleto Cove", "Paleto Bay", "Paleto Forest", "Palomino Highlands", "Palmer - Taylor Power Station",
            "Pacific Bluffs", "Pillbox Hill", "Procopio Beach", "Rancho", "Richman Glen", "Richman", "Rockford Hills",
            "Redwood Lights Track", "San Andreas", "San Chianski Mountain Range", "Sandy Shores", "Mission Row",
            "Stab City", "Maze Bank Arena", "Strawberry", "Tataviam Mountains", "Terminal", "Textile City",
            "Tongva Hills", "Tongva Valley", "Vespucci Canals", "Vespucci", "Vinewood", "Ron Alternates Wind Farm",
            "West Vinewood", "Zancudo River", "Port of South Los Santos", "Davis Quartz"
        };


        #endregion
        
        public Hud(ITickManager tick, ICommandManager command, INotificationService notification)
        {
            _notification = notification;
            _ticks = tick;
            command.Register("sb").WithHandler(ToggleSeatBelt);
            command.Register("speedlimiter").WithHandler(ToggleSpeedLimiter);
        }

        protected override Task OnStartAsync()
        {
            _ticks.On(LocationTick);
            _ticks.On(VehicleTick);
            _ticks.On(SeatbeltTick);
            _ticks.On(SpeedLimiterTick);
            _ticks.On(FetchCallsign);
            _ticks.On(ShowCallsign);
            
            API.RegisterKeyMapping("sb", "Toggle Vehicle Seatbelt", "keyboard", "y");
            API.RegisterKeyMapping("speedlimiter", "Toggle Vehicle Speed Limiter", "keyboard", "F5");
            
            return Task.FromResult(0);
        }

        private async Task FetchCallsign()
        {
            var callsign = (string)Game.Player.State.Get("PMPCallsign");

            _callSign = callsign;
            await BaseScript.Delay(5000);
            return;
        }

        private async Task ShowCallsign()
        {
            if (!Screen.Hud.IsRadarVisible) return;
            if (string.IsNullOrEmpty(_callSign)) return;
            if (_callSign == "" || _callSign.Contains("null")) return;

            var posX = 0.45f;
            var posY = 0.85f;
            var scale = 0.5f;
            var font = 4;
            
            API.SetTextScale(scale, scale);
            API.SetTextFont(font);
            API.SetTextOutline();
            API.BeginTextCommandDisplayText("STRING");
            API.AddTextComponentSubstringPlayerName($"Running As: ~b~{_callSign}");
            API.EndTextCommandDisplayText(posX, posY);
        }
        
        private async Task LocationTick()
        {
            if (!Screen.Hud.IsRadarVisible) return;
            
            var playerPos = Game.PlayerPed.Position;

            uint streetNameHash = 0;
            uint intersectionHash = 0;

            API.GetStreetNameAtCoord(playerPos.X, playerPos.Y, playerPos.Z, ref streetNameHash, ref intersectionHash);
            var streetName = API.GetStreetNameFromHashKey(streetNameHash);

            var zoneShort = API.GetNameOfZone(playerPos.X, playerPos.Y, playerPos.Z);
            var zoneIndex = zoneNameShort.IndexOf(zoneShort);
            var zoneName = zoneListComp[zoneIndex];
            
            var streetZoneText = $"{streetName} - {zoneName}";

            var posX = 0.015f;
            var posY = 0.750f;
            var scale = 0.42f;
            var font = 4;
            
            API.SetTextScale(scale, scale);
            API.SetTextFont(font);
            API.SetTextOutline();
            API.BeginTextCommandDisplayText("STRING");
            API.AddTextComponentSubstringPlayerName(streetZoneText);
            API.EndTextCommandDisplayText(posX, posY);
        }

        private async Task VehicleTick()
        {
            if (!Screen.Hud.IsRadarVisible) return;

            var currentVehicle = Game.PlayerPed.CurrentVehicle;

            if (currentVehicle == null) return;

            var isDriver = currentVehicle.Driver != null && currentVehicle.Driver == Game.PlayerPed;

            #region Speed

            var speedPosX = 0.015f;
            var speedPosY = 0.725f;
            var scale = 0.42f;
            var font = 4;

            var vehicleSpeed = currentVehicle.Speed * 2.236936;
            var speedRounded = Math.Round(vehicleSpeed, 0);
            var speedString = $"{speedRounded} ~f~MPH";

            var showSpeedLimiter = true;

            if (currentVehicle.ClassType is VehicleClass.Planes or VehicleClass.Helicopters)
            {
                var knotSpeed = currentVehicle.Speed * 1.94384;
                var knotRounded = Math.Round(knotSpeed, 0);
                var currentHeight = currentVehicle.HeightAboveGround;
                var currentFeet = currentHeight * 3.28084;
                var feetRounded = Math.Round(currentFeet, 0) - 3;
                speedString = $"{knotRounded} ~f~KTS~w~ - {feetRounded} ~g~FT";
                showSpeedLimiter = false;
            }
            
            API.SetTextScale(scale, scale);
            API.SetTextFont(font);
            API.SetTextOutline();
            API.BeginTextCommandDisplayText("STRING");
            API.AddTextComponentSubstringPlayerName(speedString);
            API.EndTextCommandDisplayText(speedPosX, speedPosY);

            #endregion

            #region SeatBelt

            var seatBeltPosX = 0.015f;
            var seatBeltPosY = 0.700f;

            var seatbeltText = "~r~Seatbelt";
            if (_seatbeltState)
            {
                seatbeltText = "~g~Seatbelt";
            }
            
            
            API.SetTextScale(scale, scale);
            API.SetTextFont(font);
            API.SetTextOutline();
            API.BeginTextCommandDisplayText("STRING");
            API.AddTextComponentSubstringPlayerName(seatbeltText);
            API.EndTextCommandDisplayText(seatBeltPosX, seatBeltPosY);

            #endregion

            #region Speed Limit

            var speedLimitPosX = 0.015f;
            var speedLimitPosY = 0.652f;

            var speedLimitText = "Speed Limiter: ~r~OFF";
            if (_speedLimiter > -1)
            {
                var vehicleSpeedLimiter = _speedLimiter * 2.236936;
                var speedLimiterRounded = Math.Round(vehicleSpeedLimiter, 0);
                speedLimitText = $"Speed Limiter: ~o~{speedLimiterRounded} MPH";
            }

            #endregion

            if (isDriver)
            {
                #region Speed Limiter
                if (showSpeedLimiter)
                {

                    API.SetTextScale(scale, scale);
                    API.SetTextFont(font);
                    API.SetTextOutline();
                    API.BeginTextCommandDisplayText("STRING");
                    API.AddTextComponentSubstringPlayerName(speedLimitText);
                    API.EndTextCommandDisplayText(speedLimitPosX, speedLimitPosY);

                }
                #endregion

                #region Fuel

                var fuel = Math.Round(currentVehicle.FuelLevel, 0);

                var fuelPosX = 0.015f;
                var fuelPosY = 0.685f;
                var fuelScale = 0.2f;

                var fuelString = $"🟩 🟩 🟩 🟩";

                if (fuel < 75)
                {
                    fuelString = $"🟩 🟩 🟩";
                }

                if (fuel < 50)
                {
                    fuelString = $"🟧 🟧";
                }
                
                if (fuel < 25)
                {
                    fuelString = $"🟧";
                }
                
                if (fuel < 15)
                {
                    fuelString = $"🟥";
                }
                
                if (fuel < 5)
                {
                    fuelString = $"🆘";
                }

                API.SetTextScale(fuelScale, fuelScale);
                API.SetTextFont(font);
                API.SetTextOutline();
                API.BeginTextCommandDisplayText("STRING");
                API.AddTextComponentSubstringPlayerName(fuelString);
                API.EndTextCommandDisplayText(fuelPosX, fuelPosY);

                #endregion
            }
        }

        private async Task SeatbeltTick()
        {
            var currentVehicle = Game.PlayerPed.CurrentVehicle;

            if (currentVehicle == null)
            {
                if (_seatbeltState)
                {
                    _seatbeltState = false;
                }
                return;
            }

            if (_seatbeltState)
            {
                API.DisableControlAction(0, 75, true);
                API.DisableControlAction(25, 75, true);
            }

            var speed = currentVehicle.Speed;

            if (!_seatbeltState && speed > (50 / 3.6) && (_vehicleSpeed - speed) > speed * 0.2)
            {
                var coords = Game.PlayerPed.Position;
                var fw = Fwv(Game.PlayerPed);
                API.SetEntityCoords(Game.PlayerPed.Handle, coords.X + (float)fw[0], coords.Y + (float)fw[1], coords.Z - 0.47f, true, true, true, false);
                API.SetEntityVelocity(Game.PlayerPed.Handle, _vehicleVelocity.X, _vehicleVelocity.Y, _vehicleVelocity.Z);
                API.SetPedToRagdoll(Game.PlayerPed.Handle, 1000, 1000, 0, false, false, false);
            }

            _vehicleSpeed = speed;
            _vehicleVelocity = API.GetEntityVelocity(currentVehicle.Handle);
        }

        private double[] Fwv(Ped player)
        {
            var hr = API.GetEntityHeading(player.Handle) + 90f;
            if (hr < 0)
            {
                var oldHr = hr;
                hr = 360f + oldHr;
            }
            
            hr = (float) (hr * 0.0174533);

            var x = Math.Cos(hr) * 2;
            var y = Math.Sin(hr) * 2;

            return new double[] {x, y};
        }

        private void ToggleSeatBelt()
        {
            _seatbeltState = !_seatbeltState;
        }

        private void ToggleSpeedLimiter()
        {
            var currentVehicle = Game.PlayerPed.CurrentVehicle;

            if (currentVehicle == null) return;
            if (currentVehicle.Driver == null) return;
            if (currentVehicle.Driver != Game.PlayerPed) return;
            var vehClass = currentVehicle.ClassType;

            if (vehClass is VehicleClass.Boats or VehicleClass.Planes or VehicleClass.Helicopters) return;

            if (_speedLimiter > -1)
            {
                _speedLimiter = -1;
                
                API.SetVehicleMaxSpeed(currentVehicle.Handle, 0.0f);
                
                _notification.Success("Speed Limiter", "Speed Limiter Disabled");
                return;
            }

            _speedLimiter = currentVehicle.Speed;
            
            var vehicleSpeed = currentVehicle.Speed * 2.236936;
            var speedRounded = Math.Round(vehicleSpeed, 0);
            API.SetVehicleMaxSpeed(currentVehicle.Handle, _speedLimiter);
            
            _notification.Success("Speed Limiter", $"Speed Limiter set to {speedRounded} MPH");
        }

        private async Task SpeedLimiterTick()
        {
            if (_speedLimiter == -1f) return;
            
            var currentVehicle = Game.PlayerPed.CurrentVehicle;

            if (currentVehicle == null)
            {
                if (_speedLimiter != -1f)
                {
                    _speedLimiter = -1f;
                }
                if (_lastVehicle != 0)
                {
                    if (!API.DoesEntityExist(_lastVehicle))
                    {
                        _lastVehicle = 0;
                    }
                    else
                    {
                        API.SetVehicleMaxSpeed(_lastVehicle, 0.0f);
                    }
                }
                return;
            }

            if (_lastVehicle != currentVehicle.Handle)
            {
                _lastVehicle = currentVehicle.Handle;
            }
            
            /*
            if (currentVehicle.Driver == null) return;
            if (currentVehicle.Driver != Game.PlayerPed) return;

            if (currentVehicle.Speed > _speedLimiter)
            {
                currentVehicle.Speed = _speedLimiter;
            }*/
        }
    }
}