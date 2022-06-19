using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace PoliceMP.Server.Services
{
    public interface ITickService
    {
        void On(Func<Task> handler);
        void Off(Func<Task> handler);
    }
}