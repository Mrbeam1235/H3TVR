# H3TVR Sosig Armor Configuration GUI

## Overview
The H3TVR Sosig Armor Configuration GUI allows you to customize the armor and equipment worn by spawned sosigs in real-time. This feature integrates with H3VR's armor system and provides an intuitive interface for armor management.

## Features

### ? Key Features
- **Real-time Armor Modification**: Change armor on existing sosigs instantly
- **Individual Sosig Selection**: Target specific sosigs or apply changes to all
- **Armor Slot Configuration**: Enable/disable and adjust spawn chances for different armor types
- **Visual Armor Browser**: Browse and select specific armor pieces from H3VR's catalog
- **Preset System**: Save and load armor configurations for quick setup
- **Integration with H3VR Asset System**: Automatically loads all available armor from the game

### ?? Controls
- **F6**: Open/Close Armor Configuration GUI
- **P**: Spawn Friendly Sosig (with current armor config)
- **O**: Spawn Enemy Sosig (with current armor config)
- **Delete**: Clear all spawned sosigs

## GUI Interface

### Sosig Selection Panel
- **Select All**: Apply armor changes to all active sosigs
- **Individual Selection**: Click checkboxes to select specific sosigs
- **Quick Actions**: Strip armor or apply random armor to individual sosigs

### Armor Slot Configuration
Configure the following armor slots:
- **Headwear**: Helmets, hats, caps
- **Facewear**: Masks, face protection
- **Eyewear**: Goggles, glasses, visors
- **Torsowear**: Body armor, vests, jackets
- **Pantswear**: Leg armor, pants
- **PantswearLower**: Shin guards, lower leg protection
- **Backpacks**: Backpacks, equipment packs
- **Decorations**: Patches, badges, accessories

Each slot can be:
- **Enabled/Disabled**: Toggle whether the slot spawns armor
- **Chance Adjusted**: Set the probability (0-100%) of armor spawning in this slot

### Individual Armor Selection
- Browse through available armor pieces for each slot
- Use ? and ? buttons to cycle through options
- **Apply** button to immediately equip selected armor on chosen sosigs

### Armor Preferences
- **Prefer Military Armor**: Prioritize military-style equipment
- **Allow Civilian Armor**: Include civilian clothing options
- **Allow Futuristic Armor**: Include sci-fi and advanced armor
- **Randomize Colors**: Vary armor colors when possible

### Action Buttons
- **Apply Current Config**: Apply all current settings to selected sosigs
- **Strip All Armor**: Remove all armor from selected sosigs
- **Random Armor**: Apply completely random armor configuration
- **Save Config**: Save current settings to JSON file
- **Load Config**: Load previously saved configuration
- **Close**: Close the GUI window

## Configuration Files

### H3TVR_ArmorConfig.json
Your personal armor configuration is automatically saved to:
```
BepInEx/config/H3TVR_ArmorConfig.json
```

### H3TVR_ArmorPresets.ini
Predefined armor presets are available in:
```
BepInEx/config/H3TVR_ArmorPresets.ini
```

Available presets:
- **LightInfantry**: Minimal protection for mobility
- **HeavyAssault**: Maximum armor for frontline combat
- **SpecialForces**: Tactical gear for special operations
- **Civilian**: Everyday clothing and light protection
- **SciFi**: Futuristic armor and equipment
- **Minimal**: Basic clothing only
- **Random**: Completely randomized setup

## Technical Details

### Armor System Integration
- Integrates with **H3VRAssetLoader** for comprehensive armor catalog
- Uses H3VR's **SosigWearable** system for proper armor attachment
- Supports armor from base game and mods
- Automatic categorization of armor pieces by type

### Performance Optimization
- **Armor Caching**: Armor lists are cached for fast access
- **Efficient Asset Loading**: Only loads armor data when needed
- **Smart GUI Updates**: GUI only refreshes when necessary

### Compatibility
- **H3VR Base Game**: Full compatibility with all base game armor
- **Modded Armor**: Automatically detects and includes modded armor pieces
- **VR Interface**: Designed for VR interaction with large, clear controls

## Usage Tips

### Getting Started
1. Spawn some sosigs using **P** (ally) or **O** (enemy)
2. Press **F6** to open the armor GUI
3. Select sosigs using the checkboxes
4. Adjust armor settings or browse individual pieces
5. Click **Apply Current Config** to see changes

### Best Practices
- **Save Configurations**: Save armor setups you like for later use
- **Use Presets**: Start with a preset and customize from there
- **Test Settings**: Use "Random Armor" to see what's possible
- **Performance**: Limit armor on large numbers of sosigs for better performance

### Troubleshooting
- **No Armor Showing**: Wait for H3VR assets to load (2-3 seconds after startup)
- **GUI Not Opening**: Check that F6 key binding isn't conflicting
- **Armor Not Applying**: Ensure sosigs are properly selected
- **Performance Issues**: Reduce number of active sosigs

## Integration with Other Systems

### Twitch Chat Integration
- Newly spawned sosigs automatically use current armor configuration
- Chat-spawned sosigs inherit GUI armor settings
- Compatible with existing chat commands

### Loadout System
- Integrates with **SosigLoadoutConfiguration** system
- Can be used alongside **SosigLoadoutUtility** for advanced setups
- Compatible with scenario-based sosig spawning

## Future Enhancements
- Preset loading from GUI
- Armor colorization controls  
- Advanced filtering options
- Bulk armor operations
- Integration with sosig templates

---

## Support
For issues, suggestions, or contributions, please refer to the main H3TVR documentation or community forums.

**Have fun customizing your sosigs!** ??