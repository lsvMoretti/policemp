using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PoliceMP.Shared.Enums
{
    public enum LocationType
    {
        [Description("Random Pavement Position")]
        RandomPositionOnPavement,
        [Description("Random Road Position")]
        RandomPositionOnStreet,
        [Description("Shop")]
        Shop,
        [Description("PetrolStationShop")]
        PetrolStationShop,
        [Description("PetrolStationForecourt")]
        PetrolStationForecourt,
        [Description("TrainStationPlatform")]
        TrainStationPlatform,
        [Description("TrainStationVicinity")]
        TrainStationVicinity,
        [Description("Park")]
        Park,
        [Description("Field")]
        Field,
        [Description("WalkingPath")]
        WalkingPath,
        [Description("RuralAttraction")]
        RuralAttraction,
        [Description("CityAttraction")]
        CityAttraction,
        [Description("AirportRunway")]
        AirportRunway,
        [Description("AirportApron")]
        AirportApron,
        [Description("AirportTerminal")]
        AirportTerminal,
        [Description("DickyDev")]
        Dev,
        [Description("CarPark")]
        CarPark
    }
}