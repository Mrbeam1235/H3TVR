# Anton Update 120 TNH System Migration Guide

## Overview

H3TVR's sosig spawning system has been updated to use the modern TNH (Take and Hold) sosig spawning system introduced in Anton Hand's Update 120 Explanation 1. This provides better reliability, performance, and compatibility with H3VR's latest features.

## What Changed in Update 120?

### Key Changes:
1. **SosigAPI**: New centralized API for sosig spawning
2. **ManagerSingleton<IM>**: Replaces direct `IM.OD` access
3. **SosigEnemyID Enum**: Direct sosig type selection instead of template searching
4. **Improved Configuration**: Better sosig config templates and initialization
5. **Modern Outfit System**: Updated outfit application methods

## New Features in H3TVR

### 1. Modern Spawn System (Default)
The plugin now uses `SosigAPI.Spawn()` for all sosig creation:

```csharp
// Old method (pre-U120):
GameObject sosigGO = Instantiate(prefab.GetGameObject(), pos, rot);
Sosig sosig = sosigGO.GetComponentInChildren<Sosig>();
sosig.Configure(config);

// New method (U120+):
Sosig sosig = SosigAPI.Spawn(template, pos, rot, IFF, true);
```

### 2. Sosig Pool System
Configure which sosig types spawn for allies and enemies:

```ini
[Chat Spawner]
UseModernSpawnSystem = true
AllySosigPool = M_Swat_Scout,M_Swat_Sniper,M_Swat_Breacher
EnemySosigPool = M_Swat_Heavy,M_Swat_Breacher,M_Swat_Sniper
```

### 3. Automatic Fallback
If the modern system isn't available or fails, the plugin automatically falls back to the legacy spawning system for compatibility.

## Configuration Options

### New Config Entries

| Setting | Default | Description |
|---------|---------|-------------|
| `UseModernSpawnSystem` | `true` | Use Update 120's TNH spawn system |
| `AllySosigPool` | `M_Swat_Scout,M_Swat_Sniper,M_Swat_Breacher` | Comma-separated sosig types for allies |
| `EnemySosigPool` | `M_Swat_Heavy,M_Swat_Breacher,M_Swat_Sniper` | Comma-separated sosig types for enemies |

### Available SosigEnemyID Types

Common sosig types you can use in pools:

**SWAT Series:**
- `M_Swat_Scout` - Light, fast scout
- `M_Swat_Sniper` - Long-range marksman
- `M_Swat_Breacher` - Close-quarters specialist
- `M_Swat_Heavy` - Heavy armor tank
- `M_Swat_Pointman` - Assault leader

**PMC Series:**
- `PMC_Scout` - Tactical scout
- `PMC_Rifle` - Standard rifleman
- `PMC_Heavy` - Armored heavy

**WW2 Series:**
- `WW2_Ally_Rifleman`
- `WW2_Ally_SMG`
- `WW2_Axis_Rifleman`
- `WW2_Axis_MG`

**And many more!** Check the H3VR SosigEnemyID enum for the full list.

## Migration Guide

### For Existing Installations

1. **Automatic Migration**: The system automatically migrates on first run
2. **Config Update**: New settings are added to your config file
3. **Backwards Compatible**: Legacy spawning still works as fallback

### Customizing Sosig Pools

Edit `BepInEx/config/H3TVR.cfg`:

```ini
[Chat Spawner]
# Use modern spawn system
UseModernSpawnSystem = true

# Ally sosigs - friendly helpers
AllySosigPool = M_Swat_Scout,PMC_Scout,WW2_Ally_Rifleman

# Enemy sosigs - aggressive attackers
EnemySosigPool = M_Swat_Heavy,PMC_Heavy,WW2_Axis_MG
```

### Advanced Customization

You can mix and match sosig types from different eras:

```ini
# Mixed modern/historical allies
AllySosigPool = M_Swat_Scout,WW2_Ally_SMG,PMC_Rifle

# All heavy enemies
EnemySosigPool = M_Swat_Heavy,PMC_Heavy,WW2_Axis_Heavy

# Sniper squad
AllySosigPool = M_Swat_Sniper,PMC_Sniper,WW2_Ally_Sniper
```

## Technical Implementation

### Modern Spawning Flow

```
1. User/Twitch triggers spawn
   ?
2. Get random SosigEnemyID from pool
   ?
3. Retrieve template from ManagerSingleton<IM>
   ?
4. Spawn using SosigAPI.Spawn()
   ?
5. Configure with template's ConfigTemplates
   ?
6. Set IFF and outfit
   ?
7. Initialize behavior (ally/enemy)
```

### Fallback System

