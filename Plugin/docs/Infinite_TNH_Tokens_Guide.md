# H3TVR Infinite Tokens for Take and Hold

## Overview

H3TVR Enhanced Edition now includes an **Infinite Tokens** toggle for Take and Hold mode. When enabled, you'll have essentially unlimited tokens (999) to spend at supply points throughout your TNH run.

## Features

? **Unlimited Supply Spending** - Never run out of tokens during a run  
? **Configurable Toggle** - Enable/disable via BepInEx configuration  
? **Automatic Detection** - Works automatically when in TNH mode  
? **Safe Implementation** - Only affects tokens, doesn't break TNH mechanics  

## Configuration

### Enable Infinite Tokens

Open your configuration file:
```
BepInEx/config/com.h3tvr.improved.cfg
```

Find the `[TakeAndHold]` section:
```ini
[TakeAndHold]
InfiniteTokens = false
```

Set to `true` to enable:
```ini
[TakeAndHold]
InfiniteTokens = true
```

### Default State

By default, infinite tokens is **disabled** to preserve the standard TNH experience.

## How It Works

When enabled, the system:
1. Monitors if you're currently in a Take and Hold game
2. Checks every frame if you're at a hold point
3. Automatically sets your token count to 999

This means:
- ? You can buy anything at supply points
- ? You can upgrade weapons freely
- ? You can purchase health/armor without worry
- ? The rest of TNH mechanics work normally

## Usage

### In-Game

Once enabled in config:
1. Start any Take and Hold game
2. Your tokens will automatically be set to 999
3. Spend as much as you want at supply points
4. Tokens will remain at 999 throughout the run

### Toggling During Gameplay

You can also enable/disable via code (for modders/advanced users):

```csharp
// Get the plugin instance
var h3tvr = FindObjectOfType<H3TVRImproved>();

// Enable infinite tokens
h3tvr.SetInfiniteTokens(true);

// Disable infinite tokens
h3tvr.SetInfiniteTokens(false);

// Check if enabled
bool isEnabled = h3tvr.IsInfiniteTokensEnabled();
```

## Use Cases

### Training/Practice
- **Testing Builds**: Try different weapon loadouts without token restrictions
- **Learning Mechanics**: Focus on combat without resource management
- **Difficulty Exploration**: Try higher difficulties with more freedom

### Fun/Casual Play
- **Relaxed Runs**: Enjoy TNH without token stress
- **Experimental Loadouts**: Try unconventional weapon combinations
- **Equipment Testing**: Test all available items freely

### Streaming/Content Creation
- **Showcase Runs**: Demonstrate weapons and equipment
- **Challenge Runs**: Create custom challenges with modified rules
- **Viewer Requests**: Fulfill viewer equipment requests instantly

## Limitations

### What It Affects
- ? Token count (set to 999)
- ? Purchasing power at supply points

### What It Does NOT Affect
- ? Hold point waves
- ? Enemy difficulty
- ? Weapon spawns
- ? Score calculation
- ? Health/armor mechanics
- ? Encryption level progression

## Technical Details

### Implementation
```csharp
private void HandleInfiniteTokens()
{
    if (!enableInfiniteTokens.Value) return;
    
    try
    {
        // Check if in TNH mode
        if (GM.TNH_Manager != null && GM.TNH_Manager.m_curHoldPoint != null)
        {
            // Set tokens to a high number (999)
            GM.TNH_Manager.m_numTokens = 999;
        }
    }
    catch (Exception ex)
    {
        Logger.LogError($"Error in HandleInfiniteTokens: {ex.Message}");
    }
}
```

### Update Frequency
- Checked every frame in the `Update()` loop
- Only applies when `enableInfiniteTokens` is `true`
- Only active when `GM.TNH_Manager` exists (in TNH mode)

### Safety
- Wrapped in try-catch for error handling
- Only modifies `m_numTokens` field
- Doesn't affect other TNH systems
- Can be toggled on/off at any time

## Compatibility

### H3VR Versions
- ? All TNH-supported versions
- ? Works with all TNH modes
- ? Compatible with other mods

### Other H3TVR Features
- ? Works alongside all H3TVR features
- ? Compatible with sosig spawning
- ? Compatible with weapon modifications
- ? Compatible with audio/visual effects

## FAQ

**Q: Will this affect my leaderboard scores?**  
A: This is a mod, so you're not competing on official leaderboards anyway. Use it for fun and practice!

**Q: Can I enable/disable it mid-run?**  
A: Yes! Change the config value and it will take effect immediately. Tokens will update on the next frame.

**Q: Does it work with custom TNH characters?**  
A: Yes! It works with all TNH characters and modes.

**Q: What if I want to use normal tokens sometimes?**  
A: Simply set `InfiniteTokens = false` in the config, and TNH will work normally.

**Q: Why 999 tokens instead of infinite?**  
A: The game uses an integer for token count. 999 is high enough to be effectively infinite while avoiding potential overflow issues.

**Q: Can I change the token amount?**  
A: Currently it's hardcoded to 999. You can modify the source code if you want a different amount.

## Configuration Examples

### Practice Mode
```ini
[TakeAndHold]
InfiniteTokens = true
```

### Standard Challenge Mode
```ini
[TakeAndHold]
InfiniteTokens = false
```

### Mixed Configuration (for modders)
```csharp
// Enable for specific scenarios
if (isTrainingMode)
{
    h3tvr.SetInfiniteTokens(true);
}
else
{
    h3tvr.SetInfiniteTokens(false);
}
```

## Tips

### Best Practices
1. **Start with default** - Try TNH normally first to learn the mechanics
2. **Use for learning** - Enable when practicing new strategies
3. **Experiment freely** - Try unusual weapon combinations
4. **Custom challenges** - Create your own rules with infinite resources

### Common Scenarios
- **New to TNH?** Try infinite tokens to learn without pressure
- **Testing weapons?** Enable it to try all equipment
- **Speedrunning practice?** Use it to optimize your loadout choices
- **Just for fun?** Enable it and go wild!

## Troubleshooting

### Tokens Not Updating
1. **Check config** - Verify `InfiniteTokens = true`
2. **Verify TNH mode** - Feature only works in TNH
3. **Check logs** - Look for errors in BepInEx console
4. **Restart game** - Sometimes config changes need a restart

### Tokens Reset to Normal
1. **Config value** - Ensure `InfiniteTokens = true` in config
2. **Mod active** - Verify H3TVR is loaded (check BepInEx console)
3. **TNH state** - Feature only works at hold points

### Errors in Console
```
Error in HandleInfiniteTokens: ...
```
- Usually safe to ignore (feature will retry next frame)
- If persistent, disable and re-enable the feature
- Report to H3TVR developers if it causes issues

## Performance Impact

- **Minimal** - Simple integer assignment each frame
- **No overhead** - Only runs when enabled
- **No memory impact** - Doesn't allocate memory
- **Safe for low-end systems** - Negligible performance cost

## Future Enhancements

Potential future features:
- [ ] Configurable token amount
- [ ] Token multiplier instead of fixed amount
- [ ] Per-hold point token adjustment
- [ ] Token regeneration rate
- [ ] Custom token rules

## Credits

- **Anton Hand** - H3VR developer, TNH game mode creator
- **RUST LTD** - H3VR game development
- **H3TVR Team** - Infinite tokens implementation

## Support

For issues or questions:
1. Check BepInEx console for errors
2. Verify configuration file syntax
3. Test with default settings
4. Report bugs with full logs

---

**Status**: ? Complete - Fully functional  
**Version**: H3TVR 1.3.0+  
**Compatibility**: All TNH modes  
**Last Updated**: January 2025
