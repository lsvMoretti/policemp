using CitizenFX.Core;

namespace PoliceMP.Client.Scripts.Boot
{
    public interface IBootSystem
    {
        void OpenMenu(Vehicle vehicle);
        bool IsMenuActive();
        bool FetchDsuStatus();
    }
}