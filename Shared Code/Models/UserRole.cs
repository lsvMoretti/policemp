using PoliceMP.Shared.Enums;

namespace PoliceMP.Shared.Models
{
    public class UserRole
    {
        /// <summary>
        /// The role of the user - Police, Fire, NHS, etc
        /// </summary>
        public UserBranch Branch { get; set; }
        
        /// <summary>
        /// The division the user is playing as. (AFO, DSU etc)
        /// </summary>
        public UserDivision Division { get; set; }
    }
}