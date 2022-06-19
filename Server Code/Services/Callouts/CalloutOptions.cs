using System.Collections.Generic;
using PoliceMP.Shared.Enums;

namespace PoliceMP.Server.Services.Callouts
{
    public class CalloutOptions
    {
        // Number between 0 and 255 that dictates how probable this callout is to be spawned.
        public byte Probability { get; set; } = 128;
        public List<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
        public List<UserDivision> UserDivisions { get; set; } = new List<UserDivision>();

        public int MinimumAppropriatePlayers { get; set; } = 1;
        public string Description { get; set; } = "";
        public CalloutGrade CalloutGrade { get; set; } = CalloutGrade.GradeOne;
    }
}