# Boss Sosig System - Complete Guide

## Overview
The Boss Sosig System adds powerful enemy sosigs with special abilities, enhanced stats, and unique behaviors to H3TVR. Bosses integrate seamlessly with the Advanced AI system and provide challenging encounters.

## Boss Types

### 1. **Tank Boss** (KeyCode.Alpha1)
**Role**: Heavy frontline fighter
- **Health**: 4.5x normal (1.5x base × 3.0x boss multiplier)
- **Speed**: 0.84x normal (0.7x speed penalty)
- **Damage**: 1.2x normal (0.8x × 1.5x)
- **AI Behavior**: Never retreats, always aggressive
- **Special Ability**: Shield Bash (AOE knockback every 10s)
- **Best Against**: Multiple weaker enemies
- **Weakness**: Slow movement, vulnerable to flanking

### 2. **Berserker Boss** (KeyCode.Alpha2)
**Role**: High-speed assault
- **Health**: 2.4x normal (0.8x base × 3.0x boss multiplier)
- **Speed**: 1.8x normal (1.5x × 1.2x)
- **Damage**: 2.25x normal (1.5x × 1.5x)
- **AI Behavior**: Constant assault, never covers
- **Special Ability**: Charge (rushes player every 10s)
- **Best Against**: Players who stay stationary
- **Weakness**: Lower health pool

### 3. **Sniper Boss** (KeyCode.Alpha3)
**Role**: Long-range precision
- **Health**: 3.0x normal (standard boss multiplier)
- **Speed**: 1.08x normal (0.9x × 1.2x)
- **Damage**: 3.0x normal (2.0x × 1.5x)
- **AI Behavior**: Prefers cover, maintains distance
- **Special Ability**: N/A (enhanced accuracy)
- **Best Against**: Players in the open
- **Weakness**: Close-quarters combat

### 4. **Summoner Boss** (KeyCode.Alpha4)
**Role**: Minion support
- **Health**: 3.6x normal (1.2x base × 3.0x boss multiplier)
- **Speed**: 1.2x normal (standard)
- **Damage**: 1.05x normal (0.7x × 1.5x)
- **AI Behavior**: Defensive, summons allies
- **Special Ability**: Spawn Minions (1-2 enemies every 15s, max 3)
- **Best Against**: Solo players
- **Weakness**: Vulnerable without minions

### 5. **Elite Boss** (KeyCode.Alpha5)
**Role**: Balanced all-rounder
- **Health**: 3.9x normal (1.3x base × 3.0x boss multiplier)
- **Speed**: 1.32x normal (1.1x × 1.2x)
- **Damage**: 1.8x normal (1.2x × 1.5x)
- **AI Behavior**: Adaptive tactical AI
- **Special Ability**: N/A (superior AI)
- **Best Against**: Most situations
- **Weakness**: No specific strengths

### 6. **Juggernaut Boss** (KeyCode.Alpha6)
**Role**: Ultimate tank
- **Health**: 6.0x normal (2.0x base × 3.0x boss multiplier)
- **Speed**: 0.6x normal (0.5x × 1.2x)
- **Damage**: 2.7x normal (1.8x × 1.5x)
- **AI Behavior**: Unstoppable advance
- **Special Ability**: Shield Bash (larger AOE than Tank)
- **Best Against**: Everything (hardest boss)
- **Weakness**: Extremely slow

### 7. **Assassin Boss** (KeyCode.Alpha7)
**Role**: Flanking specialist
- **Health**: 2.1x normal (0.7x base × 3.0x boss multiplier)
- **Speed**: 2.16x normal (1.8x × 1.2x)
- **Damage**: 2.55x normal (1.7x × 1.5x)
- **AI Behavior**: Prioritizes flanking
- **Special Ability**: N/A (extreme speed + AI flanking)
- **Best Against**: Distracted players
- **Weakness**: Lowest health of all bosses

### 8. **Commander Boss** (KeyCode.Alpha8)
**Role**: Force multiplier
- **Health**: 4.2x normal (1.4x base × 3.0x boss multiplier)
- **Speed**: 1.2x normal (standard)
- **Damage**: 1.65x normal (1.1x × 1.5x)
- **AI Behavior**: Tactical leadership
- **Special Ability**: Buff Allies (15m radius, every 8s)
- **Best Against**: Groups of weak sosigs
- **Weakness**: Less effective alone

## Configuration

### BepInEx Config (H3TVR.cfg)
```ini
[Boss Sosigs]
# Enable the boss sosig system
EnableBossSosigs = true

# Boss health multiplier (applied to base type multiplier)
BossHealthMultiplier = 3.0

# Boss damage multiplier (applied to base type multiplier)
BossDamageMultiplier = 1.5

# Boss speed multiplier (applied to base type multiplier)
BossSpeedMultiplier = 1.2

# Maximum bosses that can exist at once
MaxBossesPerSession = 3

# Cooldown between boss spawns (seconds)
BossSpawnCooldown = 120.0

# Enable boss special abilities
EnableBossAbilities = true

# Enable boss minion spawning (Summoner type)
EnableBossMinions = true
```

