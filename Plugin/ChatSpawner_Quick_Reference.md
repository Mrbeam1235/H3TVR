# Quick Reference: H3TVR Chat Spawner (H3TwitchTools Style)

## Quick Start

### Manual Spawning (Keyboard)
```
P     - Spawn ally sosig
O     - Spawn enemy sosig  
Delete - Clear all sosigs
```

### Code Spawning
```csharp
// Get the spawner instance
var spawner = EnhancedChatSpawner.Instance;

// Spawn ally
spawner.SpawningSequence("username");

// Spawn enemy
spawner.SpawningSequenceEnemy(1, "username");

// Clear all
spawner.ClearSosigs(true, true);

// Get stats
var stats = spawner.GetStats();
Console.WriteLine($"Allies: {stats.ActiveAllies}, Enemies: {stats.ActiveEnemies}");
```

## Configuration Quick Reference

```ini
[Chat Spawner]
MaxAllySosigs = 8                     # Max ally sosigs
MaxEnemySosigs = 8                    # Max enemy sosigs
SpawnCooldown = 2.0                   # Seconds between spawns
FollowDistance = 6.0                  # Ally follow distance
EnemyAggressionDistance = 20.0        # Enemy pursuit distance
EnableNameplates = true               # Show names above sosigs
EnableAutoCleanup = true              # Auto remove dead sosigs
```

## Behavior Summary

### Allies
- **Spawn**: 2-4m from player (random angle)
- **Behavior**: Follow player at 6m distance
- **Combat**: Switch to skirmish when targets appear
- **Movement**: Line of sight checks, random offsets

### Enemies
- **Spawn**: 8-15m from player (random angle)
- **Behavior**: Aggressive pursuit of player
- **Combat**: Direct assault, never idle
- **Movement**: Chase player beyond 20m range

## Common Tasks

### Change Max Sosigs
```ini
[Chat Spawner]
MaxAllySosigs = 16      # Increase ally limit
MaxEnemySosigs = 12     # Increase enemy limit
```

### Change Follow Distance
```ini
[Chat Spawner]
FollowDistance = 8.0    # Allies stay further away
```

### Disable Nameplates
```ini
[Chat Spawner]
EnableNameplates = false
```

### Change Spawn Keys
```ini
[Chat Spawner Keys]
SpawnAllyKey = F1
SpawnEnemyKey = F2
ClearSosigsKey = F3
```

## Troubleshooting

### No Sosigs Spawning
1. Check console for errors
2. Verify templates loaded (check logs on startup)
3. Check if at max sosig limit
4. Verify cooldown not active

### Sosigs Not Following
1. Check FollowDistance setting
2. Verify line of sight not blocked
3. Check if sosig is stunned
4. Review update coroutine running

### Sosigs Standing Still
1. Check IFF settings (ally=0, enemy=1)
2. Verify behavior setup completed
3. Check sosig state (idle vs assault)

## API Quick Reference

```csharp
// Spawner instance
EnhancedChatSpawner.Instance

// Public methods
.SpawningSequence(string username)                    // Spawn ally
.SpawningSequenceEnemy(int IFF, string username)      // Spawn enemy
.ClearSosigs(bool allies, bool enemies)               // Clear sosigs
.GetStats()                                           // Get statistics

// Static lists
EnhancedChatSpawner.spawnedChatters                   // Ally list
EnhancedChatSpawner.spawnedEnemyChatters              // Enemy list

// Stats structure
stats.ActiveAllies                                    // Count of allies
stats.ActiveEnemies                                   // Count of enemies
stats.QueueLength                                     // Queue length (always 0)
stats.TotalSpawned                                    // Total count
```

## Template System

### How Templates Work
1. Loads all `SosigEnemyTemplate` from scene on startup
2. Randomly selects template for each spawn
3. Applies weapons from template options
4. Applies outfit from template config
5. Sets IFF and behavior

### Adding Custom Templates
Use H3VR's native sosig template system - any templates in scene will be automatically available.

## Performance Tips

1. **Limit Active Sosigs** - Lower MaxAllySosigs/MaxEnemySosigs
2. **Enable Auto Cleanup** - Remove dead sosigs automatically
3. **Use Cooldown** - Prevent rapid spawning
4. **Clear When Done** - Use Delete key to clear all

## Integration Examples

### With SpawnManager
```csharp
var spawnManager = plugin.GetSpawnManager();
spawnManager.SpawnChatSosigFriendly();    // Spawn ally
spawnManager.SpawnChatSosigEnemy();       // Spawn enemy
spawnManager.ClearAllChatSosigs();        // Clear all
var stats = spawnManager.GetChatSosigStats();
```

### With Twitch
```csharp
// Backward compatible
chatSpawner.QueueTwitchSpawnRequest(
    username: "TwitchUser",
    displayName: "DisplayName",
    isFriendly: true
);
// Spawns immediately (no complex queuing)
```

## Key Differences from Old System

| Feature | Old | New (H3TwitchTools) |
|---------|-----|---------------------|
| Lines of code | 3000+ | 600 |
| Queue system | Complex priority queue | Direct spawn |
| AI | Advanced state machine | Simple follow/attack |
| Tracking | Per-user database | Simple lists |
| Behavior | 9 custom states | 2 simple patterns |
| Performance | Heavy monitoring | Unity native |

## What to Remember

1. **Simple is Better** - Removed complexity for reliability
2. **Proven Patterns** - Based on working H3TwitchTools code
3. **Direct Spawning** - No queue delays
4. **Easy to Debug** - Clear, focused code
5. **Stable** - Fewer moving parts = fewer bugs

## Getting Help

1. Check console for error messages
2. Review configuration values
3. Test with keyboard first
4. Check documentation
5. Report issues with logs

---

**Quick Tip**: Press `P` to spawn an ally and see if it follows you. That's the best test!
