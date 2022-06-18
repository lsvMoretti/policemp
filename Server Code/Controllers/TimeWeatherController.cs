using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Core.Shared;
using PoliceMP.Server.Services;
using PoliceMP.Server.Services.Interfaces;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;
using PoliceMP.Shared.Models.Weather;

namespace PoliceMP.Server.Controllers
{
    public class TimeWeatherController : Controller
    {
        #region Services

        private readonly ILogger<TimeWeatherController> _logger;
        private readonly IServerCommunicationsManager _comms;
        private readonly INotificationService _notification;
        private PlayerList _playerList;

        #endregion

        #region Variables

        private string _currentWeatherType = "CLEAR";
        private int _cloudiness = 0;
        private int _cloudOpacity = 100;
        private string _cloudType = "Wispy";
        private float _rainLevel = 0f;
        private DateTime _weatherOverrideTime = DateTime.MinValue;
        private readonly Timer _weatherCheckTimer = new Timer(6000)
        {
            AutoReset = true,
            Enabled = true
        };
        private Timer _timeTimer;

        private int _currentHour = 12;
        private int _currentMinute = 00;

        #endregion
        
        public TimeWeatherController(ILogger<TimeWeatherController> logger, IServerCommunicationsManager comms, INotificationService notification, PlayerList playerList)
        {
            _logger = logger;
            _comms = comms;
            _notification = notification;
            _playerList = playerList;
            
            _weatherCheckTimer.Elapsed += WeatherCheckTimerOnElapsed;
        }

        private async void WeatherCheckTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_weatherOverrideTime == DateTime.MinValue) return;

            _logger.Trace($"Override Time: {_weatherOverrideTime}");
            
            if (DateTime.Compare(DateTime.Now, _weatherOverrideTime) < 0)
            {
                _logger.Trace("Weather is being overriden!");
                return;
            }

