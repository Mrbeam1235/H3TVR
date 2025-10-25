# Steam Friends Integration - Quick Reference

## ?? Quick Setup

1. **Enable in Config**: `BepInEx/config/H3TVR.cfg` ? `[SteamFriends] Enabled = true`
2. **Launch H3VR**: Steam Friends automatically detected
3. **Start Spawning**: Use keyboard controls below

## ?? Keyboard Controls

### Basic Spawning
| Key | Action |
|-----|--------|
| `[` | Spawn Random Steam Friend (Ally) |
| `]` | Spawn Random Steam Friend (Enemy) |

### Advanced Controls
| Key | Action |
|-----|--------|
| `F7` | Spawn ALL Friends as Allies |
| `F8` | Spawn ALL Friends as Enemies |
| `F9` | Refresh Steam Friends List |
| `Home` | Show Steam Friends Stats |

### Regular Chat Sosigs (Still Work!)
| Key | Action |
|-----|--------|
| `P` | Spawn Chat Sosig (Ally) |
| `O` | Spawn Chat Sosig (Enemy) |
| `Delete` | Clear All Sosigs |
| `Insert` | Show Stats |

## ?? Config Options

```ini
[SteamFriends]
Enabled = true              # Enable/disable Steam Friends
UseRandomNames = false      # Auto-use Steam names for all spawns
RefreshInterval = 300       # Auto-refresh interval (seconds)
```

## ?? Name Priority

1. **Steam Friends** (if `UseRandomNames = true`)
2. **INI Name Lists** (fallback)
3. **Default Names** (final fallback)

## ? Quick Checks

**Is Steam Friends Working?**
- Press `Home` - See friends count
- Check console: "Steam Friends integration initialized successfully"
- Spawn with `[` - Should see friend's name

**Not Working?**
1. Steam running? ?
2. Config enabled? ?
3. Press `F9` to refresh

## ?? Common Uses

### Spawn Your Best Friend
```
Press [
? Random friend spawns as ally
```

### Fight All Your Friends
```
Press F8
? All friends spawn as enemies
? Epic battle time!
```

### Co-op with Friends
```
Press F7
? All friends spawn as allies
? Team up!
```

## ?? Stats Command

Press `Home` to see:
- Total friends count
- Online friends count
- Last refresh time
- Integration status

## ?? Troubleshooting

| Problem | Solution |
|---------|----------|
| No friends loading | Press `F9` to refresh |
| Names not showing | Set `UseRandomNames = true` |
| Steam offline | Uses INI names automatically |
| Too many sosigs | Press `Delete` to clear all |

## ?? Pro Tips

1. **Selective Spawning**: Keep `UseRandomNames = false`, use `[` and `]` for Steam friends
2. **Party Mode**: Press `F7` for instant friend party
3. **PvP Arena**: Press `F8` for endless friend waves
4. **Check Stats**: Press `Home` before spawning to see friend count

## ?? Full Documentation

See `docs/SteamFriends_Integration_Guide.md` for complete details.

## ?? Quick Config Template

```ini
[SteamFriends]
Enabled = true
UseRandomNames = false      # Change to true for auto Steam names
RefreshInterval = 300

[Chat Spawner]
MaxAllySosigs = 8          # Increase for more friends
MaxEnemySosigs = 8
```

---

**Enjoy spawning with your Steam friends! ??**
