using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using CitizenFX.Core;
using PoliceMP.Shared.Enums;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<UserAces> GetUserAces(Player player);
        Task<UserRole> GetUserRole(Player player);
        public Task<List<Player>> GetAllAdmins();
        Task SetUserRole(Player player, UserRole userRole);
        Task<int> GetUserCountByBranch(UserBranch branch);
        Task<int> GetUserCountByDivision(UserDivision division);
    }
}