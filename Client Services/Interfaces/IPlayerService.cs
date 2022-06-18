using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Services
{
    public interface IPlayerService
    {
        Task<List<int>> FetchAllPlayerNetworkIds();
        Task<string> FetchPlayerNameFromNetworkId(int networkId);
        Task<List<PlayerInfo>> FetchAllRecentPlayerInfo();
        Task<PlayerInfo> FetchPlayerInfoFromNetworkId(int networkId);
    }
}