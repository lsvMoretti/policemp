using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PoliceMP.Core.Client;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Shared.Constants;

namespace PoliceMP.Client.Scripts.Weather
{
    public class WeatherScript : Script
    {
        #region Services

        private readonly IClientCommunicationsManager _comms;
        private readonly ILogger<WeatherScript> _logger;

        #endregion

        #region Variables

        private string _currentWeatherType = "CLEAR";
        private int _cloudiness = 0;
        private int _cloudOpacity = 100;
        private string _cloudType = "Wispy";
        private float _rainLevel = 0f;
        
        #endregion

        public WeatherScript(IClientCommunicationsManager comms, ILogger<WeatherScript> logger)
        {
            _comms = comms;
            _logger = logger;
            _comms.On<int, int>(ServerEvents.ForcePlayerTime, (hour, minute) =>
            {
                API.NetworkOverrideClockTime(hour, minute, 0);
            });
            
            _comms.On<bool>(ClientEvents.PlayerSpawned, async (firstSpawn) =>
            {
                var currentWeather = await _comms.Request<List<string>>(ServerEvents.FetchWeatherFromServer);
                _currentWeatherType = currentWeather[0];
                _cloudiness = int.Parse(currentWeather[1]);
                _cloudOpacity = int.Parse(currentWeather[2]);
                _cloudType = currentWeather[3];
                _rainLevel = float.Parse(currentWeather[4]);
                OnReceiveWeatherFromServer(currentWeather);
            });
            
            _comms.On<List<string>>(ClientEvents.SendWeatherToPlayers, OnReceiveWeatherFromServer);
        }

        protected override async Task OnStartAsync()
        {
            var month = DateTime.Now.Month;
            var day = DateTime.Now.Day;

            if (month == 12 && day >= 15)
            {
                OnReceiveWeatherFromServer(null);
                return;
            }
            
            API.SetWeatherTypeNow(_currentWeatherType);
            
            var currentWeather = await _comms.Request<List<string>>(ServerEvents.FetchWeatherFromServer);
            _currentWeatherType = currentWeather[0];
            _cloudiness = int.Parse(currentWeather[1]);
            _cloudOpacity = int.Parse(currentWeather[2]);
            _cloudType = currentWeather[3];
            _rainLevel = float.Parse(currentWeather[4]);
            OnReceiveWeatherFromServer(currentWeather);
            
            var timeJson = await _comms.Request<string>(ServerEvents.FetchServerTime);
            List<int> timeList = JsonConvert.DeserializeObject<List<int>>(timeJson);
            var hour = timeList[0];
            var minute = timeList[1];
            API.NetworkOverrideClockTime(hour, minute, 0);
        }

        private void OnReceiveWeatherFromServer(List<string> currentWeather)
        {
            var month = DateTime.Now.Month;
            var day = DateTime.Now.Day;
            if (month == 12 && day is >= 15 and <= 27)
            {
                API.ForceSnowPass(true);
                API.SetWeatherTypeNowPersist("XMAS");
                API.SetForcePedFootstepsTracks(true);
                API.SetForceVehicleTrails(true);
                API.SetSnowLevel(1f);
                if (day is >= 24 and <= 25)
                {
                    API.SetWeatherTypeNowPersist("BLIZZARD");
                }
                return;
            }
            
            var weatherType = currentWeather[0];
            var cloudiness = int.Parse(currentWeather[1]);
            _currentWeatherType = weatherType;
            _cloudiness = cloudiness;
            _cloudOpacity = int.Parse(currentWeather[2]);
            _cloudType = currentWeather[3];
            _rainLevel = float.Parse(currentWeather[4]);
            
            API.SetWeatherTypeOvertimePersist(weatherType, 60);
            _logger.Debug($"Weather set to {weatherType} - Cloudiness - {_cloudiness}");
            
            _logger.Debug($"Setting Clouds to {_cloudType} - {_cloudOpacity}");

            API.SetRainLevel(_rainLevel);
            
            
            
            switch (_cloudiness)
            {
                case < 5:
                    API.ClearCloudHat();
                    break;
                case >= 5 and <= 10:
                    API.SetCloudHatOpacity(_cloudOpacity);
                    API.SetCloudHatTransition(_cloudType, 10f);
                    break;
                case > 10 and <= 20:
                    API.SetCloudHatOpacity(_cloudOpacity);
                    API.SetCloudHatTransition(_cloudType, 10f);
                    break;
                case > 20 and <= 30:
                    API.SetCloudHatOpacity(_cloudOpacity);
                    API.SetCloudHatTransition(_cloudType, 10f);
                    break;
                case > 30 and <= 40:
                    API.SetCloudHatOpacity(_cloudOpacity);
                    API.SetCloudHatTransition(_cloudType, 10f);
                    break;
                case > 40 and <= 50:
                    API.SetCloudHatOpacity(_cloudOpacity);
                    API.SetCloudHatTransition(_cloudType, 10f);
                    break;
                case > 60 and <= 70:
                    API.SetCloudHatOpacity(_cloudOpacity);
                    API.SetCloudHatTransition(_cloudType, 10f);
                    break;
                case > 80:
                    API.SetCloudHatOpacity(_cloudOpacity);
                    API.SetCloudHatTransition(_cloudType, 10f);
                    break;
            }
        }
        
    }
}