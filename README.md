All code written in this repo was written and created by **Unsociable / Moretti (lsvMoretti on Github)**. Any code written by any other developer has been removed. This code was written for a FiveM British Emergency Server.

**No support or help shall be given. Code is uploaded *as is*.**

# Table of Contents
- [Table of Contents](#table-of-contents)
- [Client Side Code](#client-side-code)
- [Client Side Services & Interfaces](#client-side-services--interfaces)
- [Server Sided Code](#server-sided-code)

# Client Side Code

- [Spawn Script](Client%20Code/Spawn/SpawnScript.cs)
  - [Spawn Script Interface](Client%20Code/Spawn/ISpawnScript.cs)
- [Pointing Feature](Client%20Code/Pointing/Pointing.cs)
- [Hud System (Including Seatbelt / Speed Limiter)](Client%20Code/HUD/Hud.cs)
- [Fuel System](Client%20Code/Fuel/FuelScript.cs)
- [Armoury System](Client%20Code/Armoury/Armoury.cs)
  - [Armoury System Interface](Client%20Code/Armoury/IAmoury.cs)
- [Anticheat Systems](Client%20Code/Anticheat/)
  - [Blacklisted Weapons](Client%20Code/Anticheat/BlacklistWeapons.cs)
  - [Health Check](Client%20Code/Anticheat/HealthCheck.cs)
  - [Skin Check](Client%20Code/Anticheat/SkinCheck.cs)
- [Boot System](Client%20Code/Boot/BootSystem.cs)
  - [Boot System Interface](Client%20Code/Boot/IBootSystem.cs)
- [Civ Armoury](Client%20Code/Civ/CivArmoury.cs)
- [Other Commands (/shuff)](Client%20Code/Commands/OtherCommands.cs)
- [Roleplay Commands (/me /do)](Client%20Code/RoleplayCommands/RoleplayCommands.cs)
- [Weapon Fire Select](Client%20Code/Weapons/FireModeSelect.cs)
- [Flashbang Script](Client%20Code/Weapons/Flashbang.cs)
- [Weather Script](Client%20Code/Weather/WeatherScript.cs)
- [What Three Word Script](Client%20Code/WhatThreeWord/WhatThreeWordScript.cs)

# Client Side Services & Interfaces

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

# Server Sided Code

- [Commands](Server%20Code/Controllers/Commands/)
  - [AFK Commands](Server%20Code/Controllers/Commands/AfkCommands.cs)
  - [DSU Commands](Server%20Code/Controllers/Commands/DsuCommands.cs)
  - [Roleplay Commands](Server%20Code/Controllers/Commands/RoleplayCommands.cs)
- [Ace Controller](Server%20Code/Controllers/AceController.cs)
- [Backup Controller](Server%20Code/Controllers/BackupController.cs)
- [Custom Character Controller](Server%20Code/Controllers/CustomCharacterController.cs)
- [Flashbang Controller](Server%20Code/Controllers/FlashbangController.cs)
- [Player Info Controller](Server%20Code/Controllers/PlayerInfoController.cs)
- [Player Service Controller](Server%20Code/Controllers/PlayerServiceController.cs)