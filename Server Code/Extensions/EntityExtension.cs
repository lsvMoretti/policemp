using CitizenFX.Core;
using CitizenFX.Core.Native;
using PoliceMP.Shared.Constants;

namespace PoliceMP.Server.Extensions
{
    public static class EntityExtension
    {
        public static void MarkEntityAsNoLongerRequired(this Entity entity)
        {
            if (!API.DoesEntityExist(entity.Handle)) return;
            
            entity.Owner.TriggerEvent(ServerEvents.MarkEntityAsNoLongerRequired, entity.NetworkId);
        }

        public static void SetPropOnGroundProperly(this Prop prop)
        {
            if (!API.DoesEntityExist(prop.Handle)) return;
            
            prop.Owner.TriggerEvent(ServerEvents.SetPropOnGroundProperly, prop.NetworkId);
        }
    }
}