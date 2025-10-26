# H3TVR TNH Customizer Integration - Complete Guide

## Overview

H3TVR now includes TNH Customizer functionality, allowing you to create custom Take and Hold characters with personalized settings, equipment pools, and difficulty modifiers. This integration is compatible with Nicole's TNH_Customizer mod.

**Mod Link:** https://thunderstore.io/c/h3vr/p/Nicole/TNH_Customizer/

## Features

? **Custom Characters** - Create unique TNH characters with custom settings  
? **Equipment Pools** - Define custom weapon and equipment pools  
? **Difficulty Modifiers** - Adjust health, tokens, and enemy stats  
? **Progression Tweaks** - Modify hold counts and requirements  
? **Resource Management** - Toggle unlimited ammo/tokens  
? **Enemy Customization** - Control enemy types and stats  
? **Built-in Presets** - 5 ready-to-use character presets  

## Quick Start

### 1. Enable TNH Customizer

Edit `BepInEx/config/com.h3tvr.improved.cfg`:

```ini
[TNH Customizer]
EnableCustomCharacters = true
ActiveCharacter = EasyMode
```

### 2. Available Preset Characters

| Character | Description | Difficulty |
|-----------|-------------|------------|
| **EasyMode** | More health, tokens, easier enemies | Easy |
| **HardMode** | Less health, tokens, tougher enemies | Hard |
| **InfiniteMode** | Unlimited ammo and tokens | Sandbox |
| **SpeedRun** | Fast-paced, no encryption | Medium |
| **RealisticMode** | Low health, realistic combat | Very Hard |

### 3. Select a Character

```ini
[TNH Customizer]
ActiveCharacter = EasyMode
```

### 4. Start TNH

Launch TNH and your custom character settings will be applied automatically!

## Configuration Reference

### Basic Settings

```ini
[TNH Customizer]
# Enable/disable custom characters
EnableCustomCharacters = true

# Enable custom equipment pools
EnableCustomPools = true

# Enable progression modifications
EnableProgressionMods = true

# Enable spawn modifications
EnableSpawnMods = true

# Active character name
ActiveCharacter = Default

# Starting resources
StartingTokens = 3
MaxHealth = 1000

# Modifiers
UnlimitedAmmo = false
UnlimitedTokens = false
HealthMultiplier = 1.0
SosigHealthMultiplier = 1.0
SosigSpeedMultiplier = 1.0
RequiredHolds = 5
```

## Creating Custom Characters

### Method 1: Configuration File

1. Create a new file: `BepInEx/config/H3TVR_TNH_Characters/MyCharacter.ini`
2. Use the template from `H3TVR_TNH_Character_Example.ini`
3. Configure your character settings
4. Activate in main config

### Method 2: Code Integration

```csharp
// Get TNH Customizer instance
var tnhCustomizer = TNHCustomizerIntegration.Instance;

// Create new character
var myCharacter = new TNHCustomizerIntegration.CustomTNHCharacter
{
    CharacterName = "MyCharacter",
    DisplayName = "My Custom Character",
    Description = "A personalized TNH experience",
    StartingTokens = 5,
    StartingHealth = 1200,
    RequiredHolds = 6,
    HealthMultiplier = 1.5f,
    EnemyHealthMultiplier = 1.2f,
    DisableEncryption = false
};

// Register character
tnhCustomizer.CreateCustomCharacter(myCharacter);

// Activate character
tnhCustomizer.SetActiveCharacter("MyCharacter");
```

## Character Settings Explained

### Starting Settings

| Setting | Description | Default | Range |
|---------|-------------|---------|-------|
| `StartingTokens` | Initial tokens at game start | 3 | 0-999 |
| `StartingHealth` | Maximum health | 1000 | 100-10000 |
| `RequiredHolds` | Holds needed to complete | 5 | 1-20 |

### Player Modifiers

| Setting | Description | Default | Range |
|---------|-------------|---------|-------|
| `UnlimitedAmmo` | Infinite ammo for all weapons | false | true/false |
| `UnlimitedTokens` | Infinite tokens at supply points | false | true/false |
| `HealthMultiplier` | Player health multiplier | 1.0 | 0.1-10.0 |
| `DisableEncryption` | Skip encryption challenges | false | true/false |