## Key Bindings

### Default Controls
```ini
[KeyBindings]
# Random boss spawn
KeyBindForSpawnBossRandom = B

# Specific boss types
KeyBindForSpawnBossTank = Alpha1
KeyBindForSpawnBossBerserker = Alpha2
KeyBindForSpawnBossSniper = Alpha3
KeyBindForSpawnBossSummoner = Alpha4
KeyBindForSpawnBossElite = Alpha5
KeyBindForSpawnBossJuggernaut = Alpha6
KeyBindForSpawnBossAssassin = Alpha7
KeyBindForSpawnBossCommander = Alpha8

# Clear all bosses
KeyBindForClearBosses = Backspace
```

## Usage Examples

### Manual Boss Spawning
```csharp
// Spawn random boss
Press B

// Spawn specific boss types
Press 1 = Tank Boss
Press 2 = Berserker Boss
Press 3 = Sniper Boss
Press 4 = Summoner Boss
Press 5 = Elite Boss
Press 6 = Juggernaut Boss
Press 7 = Assassin Boss
Press 8 = Commander Boss

// Clear all bosses
Press Backspace
```

### Programmatic Boss Spawning
```csharp
// Get boss system instance
var advancedSpawner = AdvancedChatSosigSpawner.Instance;

// Spawn random boss
BossSosigSystem.BossType randomType = BossSosigSystem.GetRandomBossType();
advancedSpawner.SpawningSequenceBoss(randomType, "Boss Name");

// Spawn specific boss type
advancedSpawner.SpawningSequenceBoss(BossSosigSystem.BossType.Juggernaut, "Big Boss");

// Check active boss count
int activeBosses = BossSosigSystem.GetActiveBossCount();

// Clear all bosses
BossSosigSystem.ClearAllBosses();
```

### Twitch Integration
Bosses can be spawned via Twitch chat commands:
```
!spawnenemy boss:tank
!spawnenemy boss:berserker
!spawnenemy boss:elite
```

The behavior parameter supports:
- `boss` = Random boss type
- `boss:tank` = Specific boss type
- `boss:summoner` = Summoner with minions

## Boss Mechanics

### Enrage System
When a boss drops below 30% health:
- **Damage**: +50% (×1.5 multiplier)
- **Speed**: +30% (×1.3 multiplier)
- **Ability Cooldown**: -30% (×0.7 multiplier)
- **Visual**: Nameplate shows "? ENRAGED ?"

### Special Abilities

#### Shield Bash (Tank/Juggernaut)
- **Radius**: 3m
- **Effect**: Knockback all nearby objects
- **Force**: 500 units
- **Cooldown**: 10s
- **Trigger**: Automatic

#### Berserker Charge
- **Effect**: High-speed rush toward player
- **Speed**: 2x normal movement
- **Range**: Unlimited
- **Cooldown**: 10s
- **Trigger**: Automatic

#### Spawn Minions (Summoner)
- **Count**: 1-2 per cast
- **Max Total**: 3 active minions
- **Type**: Same faction as Summoner
- **Position**: 3m radius around Summoner
- **Cooldown**: 15s
- **Trigger**: Automatic

#### Buff Allies (Commander)
- **Radius**: 15m
- **Effect**: +20% speed/damage to nearby allies
- **Duration**: 8s
- **Cooldown**: 8s (permanent uptime)
- **Trigger**: Automatic

### Boss AI Integration

Bosses automatically receive Advanced AI if enabled:
```ini
[Advanced AI]
EnableAdvancedAI = true
```

**Boss AI Features**:
- Cover-seeking behavior (except Berserker/Tank)
- Tactical retreats at low health (except Tank/Juggernaut)
- Flanking maneuvers (enhanced for Assassin)
- Target prioritization
- Friendly fire prevention (if boss is ally)

### Boss Spawning Rules

#### Spawn Restrictions
1. **Maximum Active**: Cannot exceed `MaxBossesPerSession`
2. **Cooldown**: Must wait `BossSpawnCooldown` seconds between spawns
3. **Sosig Limit**: Counts toward max enemy sosigs
4. **TNH Mode**: Supports TNH spawn points and IFF

#### Spawn Locations
- **Normal Mode**: 20-30m from player
- **TNH Mode**: Uses TNH attack vectors
- **Boss Distance**: 1.5x farther than regular enemies

### Boss Nameplate System

Bosses display special nameplates:
```
? BOSS_Tank ?
? Big Boss ?
? ENRAGED ? Elite Boss ? ?
```

