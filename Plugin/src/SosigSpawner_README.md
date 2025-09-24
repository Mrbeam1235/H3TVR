# Advanced Sosig Spawner System

The Advanced Sosig Spawner System is a comprehensive solution for spawning customizable sosigs in H3VR with full control over faction, equipment, and armor configurations.

## Features

### 🎯 Core Functionality
- **GUI-based spawning interface** - Easy-to-use in-game menu (Default: F9 key)
- **Faction control** - Spawn as allies, enemies, or neutral units
- **Custom armor configuration** - Fine-tune armor pieces and appearance chances
- **Multiple spawn modes** - Single sosig or squad spawning
- **Real-time management** - Track and manage all spawned sosigs

### ⚔️ Faction System
- **Friendly (IFF 0)** - Allied to player, will follow and assist
- **Enemy (IFF 1)** - Hostile to player, will attack on sight  
- **Neutral (IFF 2)** - Non-combatant, ignores player
- **Custom IFF** - Define your own faction codes (0-10)

### 🛡️ Armor Customization
Control the appearance of spawned sosigs with detailed armor settings:
- **Headwear** - Helmets, hats, and head protection
- **Facewear** - Masks, face protection, and accessories
- **Eyewear** - Goggles, glasses, and eye protection
- **Torsowear** - Body armor, vests, and chest equipment
- **Pantswear** - Leg armor and protective gear
- **Backpacks** - Backpacks and rear equipment
- **Decorations** - Additional aesthetic items

Each armor piece has configurable spawn chances (0-100%) for variety.

## Quick Start Guide

### 1. Installation
The sosig spawner is automatically integrated with the H3TVR plugin. On first run, it will create example INI configuration files in your BepInEx/config folder.

### 2. Configuration Files
The system automatically creates three INI files:
- **H3TVR_AllyConfig.ini** - Defines friendly sosig loadouts
- **H3TVR_EnemyConfig.ini** - Defines enemy sosig loadouts  
- **H3TVR_BossConfig.ini** - Defines boss-level enemies with special abilities

These files contain detailed examples and documentation for customizing sosig loadouts.

### 3. Control Methods

#### GUI Menu (Detailed Control)
- Press **F9** (default) to open the Advanced Sosig Spawner menu
- Select loadouts, configure armor, and spawn sosigs with full control

#### Quick Spawn Keybinds (Fast Action)
- **F10** - Quick spawn random ally
- **F11** - Quick spawn random enemy  
- **F12** - Quick spawn ally squad (3 units)
- **B** - Quick spawn random boss enemy
- **Delete** - Clear all spawned sosigs

### 4. Basic Spawning
1. **Via GUI**: Open menu → Select loadout → Configure settings → Spawn
2. **Via Keybinds**: Press F10/F11/F12 for instant spawning
3. All sosigs are automatically tracked and managed

### 5. PuttersPrettyVoice Integration
- Sosigs will play voice clips from the PuttersPrettyVoice folder
- Voice clips specified in INI files will be used when available
- Volume and enable/disable controls in GUI menu

## Boss Spawning System

The Advanced Sosig Spawner includes a comprehensive boss system for challenging encounters.

### Boss Features
- **Enhanced Stats** - Bosses have significantly higher health and speed
- **Special Abilities** - Regeneration, enrage mechanics, damage immunity
- **Minion Support** - Bosses can spawn supporting enemies
- **Visual Effects** - Special spawn/death effects and scaling
- **Audio Integration** - Boss-specific music and voice lines
- **Multiple Phases** - Enrage mechanics when health is low

### Boss Mechanics

#### Damage Immunity
- Bosses spawn with temporary invulnerability
- Duration configurable per boss (3-10 seconds typical)
- Prevents instant kills and gives dramatic entrance

#### Health Regeneration  
- Some bosses slowly heal over time
- Regeneration rate configurable (0.03-0.1 per second)
- Creates pressure to maintain offensive

#### Enrage System
- Bosses become more dangerous when injured
- Triggers at configurable health threshold (15-40%)
- Applies damage and speed multipliers (1.5x-2.5x)