### Enemy Settings

| Setting | Description | Default | Range |
|---------|-------------|---------|-------|
| `EnemyHealthMultiplier` | Enemy health multiplier | 1.0 | 0.1-10.0 |
| `EnemySpeedMultiplier` | Enemy movement speed multiplier | 1.0 | 0.1-3.0 |
| `EnemyPool` | List of enemy sosig types | varies | SosigEnemyID enum |

## Preset Characters Guide

### Easy Mode
**Best for:** Beginners, learning TNH mechanics

- **Starting Tokens:** 10
- **Health:** 2000 (2x normal)
- **Required Holds:** 3 (shorter run)
- **Enemy Health:** 0.5x (easier to kill)
- **Enemy Speed:** 0.8x (slower)
- **Encryption:** Disabled

### Hard Mode
**Best for:** Experienced players, challenge seekers

- **Starting Tokens:** 1
- **Health:** 500 (0.5x normal)
- **Required Holds:** 7 (longer run)
- **Enemy Health:** 2.0x (harder to kill)
- **Enemy Speed:** 1.5x (faster)
- **Encryption:** Enabled

### Infinite Mode
**Best for:** Sandbox play, testing weapons

- **Starting Tokens:** 999
- **Unlimited Ammo:** Yes
- **Unlimited Tokens:** Yes
- **Encryption:** Disabled
- **Normal difficulty** enemies

### Speed Run
**Best for:** Fast-paced action, speedrunning

- **Starting Tokens:** 5
- **Enemy Health:** 0.8x
- **Enemy Speed:** 1.2x (faster enemies!)
- **Encryption:** Disabled
- **Standard 5 holds**

### Realistic Mode
**Best for:** Simulation players, hardcore challenge

- **Starting Tokens:** 2
- **Health:** 100 (0.1x normal - one-shot kills!)
- **Enemy Health:** 0.3x (realistic)
- **Normal enemy speed**
- **Encryption:** Enabled

## Equipment Pools

### Weapon Pool Configuration

Define which weapons appear at supply points:

```ini
[Weapon Pools]
# Primary weapons (rifles, shotguns, etc.)
PrimaryWeaponPool=AssaultRifle_M4,AssaultRifle_AK74,Shotgun_M870

# Secondary weapons (pistols, SMGs)
SecondaryWeaponPool=Pistol_M1911,Pistol_Glock17,SMG_MP5

# Tertiary (melee, special weapons)
TertiaryWeaponPool=Grenade_M67,C4_Explosive

# Shields
ShieldPool=Shield_Riot,Shield_Ballistic

# Consumables
ConsumablePool=HealthKit,Ammo_556,Ammo_9mm
```

### Enemy Pool Configuration

Control which enemy types spawn:

```ini
[Enemy Settings]
# SWAT enemies
EnemyPool=M_Swat_Scout,M_Swat_Heavy,M_Swat_Sniper

# PMC enemies
EnemyPool=M_PMC_Scout,M_PMC_Heavy,M_PMC_Sniper

# Mixed
EnemyPool=M_Swat_Scout,M_PMC_Heavy,M_Zombies_Ranged
```

## Advanced Features

### Dynamic Difficulty Scaling

Combine modifiers for custom difficulty:

```ini
[Player Modifiers]
# Glass cannon build
HealthMultiplier = 0.5
UnlimitedAmmo = true

[Enemy Settings]
# Faster but weaker enemies
EnemyHealthMultiplier = 0.7
EnemySpeedMultiplier = 1.5
```

### Themed Runs

**Zombie Apocalypse:**
```ini
[Enemy Settings]
EnemyPool=M_Zombies_Melee,M_Zombies_Ranged
EnemyHealthMultiplier=0.3
EnemySpeedMultiplier=0.8

[Player Modifiers]
HealthMultiplier=2.0
```

