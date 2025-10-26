# H3TVR TNH Encryption Node Disable

## Overview

H3TVR Enhanced Edition now includes a **Disable Encryption Nodes** toggle for Take and Hold mode. When enabled, encryption challenges are automatically bypassed, allowing you to focus on combat without interruption.

## Features

? **Skip Encryption Challenges** - No more hacking mini-games  
? **Configurable Toggle** - Enable/disable via BepInEx configuration  
? **Automatic Detection** - Works automatically when in TNH mode  
? **Safe Implementation** - Only affects encryption, doesn't break TNH mechanics  
? **Works with Infinite Tokens** - Can be used together or separately  

## Configuration

### Enable Encryption Disable

Open your configuration file:
```
BepInEx/config/com.h3tvr.improved.cfg
```

Find the `[TakeAndHold]` section:
```ini
[TakeAndHold]
DisableEncryptionNodes = false
```

Set to `true` to disable encryption:
```ini
[TakeAndHold]
DisableEncryptionNodes = true
```

### Default State

By default, encryption node disable is **off** to preserve the standard TNH experience.

## How It Works

When enabled, the system:
1. Monitors if you're currently in a Take and Hold game
2. Checks every frame if you're at a hold point
3. Automatically deactivates encryption nodes when they appear

This means:
- ? Encryption challenges are skipped automatically
- ? You can proceed directly to combat
- ? No need to complete hacking mini-games
- ? The rest of TNH mechanics work normally

## Usage

### In-Game

Once enabled in config:
1. Start any Take and Hold game
2. When you reach a hold point with encryption
3. The encryption node will automatically be disabled
4. You can focus on the combat waves

### Toggling During Gameplay

You can also enable/disable via code (for modders/advanced users):

```csharp
// Get the plugin instance
var h3tvr = FindObjectOfType<H3TVRImproved>();

// Disable encryption nodes
h3tvr.SetEncryptionNodes(true);

// Enable encryption nodes (normal TNH)
h3tvr.SetEncryptionNodes(false);

// Check if disabled
bool isDisabled = h3tvr.IsEncryptionDisabled();
```

## Use Cases

### Training/Practice
- **Combat Focus**: Practice combat without encryption interruptions
- **Speed Training**: Improve your combat speed without hacking delays
- **Difficulty Exploration**: Try higher difficulties with one less challenge

### Fun/Casual Play
- **Action-Focused**: Pure combat gameplay
- **Relaxed Runs**: Enjoy TNH without puzzle interruptions
- **Time Savers**: Faster runs without hacking delays

### Streaming/Content Creation
- **Viewer Engagement**: More action, less waiting
- **Challenge Runs**: Create custom challenges with modified rules
- **Fast-Paced Content**: Keep the action flowing for viewers

## Combining with Infinite Tokens

You can use both features together for maximum freedom:

```ini
[TakeAndHold]
InfiniteTokens = true
DisableEncryptionNodes = true
```

This gives you:
- Unlimited supply points spending
- No encryption challenges
- Pure combat-focused gameplay

## Limitations

### What It Affects
- ? Encryption node activation (disabled)
- ? Hacking mini-games (skipped)

### What It Does NOT Affect
- ? Hold point waves
- ? Enemy difficulty
- ? Weapon spawns
- ? Score calculation
- ? Health/armor mechanics
- ? Hold point progression
- ? Token economy (unless infinite tokens enabled)

## Technical Details

### Implementation
```csharp
private void DisableEncryptionNodes()
{
    try
    {
        if (GM.TNH_Manager == null || GM.TNH_Manager.m_curHoldPoint == null) return;
        
        // Get current hold point
        var holdPoint = GM.TNH_Manager.m_curHoldPoint;
        
        // Check if there are encryption systems
        if (holdPoint.m_systemNode != null)
        {
            // Mark encryption as complete/disabled
            if (holdPoint.m_systemNode.m_hasActivated == false)
            {
                // Automatically complete encryption
                holdPoint.m_systemNode.m_numHitsLeft = 0;
                
                // Deactivate the node
                holdPoint.m_systemNode.gameObject.SetActive(false);
            }
        }
    }
    catch (Exception ex)
    {
        Logger.LogDebug($"Error disabling encryption nodes: {ex.Message}");
    }
}
```

