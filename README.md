All code written in this repo was written and created by **Unsociable / Moretti (lsvMoretti on Github)**. Any code written by any other developer has been removed. This code was written for a FiveM British Emergency Server.

**No support or help shall be given. Code is uploaded *as is*.**

----
# Table of Contents
- [Table of Contents](#table-of-contents)
- [Client Side Code](#client-side-code)
  - [Client Side Scripts](#client-side-scripts)
  - [Client Side Services & Interfaces](#client-side-services--interfaces)
  - [Client Side Extensions](#client-side-extensions)
- [Server Sided Code](#server-sided-code)
  - [Controllers (Scripts)](#controllers-scripts)
  - [Extensions](#extensions)
  - [Services](#services)
- [Shared Code](#shared-code)
- [Discord Bot](#discord-bot)

----
# Client Side Code
## Client Side Scripts
- [Anticheat Systems](Client%20Code/Anticheat/)
  - [Blacklisted Weapons](Client%20Code/Anticheat/BlacklistWeapons.cs)
  - [Health Check](Client%20Code/Anticheat/HealthCheck.cs)
  - [Skin Check](Client%20Code/Anticheat/SkinCheck.cs)
- [Armoury System](Client%20Code/Armoury/Armoury.cs)
  - [Armoury System Interface](Client%20Code/Armoury/IAmoury.cs)
- [Boot System](Client%20Code/Boot/BootSystem.cs)
  - [Boot System Interface](Client%20Code/Boot/IBootSystem.cs)
- [Civ Armoury](Client%20Code/Civ/CivArmoury.cs)
- [Commands (/shuff)](Client%20Code/Commands/OtherCommands.cs)
- [Dog System](Client%20Code/Dsu/Dog.cs)
- [DSU Naming System](Client%20Code/Dsu/DsuNaming.cs)
- [Flashbang Script](Client%20Code/Weapons/Flashbang.cs)
- [Fuel System](Client%20Code/Fuel/FuelScript.cs)
- [Hud System (Including Seatbelt / Speed Limiter)](Client%20Code/HUD/Hud.cs)
- [Pointing Feature](Client%20Code/Pointing/Pointing.cs)
- [Roleplay Commands (/me /do)](Client%20Code/RoleplayCommands/RoleplayCommands.cs)
- [Spawn Script](Client%20Code/Spawn/SpawnScript.cs)
  - [Spawn Script Interface](Client%20Code/Spawn/ISpawnScript.cs)
- [Weapon Fire Select](Client%20Code/Weapons/FireModeSelect.cs)
- [Weather Script](Client%20Code/Weather/WeatherScript.cs)
- [What Three Word Script](Client%20Code/WhatThreeWord/WhatThreeWordScript.cs)

----
## Client Side Services & Interfaces
- [Custom Character Service](Client%20Services/CustomCharacterService.cs)
  - [Custom Character Service Interface](Client%20Services/Interfaces/ICustomCharacterService.cs)
- [Feature Service](Client%20Services/FeatureService.cs)
  - [Feature Service Interface](Client%20Services/Interfaces/IFeatureService.cs)
- [Permission Service](Client%20Services/PermissionService.cs)
  - [Permission Service Interface](Client%20Services/Interfaces/IPermissionService.cs)
- [Player Service](Client%20Services/PlayerService.cs)
  - [Player Service Interface](Client%20Services/Interfaces/IPlayerService.cs)
- [Vehicle Info Service](Client%20Services/VehicleInfoService.cs)
  - [Vehicle Info Service Interface](Client%20Services/Interfaces/IVehicleInfoService.cs)


----
## Client Side Extensions
- [Vector3 Extension](Client%20Extensions/Vector3Extension.cs)

----
# Server Sided Code
## Controllers (Scripts)

- [Commands](Server%20Code/Controllers/Commands/)
  - [AFK Commands](Server%20Code/Controllers/Commands/AfkCommands.cs)
  - [DSU Commands](Server%20Code/Controllers/Commands/DsuCommands.cs)
  - [Roleplay Commands](Server%20Code/Controllers/Commands/RoleplayCommands.cs)
- [Ace Controller](Server%20Code/Controllers/AceController.cs)
- [Backup Controller](Server%20Code/Controllers/BackupController.cs)
- [Custom Character Controller](Server%20Code/Controllers/CustomCharacterController.cs)
- [Dog Controller](Server%20Code/Controllers/DogController.cs)
- [Flashbang Controller](Server%20Code/Controllers/FlashbangController.cs)
- [Player Info Controller](Server%20Code/Controllers/PlayerInfoController.cs)
- [Player Service Controller](Server%20Code/Controllers/PlayerServiceController.cs)
- [Time & Weather Controller](Server%20Code/Controllers/TimeWeatherController.cs)
- [Added WhatThreeWord Controller](Server%20Code/Controllers/WhatThreeWordsController.cs)

----
## Extensions
- [Entity Extension](Server%20Code/Extensions/EntityExtension.cs)
- [Enum Extension](Server%20Code/Extensions/EnumExtension.cs)
- [Vector3 Extension](Server%20Code/Extensions/Vector3Extension.cs)

----
## Services
- [Callouts](Server%20Code/Services/Callouts/)
  - [Callout Types](Server%20Code/Services/Callouts/CalloutTypes)
    - [Broken Down Vehicle](Server%20Code/Services/Callouts/CalloutTypes/BrokenDownVehicle.cs)
    - [Domestic Abuse](Server%20Code/Services/Callouts/CalloutTypes/DomesticAbuse.cs)
    - [Stabbing](Server%20Code/Services/Callouts/CalloutTypes/Stabbing.cs)
    - [Vehicle Break In](Server%20Code/Services/Callouts/CalloutTypes/VehicleBreakIn.cs)
  - [Callout](Server%20Code/Services/Callouts/Callout.cs)
  - [Callout Manager](Server%20Code/Services/Callouts/CalloutManager.cs)
  - [Callout Manager Builder](Server%20Code/Services/Callouts/CalloutManagerBuilder.cs)
  - [Callout Options](Server%20Code/Services/Callouts/CalloutOptions.cs)
  - [ICallout](Server%20Code/Services/Callouts/ICallout.cs)
  - [Service Collection Extension](Server%20Code/Services/Callouts/ServiceCollectionExtension.cs)
- [Activity Service](Server%20Code/Services/ActivityService.cs)
- [Feature Service](Server%20Code/Services/FeatureService.cs)
  - [IFeature Service](Server%20Code/Services/Interfaces/IFeatureService.cs)
- [Permission Service](Server%20Code/Services/PermissionService.cs)
  - [IPermission Service](Server%20Code/Services/Interfaces/IPermissionService.cs)
- [Random Entity Service](Server%20Code/Services/RandomEntityService.cs)
- [Sonoran Service](Server%20Code/Services/SonoranService.cs)
  - [ISonoran Service](Server%20Code/Services/Interfaces/ISonoranService.cs)
- [Tick Service](Server%20Code/Services/TickService.cs)
- [Vehicle Info Service](Server%20Code/Services/VehicleInfoService.cs)

----
# Shared Code
- [Constants](Shared%20Code/Constants/)
  - [Callout Events](Shared%20Code/Constants/CalloutEvents.cs)
  - [Feature Toggle](Shared%20Code/Constants/FeatureToggle.cs)
- [Enums](Shared%20Code/Enums/)
  - [Callout Grade](Shared%20Code/Enums/CalloutGrade.cs)
  - [Dog Anim](Shared%20Code/Enums/DogAnim.cs)
  - [Dog FX](Shared%20Code/Enums/DogFx.cs)
  - [Dog Sound](Shared%20Code/Enums/DogSound.cs)
  - [Location Type](Shared%20Code/Enums/LocationType.cs)
  - [Script Task Hash](Shared%20Code/Enums/ScriptTaskHash.cs)
  - [User Branch](Shared%20Code/Enums/UserBranch.cs)
  - [User Division](Shared%20Code/Enums/UserDivision.cs)
  - [Vehicle Data Class](Shared%20Code/Enums/VehicleDataClass.cs)
- [Models](Shared%20Code/Models/)
  - [Weather](Shared%20Code/Models/Weather/)
    - [Clouds](Shared%20Code/Models/Weather/Clouds.cs)
    - [ConvertTemp](Shared%20Code/Models/Weather/ConvertTemp.cs)
    - [Coord](Shared%20Code/Models/Weather/Coord.cs)
    - [Main](Shared%20Code/Models/Weather/Main.cs)
    - [OpenWeather](Shared%20Code/Models/Weather/OpenWeather.cs)
    - [Sys](Shared%20Code/Models/Weather/Sys.cs)
    - [Weather](Shared%20Code/Models/Weather/Weather.cs)
    - [Wind](Shared%20Code/Models/Weather/Wind.cs)
  - [Custom Character](Shared%20Code/Models/CustomCharacter.cs)
  - [Player Info](Shared%20Code/Models/PlayerInfo.cs)
  - [Position List](Shared%20Code/Models/PositionList.cs)
  - [Road Management Item](Shared%20Code/Models/RoadManagementItem.cs)
  - [User Aces](Shared%20Code/Models/UserAces.cs)
  - [User Role](Shared%20Code/Models/UserRole.cs)
  
---
# Discord Bot
  - [Discord Bot](Discord%20Bot/)
