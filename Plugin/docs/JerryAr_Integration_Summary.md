# JerryAr Mods Integration - Quick Reference

## Added Features

### ?? Air Strike Smoke Grenade
- **Keybind**: F10
- **Function**: Spawns air strike smoke grenade from player head
- **Mod Required**: https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/
- **Item ID**: `JerryAr_AirStrikeSmokeGrenade`

### ?? Titan Machine (AI Enemy)
- **Keybind**: F11
- **Function**: Spawns hostile Titan Machine AI in front of player
- **Mod Required**: https://thunderstore.io/c/h3vr/p/JerryAr/TitanMachine/
- **Item ID**: `JerryAr_TitanMachine`

## Code Changes

### SpawnManager.cs
```csharp
? SpawnAirStrikeGrenade() - Spawns air strike smoke grenade
? SpawnTitanMachine() - Spawns Titan Machine as AI enemy
```

### H3TVRImproved.cs
```csharp
? Added "SpawnAirStrike" keybinding (F10)
? Added "SpawnTitanMachine" keybinding (F11)
```

### InputHandler.cs
```csharp
? Added SpawnAirStrike input handler
? Added SpawnTitanMachine input handler
```

## Features

### Air Strike Grenade
- ? Spawns from player head
- ? Automatic throw forward (500 force)
- ? Random torque for realism
- ? Audio feedback (before/after)
- ? Mod detection and validation
- ? Debug logging

### Titan Machine
- ? Spawns 5m in front of player
- ? Configured as hostile AI (IFF 1)
- ? Running assault speed
- ? Commands to attack player
- ? Audio feedback (before/after)
- ? Mod detection and validation
- ? Debug logging
- ? Graceful fallback if no Sosig component

## Audio Files (Optional)

### Air Strike
- `danger_close/airstrike_call.wav` (before)
- `danger_close/airstrike_deployed.wav` (after)

### Titan Machine
- `weapons/titan_materializing.wav` (before)
- `weapons/titan_active.wav` (after)

## Configuration Example

**BepInEx/config/H3TVR.cfg**
```ini
[KeyBindings]
KeyBindForSpawnAirStrike = F10
KeyBindForSpawnTitanMachine = F11
```

## Testing Checklist

- [?] Code compiles without errors
- [?] Keybindings added to configuration
- [?] Input handlers connected
- [?] Spawn methods implemented
- [?] Audio integration included
- [?] Mod detection/validation logic
- [?] Debug logging for troubleshooting
- [?] Documentation created

## User Instructions

1. Install required JerryAr mods from Thunderstore
2. Press **F10** to spawn Air Strike Smoke Grenade
3. Press **F11** to spawn Titan Machine as enemy

## Developer Notes

### Error Handling
Both methods include:
- Player body validation
- ItemManager dictionary checks
- Mod installation verification
- Try-catch exception handling
- Detailed logging

### Extensibility
Easy to add more JerryAr mods using the same pattern:
1. Add keybinding in `InitializeKeyBindings()`
2. Create spawn method in `SpawnManager.cs`
3. Add input handler in `InputHandler.cs`
4. Update documentation

## Build Status
? **Build Successful** - All changes compiled without errors