#### Minion Spawning
- Bosses can spawn supporting enemies
- 0-6 minions depending on boss type
- Minions use existing enemy configurations

### Boss Controls
- **B Key** - Quick spawn random boss
- **GUI Menu** - Select specific boss loadouts
- **Boss Settings** - Adjust global boss multipliers in menu

## Advanced Configuration

### Custom IFF Codes
When using "Custom" faction mode:
- Enter any IFF code from 0-10
- Code 0 = Friendly to player
- Code 1 = Enemy to player  
- Codes 2-10 = Various neutral/custom factions

### Armor Configuration Detail
Each armor type can be individually controlled:

**Enable/Disable**: Toggle whether the armor type can spawn at all
**Spawn Chance**: Probability (0-100%) that the armor will spawn on each sosig

Example configurations:
- **Military Unit**: High torsowear (90%), headwear (80%), backpacks (60%)
- **Civilian**: High regular wear (90%), low military gear (10%)
- **Elite Soldier**: Force all armor pieces (100% chance)

### Behavioral Settings
- **Auto Follow Player**: Friendly sosigs will follow and assist the player
- **Enable Nameplates**: Show names above spawned sosigs
- **Custom Names**: Override default names with your own text

## Configuration Options

### BepInEx Config Settings
The following settings can be configured in the BepInEx config file:

```ini
[Sosig Spawner]
SpawnMenuKey = F9                           # Key to open spawner menu
SpawnAllyKey = F10                          # Quick spawn ally key
SpawnEnemyKey = F11                         # Quick spawn enemy key  
SpawnSquadKey = F12                         # Quick spawn squad key
SpawnBossKey = B                            # Quick spawn boss key
ClearAllKey = Delete                        # Clear all spawned sosigs key
SpawnDistance = 2.0                         # Distance from player to spawn sosigs
EnableCustomArmor = true                    # Enable armor customization
EnableFactionControl = true                 # Enable IFF/faction control
DefaultIFF = 0                              # Default faction code
AutoFollowPlayer = true                     # Make friendly sosigs follow player
EnableNameplates = true                     # Show nameplates on spawned sosigs
AllyConfigPath = BepInEx/config/H3TVR_AllyConfig.ini    # Path to ally INI file
EnemyConfigPath = BepInEx/config/H3TVR_EnemyConfig.ini  # Path to enemy INI file
BossConfigPath = BepInEx/config/H3TVR_BossConfig.ini    # Path to boss INI file
EnablePuttersPrettyVoice = true            # Enable voice integration
VoiceVolume = 0.7                          # Voice volume (0.0 - 1.0)

# Boss-specific settings
EnableBossSpawning = true                   # Enable boss spawning system
BossSpawnDistance = 5.0                     # Distance from player to spawn bosses
BossSpecialEffects = true                   # Enable boss visual/audio effects
BossHealthMultiplier = 3.0                  # Global boss health multiplier
BossSpeedMultiplier = 1.2                   # Global boss speed multiplier
BossImmuneToDamage = true                   # Give bosses spawn immunity
BossImmunityDuration = 3.0                  # Boss immunity duration (seconds)
```

### INI Configuration Files

#### Ally Configuration (H3TVR_AllyConfig.ini)
Define friendly sosig loadouts with custom equipment, behavior, and stats:

```ini
[Loadout Name]
description=Description of the loadout
iff=0                           # IFF code (0=friendly)
followplayer=true               # Should follow player
weaponprimary=AssaultRifle_M4   # Primary weapon ID
weaponsecondary=Pistol_M1911    # Secondary weapon ID
weapontertiary=                 # Tertiary equipment ID
healthmultiplier=1.0            # Health multiplier
speedmultiplier=1.0             # Speed multiplier
enablevoice=true                # Enable voice clips
voiceclips=clip1.wav,clip2.wav  # Comma-separated voice files
headwearchance=0.8              # Armor spawn chances (0.0-1.0)
facewearchance=0.3
eyewearchance=0.4
torsowearchance=0.9
pantswearchance=0.7
backpackchance=0.6
decorationchance=0.1
```

#### Enemy Configuration (H3TVR_EnemyConfig.ini)
Define hostile sosig loadouts with aggressive behavior and equipment:

