# Infinite TNH Tokens - Quick Reference

## Quick Enable

1. Open `BepInEx/config/com.h3tvr.improved.cfg`
2. Find `[TakeAndHold]` section
3. Set `InfiniteTokens = true`
4. Start TNH - enjoy 999 tokens!

## What It Does

? Sets tokens to 999 in Take and Hold  
? Updates automatically during gameplay  
? Works with all TNH modes and characters  

## Configuration

```ini
[TakeAndHold]
InfiniteTokens = false  # Default: disabled
```

## Quick Toggle

```csharp
// Enable
h3tvr.SetInfiniteTokens(true);

// Disable
h3tvr.SetInfiniteTokens(false);

// Check status
bool enabled = h3tvr.IsInfiniteTokensEnabled();
```

## Use Cases

| Scenario | Setting |
|----------|---------|
| **Normal Play** | `false` |
| **Practice/Training** | `true` |
| **Testing Weapons** | `true` |
| **Learning TNH** | `true` |
| **Challenge Runs** | `false` |
| **Content Creation** | `true` |

## FAQ

**Q: Will it break TNH?**  
A: No, only affects token count. Everything else works normally.

**Q: Can I change it mid-run?**  
A: Yes! Edit config and it updates immediately.

**Q: Does it affect scoring?**  
A: You're using mods, so unofficial anyway. Use it for fun!

**Q: Why 999 instead of infinite?**  
A: High enough to be effectively infinite, avoids potential issues.

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Tokens not 999 | Check config: `InfiniteTokens = true` |
| Not working | Verify you're in TNH mode |
| Still running out | Config might not be saved, restart game |

## Performance

- **Impact**: Negligible
- **Memory**: None
- **CPU**: Minimal (one assignment per frame)
- **Safe for**: All systems

## Tips

?? **For Practice**: Enable it to learn TNH mechanics  
?? **For Fun**: Try all weapons without restrictions  
?? **For Testing**: Experiment with different loadouts  
?? **For Streaming**: Show off all equipment freely  

---

**Default**: Disabled (standard TNH experience)  
**Recommended**: Enable for practice, disable for challenge
