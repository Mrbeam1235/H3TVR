# Enhanced Chat Spawner - Names & Armor Guide

## Overview
The Enhanced Chat Spawner now includes:
- **Name Loading from INI files** - Customizable names for allies and enemies
- **Armor GUI Integration** - Use the F6 armor menu to configure sosig armor
- **Dynamic Nameplates** - Names displayed above sosig heads

## Name Configuration

### Ally Names File: `config/H3TVR_AllyNames.ini`
```ini
# H3TVR Ally Names Configuration
[AllyNames]
Sergeant Johnson
Corporal Smith
Private Miller
# Add more names here...
```

### Enemy Names File: `config/H3TVR_EnemyNames.ini`
```ini
# H3TVR Enemy Names Configuration
[EnemyNames]
Hostile Alpha
Enemy Grunt
Rogue Agent
# Add more names here...
```

## Name System Features

### Automatic Name Loading
- Names are loaded from INI files automatically
- Files are monitored for changes every 30 seconds
- Support for both plain text names and INI key=value format
- Comments supported with # or ; prefixes

### Name Display
- Names appear as floating nameplates above sosig heads
- Green nameplates for allies, red for enemies
- Nameplates always face the camera
- Configurable height, scale, and colors

## Armor GUI Integration

### Using the Armor System
1. **Open Armor Menu**: Press `F6` to open the armor configuration GUI
2. **Select Preset**: Choose from predefined armor sets (Standard, Heavy Assault, Elite, etc.)
3. **Configure Settings**: Adjust armor chances for different pieces
4. **Apply to Sosigs**: Armor is automatically applied when sosigs spawn

### Armor Presets (from config/H3TVR_ChatSosigArmor.ini)
- **Standard**: Basic military gear
- **Heavy Assault**: Maximum protection armor
- **Stealth Ops**: Lightweight stealth gear
- **Riot Control**: Riot control equipment
- **Civilian**: Basic civilian clothing
- **Tactical Elite**: Elite tactical equipment
- **Berserker**: Minimal armor for mobility
- **Juggernaut**: Maximum protection heavy armor

### Custom Armor Configuration
You can modify armor chances for each faction:
- **Headwear**: Helmets, hats, caps
- **Facewear**: Masks, face protection
- **Eyewear**: Glasses, goggles
- **Torsowear**: Body armor, vests
- **Pantswear**: Leg armor, tactical pants
- **Backpacks**: Equipment bags
- **Decorations**: Additional gear

## Configuration Options

### Enhanced Chat Spawner Settings
```ini
[Enhanced Chat Spawner]
MaxAllySosigs = 8                    # Maximum ally sosigs
MaxEnemySosigs = 8                   # Maximum enemy sosigs
EnableNameplates = true              # Show nameplates
DefaultAllyArmor = Standard          # Default ally armor preset
DefaultEnemyArmor = Heavy Assault    # Default enemy armor preset
AllyNamesFile = H3TVR_AllyNames.ini  # Ally names file
EnemyNamesFile = H3TVR_EnemyNames.ini # Enemy names file

[Enhanced Chat Spawner Nameplates]
NameplateHeight = 2.5               # Height above head
NameplateScale = 0.02               # Size scale
AllyNameplateColor = 0,1,0,1        # Green (R,G,B,A)
EnemyNameplateColor = 1,0,0,1       # Red (R,G,B,A)
```

## Keyboard Controls
- **P**: Spawn manual ally sosig
- **O**: Spawn manual enemy sosig
- **Delete**: Clear all sosigs
- **F6**: Toggle armor configuration menu

## How It Works

### Name Selection Process
1. System loads names from INI files on startup
2. When spawning a sosig, randomly selects a name from appropriate list
3. Creates nameplate with selected name
4. Name is displayed above sosig head throughout its lifetime

### Armor Application Process
1. When a sosig spawns, system checks for armor GUI integration
2. If armor GUI is available, applies faction-specific armor based on current settings
3. If GUI not available, falls back to basic template armor
4. Armor is applied based on configured chances for each piece type

### Nameplate System
1. Creates a world-space UI canvas attached to sosig head
2. Positions nameplate above head at configurable height
3. Text always faces camera for optimal visibility
4. Color-coded by faction (green=ally, red=enemy)
5. Automatically destroyed when sosig dies

## Troubleshooting

### Names Not Loading
- Check that INI files exist in config/ directory
- Verify file format (see examples above)
- Check console log for loading errors
- Files are auto-created with defaults if missing

### Armor Not Applied
- Press F6 to ensure armor system is initialized
- Check that armor presets exist in config/H3TVR_ChatSosigArmor.ini
- Verify armor system is enabled in GUI
- Check console log for armor application errors

### Nameplates Not Visible
- Ensure EnableNameplates = true in config
- Check nameplate height and scale settings
- Verify camera is available in scene
- Look for nameplate creation errors in console

## Advanced Usage

### Custom Name Formats
```ini
# Plain text names
Soldier Alpha
Soldier Beta

# Key=Value format  
name1=Sergeant Johnson
name2=Corporal Smith

# Mixed format supported
Soldier Charlie
name3=Lieutenant Davis
```

### Armor Integration in Code
```csharp
// Apply armor via GUI system
var armorIntegration = plugin.GetSosigArmorWristMenu();
if (armorIntegration != null)
{
    armorIntegration.ApplyArmorToSosig(sosig, isFriendly);
}
```

## File Locations
- Name files: `BepInEx/config/H3TVR_AllyNames.ini` and `H3TVR_EnemyNames.ini`
- Armor config: `BepInEx/config/H3TVR_ChatSosigArmor.ini`
- Main config: `BepInEx/config/H3TVR.cfg`

The system provides a seamless integration between name management, armor configuration, and visual display, making sosig spawning more immersive and customizable.