If modern spawn fails:
```
Modern Spawn Failed
   ?
Try Legacy Template System
   ?
If that fails: Log error
```

## Benefits of Update 120 System

### Performance
- ? **Faster spawning** - Direct API calls instead of manual instantiation
- ? **Better memory management** - Proper pooling and cleanup
- ? **Reduced overhead** - Optimized initialization

### Reliability
- ? **Fewer null references** - Proper validation in SosigAPI
- ? **Better error handling** - Graceful fallbacks
- ? **More stable IFF** - Improved faction system

### Features
- ? **Modern outfit system** - Better accessory attachment
- ? **Improved AI** - Uses latest sosig behaviors
- ? **Better weapons** - Proper inventory management

## Troubleshooting

### Sosigs Not Spawning

**Issue**: Sosigs fail to spawn with modern system

**Solutions**:
1. Check BepInEx console for errors
2. Verify sosig pool IDs are correct
3. Try disabling modern system temporarily:
   ```ini
   UseModernSpawnSystem = false
   ```
4. Check if ManagerSingleton is available

### Invalid Sosig IDs

**Issue**: "Invalid ally/enemy sosig ID" warnings

**Solution**: Check your sosig pool configuration. Each ID must match exactly:
```ini
# Wrong:
AllySosigPool = SWAT_Scout,Sniper

# Correct:
AllySosigPool = M_Swat_Scout,M_Swat_Sniper
```

### Legacy Mode Active

**Issue**: System falls back to legacy mode automatically

**Reasons**:
- H3VR version too old (pre-U120)
- ManagerSingleton not initialized
- SosigAPI not available

**Check**: Look for "Using modern spawn system" vs "Fallback to legacy" in logs

## Performance Recommendations

### Optimal Settings for Performance

```ini
[Chat Spawner]
MaxAllySosigs = 6
MaxEnemySosigs = 6
SpawnCooldown = 2.0
EnableAutoCleanup = true
```

### For Better Performance

1. **Limit sosig count** - Lower max sosigs reduces overhead
2. **Enable auto-cleanup** - Removes dead sosigs automatically
3. **Use mixed pools** - Variety prevents repetitive spawning
4. **Increase spawn cooldown** - Prevents spawn spam

### For Better Experience

1. **Diverse pools** - Mix sosig types for variety
2. **Theme matching** - Match sosigs to your scene/game
3. **Balanced IFF** - Set appropriate faction codes
4. **Custom distances** - Adjust spawn distances for your play style

## Developer Notes

### Adding New Sosig Types

To add custom sosig types to pools:

1. Find the SosigEnemyID in H3VR's enum
2. Add to config pool:
   ```ini
   AllySosigPool = M_Swat_Scout,YOUR_CUSTOM_ID
   ```
3. Test spawning
4. Adjust IFF if needed

### Creating Custom Pools

Example themed pools:

**Zombie Apocalypse:**
```ini
AllySosigPool = M_Swat_Heavy,PMC_Heavy
EnemySosigPool = Zombie_Standard,Zombie_Runner,Zombie_Tank
```

**Historical WW2:**
```ini
AllySosigPool = WW2_Ally_Rifleman,WW2_Ally_SMG
EnemySosigPool = WW2_Axis_Rifleman,WW2_Axis_MG
```

**PMC Operations:**
```ini
AllySosigPool = PMC_Scout,PMC_Rifle,PMC_Sniper
EnemySosigPool = M_Swat_Heavy,M_Swat_Pointman
```

## Version Compatibility

| H3VR Version | Support Status | Recommended Setting |
|--------------|---------------|---------------------|
| Update 120+ | ? Full Support | `UseModernSpawnSystem = true` |
| Update 115-119 | ?? Legacy Only | `UseModernSpawnSystem = false` |
| Pre-Update 115 | ? Not Tested | Use at own risk |

## Future Enhancements

Planned improvements:
- [ ] Custom sosig templates
- [ ] Advanced AI behavior options
- [ ] Dynamic difficulty scaling
- [ ] Sosig squad formations
- [ ] Voice line integration
- [ ] Custom outfit presets

## Credits

- **Anton Hand** - H3VR developer, Update 120 TNH system
- **RUST LTD** - H3VR game development
- **Arpytrooper** - Original H3TwitchTools design inspiration
- **H3TVR Team** - Integration and enhancement

## Support

For issues:
1. Check BepInEx console logs
2. Verify configuration syntax
3. Test with default settings
4. Report bugs with full error logs

---

**Last Updated**: December 2024  
**System Version**: H3TVR 1.3.0+  
**H3VR Compatibility**: Update 120+
