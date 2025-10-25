# Steam Friends Integration with Advanced Sosig Spawner

## Overview

The Steam Friends Integration system connects your Steam friends list with H3TVR's Advanced Sosig Spawner, allowing you to spawn sosigs using your actual Steam friends' names!

## Features

- ? **Auto-detect Steam friends** - Automatically loads your Steam friends list
- ? **Random or specific spawning** - Spawn random friends or all friends at once
- ? **Ally and enemy modes** - Spawn friends as allies or enemies
- ? **Auto-refresh** - Automatically refreshes friends list periodically
- ? **Fallback to INI names** - Uses default names if Steam unavailable
- ? **Update 120 TNH System** - Full integration with modern sosig spawning

## How It Works

### Automatic Integration

When H3TVR loads, it automatically:
1. Detects if Steam is running
2. Loads your Steam friends list
3. Links with the Advanced Sosig Spawner
4. Makes friends available for spawning

### Name Selection Priority

When spawning a sosig, the system uses this priority:

1. **Steam Friends** (if enabled and `UseSteamFriendsRandomNames` is true)
2. **INI Name Lists** (from `H3TVR_AllyNames.ini` / `H3TVR_EnemyNames.ini`)
3. **Default Names** ("Ally" / "Enemy")

## Configuration

### BepInEx Config Options

Located in `BepInEx/config/H3TVR.cfg`:

```ini
[SteamFriends]

# Enable Steam Friends integration
# Default: true
Enabled = true

# Use random Steam friend names instead of INI names
# Default: false
UseRandomNames = false

# Auto-refresh interval (seconds)
# Default: 300 (5 minutes)
RefreshInterval = 300
```

### Configuration Explained

#### Enabled
- **true**: Steam Friends integration is active
- **false**: Steam Friends integration is disabled (uses INI names only)

#### UseRandomNames
- **true**: Sosigs will use Steam friend names automatically
- **false**: Sosigs use INI names, but Steam friends can be spawned with specific keys

#### RefreshInterval
- How often to refresh the Steam friends list
- Recommended: 300 seconds (5 minutes)
- Set lower if friends come online/offline frequently

## Keyboard Controls

### Basic Steam Friends Spawning

| Key | Action | Description |
|-----|--------|-------------|
| `[` (Left Bracket) | Spawn Steam Friend (Ally) | Spawns one random Steam friend as an ally |
| `]` (Right Bracket) | Spawn Steam Friend (Enemy) | Spawns one random Steam friend as an enemy |

### Advanced Steam Friends Controls

| Key | Action | Description |
|-----|--------|-------------|
| `F7` | Spawn All Friends (Allies) | Spawns ALL Steam friends as allies |
| `F8` | Spawn All Friends (Enemies) | Spawns ALL Steam friends as enemies |
| `F9` | Refresh Friends List | Manually refresh Steam friends list |
| `Home` | Show Steam Friends Stats | Display friends list stats in console |

### Regular Chat Sosig Controls (Still Work!)

| Key | Action |
|-----|--------|
| `P` | Spawn Chat Sosig (Ally) |
| `O` | Spawn Chat Sosig (Enemy) |
| `Delete` | Clear All Chat Sosigs |
| `Insert` | Show Chat Sosig Stats |

## Usage Examples

### Example 1: Spawn Random Steam Friend as Ally

1. Press `[` (Left Bracket)
2. A random Steam friend spawns as an ally
3. Their Steam name appears above their head

### Example 2: Spawn All Friends for Fun

1. Press `F7`
2. ALL your Steam friends spawn as allies
3. Have a party with your entire friends list!

### Example 3: Fight Your Friends

1. Press `F8`
2. ALL your Steam friends spawn as enemies
3. Prepare for an epic battle!

### Example 4: Check Who's Available

1. Press `Home`
2. Check console/logs for friends list
3. See how many friends are online/offline

## Integration with Advanced Chat Spawner

### How Names Are Used

When `UseRandomNames = true`:
```csharp
// Spawning ally sosig
SpawningSequence("username") 
  ? Checks Steam Friends first
  ? If available, uses Steam friend name
  ? Otherwise falls back to INI names
```

When `UseRandomNames = false`:
```csharp
// Regular spawning uses INI names
SpawningSequence("username") ? Uses INI names

// Steam Friends keyboard shortcuts still work
Press [ ? Uses Steam friend name specifically
```

## Troubleshooting

### Steam Friends Not Loading

**Problem**: No Steam friends detected

**Solutions**:
1. Verify Steam is running
2. Check `Enabled = true` in config
3. Restart H3VR
4. Press `F9` to manually refresh

**Check Logs**:
```
[Info   : H3TVR] Steam Friends integration initialized successfully with X friends
```

