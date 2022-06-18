using System.Threading.Tasks;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<UserAces> GetUserAces();
        UserRole CurrentUserRole { get; }
        void SetUserRole(UserRole userRole);
        UserRole GetUserRole(int targetNetId);
    }
}