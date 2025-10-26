# TNH Customizer Integration - Quick Reference

## ?? Quick Start

### Enable TNH Customizer
```ini
[TNH Customizer]
EnableCustomCharacters = true
ActiveCharacter = EasyMode
```

### Select Preset Character
| Character | Difficulty | Features |
|-----------|------------|----------|
| `EasyMode` | Easy | 2x health, 10 tokens, weaker enemies |
| `HardMode` | Hard | 0.5x health, 1 token, tougher enemies |
| `InfiniteMode` | Sandbox | Unlimited ammo/tokens |
| `SpeedRun` | Medium | Fast pace, no encryption |
| `RealisticMode` | Very Hard | 0.1x health (one-shot kills!) |

## ?? Configuration Keys

### Basic Settings
```ini
[TNH Customizer]
EnableCustomCharacters = true     # Master toggle
EnableCustomPools = true          # Custom weapon pools
EnableProgressionMods = true      # Modify progression
EnableSpawnMods = true            # Modify enemy stats
ActiveCharacter = Default         # Character name
```

### Resource Modifiers
```ini
StartingTokens = 3               # Initial tokens
MaxHealth = 1000                 # Max health
UnlimitedAmmo = false            # Infinite ammo
UnlimitedTokens = false          # Infinite tokens
```

### Player Stats
```ini
HealthMultiplier = 1.0           # Player health multiplier
```

### Enemy Stats
```ini
SosigHealthMultiplier = 1.0      # Enemy health multiplier
SosigSpeedMultiplier = 1.0       # Enemy speed multiplier
RequiredHolds = 5                # Holds to complete
```

## ?? Preset Character Stats

### Easy Mode
- Health: 2000 (2x)
- Tokens: 10
- Holds: 3
- Enemy Health: 0.5x
- Enemy Speed: 0.8x
- Encryption: OFF

### Hard Mode
- Health: 500 (0.5x)
- Tokens: 1
- Holds: 7
- Enemy Health: 2.0x
- Enemy Speed: 1.5x
- Encryption: ON

### Infinite Mode
- Health: 1000
- Tokens: 999
- Unlimited Ammo: YES
- Unlimited Tokens: YES
- Encryption: OFF

### Speed Run
- Health: 1000
- Tokens: 5
- Enemy Health: 0.8x
- Enemy Speed: 1.2x (faster!)
- Encryption: OFF

### Realistic Mode
- Health: 100 (0.1x - one-shot!)
- Tokens: 2
- Enemy Health: 0.3x (realistic)
- Encryption: ON

## ?? API Quick Usage

### C# Code Examples

#### Set Active Character
```csharp
TNHCustomizerIntegration.Instance.SetActiveCharacter("HardMode");
```

#### Get Available Characters
```csharp
var chars = TNHCustomizerIntegration.Instance.GetAvailableCharacters();
foreach (var name in chars)
    Debug.Log(name);
```

#### Create Custom Character
```csharp
var custom = new TNHCustomizerIntegration.CustomTNHCharacter
{
    CharacterName = "MyChar",
    DisplayName = "My Character",
    StartingTokens = 7,
    RequiredHolds = 6,
    HealthMultiplier = 1.5f
};
TNHCustomizerIntegration.Instance.CreateCustomCharacter(custom);
```

## ?? Custom Character File Format

Create `BepInEx/config/H3TVR_TNH_Characters/MyChar.ini`:

```ini
[Character Info]
CharacterName=MyChar
DisplayName=My Character
Description=Custom TNH character

[Starting Settings]
StartingTokens=5
StartingHealth=1200
RequiredHolds=6

[Enemy Settings]
EnemyHealthMultiplier=1.2
EnemySpeedMultiplier=1.1

[Player Modifiers]
UnlimitedAmmo=false
HealthMultiplier=1.5
DisableEncryption=false
```

## ??? Troubleshooting

### Character Not Active
1. Check `EnableCustomCharacters = true`
2. Verify character name matches exactly
3. Restart H3VR

### Settings Not Applying
1. Enable specific feature toggles
2. Check multiplier ranges (0.1-10.0)
3. Verify in TNH mode

## ? Integration with H3TVR

### Compatible Features
? Infinite Tokens (H3TVR)  
? Disable Encryption (H3TVR)  
? Chat Sosig Spawner  
? Boss System  
? Advanced AI  

### Combined Config Example
```ini
[TakeAndHold]
InfiniteTokens = true
DisableEncryptionNodes = true

[TNH Customizer]
ActiveCharacter = SpeedRun
EnableCustomCharacters = true
```

## ?? Performance Tips

### Optimized Settings
```ini
# Good performance
RequiredHolds = 5
SosigHealthMultiplier = 1.0
```

### May Impact Performance
```ini
# Use with caution
SosigHealthMultiplier = 10.0
RequiredHolds = 20
```

## ?? More Information

- Full Guide: `docs/TNH_Customizer_Integration_Guide.md`
- Example Config: `config/H3TVR_TNH_Character_Example.ini`
- H3VR TNH Customizer: https://thunderstore.io/c/h3vr/p/Nicole/TNH_Customizer/

---

**Make TNH exactly how you want it!** ??