```ini
[Enemy Type]
description=Description of the enemy type
iff=1                           # IFF code (1=enemy)
followplayer=false              # Enemies don't follow player
weaponprimary=AssaultRifle_AK74 # Primary weapon ID  
weaponsecondary=Pistol_Makarov  # Secondary weapon ID
healthmultiplier=1.5            # Increased health for difficulty
speedmultiplier=1.0             # Normal speed
enablevoice=true                # Enable combat voices
voiceclips=enemy_alert.wav,enemy_attack.wav
# Armor configuration...
```

#### Boss Configuration (H3TVR_BossConfig.ini)
Define powerful boss enemies with special abilities and enhanced stats:

```ini
[Boss Name]
description=Description of the boss
iff=1                           # IFF code (1=enemy for bosses)
followplayer=false              # Bosses don't follow player
weaponprimary=LMG_M249         # Heavy weapons for bosses
weaponsecondary=Pistol_Desert_Eagle
healthmultiplier=4.0            # Much higher health than normal
speedmultiplier=1.1             # Slightly faster
enablevoice=true                # Boss voice lines
voiceclips=boss_roar.wav,boss_die.wav

# Boss-specific properties
isboss=true                     # Must be true for boss loadouts
bossscale=1.2                   # Size multiplier (1.2 = 20% larger)
hasdamageimmunity=true          # Temporary invulnerability on spawn
immunityduration=5.0            # Immunity duration in seconds
hasspecialeffects=true          # Enable visual/audio effects
bossmusic=boss_theme.wav        # Background music during fight
spawneffect=explosion_spawn     # Visual effect on spawn
deatheffect=massive_explosion   # Visual effect on death
minionstospawn=2                # Number of minions to spawn
miniontypes=Standard Grunt,Heavy Assault  # Types of minions
regenerateshealth=false         # Boss slowly heals over time
regenerationrate=0.1            # Health regen per second
enragesatlowhealth=true         # Becomes more aggressive when injured
enragethreshold=0.25            # Health % that triggers enrage (25%)
enragemultiplier=2.0            # Damage/speed boost when enraged
# Armor configuration (usually maxed for bosses)
```

## Loadout System

### Default Loadouts
The system includes several pre-configured loadouts:

#### Ally Loadouts
1. **Standard Soldier** - Basic friendly military unit
2. **Elite Operative** - High-tier special forces
3. **Support Medic** - Medical support unit  
4. **Heavy Gunner** - Heavy weapons specialist
5. **Scout Sniper** - Long-range reconnaissance
6. **Engineer** - Technical specialist

#### Enemy Loadouts  
1. **Standard Grunt** - Basic hostile infantry
2. **Heavy Assault** - Heavily armored trooper
3. **Elite Sniper** - Long-range marksman
4. **Commando** - Special operations enemy
5. **Berserker** - Aggressive close-combat specialist
6. **Demolitions Expert** - Explosive specialist
7. **Boss Unit** - Elite commander (from enemy config)

#### Boss Loadouts
1. **Warlord Supreme** - Heavily armored commander with minions
   - 4x health, spawns 2 minions, enrages at 25% health
2. **Shadow Assassin** - Stealth specialist with regeneration
   - 2.5x health, 1.8x speed, regenerates health over time
3. **Demolition King** - Explosive specialist with area destruction
   - 3.5x health, spawns explosive minions
4. **Berserker Chieftain** - Savage melee specialist
   - 5x health, 1.5x speed, enrages at 40% health with 2.5x multiplier
5. **Cyber Overlord** - High-tech boss with energy weapons
   - 3x health, 1.3x speed, 8-second spawn immunity
6. **Undead General** - Necromantic commander
   - 2.8x health, spawns 5 minions, regenerates slowly
7. **Mech Titan** - Massive mechanical boss
   - 6x health, 1.5x scale, 10-second immunity, spawns 6 minions

### Creating Custom Loadouts
Create custom loadouts by editing the INI configuration files:

