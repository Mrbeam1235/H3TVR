# ChatWatcher + AdvancedChatSosigSpawner Quick Reference

## Quick Setup (30 Seconds)

### 1. Enable ChatWatcher
```ini
# BepInEx/config/H3TVR.cfg
[Chat Spawner Integration]
EnableChatWatcher = true
```

### 2. Set File Paths
```ini
[Chat Watcher - File Mode]
AllyChatFilePath = BepInEx/config/H3TVR_AllyChat.txt
EnemyChatFilePath = BepInEx/config/H3TVR_EnemyChat.txt
```

### 3. Add Usernames to Files
```
# BepInEx/config/H3TVR_AllyChat.txt
ViewerName123
```

**Done!** Ally sosig spawns automatically.

---

## Keyboard Controls

| Key | Action |
|-----|--------|
| **P** | Spawn Ally Sosig |
| **O** | Spawn Enemy Sosig |
| **Delete** | Clear All Sosigs |

*Configurable in: `[Chat Watcher - Keys]`*

---

## File Formats

### Simple (Recommended)
```
ViewerName1
ViewerName2
ViewerName3
```

### JSON (H3TwitchTools)
```
{"username":"ViewerName1"}
{"username":"ViewerName2"}
```

### Both Work!
```
ViewerName1
{"username":"ViewerName2"}
ViewerName3
```

---

## Configuration Highlights

### Spawn Limits
```ini
[Chat Spawner]
MaxAllySosigs = 8          # Max friendly sosigs
MaxEnemySosigs = 8         # Max hostile sosigs
MaxSosigsPerUser = 2       # Per Twitch user limit
SpawnCooldown = 2.0        # Seconds between spawns
```

### Random Names
```ini
[Chat Spawner Advanced]
UseRandomNames = true      # Use names from INI files
AllyNamesFile = BepInEx/config/H3TVR_AllyNames.ini
EnemyNamesFile = BepInEx/config/H3TVR_EnemyNames.ini
```

### File Watching
```ini
[Chat Watcher - File Mode]
EnableFileWatching = true  # Monitor chat files
FileCheckInterval = 0.5    # Check every 0.5 seconds
ClearFileAfterRead = true  # Auto-clear files
```

---

## OBS Integration

### Method 1: OBS Chat Script
```python
# Python script to write chat to file
import os

def on_chat_message(username):
    with open("BepInEx/config/H3TVR_AllyChat.txt", "a") as f:
        f.write(username + "\n")
```

### Method 2: Streamlabs Chatbot
1. Create custom command: `!spawn`
2. Action: Write `$user` to ally chat file
3. Cooldown: 2 seconds (matches SpawnCooldown)

### Method 3: OBS Text Source
1. Add "Text (GDI+)" source
2. Check "Read from file"
3. File: Your chat log
4. Script filters to ally/enemy files

---

## Troubleshooting

### Sosigs Not Spawning?
? Check `EnableChatWatcher = true`  
? Verify file paths exist  
? Check file permissions  
? Look for errors in BepInEx console  

### Files Not Being Cleared?
? Set `ClearFileAfterRead = true`  
? Check file isn't read-only  
? Restart H3VR  

### Too Many Sosigs?
? Reduce `MaxAllySosigs` / `MaxEnemySosigs`  
? Increase `SpawnCooldown`  
? Enable `MaxSosigsPerUser` limit  

### Duplicate Sosigs?
? ChatWatcher prevents duplicates automatically  
? Clear cache: Delete key or restart H3VR  

---

## API Usage

### Spawn from Code
```csharp
// Get spawner instance
var spawner = AdvancedChatSosigSpawner.Instance;

// Spawn ally
spawner.SpawningSequence("TwitchUsername");

// Spawn enemy
spawner.SpawningSequenceEnemy(1, "TwitchUsername");

// Check if ChatWatcher active
bool active = spawner.IsChatWatcherEnabled();

// Get statistics
var stats = spawner.GetStats();
Console.WriteLine($"Active: {stats.TotalActive}");
Console.WriteLine($"ChatWatcher: {stats.ChatWatcherActive}");
```

### Clear Sosigs from Code
```csharp
// Clear all
spawner.ClearAllSosigs();

// Clear just allies
spawner.ClearSosigs(clearAllies: true, clearEnemies: false);

// Clear just enemies
spawner.ClearSosigs(clearAllies: false, clearEnemies: true);
```

---

## Advanced Features

### Boss Sosigs
```csharp
spawner.SpawningSequenceBoss(
    BossSosigSystem.BossType.Tank,
    "BossUsername"
);
```

### Queue Spawn with Priority
```csharp
spawner.QueueSpawn(
    username: "TwitchUser",
    displayName: "Custom Name",
    isFriendly: true,
    armorPreset: "Heavy",
    priority: SpawnPriority.High
);
```

