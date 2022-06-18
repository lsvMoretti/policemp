using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Server.Networking;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Models;
using System.Threading.Tasks;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Constants.States;
using PoliceMP.Server.Extensions;
namespace PoliceMP.Server.Controllers
{
    public class AceController : Controller
    {
        private readonly IServerCommunicationsManager _comms;
        private readonly PlayerList _playerList;
        private readonly IPermissionService _perms;

        public AceController(IServerCommunicationsManager comms, PlayerList playerList, IPermissionService perms)
        {
            _comms = comms;
            _playerList = playerList;
            _perms = perms;
            _comms.OnRequest(ServerEvents.FetchUserAces, FetchUserAces);
            _comms.On<UserRole>(ServerEvents.SendUserRole, OnReceieveUserRole);
            //_comms.OnRequest<int, UserRole>(ServerEvents.FetchUserRoleByNetId, FetchUserRoleByNetId);
        }

//        private Task<UserRole> FetchUserRoleByNetId(Player player, int networkId)
//        {
//            var target = _playerList[networkId];
//
//            UserRole userRole = target?.State.Get(PlayerStates.CurrentRole);
//
//            if (userRole == null)
//            {
//                return Task.FromResult(new UserRole
//                {
//                    Branch = UserBranch.Police,
//                    Division = UserDivision.Ert
//                });
//            }
//
//            return Task.FromResult(userRole);
//        }
        
        private void OnReceieveUserRole(Player player, UserRole userRole)
        {
            if (player.Character == null) return;

            player.State.Set<UserRole>(PlayerStates.CurrentRole, userRole, true);
        }

        private async Task<UserRole> FetchUserRoleByNetId(Player player, int networkId)
        {
            var target = _playerList.ToList().FirstOrDefault(p => p.Character?.NetworkId == networkId);
            var userRole = await _perms.GetUserRole(target);
            return userRole;
        }

        private async Task<UserAces> FetchUserAces(Player player)
        {
            var userAces = await _perms.GetUserAces(player);
            return userAces;
        }
    }
}