using System.Threading.Tasks;
using CitizenFX.Core;

namespace PoliceMP.Server.Services
{
    public interface ISonoranService
    {
        Task SendPanicEvent(Player player);
    }
}