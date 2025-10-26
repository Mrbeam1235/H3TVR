# TNH Specific Encryption Disable - Feature Guide

## Overview

The TNH Specific Encryption Disable feature gives you **granular control** over which encryption challenges to skip in Take and Hold mode. You can now disable specific encryption types or all encryptions at once, with optional auto-completion delay.

## Configuration Options

### BepInEx Config Location
```
BepInEx/config/com.h3tvr.improved.cfg
```

### Available Settings

#### Master Controls
```ini
[TakeAndHold]
InfiniteTokens = false           # Infinite tokens (999)
DisableEncryptionNodes = false   # Disable ALL encryptions (legacy setting)

[TakeAndHold.Encryption]
DisableAllEncryptions = false    # Master switch for all encryptions (NEW!)
```

#### Specific Encryption Types
```ini
[TakeAndHold.Encryption]
DisableType1 = false    # Disable Type 1 (Pattern matching)
DisableType2 = false    # Disable Type 2 (Sequence)
DisableType3 = false    # Disable Type 3 (Timed)
```

#### Auto-Completion Options
```ini
[TakeAndHold.Encryption]
AutoComplete = false         # Auto-complete instead of instant disable
CompletionDelay = 2.0        # Delay in seconds before auto-completing
```

## Usage Examples

### Example 1: Disable All Encryptions Instantly
```ini
[TakeAndHold.Encryption]
DisableAllEncryptions = true
AutoComplete = false
```

**Result:** All encryption nodes are instantly completed when you enter a hold point.

### Example 2: Disable Only Pattern Encryptions
```ini
[TakeAndHold.Encryption]
DisableAllEncryptions = false
DisableType1 = true      # Pattern matching only
DisableType2 = false
DisableType3 = false
```

**Result:** Pattern matching encryptions are skipped, but sequence and timed encryptions remain active.

### Example 3: Auto-Complete All Encryptions with Delay
```ini
[TakeAndHold.Encryption]
DisableAllEncryptions = true
AutoComplete = true
CompletionDelay = 3.0
```

**Result:** All encryptions auto-complete 3 seconds after activation (gives you time to see them but not complete them manually).

### Example 4: Disable Only Timed Encryptions
```ini
[TakeAndHold.Encryption]
DisableAllEncryptions = false
DisableType1 = false
DisableType2 = false
DisableType3 = true      # Timed only
```

**Result:** Timed pressure encryptions are disabled, but pattern and sequence encryptions remain.

### Example 5: Mixed Disable with Auto-Complete
```ini
[TakeAndHold.Encryption]
DisableAllEncryptions = false
DisableType1 = true      # Instant disable pattern
DisableType2 = false
DisableType3 = true      # Instant disable timed
AutoComplete = true      # If enabled, would delay the disable
CompletionDelay = 1.5
```

**Result:** Pattern and timed encryptions are disabled (with 1.5s delay if AutoComplete is true).

## Encryption Types Explained

### Type 1: Pattern Matching
**Description:** Hit targets in a specific pattern or sequence  
**Examples:**
- Hit 4 specific panels in order
- Match the displayed pattern
- Sequential activation challenges

**When to Disable:**
- You don't like memorization challenges
- You want faster hold phases
- You prefer combat over puzzles

### Type 2: Sequence
**Description:** Complete ordered tasks  
**Examples:**
- Multi-step activation
- Progressive unlocking
- Timed sequences

**When to Disable:**
- You want simpler hold objectives
- You prefer direct combat
- Sequence timing is too tight

### Type 3: Timed
**Description:** Complete under time pressure  
**Examples:**
- Beat the clock challenges
- Time-limited patterns
- Speed-based encryptions

**When to Disable:**
- You don't like time pressure
- You want more relaxed gameplay
- Timed challenges feel too stressful

## Priority System

The system uses this priority order:

1. **DisableAllEncryptions** (overrides everything)
2. **DisableEncryptionNodes** (legacy, overrides specific types)
3. **Specific Type Settings** (DisableType1/2/3)
4. **AutoComplete** (modifies behavior of above)

### Priority Examples

```ini
# Example 1: DisableAllEncryptions takes priority
DisableAllEncryptions = true
DisableType1 = false    # Ignored - all encryptions disabled anyway
DisableType2 = true     # Ignored
DisableType3 = false    # Ignored
```

```ini
# Example 2: Specific types only apply when master switches are off
DisableAllEncryptions = false
DisableEncryptionNodes = false
DisableType1 = true     # Active - pattern encryptions disabled
DisableType2 = false    # Active - sequence encryptions enabled
DisableType3 = true     # Active - timed encryptions disabled
```

## Auto-Completion vs Instant Disable

### Instant Disable (AutoComplete = false)
- Encryption node disappears immediately
- No delay or animation
- Fastest option
- **Best for:** Speed runs, no-nonsense gameplay

