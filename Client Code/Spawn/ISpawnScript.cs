using System.Threading.Tasks;

namespace PoliceMP.Client.Scripts.Spawn
{
    public interface ISpawnScript
    {
        public Task CreateSpawnSelectionMenu(bool firstLoadIn);
    }
}