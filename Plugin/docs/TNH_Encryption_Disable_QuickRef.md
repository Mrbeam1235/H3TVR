# TNH Encryption Disable - Quick Reference

## ? Quick Setup (2 Steps)

### 1. Enable in Config
```ini
[TakeAndHold]
DisableEncryptionNodes = true
```

### 2. Play TNH!
- Start any Take and Hold game
- Encryption nodes will be automatically disabled
- Focus on pure combat

---

## ?? What It Does

| Feature | Status |
|---------|--------|
| **Encryption Challenges** | ? Skipped automatically |
| **Hacking Mini-Games** | ? Bypassed |
| **Combat Waves** | ? Normal difficulty |
| **Tokens** | ? Not affected (use InfiniteTokens for that) |
| **Score** | ? Not affected |

---

## ?? Common Configurations

### Combat Focus
```ini
DisableEncryptionNodes = true
InfiniteTokens = false
```
**Result**: Pure combat, normal token economy

### Easy Mode
```ini
DisableEncryptionNodes = true
InfiniteTokens = true
```
**Result**: No encryption, unlimited tokens

### Standard TNH
```ini
DisableEncryptionNodes = false
InfiniteTokens = false
```
**Result**: Normal TNH experience

---

## ?? In-Code Control

```csharp
// Get plugin instance
var h3tvr = FindObjectOfType<H3TVRImproved>();

// Disable encryption
h3tvr.SetEncryptionNodes(true);

// Enable encryption (normal)
h3tvr.SetEncryptionNodes(false);

// Check status
bool disabled = h3tvr.IsEncryptionDisabled();
```

---

## ? Verification

**How to tell it's working:**
1. Start TNH game
2. Reach a hold point with encryption
3. Encryption node should be inactive/skipped
4. Combat waves begin immediately

**Check BepInEx console for:**
```
[TNH] Disabled encryption node
```

---

## ?? Use Cases

| Scenario | Recommended Setting |
|----------|-------------------|
| **Speed Runs** | Enabled |
| **Combat Training** | Enabled |
| **New Players** | Enabled |
| **Challenge Runs** | Disabled |
| **Standard Play** | Disabled |
| **Streaming** | Enabled (more action) |

---

## ?? Combining Features

### With Infinite Tokens
```ini
InfiniteTokens = true           # Unlimited supply spending
DisableEncryptionNodes = true   # No encryption
```
**Best for**: Casual/relaxed gameplay

### Solo Encryption Disable
```ini
InfiniteTokens = false          # Normal tokens
DisableEncryptionNodes = true   # No encryption
```
**Best for**: Combat-focused training

---

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| Encryption still appears | Check config: `DisableEncryptionNodes = true` |
| Node visible but inactive | This is normal - visual remains, functionality disabled |
| Not working in TNH | Verify you're in an active TNH game |
| Config not loading | Restart H3VR after config changes |

---

## ?? Feature Comparison

| Feature | Infinite Tokens | Encryption Disable |
|---------|----------------|-------------------|
| **Affects** | Token count | Encryption nodes |
| **Impact** | Supply points | Hacking challenges |
| **Default** | Off | Off |
| **Use Case** | Resource freedom | Combat focus |
| **Can Combine?** | ? Yes | ? Yes |

---

## ?? Pro Tips

1. **Try both separately** - Test infinite tokens and encryption disable individually
2. **Speedrun setup** - Enable encryption disable for faster runs
3. **Training mode** - Disable encryption to maximize combat practice
4. **Stream-friendly** - More action, less downtime for viewers
5. **Casual mode** - Enable both for maximum relaxation

---

## ?? Config File Location

```
H3VR/BepInEx/config/com.h3tvr.improved.cfg
```

**Look for:**
```ini
[TakeAndHold]
DisableEncryptionNodes = false  # Set to true
InfiniteTokens = false          # Optional: Set to true
```

---

## ?? Related Features

- **Infinite Tokens** - Unlimited supply point spending
- **Advanced AI** - Enhanced sosig behaviors
- **Boss System** - Spawn challenging boss sosigs
- **Steam Friends** - Spawn friends as sosigs

---

**Status**: ? Working  
**Performance**: Minimal impact  
**Compatibility**: All TNH modes  
**Version**: H3TVR 1.3.0+