### Auto-Complete (AutoComplete = true)
- Encryption node stays visible
- Completes after configured delay
- Gives illusion of attempted completion
- **Best for:** Feeling like you "did" the challenge without actual effort

## Performance Impact

| Setting | CPU Impact | Memory Impact |
|---------|------------|---------------|
| Disable All | Minimal | None |
| Specific Types | Minimal | None |
| Auto-Complete | Minimal | None (coroutine only) |

**Note:** All encryption disabling is very lightweight.

## Troubleshooting

### Encryptions Still Appearing

**Possible Causes:**
1. Settings not saved to config file
2. H3TVR not detecting TNH mode
3. Hold point not active yet

**Solutions:**
```ini
# Ensure these are set correctly:
[TakeAndHold.Encryption]
DisableAllEncryptions = true
```

**Debug:** Check BepInEx console for:
```
[TNH] Disabled all encryption nodes
[TNH] Disabled specific encryption type
```

### Auto-Complete Not Working

**Check:**
1. `AutoComplete = true`
2. `CompletionDelay` is set (default: 2.0)
3. BepInEx console shows auto-complete message

**Debug Console Message:**
```
[TNH] Auto-completed encryption after 2.0s delay
```

### Wrong Encryption Type Disabled

**Note:** H3VR doesn't expose encryption types directly, so type detection is best-effort.

**Workaround:** Use `DisableAllEncryptions = true` if specific types aren't working as expected.

## Best Practices

### For Casual Play
```ini
DisableAllEncryptions = true
AutoComplete = false
```
No encryption challenges - pure combat.

### For Speed Runs
```ini
DisableAllEncryptions = true
AutoComplete = false
CompletionDelay = 0.0
```
Instant completion for maximum speed.

### For Balanced Challenge
```ini
DisableAllEncryptions = false
DisableType3 = true       # Disable only timed pressure
DisableType1 = false
DisableType2 = false
```
Keep pattern and sequence, skip time pressure.

### For Immersion with Ease
```ini
DisableAllEncryptions = true
AutoComplete = true
CompletionDelay = 5.0
```
Encryptions complete themselves after 5 seconds (feels like you're hacking them).

## API Reference (For Developers)

### Check Encryption Settings
```csharp
// Check if encryption disabling is active
bool disableAll = plugin.Config.Bind<bool>("TakeAndHold.Encryption", "DisableAllEncryptions", false).Value;

// Check specific type
bool disableType1 = plugin.Config.Bind<bool>("TakeAndHold.Encryption", "DisableType1", false).Value;
```

### Manually Trigger Encryption Disable
```csharp
// Get current hold point
var holdPoint = GM.TNH_Manager?.m_curHoldPoint;

if (holdPoint?.m_systemNode != null)
{
    // Instant disable
    holdPoint.m_systemNode.m_numHitsLeft = 0;
    holdPoint.m_systemNode.gameObject.SetActive(false);
}
```

## Comparison with Legacy System

### Old System (DisableEncryptionNodes)
```ini
[TakeAndHold]
DisableEncryptionNodes = true    # All or nothing
```
- Only one option: disable everything
- No granular control
- No auto-completion option

### New System (TakeAndHold.Encryption)
```ini
[TakeAndHold.Encryption]
DisableAllEncryptions = true     # Master switch
DisableType1 = false             # Specific types
DisableType2 = false
DisableType3 = true
AutoComplete = true              # Auto-completion
CompletionDelay = 2.0
```
- Granular type control
- Auto-completion option
- Configurable delay
- More flexibility

## Related Features

### Infinite Tokens
```ini
[TakeAndHold]
InfiniteTokens = true    # 999 tokens
```
Combine with encryption disabling for maximum ease.

### Combined Configuration
```ini
[TakeAndHold]
InfiniteTokens = true
DisableEncryptionNodes = false    # Use new system instead

[TakeAndHold.Encryption]
DisableAllEncryptions = true
AutoComplete = false
```

## Console Commands (Future)

Planned feature for runtime control:
```
/tnh encrypt disable all
/tnh encrypt enable all
/tnh encrypt disable type1
/tnh encrypt autocomplete 3.0
```

## Summary

The TNH Specific Encryption Disable feature provides:

? **Granular Control** - Disable specific encryption types  
? **Master Switch** - Disable all encryptions at once  
? **Auto-Completion** - Automatic completion after delay  
? **Flexible Configuration** - Mix and match settings  
? **Performance** - Minimal impact on game performance  
? **Backwards Compatible** - Legacy DisableEncryptionNodes still works  

**Use it to customize your TNH experience exactly how you want it!**

---

**Version:** H3TVR 1.4.0+  
**Status:** ? Implemented  
**Config Section:** `[TakeAndHold.Encryption]`  
**Last Updated:** December 2024