**Nameplate Colors**:
- **Normal**: Red (enemy)
- **Enraged**: Bright red with lightning bolts
- **Custom**: Use provided username

## Advanced Features

### Boss Health Calculation
```csharp
// Base health for each link
foreach (var link in sosig.Links)
{
    link.m_integrity *= BossHealthMultiplier * TypeMultiplier;
}

// Example: Juggernaut
// Base = 1.0 per link
// Type = 2.0x (Juggernaut modifier)
// Boss = 3.0x (config multiplier)
// Final = 1.0 × 2.0 × 3.0 = 6.0x per link
```

### Boss Update Loop
Bosses run a 1-second update loop:
```csharp
while (alive)
{
    UpdateBossHealth();      // Check enrage threshold
    UpdateBossAbilities();   // Execute special abilities
    UpdateMinions();         // Clean up dead minions
    yield return 1 second;
}
```

### Boss Death
When a boss dies:
1. Logs death message with health stats
2. Cleans up all spawned minions (Summoner)
3. Removes from active boss tracking
4. Plays death effects (if configured)
5. Destroys boss component

## Integration with Other Systems

### Advanced AI
```csharp
// Boss automatically gets Advanced AI
if (AdvancedSosigAI.EnableAdvancedAI)
{
    var advancedAI = sosig.gameObject.AddComponent<AdvancedSosigAI>();
    advancedAI.Initialize(sosig, logger);
}
```

### Advanced Chat Spawner
```csharp
// Boss spawning uses same system as regular sosigs
Sosig SpawningSequenceBoss(BossType type, string username)
{
    // Uses modern/legacy spawn system
    // Applies boss stats
    // Attaches boss component
    // Returns sosig reference
}
```

### Audio System (Optional)
```csharp
// Boss spawn sound
audioManager?.PlayDangerCloseSound("boss_spawn", 
    playerPos, false, "boss/boss_appears.wav", 1.0f);

// Boss-specific sounds
audioManager?.PlayDangerCloseSound($"boss_{bossType}", 
    playerPos, false, $"boss/{bossType}_appears.wav", 1.0f);
```

## Performance Considerations

### Update Frequency
- **Boss Update**: 1 second intervals
- **Ability Check**: Every update (1s)
- **Minion Check**: Every update (1s)
- **AI Update**: 2 second intervals (Advanced AI)

### Resource Usage
- **CPU**: Minimal (1s update loop)
- **Memory**: ~1KB per active boss
- **Max Bosses**: Limited by config (default: 3)

### Optimization Tips
1. Limit `MaxBossesPerSession` to 2-3
2. Disable `EnableBossMinions` if not needed
3. Increase `BossSpawnCooldown` for less frequent spawns
4. Use specific boss types instead of random

## Troubleshooting

### Boss Not Spawning
**Check**:
- Max bosses reached? (`GetActiveBossCount()`)
- Spawn cooldown active?
- Max enemy sosigs reached?
- Boss system enabled in config?

**Solution**:
```csharp
// Clear existing bosses
BossSosigSystem.ClearAllBosses();

// Increase max bosses
MaxBossesPerSession = 5

// Reduce cooldown
BossSpawnCooldown = 60
```

### Boss Too Easy/Hard
**Adjust**:
```ini
# Increase difficulty
BossHealthMultiplier = 5.0
BossDamageMultiplier = 2.0

# Decrease difficulty
BossHealthMultiplier = 2.0
BossDamageMultiplier = 1.2
```

### Boss Abilities Not Working
**Check**:
```ini
# Enable abilities
EnableBossAbilities = true

# Enable minions (Summoner)
EnableBossMinions = true
```

### Boss Not Using Advanced AI
**Check**:
```ini
[Advanced AI]
EnableAdvancedAI = true
```

## Future Enhancements

### Potential Features
1. **Boss Loot Drops**: Reward players for defeating bosses
2. **Boss Music**: Dynamic music when boss is active
3. **Boss Phases**: Multiple health-based phases
4. **Boss Voice Lines**: Audio callouts
5. **Boss Announcements**: On-screen notifications
6. **Boss Variants**: Different skin/weapon combinations
7. **Boss Progression**: Bosses get stronger over time
8. **Boss Encounters**: Scripted boss battles

### Customization
```csharp
// Create custom boss type
public class CustomBoss : BossSosig
{
    protected override void CustomAbility()
    {
        // Your custom ability code
    }
}
```

## Credits
- **Advanced AI Integration**: AdvancedSosigAI system
- **Spawn System**: AdvancedChatSosigSpawner
- **H3VR**: Native sosig AI and mechanics

---

**Version**: 1.2.0  
**Status**: Production Ready  
**Last Updated**: 2025-01-27
