namespace PoliceMP.Shared.Constants
{
    public static class CalloutEvents
    {
        #region Various Used Events

        public static string SetUserWaypointPosition = "PoliceMP:Callouts:SetWaypointPosition";

        #endregion
        
        #region Stabbing

        public const string OnStabbingAttackerSpawn = "PoliceMP:Callouts:OnStabbingAttackerSpawn";
        public const string OnStabbingVictimSpawn = "PoliceMP:Callouts:OnStabbingVictimSpawn";
        public const string OnStabbingAttackerDropKnife = "PoliceMP:Callouts:OnStabbingAttackerDropKnife";
        public const string OnStabbingVictimAttacked = "PoliceMP:Callouts:OnStabbingVictimAttacked";
        public const string OnStabbingAttackerReFollow = "PoliceMP:Callouts:OnStabbingAttackerReFollow";
        public const string OnStabbingAttackerBlendIn = "PoliceMP:Callouts:OnStabbingAttackerBlendIn";
        public const string OnStabbingAttackerPanic = "PoliceMP:Callouts:OnStabbingAttackerPanic";
        public const string OnStabbingAttackerKnifeSpawned = "PoliceMP:Callouts:OnStabbingAttackerKnifeSpawned";

        #endregion

        #region Vehicle Break In

        public const string VehicleBreakInGetDoorPosition = "PoliceMP:Callouts:VehicleBreakInGetDoorPosition";
        public const string VehicleBreakInSetPedToBreakIn = "PoliceMP:Callouts:VehicleBreakInSetPedToBreakIn";
        public const string VehicleBreakInVehicleBrokenInto = "PoliceMP:Callouts:VehicleBreakInVehicleBrokenInto";
        public const string VehicleBreakInPlayPedAnim = "PoliceMP:Callouts:VehicleBreakInPlayAnim";
        public const string VehicleBreakInWanderArea = "PoliceMP:Callouts:VehicleBreakInWanderArea";

        #endregion

        #region Suspicious Package

        public const string CreateExplosion = "PoliceMP:Callouts:SusPackage:CreateExplosion";

        #endregion

        #region Fight Base

        public static class FightBase
        {
            public const string InitializePed = "PoliceMP:Callouts:FightBase:InitializePed";
            public const string UnfreezePed = "PoliceMP:Callouts:FightBase:UnfreezePeds";
            public const string GetPedRelationship = "PoliceMP:Callouts:FightBase:GetPedRelationship";
            public const string SetPedRelationship = "PoliceMP:Callouts:FightBase:SetPedRelationship";
            public const string InitializeRelationships = "PoliceMP:Callouts:FightBase:InitializeRelationships";
            public const string MakePedFight = "PoliceMP:Callouts:FightBase:MakePedFight";
        }

        #endregion

        #region Domesitc Abuse

        public const string DomesticAbuseAttackerSpeechStart = "PoliceMP:Callouts:DomesticAbuseAttackerSpeechStart";
        public const string DomesticAbuseVictimSpeechStart = "PoliceMP:Callouts:DomesticAbuseVictimSpeechStart";
        public const string DomesticAbuseVictimScreamStop = "PoliceMP:Callouts:DomesticAbuseVictimScreamStop";
        public const string DomesticAbuseVictimCower = "PoliceMP:Callouts:DomesticAbuseVictimCower";

        #endregion
        
        #region BrokenDownVehicle
        public const string BrokenDownVehicleWander = "PoliceMP:Callouts:BrokenDownVehicleWander";
        public const string BrokenDownVehicleBreakDown = "PoliceMP:Callouts:BrokenDownVehicleBreakDown";
        #endregion
    }
}