### Twitch Integration
```csharp
spawner.QueueTwitchSpawnRequest(
    username: "viewer123",
    displayName: "Viewer",
    isFriendly: true,
    armorPreset: null,
    priority: SpawnPriority.Normal
);
```

---

## File Locations

### Default Paths:
- **Ally Chat**: `BepInEx/config/H3TVR_AllyChat.txt`
- **Enemy Chat**: `BepInEx/config/H3TVR_EnemyChat.txt`
- **Ally Names**: `BepInEx/config/H3TVR_AllyNames.ini`
- **Enemy Names**: `BepInEx/config/H3TVR_EnemyNames.ini`

### Custom Paths (Supported):
```ini
# Absolute paths
AllyChatFilePath = C:\StreamFiles\ally_chat.txt

# Relative to BepInEx
AllyChatFilePath = BepInEx/config/custom_ally.txt

# Relative to H3VR folder
AllyChatFilePath = StreamerTools/chat.txt
```

---

## Performance Tips

### Optimize File Checking:
```ini
FileCheckInterval = 1.0    # Slower checking (less CPU)
# vs
FileCheckInterval = 0.1    # Faster checking (more CPU)
```

### Limit Active Sosigs:
```ini
MaxAllySosigs = 4          # Fewer sosigs = better FPS
MaxEnemySosigs = 4
```

### Clear Dead Sosigs:
```ini
EnableAutoCleanup = true   # Auto-remove dead sosigs
SosigLifetime = 120.0      # Despawn after 2 minutes
```

---

## Common Patterns

### Streamer vs Viewer:
```ini
# High limits for viewers
MaxAllySosigs = 16
MaxSosigsPerUser = 1

# Low limits for focused gameplay
MaxAllySosigs = 4
MaxSosigsPerUser = 2
```

### Testing vs Production:
```ini
# Testing (manual spawning)
EnableChatWatcher = false
# Use P/O keys

# Production (file-based)
EnableChatWatcher = true
# Use chat file
```

### Custom Names vs Twitch Names:
```ini
# Show Twitch usernames
UseRandomNames = false

# Show custom names from INI
UseRandomNames = true
```

---

## Feature Compatibility

| Feature | ChatWatcher | Manual Keys | Both |
|---------|-------------|-------------|------|
| Spawn Allies | ? | ? | ? |
| Spawn Enemies | ? | ? | ? |
| Clear Sosigs | ? | ? | ? |
| Random Names | ? | ? | ? |
| Steam Names | ? | ? | ? |
| Boss Sosigs | ? | ? | ? |
| Per-User Limits | ? | ? | ? |
| Cooldown | ? | ? | ? |

---

## Example Workflows

### Workflow 1: OBS + Chat File
1. Viewer types `!spawn` in chat
2. OBS/Streamlabs writes username to file
3. ChatWatcher detects change
4. Sosig spawns in-game
5. File is cleared
6. Ready for next viewer

### Workflow 2: Manual Testing
1. Press **P** key
2. Ally sosig spawns instantly
3. Press **O** key
4. Enemy sosig spawns instantly
5. Press **Delete**
6. All sosigs cleared

### Workflow 3: Mixed Mode
1. Use file for viewer spawns
2. Use keys for manual spawns
3. Both tracked together
4. Single limit enforced

---

## Configuration Template

```ini
#########################
# ChatWatcher Integration
#########################
[Chat Spawner Integration]
EnableChatWatcher = true

[Chat Watcher - File Mode]
EnableFileWatching = true
AllyChatFilePath = BepInEx/config/H3TVR_AllyChat.txt
EnemyChatFilePath = BepInEx/config/H3TVR_EnemyChat.txt
FileCheckInterval = 0.5
ClearFileAfterRead = true

[Chat Watcher - Keys]
ManualAllySpawnKey = P
ManualEnemySpawnKey = O
ClearAllSosigsKey = Delete

#########################
# Sosig Configuration
#########################
[Chat Spawner]
MaxAllySosigs = 8
MaxEnemySosigs = 8
SpawnCooldown = 2.0
EnableNameplates = true
EnableAutoCleanup = true

[Chat Spawner Advanced]
UseRandomNames = true
MaxSosigsPerUser = 2
AllyNamesFile = BepInEx/config/H3TVR_AllyNames.ini
EnemyNamesFile = BepInEx/config/H3TVR_EnemyNames.ini
```

---

## Quick Reference Card

```
?????????????????????????????????????????
?   ChatWatcher Quick Reference         ?
?????????????????????????????????????????
?  SPAWN ALLY:    P key or file         ?
?  SPAWN ENEMY:   O key or file         ?
?  CLEAR ALL:     Delete key            ?
?                                       ?
?  Ally File:  H3TVR_AllyChat.txt      ?
?  Enemy File: H3TVR_EnemyChat.txt     ?
?                                       ?
?  Format: One username per line        ?
?  OR: {"username":"Name"}              ?
?????????????????????????????????????????
```

---

*Last Updated: January 2025*  
*Status: Production Ready ?*