### Update Frequency
- Checked every frame in the `Update()` loop
- Only applies when `disableEncryptionNodes` is `true`
- Only active when `GM.TNH_Manager` exists (in TNH mode)

### Safety
- Wrapped in try-catch for error handling
- Only modifies encryption node state
- Doesn't affect other TNH systems
- Can be toggled on/off at any time

## Compatibility

### H3VR Versions
- ? All TNH-supported versions
- ? Works with all TNH modes
- ? Compatible with other mods

### Other H3TVR Features
- ? Works alongside all H3TVR features
- ? Compatible with infinite tokens
- ? Compatible with sosig spawning
- ? Compatible with weapon modifications
- ? Compatible with audio/visual effects

## FAQ

**Q: Will this affect my leaderboard scores?**  
A: This is a mod, so you're not competing on official leaderboards anyway. Use it for fun and practice!

**Q: Can I enable/disable it mid-run?**  
A: Yes! Change the config value and it will take effect immediately on the next encryption node.

**Q: Does it work with custom TNH characters?**  
A: Yes! It works with all TNH characters and modes.

**Q: What if I want encryption challenges sometimes?**  
A: Simply set `DisableEncryptionNodes = false` in the config, and TNH will work normally.

**Q: Does it skip all encryption or just make it easier?**  
A: It completely bypasses encryption nodes - they won't activate at all.

**Q: Can I use this with Infinite Tokens?**  
A: Yes! Both features work independently and can be combined.

## Configuration Examples

### Combat Focus Mode
```ini
[TakeAndHold]
DisableEncryptionNodes = true
InfiniteTokens = false
```

### Easy Mode (Everything Disabled)
```ini
[TakeAndHold]
DisableEncryptionNodes = true
InfiniteTokens = true
```

### Standard Challenge Mode
```ini
[TakeAndHold]
DisableEncryptionNodes = false
InfiniteTokens = false
```

### Mixed Configuration (for modders)
```csharp
// Disable encryption for specific scenarios
if (isSpeedRun)
{
    h3tvr.SetEncryptionNodes(true);
}
else
{
    h3tvr.SetEncryptionNodes(false);
}
```

## Tips

### Best Practices
1. **Start with default** - Try TNH normally first to learn the mechanics
2. **Use for training** - Enable when practicing combat specifically
3. **Speed runs** - Disable encryption for faster completion times
4. **Custom challenges** - Create your own rules with encryption on/off

### Common Scenarios
- **New to TNH?** Try with encryption disabled to focus on combat
- **Practicing aim?** Disable encryption to maximize combat time
- **Speedrunning?** Use it to eliminate non-combat delays
- **Just for fun?** Enable it and focus on pure action!

## Troubleshooting

### Encryption Still Appears
1. **Check config** - Verify `DisableEncryptionNodes = true`
2. **Verify TNH mode** - Feature only works in TNH
3. **Check logs** - Look for errors in BepInEx console
4. **Restart game** - Sometimes config changes need a restart

### Encryption Disabled But Node Visible
1. **Node remains visible** - Visual model may stay but functionality is disabled
2. **Try interacting** - Node should not require completion
3. **Wave should start** - Combat waves should begin immediately

### Errors in Console
```
Error disabling encryption nodes: ...
```
- Usually safe to ignore (feature will retry next frame)
- If persistent, disable and re-enable the feature
- Report to H3TVR developers if it causes issues

## Performance Impact

- **Minimal** - Simple node deactivation each frame
- **No overhead** - Only runs when enabled
- **No memory impact** - Doesn't allocate memory
- **Safe for low-end systems** - Negligible performance cost

## Future Enhancements

Potential future features:
- [ ] Encryption difficulty slider (partial disabling)
- [ ] Encryption time limit reduction
- [ ] Auto-complete after X seconds
- [ ] Encryption skip animation
- [ ] Per-hold point encryption control

## Credits

- **Anton Hand** - H3VR developer, TNH game mode creator
- **RUST LTD** - H3VR game development
- **H3TVR Team** - Encryption disable implementation

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
