# JerryAr Mod Integration Guide

## Overview
H3TVR Enhanced Edition now includes support for spawning items from JerryAr's mods. This integration adds two powerful new features:

1. **Air Strike Smoke Grenade** - Call in air strikes with smoke grenades
2. **Titan Machine** - Spawn hostile AI Titan Machines

---

## Air Strike Smoke Grenade

### Description
Spawns an Air Strike Smoke Grenade that can be thrown to call in air support.

### Mod Link
https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/

### Default Keybind
**F10** - Spawn Air Strike Smoke Grenade

### Features
- Spawns from player head position
- Automatically thrown forward with force
- Includes audio feedback (before/after spawn)
- Validates mod installation and provides debug info

### Item ID
`JerryAr_AirStrikeSmokeGrenade`

### Usage
1. Press **F10** (default) to spawn the grenade
2. The grenade will be thrown forward automatically
3. Use the grenade's functionality as designed by the mod

### Configuration
You can change the keybind in:
```
BepInEx/config/H3TVR.cfg
```

Look for:
```ini
[KeyBindings]
KeyBindForSpawnAirStrike = F10
```

### Audio Support
Custom audio files can be placed in:
- `BepInEx/plugins/H3TVR/Audio/danger_close/airstrike_call.wav` (before spawn)
- `BepInEx/plugins/H3TVR/Audio/danger_close/airstrike_deployed.wav` (after spawn)

---

## Titan Machine AI

### Description
Spawns a Titan Machine as a hostile AI enemy that will attack the player.

### Mod Link
https://thunderstore.io/c/h3vr/p/JerryAr/TitanMachine/

### Default Keybind
**F11** - Spawn Titan Machine (AI Enemy)

### Features
- Spawns 5 meters in front of player
- Automatically configured as hostile AI (if sosig component available)
- Set to enemy team (IFF 1)
- Commands to assault player position
- Includes audio feedback (before/after spawn)
- Validates mod installation and provides debug info

### Item ID
`JerryAr_TitanMachine`

### Usage
1. Press **F11** (default) to spawn the Titan Machine
2. The Titan will spawn in front of you as an enemy
3. Engage in combat!

### AI Behavior
If the Titan Machine has a Sosig component, it will be configured to:
- **Team**: Enemy (IFF 1)
- **Movement**: Running speed
- **Behavior**: Assault player position

If no Sosig component is found, the Titan will use its default AI (if any).

### Configuration
You can change the keybind in:
```
BepInEx/config/H3TVR.cfg
```

Look for:
```ini
[KeyBindings]
KeyBindForSpawnTitanMachine = F11
```

### Audio Support
Custom audio files can be placed in:
- `BepInEx/plugins/H3TVR/Audio/weapons/titan_materializing.wav` (before spawn)
- `BepInEx/plugins/H3TVR/Audio/weapons/titan_active.wav` (after spawn)

---

## Installation Requirements

### Required Mods
To use these features, you must install the corresponding JerryAr mods:

1. **Air Strike Smoke Grenade**
   - https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/
   
2. **Titan Machine**
   - https://thunderstore.io/c/h3vr/p/JerryAr/TitanMachine/

### Installation Steps
1. Install the desired JerryAr mods from Thunderstore
2. Install H3TVR Enhanced Edition
3. Launch H3VR
4. Use the keybinds to spawn the items

---

## Troubleshooting

### Item Not Spawning
If items don't spawn, check the BepInEx console for error messages.

**Common Issues:**

1. **Mod Not Installed**
   ```
   [Warning] Air Strike Smoke Grenade not available. Install: https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/
   ```
   **Solution:** Install the mod from Thunderstore

2. **Wrong Item ID**
   The console will list available items matching the search terms. Check if the Item ID has changed.
   
3. **Mod Load Order**
   Ensure JerryAr mods load before H3TVR in the BepInEx load order.

### Debugging
Enable verbose logging by checking the BepInEx console when pressing the keybinds. H3TVR will:
- List expected Item IDs
- Show available similar items
- Report successful spawns
- Display configuration details

---

## Technical Details

### Spawn Positions
- **Air Strike Grenade**: Player head + 0.25m up
- **Titan Machine**: Player head + 5m forward

### Force Applied
- **Air Strike Grenade**: 500 units forward + random torque
- **Titan Machine**: None (spawns in place)

### Spawn Validation
Both spawn methods include:
- Player body validation
- ItemManager dictionary check
- Mod installation verification
- Item ID validation
- Error handling and logging

### Audio System Integration
Both features integrate with H3TVR's AudioManager:
- Configurable audio files
- 3D spatial audio
- Volume control
- Before/after spawn sounds

---

## Advanced Configuration

### Custom Item IDs
If the mod author changes the Item IDs, you can update them in the code:

**SpawnManager.cs**
```csharp
// Change these if Item IDs are different
string airStrikeID = "JerryAr_AirStrikeSmokeGrenade";
string titanID = "JerryAr_TitanMachine";
```

### AI Configuration
Modify Titan Machine AI behavior in SpawnManager.cs:
```csharp
sosig.SetIFF(1); // Team: 0=player, 1=enemy, 2=neutral
sosig.SetAssaultSpeed(Sosig.SosigMoveSpeed.Running); // Speed
sosig.CommandAssaultPoint(targetPosition); // Target
```

---

## Future Enhancements

Potential future additions:
- Configuration for spawn distance
- Multiple Titan spawn option
- Air strike count configuration
- Team selection for Titan Machines
- Custom AI behaviors
- Spawn cooldowns

---

## Credits

### Mod Authors
- **JerryAr** - Creator of Air Strike Smoke Grenade and Titan Machine mods

### Integration
- **H3TVR Enhanced Edition Team** - Integration into H3TVR

---

## Version History

### v1.0.0 (Current)
- Initial integration of Air Strike Smoke Grenade
- Initial integration of Titan Machine AI
- Default keybinds (F10, F11)
- Audio support
- Debug logging
- Mod detection and validation

---

## Related Documentation
- [Custom Audio System Guide](Custom_Audio_System_Guide.md)
- [InputHandler Documentation](../src/InputHandler.cs)
- [SpawnManager Documentation](../src/SpawnManager.cs)
- [H3TVR Configuration Guide](H3TVR_Configuration.md)
