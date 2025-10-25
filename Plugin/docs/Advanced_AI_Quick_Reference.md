# Advanced AI Quick Reference

## Enable/Disable

### BepInEx Config
```ini
[Advanced AI]
EnableAdvancedAI = true   # Master switch
```

## AI States

| State | Description | When Used |
|-------|-------------|-----------|
| Following | Following player | Ally with no threats |
| Assault | Attacking enemy | Active combat |
| TakingCover | Using cover | Under fire, has cover |
| Suppressing | Suppressive fire | Supporting attack |
| Flanking | Tactical movement | Maneuvering around enemy |
| Retreating | Falling back | Low health |
| HoldingPosition | Defending area | Stationary defense |

## Ally Behavior

```
Health > 60% + Target: Assault
Health < 60% + Cover: Take Cover
Health < 30%: Retreat or Cover
No Target: Follow Player
```

## Enemy Behavior

```
Health > 50% + Close Range: Assault
Health < 50% + Far Range: Cover
Health < 30%: Retreat or Cover
Always: Aggressive toward player
```

## Friendly Fire Prevention

| Feature | Status |
|---------|--------|
| IFF Enforcement | ? Active |
| Player Targeting Block | ? Active |
| Continuous Checking | ? Active (2s intervals) |
| Auto-Correction | ? Active |

## Configuration Quick Settings

### Maximum Tactical Behavior
```ini
EnableAdvancedAI = true
EnableCoverSystem = true
EnableTacticalMovement = true
EnableSquadCoordination = false  # Performance impact
PreventFriendlyFire = true
CoverSearchRadius = 20.0
```

### Balanced Settings (Recommended)
```ini
EnableAdvancedAI = true
EnableCoverSystem = true
EnableTacticalMovement = true
EnableSquadCoordination = false
PreventFriendlyFire = true
CoverSearchRadius = 15.0
```

### Minimal AI (Performance Mode)
```ini
EnableAdvancedAI = false  # Disables all Advanced AI
```

## Public API Methods

```csharp
// Force specific state
advancedAI.ForceState(AIState.TakingCover);

// Get current state
AIState currentState = advancedAI.GetCurrentState();

// Check if in combat
bool inCombat = advancedAI.IsInCombat();

// Get health percentage
float health = advancedAI.GetHealth();

// Check if ally
bool isAlly = advancedAI.IsAlly();

// Manually enforce friendly fire prevention
advancedAI.EnforceFriendlyFirePreventionNow();
```

## Performance

| Metric | Value |
|--------|-------|
| Update Interval | 2 seconds |
| CPU per Sosig | Minimal |
| Memory per Sosig | ~2KB |
| Recommended Max | 16 sosigs |

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Allies shoot player | Enable `PreventFriendlyFire` |
| No cover behavior | Check `EnableCoverSystem = true` |
| Sosigs stand still | Verify AI is enabled |
| Performance issues | Reduce sosig count |
| State not changing | Check 3s state change cooldown |

## Debug Logging

Enable detailed logging in code:
```csharp
logger.LogDebug("AI state changed to: {state}");
```

Look for these log entries:
- `[AdvancedAI] Initialized for sosig`
- `[AdvancedAI] Reset ally IFF to 0`
- `[AdvancedAI] Prevented ally from targeting player`
- State change logs

## Quick Tips

?? **For Streamers**: Enable all features for maximum chaos
?? **For Performance**: Disable squad coordination
?? **For Realism**: Enable cover + tactical movement
?? **For Fun**: Mix ally and enemy sosigs with full AI

## Integration Status

| Component | Status |
|-----------|--------|
| Build | ? Successful |
| Chat Spawner | ? Integrated |
| Config System | ? Complete |
| Friendly Fire Prevention | ? Active |
| Cover System | ? Functional |
| Documentation | ? Complete |

---

**Ready to use!** Just spawn sosigs and watch them use tactical behaviors automatically.
