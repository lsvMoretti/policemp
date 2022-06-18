using System.Collections.Generic;
using System.Threading.Tasks;
using PoliceMP.Shared.Models;

namespace PoliceMP.Client.Services.Interfaces
{
    public interface ICustomCharacterService
    {
        List<CustomCharacter> FetchCustomCharacters();

        /// <summary>
        /// Show the Character Creator
        /// </summary>
        /// <param name="gender">-1 Select, 0 Male, 1 Female</param>
        void ShowCharacterCreator(int gender);

        void SetCharacterAppearance(string name, string appearanceJson);
        void SetCharacterAppearance(CustomCharacter customCharacter);
        void DeleteCustomCharacter(string name, string appearance);
        void SaveOutfitToCharacter(string name, string appearanceJson);
    }
}