1. **Open the INI files** in any text editor
2. **Copy an existing section** like `[Standard Soldier]`  
3. **Rename the section** to your custom loadout name
4. **Modify the values** for weapons, armor, health, speed, etc.
5. **Add voice clips** by placing audio files in PuttersPrettyVoice folder
6. **Save and restart** H3VR to load the new configuration

#### Example Custom Loadout:
```ini
[My Custom Soldier]
description=My personalized soldier loadout
iff=0
followplayer=true
weaponprimary=AssaultRifle_SCAR
weaponsecondary=Pistol_Glock17
healthmultiplier=1.2
speedmultiplier=1.1
enablevoice=true
voiceclips=custom_ready.wav,custom_moving.wav
headwearchance=1.0
torsowearchance=1.0
# ... other settings
```

## API Integration

### For Mod Developers
The spawner system provides an API for integration with other mods:

```csharp
// Check if spawner is available
bool available = SosigSpawnerAPI.IsSpawnerAvailable();

// Get the spawner manager
SosigSpawnerManager spawner = SosigSpawnerAPI.GetSpawner();

// Get all spawned sosigs
List<Sosig> allSpawned = SosigSpawnerAPI.GetAllSpawnedSosigs();

// Get count of spawned sosigs
int count = SosigSpawnerAPI.GetSpawnedSosigCount();
```

### Event Integration
The spawner integrates with other H3TVR systems:
- **Slomo Integration**: Spawned sosigs can be configured to behave differently during slomo
- **Performance Management**: Automatic cleanup and optimization for large numbers of sosigs
- **Chat Integration**: Compatible with existing chat spawner systems

## Troubleshooting

### Common Issues

**Q: The spawner menu won't open**
A: Check that EnableSosigSpawner is set to true in your config file

**Q: Spawned sosigs have no armor**
A: Ensure EnableCustomArmor is enabled and armor chances are set above 0%

**Q: Sosigs spawn with wrong faction**
A: Verify your IFF selection in the spawner menu and faction settings

**Q: Performance issues with many sosigs**
A: Use "Clear All Spawned" regularly, or adjust SpawnerUpdateInterval in config

**Q: Spawned sosigs don't follow player**
A: Check AutoFollowPlayer setting and ensure you're spawning friendly (IFF 0) sosigs

### Debug Information
Enable debug logging by setting the BepInEx log level to "Debug" to see detailed spawner information.

## PuttersPrettyVoice Integration

The Advanced Sosig Spawner includes full integration with PuttersPrettyVoice for immersive audio experiences.

### Voice Features
- **Automatic voice playback** when sosigs spawn
- **Contextual voice clips** based on sosig actions and status
- **Configurable volume** through GUI and config files
- **Custom voice sets** per loadout via INI configuration

### Setting Up Voice Clips
1. **Place audio files** in `Assets/CompletedBounties/jediSpawner/PuttersPrettyVoice/`
2. **Use WAV format** for best compatibility
3. **Name files descriptively** (e.g., `ally_greeting.wav`, `enemy_alert.wav`)
4. **Reference in INI files** using the `voiceclips` parameter

### Voice Configuration
```ini
# In your loadout section:
enablevoice=true
voiceclips=soldier_ready.wav,soldier_roger.wav,soldier_covering.wav
```

### Voice Controls
- **Toggle voice system** in GUI menu
- **Adjust volume** with slider (0-100%)
- **Per-loadout control** via INI enable/disable
- **Automatic 3D positioning** for realistic audio

## Version History

### v2.0.0
- **INI-based configuration system** for loadouts
- **Configurable keybinds** for all spawn functions
- **PuttersPrettyVoice integration** with full audio support
- **Quick spawn system** with dedicated hotkeys
- **Enhanced armor configuration** via INI files
- **Health and speed multipliers** for advanced customization
- **Example configuration files** with detailed documentation

### v1.0.0
- Initial release of Advanced Sosig Spawner System
- GUI-based spawning interface
- Faction and armor customization
- Integration with H3TVR plugin
- Four default loadout configurations
- API for mod developers

## Credits

Created as part of the H3TVR plugin system for enhanced H3VR gameplay experiences.

## Support

For support, bug reports, or feature requests, please contact the H3TVR development team or submit issues through the appropriate channels.