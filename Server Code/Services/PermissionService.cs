using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Client.Services.Interfaces;
using PoliceMP.Core.Server.Communications.Interfaces;
using PoliceMP.Core.Shared;
using PoliceMP.Server.Extensions;
using PoliceMP.Shared.Constants;
using PoliceMP.Shared.Constants.States;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;

namespace PoliceMP.Server.Services
{
    public class PermissionService : IPermissionService
    {
        private static Dictionary<string, UserRole> _userRoles = new();
        private readonly PlayerList _playerList;
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(PlayerList playerList, ILogger<PermissionService> logger, IServerCommunicationsManager comms)
        {
            _playerList = playerList;
            _logger = logger;
            comms.On<UserRole>(ServerEvents.SendUserRole, SetUserRole);
        }

        public Task<List<Player>> GetAllAdmins()
        {
            return Task.FromResult(_playerList.Where(player => API.IsPlayerAceAllowed(player.Handle, "Police.adminAuth") || API.IsPlayerAceAllowed(player.Handle, "Police.adminAuth")).ToList());
        }
        
        public Task<UserAces> GetUserAces(Player player)
        {
            var userAces = new UserAces
            {
                IsWhiteListed = API.IsPlayerAceAllowed(player.Handle, "Police.whitelisted"),
                IsAdmin = API.IsPlayerAceAllowed(player.Handle, "Police.adminAuth"),
                IsDeveloper = API.IsPlayerAceAllowed(player.Handle, "Police.developer"),
                IsModerator = API.IsPlayerAceAllowed(player.Handle, "Police.modAuth"),
                IsAfoTrained = API.IsPlayerAceAllowed(player.Handle, "Police.afoTrained"),
                IsRpuTrained = API.IsPlayerAceAllowed(player.Handle, "Police.rpuTrained"),
                IsCidTrained = API.IsPlayerAceAllowed(player.Handle, "Police.cidTrained"),
                IsNpasTrained = API.IsPlayerAceAllowed(player.Handle, "Police.npasTrained"),
                IsMpuTrained = API.IsPlayerAceAllowed(player.Handle, "Police.mpuTrained"),
                IsDsuTrained = API.IsPlayerAceAllowed(player.Handle, "Police.dogTrained"),
                IsBtpTrained = API.IsPlayerAceAllowed(player.Handle, "Police.btpTrained"),
                IsFireTrained = API.IsPlayerAceAllowed(player.Handle, "Fire.Trained"),
                IsBasicDonator = API.IsPlayerAceAllowed(player.Handle, "Don.basic"),
                IsProDonator = API.IsPlayerAceAllowed(player.Handle, "Don.pro"),
                IsCivTrained = API.IsPlayerAceAllowed(player.Handle, "Civ.Trained"),
                IsControl = API.IsPlayerAceAllowed(player.Handle, "control.trained"),
                IsContentCreator = API.IsPlayerAceAllowed(player.Handle, "Content.Creator"),
                IsRetired = API.IsPlayerAceAllowed(player.Handle, "Retired"),
                IsHighwaysTrained = API.IsPlayerAceAllowed(player.Handle, "HETO.Trained"),
                IsCollegeStaff = API.IsPlayerAceAllowed(player.Handle, "Police.collegeStaff"),
                IsSeniorCiv = API.IsPlayerAceAllowed(player.Handle, "SeniorCiv.Trained"),
                IsFireBoroughCommander = API.IsPlayerAceAllowed(player.Handle, "fire.boroughCommander"),
                IsNhsHems = API.IsPlayerAceAllowed(player.Handle, "nhs.hems"),
                IsNhsParamedic = API.IsPlayerAceAllowed(player.Handle, "nhs.paramedic"),
                IsNhsClinicalAdv = API.IsPlayerAceAllowed(player.Handle, "nhs.clinicalADV"),
                IsNhsClinicalTl = API.IsPlayerAceAllowed(player.Handle, "nhs.clinicalTL"),
                IsNhsDoctor = API.IsPlayerAceAllowed(player.Handle, "nhs.doctor"),
                IsNhsSectionLeader = API.IsPlayerAceAllowed(player.Handle, "nhs.sectionleader"),
                IsNhsHemsTl = API.IsPlayerAceAllowed(player.Handle, "nhs.hemsTL"),
                IsCivGunTrained = API.IsPlayerAceAllowed(player.Handle, "command.givegun"),
                IsBandOne = API.IsPlayerAceAllowed(player.Handle, "staff.bandOne"),
                IsBandTwo = API.IsPlayerAceAllowed(player.Handle, "staff.bandTwo"),
                IsBandThree = API.IsPlayerAceAllowed(player.Handle, "staff.bandThree"),
                IsBandFour = API.IsPlayerAceAllowed(player.Handle, "staff.bandFour"),
                HasCityPoliceDlc = API.IsPlayerAceAllowed(player.Handle, "Don.colp"),
            };

            return Task.FromResult(userAces);
        }

        public Task<UserRole> GetUserRole(Player player)
        {
            var doesContain = _userRoles.TryGetValue(player.Handle, out var userRole);
            if (!doesContain)
            {
                userRole = new UserRole
                {
                    Branch = UserBranch.Police,
                    Division = UserDivision.Ert
                };
            }
            return Task.FromResult(userRole);
        }

        public Task SetUserRole(Player player, UserRole userRole)
        {
            var doesContain = _userRoles.TryGetValue(player.Handle, out UserRole currentRole);
            if (doesContain)
            {
                lock (_userRoles)
                {
                    _userRoles.Remove(player.Handle);
                    _userRoles.Add(player.Handle, userRole);
                }
            }
            else
            {
                lock (_userRoles)
                {
                    _userRoles.Add(player.Handle, userRole);
                }
            }
            return Task.CompletedTask;
        }
        public async Task<int> GetUserCountByBranch(UserBranch branch)
        {
            var count = 0;
            foreach (var player in _playerList)
            {
                var role = await GetUserRole(player);
                if (role.Branch == branch)
                {
                    count++;
                }
            }
            return count;
        }
        
        public async Task<int> GetUserCountByDivision(UserDivision division)
        {
            var count = 0;
            foreach (var player in _playerList)
            {
                var role = await GetUserRole(player);
                if (role.Division == division)
                {
                    count++;
                }
            }
            return count;
        }
    }
}