**PMC Ops:**
```ini
[Enemy Settings]
EnemyPool=M_PMC_Scout,M_PMC_Heavy,M_PMC_Sniper
EnemyHealthMultiplier=1.5
EnemySpeedMultiplier=1.2
```

**WW2 Mode:**
```ini
[Enemy Settings]
EnemyPool=WW2_Axis_Rifleman,WW2_Axis_MG

[Weapon Pools]
PrimaryWeaponPool=Rifle_M1Garand,Rifle_Thompson,SMG_Grease
```

## Integration with H3TVR Features

### Compatible Features
? Infinite Tokens (H3TVR)  
? Disable Encryption Nodes (H3TVR)  
? Advanced Chat Sosig Spawner  
? Boss Sosig System  
? Advanced AI  
? Steam Friends Integration  

### Combined Settings Example

```ini
[TakeAndHold]
# H3TVR native settings
InfiniteTokens = true
DisableEncryptionNodes = true

[TNH Customizer]
# Custom character with modifiers
ActiveCharacter = SpeedRun
EnableCustomCharacters = true
```

## API Reference

### Get Available Characters

```csharp
var characters = TNHCustomizerIntegration.Instance.GetAvailableCharacters();
foreach (var charName in characters)
{
    Logger.LogInfo($"Available character: {charName}");
}
```

### Get Character Details

```csharp
var character = TNHCustomizerIntegration.Instance.GetCharacter("EasyMode");
if (character != null)
{
    Logger.LogInfo($"Character: {character.DisplayName}");
    Logger.LogInfo($"Tokens: {character.StartingTokens}");
    Logger.LogInfo($"Health: {character.StartingHealth}");
}
```

### Set Active Character

```csharp
bool success = TNHCustomizerIntegration.Instance.SetActiveCharacter("HardMode");
if (success)
{
    Logger.LogInfo("Hard Mode activated!");
}
```

### Create New Character

```csharp
var newChar = new TNHCustomizerIntegration.CustomTNHCharacter
{
    CharacterName = "MyChar",
    DisplayName = "My Character",
    StartingTokens = 7,
    RequiredHolds = 6
};

TNHCustomizerIntegration.Instance.CreateCustomCharacter(newChar);
```

## Troubleshooting

### Character Not Applying

**Issue:** Custom character settings not active in TNH

**Solutions:**
1. Verify `EnableCustomCharacters = true` in config
2. Check `ActiveCharacter` matches character name exactly
3. Restart H3VR after config changes
4. Check BepInEx console for errors

### Weapon Pools Not Working

**Issue:** Custom weapon pools not appearing

**Solutions:**
1. Enable `EnableCustomPools = true`
2. Verify weapon IDs are valid FVRObject ItemIDs
3. Check logs for invalid item warnings
4. Use comma-separated lists without spaces

### Enemy Multipliers Not Applying

**Issue:** Enemy health/speed not changing

**Solutions:**
1. Enable `EnableSpawnMods = true`
2. Verify multiplier values are reasonable (0.1-10.0)
3. Check that enemies are spawning in TNH mode
4. Look for errors in console

## Performance Considerations

### Optimized Settings

```ini
# Good performance
MaxAllySosigs = 6
MaxEnemySosigs = 6
EnableCustomPools = true
EnableSpawnMods = true
```

### High Performance Impact

```ini
# May impact performance
SosigHealthMultiplier = 10.0  # Very high health
RequiredHolds = 20            # Long sessions
```

## Future Enhancements

Planned features:
- [ ] Custom spawn rules
- [ ] Custom supply point types
- [ ] Custom objectives
- [ ] Character progression systems
- [ ] Loadout presets
- [ ] Achievement tracking

## Credits

- **Nicole** - Original TNH_Customizer mod
- **H3TVR Team** - Integration implementation
- **Anton Hand** - H3VR and TNH game mode

## Support

For issues or questions:
1. Check BepInEx console for errors
2. Verify configuration syntax
3. Test with default preset characters
4. Report bugs with full error logs

---

**Status:** ? Complete - Fully functional  
**Version:** H3TVR 1.4.0+  
**Compatibility:** All TNH modes  
**Last Updated:** January 2025
