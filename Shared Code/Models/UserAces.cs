namespace PoliceMP.Shared.Models
{
    public class UserAces
    {
        public bool IsWhiteListed { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsDeveloper { get; set; }
        public bool IsModerator { get; set; }
        public bool IsAfoTrained { get; set; }
        public bool IsRpuTrained { get; set; }
        public bool IsCidTrained { get; set; }
        public bool IsNpasTrained { get; set; }
        public bool IsMpuTrained { get; set; }
        public bool IsDsuTrained { get; set; }
        public bool IsBtpTrained { get; set; }
        public bool IsFireTrained { get; set; }
        public bool IsBasicDonator { get; set; }
        public bool IsProDonator { get; set; }
        public bool IsCivTrained { get; set; }
        public bool IsSeniorCiv { get; set; }
        public bool IsContentCreator { get; set; }
        public bool IsControl { get; set; }
        public bool IsRetired { get; set; }
        public bool IsHighwaysTrained { get; set; }
        public bool IsCollegeStaff { get; set; }
        public bool IsFireBoroughCommander { get; set; }
        public bool IsNhsHems { get; set; }
        public bool IsNhsParamedic { get; set; }
        public bool IsNhsClinicalAdv { get; set; }
        public bool IsNhsClinicalTl { get; set; }
        public bool IsNhsDoctor { get; set; }
        public bool IsNhsSectionLeader { get; set; }
        public bool IsNhsHemsTl { get; set; }
        public bool IsCivGunTrained { get; set; }
        public bool IsBandOne { get; set; }
        public bool IsBandTwo { get; set; }
        public bool IsBandThree { get; set; }
        public bool IsBandFour { get; set; }
        
        public bool HasCityPoliceDlc { get; set; }

        public UserAces()
        {
        }
    }
}