            _weatherOverrideTime = DateTime.MinValue;
            await SetCurrentWeather();
        }

        public override async Task Started()
        {
            _comms.OnRequest(ServerEvents.FetchWeatherFromServer, OnReceiveWeatherRequest);
            
            var weatherTimer = new Timer(900000)
            {
                AutoReset = true,
                Enabled = true
            };
            
            weatherTimer.Elapsed += async (sender, args) =>
            {
                await WeatherTimerOnElapsed(sender, args);
            };

            _timeTimer = new Timer(15000)
            {
                AutoReset = true,
                Enabled = true
            };
            
            _timeTimer.Elapsed += TimeTimerOnElapsed;

            await SetCurrentWeather();
            
            _comms.On<string>(ServerEvents.ForceSetWeatherToServer, OnForceSetWeatherFromClient);
            _comms.On<string, string, string>(ServerEvents.AdminManageServerTime, OnReceiveManageServerTime);
            _comms.OnRequest(ServerEvents.FetchServerTime, (player) =>
            {
                List<int> timeList = new List<int>
                {
                    _currentHour,
                    _currentMinute
                };
                return Task.FromResult(JsonConvert.SerializeObject(timeList));
            });
        }

        private void OnReceiveManageServerTime(Player player, string itemData, string hourString, string minuteString)
        {
            var hour = int.Parse(hourString);
            var minute = int.Parse(minuteString);

            switch (itemData)
            {
                case "RESETTIME":
                    _timeTimer.Enabled = true;
                    return;
                case "FREEZETIME":
                    _timeTimer.Enabled = false;
                    return;
                case "SETTIME":
                    _currentHour = hour;
                    _currentMinute = minute;
                    _comms.ToClient(ServerEvents.ForcePlayerTime, _currentHour, _currentMinute);
                    return;
            }
        }
        
        private void TimeTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            _currentMinute++;
            if (_currentMinute >= 59)
            {
                _currentMinute = 0;
                _currentHour++;
                if (_currentHour >= 24)
                {
                    _currentHour = 0;
                }
            }
            //_logger.Debug($"Setting time to {_currentHour}:{_currentMinute}");

            _comms.ToClient(ServerEvents.ForcePlayerTime, _currentHour, _currentMinute);
        }


        private async void OnForceSetWeatherFromClient(Player player, string weatherType)
        {
            if (weatherType == "RESET")
            {
                _weatherOverrideTime = DateTime.MinValue;
                await SetCurrentWeather();
                return;
            }
            _weatherOverrideTime = DateTime.Now.AddHours(1);
            _currentWeatherType = weatherType;
            _comms.ToClient(ClientEvents.SendWeatherToPlayers, new List<string>
            {
                _currentWeatherType,
                "0",
                "0",
                "Wispy",
                (0f).ToString()
            });
        }
        
        private Task<List<string>> OnReceiveWeatherRequest(Player player)
        {
            return Task.FromResult(new List<string>
            {
                _currentWeatherType,
                _cloudiness.ToString(),
                _cloudOpacity.ToString(),
                _cloudType,
                (0f).ToString()
            });
        }
        
        private async Task WeatherTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            await SetCurrentWeather();
        }

        private async Task SetCurrentWeather()
        {
            var currentWeather = await FetchCurrentWeather();
            _currentWeatherType = currentWeather[0];
            _cloudiness = int.Parse(currentWeather[1]);
            _cloudOpacity = int.Parse(currentWeather[2]);
            _cloudType = currentWeather[3];
            _rainLevel = float.Parse(currentWeather[4]);
            _comms.ToClient(ClientEvents.SendWeatherToPlayers, new List<string>
            {
                _currentWeatherType,
                _cloudiness.ToString(),
                _cloudOpacity.ToString(),
                _cloudType,
                _rainLevel.ToString()
            });
            
        }
        
        private async Task<List<string>> FetchCurrentWeather()
        {
            try
            {
                var locationId = "2643743";
                var appId = "37c1a999011411a01b4d200ea16e9b9a";
                var url = new Uri(
                    $"http://api.openweathermap.org/data/2.5/weather?id={locationId}&mode=json&units=metric&APPID={appId}");
                using var webClient = new WebClient();
            
                var currentWeatherJson = await webClient.DownloadStringTaskAsync(url);

                if (string.IsNullOrEmpty(currentWeatherJson))
                {
                    _logger.Error("Current Weather JSON is null!");
                    return new List<string>()
                    {
                        "Clear",
                        "0",
                        "100",
                        "Wispy",
                        (0f).ToString()
                    };
                }

                var currentWeather = JsonConvert.DeserializeObject<OpenWeather>(currentWeatherJson);

                int currentWeatherId = currentWeather.weather.First().id;

                var currentWeatherType = currentWeatherId switch
                {
                    701 =>
                        // Smoke
                        "SMOG",
                    711 =>
                        // Mist
                        "SMOG",
                    721 =>
                        // Haze
                        "CLOUDS",
                    741 =>
                        //Fog
                        "FOGGY",
                    800 => "CLEAR",
                    801 => "CLOUDS",
                    802 => "CLOUDS",
                    803 => "CLOUDS",
                    804 => "OVERCAST",
                    _ => currentWeatherId switch
                    {
                        >= 200 and < 232 => "THUNDER",
                        >= 300 and < 400 => "OVERCAST",
                        >= 500 and < 600 => "RAIN",
                        //Snow
                        >= 600 and < 700 when currentWeatherId == 600 || currentWeatherId == 601 => "SNOWLIGHT",
                        >= 600 and < 700 => "BLIZZARD",
                        _ => "CLEAR"
                    }
                };

                var textInfo = new CultureInfo("en-GB", false).TextInfo;
                
                foreach (var player in _playerList)
                {
                    _notification.Success(player, "Weather", $"The current weather in {currentWeather.name} is {textInfo.ToTitleCase(currentWeather.weather.First().description)}.<br>" +
                                                             $"Temperature: {Math.Round(currentWeather.main.temp, 1)}°C");
                }
                
                var lowClouds = new List<string>
                {
                    "stratoscumulus",
                    "shower",
                    "altostratus",
                    "Cloud 01",
                    "Stormy 01",
                    "Nimbus"
                };

                var highClouds = new List<string>
                {
                    "Cirrus",
                    "cirrocumulus",
                    "Clear 01",
                    "Contrails",
                    "Horizon",
                    "Puffs",
                    "Stripey",
                    "Wispy"
                };

                var cloudType = "Clear 01";

                var rnd = new Random();
                
                var minValue = _cloudiness - 10;
                if (minValue < 0)
                {
                    minValue = 0;
                }

                var maxValue = _cloudiness + 10;
                if (maxValue > 99)
                {
                    maxValue = 99;
                }

                var opacity = rnd.Next(minValue, maxValue + 1);
                
                cloudType = currentWeather.clouds.all < 50 ? highClouds[rnd.Next(highClouds.Count)] : lowClouds[rnd.Next(lowClouds.Count)];

                float rainLevel = 0f;
                if (currentWeatherType == "RAIN")
                {
                    rainLevel = (float)rnd.Next(6) / 10;
                }

                return new List<string> {currentWeatherType, currentWeather.clouds.all.ToString(), opacity.ToString(), cloudType, rainLevel.ToString()};
            }
            catch (Exception e)
            {
                _logger.Error(e.ToString());
                return new List<string>
                {
                    "Clear",
                    "0",
                    "100",
                    "Wispy",
                    (0f).ToString()
                };
            }
        }
    }
}