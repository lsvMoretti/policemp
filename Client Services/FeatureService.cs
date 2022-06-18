using System.Collections.Generic;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PoliceMP.Client.Services.Interfaces;

namespace PoliceMP.Client.Services
{
    public class FeatureService : IFeatureService
    {
        public FeatureService()
        {
        }

        public bool IsFeatureEnabled(string feature)
        { 
            var featureString = API.GetConvar($"feature_{feature}", "");

            var tryParse = bool.TryParse(featureString, out bool isEnabled);
            
            return tryParse && isEnabled;
        }
    }
}