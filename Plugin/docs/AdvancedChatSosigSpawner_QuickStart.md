# Advanced Chat Sosig Spawner - Quick Start Guide

## What Changed?
The `AdvancedChatSosigSpawner` now works **without** requiring `TwitchChatManager`. You can use it as a standalone sosig spawner!

## Quick Start (Standalone Mode)

### 1. Disable Twitch Integration
Edit `BepInEx/config/H3TVR.cfg`:
```ini
[Chat Sosigs]
EnableTwitchChatSosigs = false
```

### 2. Use Keyboard Controls
| Key | Action |
|-----|--------|
| **P** | Spawn friendly sosig |
| **O** | Spawn enemy sosig |
| **Delete** | Clear all sosigs |
| **Insert** | Show stats |
| **F6** | Open armor GUI |

### 3. That's It!
No Twitch setup needed. Just spawn sosigs and have fun!

## Configuration (Standalone Mode)

Edit `BepInEx/config/H3TVR.cfg`:

```ini
[Chat Spawner]
# Basic settings
MaxAllySosigs = 8
MaxEnemySosigs = 8
SpawnCooldown = 2.0
EnableNameplates = true
EnableAutoCleanup = true

# Sosig pools (Update 120 TNH System)
AllySosigPool = M_Swat_Scout,M_Swat_Sniper,M_Swat_Breacher
EnemySosigPool = M_Swat_Heavy,M_Swat_Breacher,M_Swat_Riot

# Advanced features
EnableArmorCustomization = true
UseRandomNames = true
MaxSosigsPerUser = 2
EnableCoverAI = true

# Name files
AllyNamesFile = BepInEx/config/H3TVR_AllyNames.ini
EnemyNamesFile = BepInEx/config/H3TVR_EnemyNames.ini
```

## Optional: Enable Twitch Integration

If you want chat spawning:

```ini
[Chat Sosigs]
EnableTwitchChatSosigs = true
EnableLegacyFileMode = false

[Twitch Integration]
TwitchChannel = your_channel_name
AutoConnect = false
```

Then press **F8** in-game to configure OAuth.

## Name Files

Create custom sosig names in:
- `BepInEx/config/H3TVR_AllyNames.ini`
- `BepInEx/config/H3TVR_EnemyNames.ini`

Example:
```ini
# H3TVR Ally Names
Guardian
Protector
Defender
Support Bot
Friendly Unit
```

## Armor Presets

Edit `BepInEx/config/H3TVR_ArmorPresets.ini` for custom armor:
```ini
[Preset_Heavy]
Type = Armor
FactionIFF = 0
HeadArmor = ArmorHelmet_MetallicSWAT
TorsoArmor = ArmorVest_HeavyMetal
```

Press **F6** in-game to open the armor configuration GUI.

## Troubleshooting

### Sosigs won't spawn
- Check console for errors
- Verify max sosig limits not reached
- Try clearing existing sosigs (Delete key)

### No nameplates
- Set `EnableNameplates = true` in config
- Check if nameplate assets loaded

### Wrong sosig types spawning
- Edit `AllySosigPool` and `EnemySosigPool` in config
- See SosigEnemyID enum for valid types

## SosigEnemyID Reference

Common valid IDs for sosig pools:

### SWAT / Police
- M_Swat_Scout
- M_Swat_Sniper
- M_Swat_Heavy
- M_Swat_Breacher
- M_Swat_Riot

### Military
- M_Soldier_Scout
- M_Soldier_Sniper
- M_Soldier_Heavy

### Mercenaries
- M_Merc_Scout
- M_Merc_Sniper
- M_Merc_Heavy

### PMC
- M_PMC_Scout
- M_PMC_Sniper
- M_PMC_Heavy

### Zombies
- M_Zombies_Melee
- M_Zombies_Ranged

Check H3VR's SosigEnemyID enum for complete list!

## Advanced Usage

### Spawn with specific armor
1. Press F6 to open armor GUI
2. Select preset for allies/enemies
3. Spawn sosig (P or O key)
4. Armor automatically applied

### Custom behaviors
Edit configuration:
```ini
FollowDistance = 6.0          # How close allies follow
EnemyAggressionDistance = 20.0 # When enemies attack
EnableCoverAI = true           # Smart cover-taking
UpdateInterval = 1.0           # AI update frequency
```

### Per-user limits (for Twitch)
```ini
MaxSosigsPerUser = 2  # Each Twitch user can spawn 2 sosigs max
```

## Console Commands

Look for these log messages:
- `Advanced Chat Sosig Spawner initialized (standalone mode)` = Working correctly
- `Spawned ally sosig` = Successful spawn
- `Max ally sosigs reached` = Hit spawn limit

## Summary

? **Works standalone** - No Twitch needed  
? **Simple keyboard controls** - Just press P/O/Delete  
? **Highly configurable** - Edit config file  
? **Custom names** - Edit INI files  
? **Custom armor** - Use armor GUI (F6)  
? **Update 120 TNH** - Modern spawn system  
? **Optional Twitch** - Add chat integration if wanted  

Enjoy your sosigs! ??
