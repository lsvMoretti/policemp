using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Newtonsoft.Json;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Client.Communications.Interfaces;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Constants.States;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;
using PoliceMP.Core.Client.Extensions;

namespace PoliceMP.Client.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IClientCommunicationsManager _comms;
        private UserAces _userAces;
        private UserRole _userRole;

        public UserRole CurrentUserRole =>
            _userRole ??= Game.Player.State.Get<UserRole>(PlayerStates.CurrentRole);

        public static readonly UserRole DefaultRole = new() {Branch = UserBranch.Police, Division = UserDivision.Rpu};

        public PermissionService(IClientCommunicationsManager comms)
        {
            _comms = comms;

            
            API.AddStateBagChangeHandler(PlayerStates.CurrentRole, $"player:{API.GetPlayerServerId(API.PlayerId())}",
                new Action<string, string, dynamic, int, bool>(HandleUserRoleChange));
        }

        private async void HandleUserRoleChange(string bagName, string key, dynamic value, int reserved,
            bool replicated)
        {
            Debug.WriteLine($"Got user role from {bagName} - {value}");

            try
            {
                _userRole = JsonConvert.DeserializeObject<UserRole>(value) ?? DefaultRole;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                _userRole = DefaultRole;
            }

            _userAces = await _comms.Request<UserAces>(ServerEvents.FetchUserAces);
        }

        public async Task<UserAces> GetUserAces()
        {
            return _userAces ??= await _comms.Request<UserAces>(ServerEvents.FetchUserAces);
        }

        public void SetUserRole(UserRole userRole)
        {
            _userRole = userRole;
            Game.PlayerPed.State.Set(PlayerStates.CurrentRole, userRole, true);
            _comms.ToServer(ServerEvents.SendUserRole, userRole);
        }

        public UserRole GetUserRole(int targetNetworkId)
        {
            return new Player(targetNetworkId).State?.Get<UserRole>(PlayerStates.CurrentRole) ?? DefaultRole;
        }
    }
}