### Names Not Showing

**Problem**: Sosigs spawn but don't have friend names

**Solutions**:
1. Set `UseRandomNames = true` in config
2. Use Steam Friends keyboard shortcuts (`[`, `]`)
3. Verify friends list loaded (press `Home`)

### Steam Not Available

**Problem**: "Steam is not initialized" error

**Expected Behavior**: 
- System falls back to INI names automatically
- No crashes or errors
- Regular chat sosigs still work

### Too Many Sosigs

**Problem**: Spawning all friends creates too many sosigs

**Solutions**:
1. Limit with `MaxAllySosigs` / `MaxEnemySosigs` config
2. Press `Delete` to clear all sosigs
3. Spawn selectively with `[` and `]` instead of `F7`/`F8`

## Technical Details

### Steam API Integration

Uses **Steamworks.NET**:
- `SteamFriends.GetFriendCount()` - Get total friends
- `SteamFriends.GetFriendByIndex()` - Get specific friend
- `SteamFriends.GetFriendPersonaName()` - Get friend's display name
- `SteamFriends.GetFriendPersonaState()` - Check if friend is online

### Auto-Refresh System

```csharp
// Refreshes automatically every 5 minutes (default)
private void Update()
{
    if (Time.time - lastRefreshTime > REFRESH_INTERVAL)
    {
        RefreshFriendsList();
    }
}
```

### Name Fallback Chain

```
Steam Friend Name
    ? (if not available)
INI Name Lists
    ? (if empty)
Default Names ("Ally" / "Enemy")
```

## Advanced Usage

### Customize Sosig Pools for Friends

Edit `BepInEx/config/H3TVR.cfg`:

```ini
[Chat Spawner]
# Sosig types for allies (your friends!)
AllySosigPool = M_Swat_Scout,M_Swat_Sniper,M_Merc_Scout

# Sosig types for enemies
EnemySosigPool = M_Swat_Heavy,M_Swat_Breacher,M_Soldier_Heavy
```

### Create Named INI Lists as Backup

Even with Steam Friends enabled, you can still customize INI name lists:

**BepInEx/config/H3TVR_AllyNames.ini**:
```ini
# Backup ally names if Steam unavailable
BestFriend
GamingBuddy
TacticalPartner
```

**BepInEx/config/H3TVR_EnemyNames.ini**:
```ini
# Enemy names
Rival
Opponent
Challenger
```

## Compatibility

### Compatible With
- ? Advanced Chat Sosig Spawner
- ? Update 120 TNH System
- ? Sosig Armor Customization
- ? INI Name Lists
- ? All H3VR gamemodes

### Requirements
- Steam must be running
- H3VR launched through Steam
- Steamworks.NET (included with H3VR)

### Optional
- Steam Friends (works without any friends)
- Steam Community profile (can be private)

## Performance Notes

- Friends list cached in memory (fast access)
- Auto-refresh every 5 minutes (configurable)
- Minimal performance impact
- Supports hundreds of friends

## FAQ

### Q: Will this work in offline mode?
**A**: No, Steam must be online. Falls back to INI names automatically.

### Q: Can I use both Steam names and INI names?
**A**: Yes! Set `UseRandomNames = false` and use keyboard shortcuts for Steam friends.

### Q: How many friends can I spawn?
**A**: Limited by `MaxAllySosigs` and `MaxEnemySosigs` config values (default: 8 each).

### Q: Do friends need to be online?
**A**: No! All friends in your Steam friends list work (online or offline).

### Q: Can I exclude specific friends?
**A**: Not currently. All friends in list are available.

### Q: Will this notify my friends?
**A**: No! This only reads names from your local Steam friends list.

## Example Scenarios

### Scenario 1: Co-op Feel
```
Config: UseRandomNames = true
Action: Press P to spawn ally
Result: Random Steam friend spawns with their actual name!
```

### Scenario 2: PvP Arena
```
Action: Press F8
Result: All Steam friends spawn as enemies
Setup: Unlimited sosigs for endless waves
```

### Scenario 3: Backup Names
```
Situation: Steam offline
Behavior: Automatically uses INI names
No crashes: Seamless fallback
```

## Credits

**System Integration**: H3TVR Enhanced Edition Team
**Steam API**: Valve Corporation
**Steamworks.NET**: Riley Labrecque

## Support

Issues or questions? Check:
1. BepInEx console logs
2. Press `Home` for Steam Friends stats
3. Verify config settings
4. Test with `F9` refresh

## Changelog

### Version 1.0 (Current)
- Initial Steam Friends integration
- Auto-detection and loading
- Random and bulk spawning
- Auto-refresh system
- Keyboard controls
- Fallback to INI names
