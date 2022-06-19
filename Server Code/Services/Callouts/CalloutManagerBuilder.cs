using System;
using System.Collections.Generic;
using CitizenFX.Core;
using Microsoft.Extensions.DependencyInjection;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Server.Commands.Interfaces;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Interfaces.Services;
using PoliceMP.Core.Shared;

namespace PoliceMP.Server.Services.Callouts
{
    public interface ICalloutManagerBuilder
    {
        ICalloutManagerBuilder AddCallout<T>() where T : ICallout;
        ICalloutManagerBuilder AddCallout<T>(Action<CalloutOptions> configure);
        IServiceCollection Build();
    }

    public class CalloutManagerBuilder : ICalloutManagerBuilder
    {
        private readonly IServiceCollection _serviceCollection;
        private readonly Dictionary<Type, CalloutOptions> _calloutList = new ();  
        
        public CalloutManagerBuilder(IServiceCollection serviceCollection)
        {
            _serviceCollection = serviceCollection;
        }

        public ICalloutManagerBuilder AddCallout<T>() where T : ICallout
            => AddCallout<T>(null);

        public ICalloutManagerBuilder AddCallout<T>(Action<CalloutOptions> configure)
        {
            var options = new CalloutOptions();
            configure?.Invoke(options);

            _calloutList.Add(typeof(T), options);
            return this;
        }

        public IServiceCollection Build()
        {
            foreach (var calloutType in _calloutList.Keys)
            {
                _serviceCollection.AddTransient(calloutType);
            }
            _serviceCollection.AddSingleton<ICalloutManager>(sp => new CalloutManager(sp, sp.GetRequiredService<ILogger<CalloutManager>>(), _calloutList, sp.GetRequiredService<INotificationService>(), sp.GetRequiredService<IServerCommunicationsManager>(), sp.GetRequiredService<PlayerList>(), sp.GetRequiredService<IPermissionService>()));
            return _serviceCollection;
        }
    }
}