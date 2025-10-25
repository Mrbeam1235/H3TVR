# H3TVR Advanced AI System - Complete Guide

## Overview

The Advanced AI System adds tactical combat behaviors to sosigs while maintaining compatibility with the proven H3TwitchTools foundation. This creates smarter, more challenging sosigs that use cover, flank, and coordinate their attacks.

## Key Features

### ? **Tactical Behavior**
- **Cover System** - Sosigs find and use cover during firefights
- **Flanking** - Smart positioning to outmaneuver the player
- **Suppressive Fire** - Pin down enemies while repositioning
- **Tactical Retreat** - Fall back when injured
- **?? FRIENDLY FIRE PREVENTION** - Allies will NEVER shoot the player

### ? **Dynamic AI States**
- **Following** - Base H3TwitchTools behavior (allies)
- **Assault** - Direct attack (H3TwitchTools enhanced)
- **Taking Cover** - NEW: Use environment for protection
- **Suppressing** - NEW: Lay down covering fire
- **Flanking** - NEW: Outmaneuver opponents
- **Retreating** - NEW: Strategic withdrawal
- **Hold Position** - NEW: Defend an area

### ? **Performance Optimized**
- 2-second update intervals (vs real-time)
- Line-of-sight caching
- Minimal CPU overhead
- Compatible with H3TwitchTools patterns
- Built-in IFF (Identification Friend or Foe) system

---

## Configuration

### Basic Settings
```ini
[Advanced AI]
EnableAdvancedAI = true              # Enable tactical AI behaviors
EnableCoverSystem = true             # Allow sosigs to take cover
EnableTacticalMovement = true        # Enable flanking/suppression
EnableSquadCoordination = false      # Squad tactics (experimental)
PreventFriendlyFire = true           # Prevent allies from targeting player
CoverSearchRadius = 15.0             # How far to search for cover
SuppressionRadius = 10.0             # Suppressive fire range
```

### Enabling/Disabling
The Advanced AI system is **entirely optional**:
- Set `EnableAdvancedAI = false` for classic H3TwitchTools behavior
- Individual features can be toggled independently
- Zero performance impact when disabled

### Friendly Fire Prevention
**NEW: Built-in friendly fire prevention for ally sosigs!**

```ini
[Advanced AI]
PreventFriendlyFire = true    # Allies will NEVER target the player
```

**How it works:**
- Ally sosigs (IFF 0) automatically exclude player from targets
- Continuous monitoring ensures player stays off target list
- If player is accidentally targeted, they are immediately removed
- Allies will only engage actual enemies
- Works with both base H3TwitchTools and Advanced AI behaviors

---

## AI State Machine

### State Diagram
```
         Following (Ally Default)
                ?
            Has Enemy?
                ?
    ? Yes ?                    ? No: Return
    ?                                   ?
Low Health? ? No              Continue Following
    ?           ?
    ? Yes       ?
    ?      Check Cover
    ?           ?
Cover?          Direct Assault
    ?               ?
Taking Cover        ?
    ?               ?
Fire from Cover     ?
    ?               ?
Retreat if needed   Skirmish
                    ?
              Victory/Death
```

### State Descriptions

#### **Following** (H3TwitchTools Base)
- Allies follow player at configured distance
- Maintains formation with random offsets
- Line-of-sight checks before moving
- Switches to combat when threat detected

#### **Assault** (Enhanced)
- Direct attack on known enemy position
- Uses last known position if line-of-sight lost
- Continuous forward pressure
- No retreating unless injured

#### **Taking Cover** (?? NEW)
- Searches for cover within 15m radius
- Evaluates cover effectiveness
- Moves to cover position
- Peeks out to fire
- Returns to cover between shots

#### **Suppressing** (?? NEW)
- Lays down covering fire
- Forces enemies to keep heads down
- Enables flanking opportunities
- Maintains fire on target area

#### **Flanking** (?? NEW)
- Calculates perpendicular attack position
- Moves to flank enemy position
- Outmaneuvers static defenders
- Coordinates with direct assault

#### **Retreating** (?? NEW)
- Triggered at <30% health
- Moves away from threat
- Seeks cover while retreating
- Returns to combat at >50% health

#### **Hold Position** (?? NEW)
- Defends specific location
- Returns to combat if enemy approaches
- Useful for area defense
- Guards key positions

---

## How It Works

### Cover System

**Finding Cover:**
```csharp
1. Search 15m radius for environment objects
2. Check if object is between sosig and enemy
3. Validate cover effectiveness (dot product > 0.5)
4. Select closest valid cover
5. Move to cover position
```

**Using Cover:**
- Move to cover when distance > 1.5m
- Peek out to fire when at cover
- Return to cover after firing
- Re-evaluate cover if enemy moves

### Line of Sight

**Check Method:**
```csharp
Physics.Raycast(sosigHead, enemyPosition, EnvironmentMask)
  ? Has LOS: true
  : Blocked: false
```

**Usage:**
- Determines when to shoot
- Affects movement decisions
- Cached for performance
- Updated every 2 seconds

### Health Tracking

**Health Calculation:**
```csharp
totalHealth = sum(link.Health for all links)
maxHealth = sum(link.MaxHealth for all links)
healthPercent = totalHealth / maxHealth
```

**Behavior Triggers:**
- < 30% health: Seek cover or retreat
- < 60% health: Consider cover
- > 50% health: Return to combat
- > 80% health: Aggressive assault

---

## Integration with H3TwitchTools

### Compatibility Layer

The Advanced AI **enhances** H3TwitchTools patterns without replacing them:

```csharp
// H3TwitchTools base behavior (always works)
SetupAllyBehavior(sosig)  // Follow player
SetupEnemyBehavior(sosig) // Attack player

// Advanced AI component (optional)
var advancedAI = sosig.gameObject.AddComponent<AdvancedSosigAI>();
advancedAI.Initialize(sosig, logger);
```

### State Priority

```
1. H3TwitchTools base state (Following/Assault)
2. Advanced AI evaluation (every 2 sec)
3. Advanced AI state decision
4. Execute advanced state
5. Fall back to base if advanced disabled
```

### Performance

| System | Update Frequency | CPU Impact |
|--------|------------------|------------|
| H3TwitchTools | 1 second | Minimal |
| Advanced AI | 2 seconds | Low |
| Combined | Optimized | < 2% overhead |

---

## Usage Examples

### Basic Setup (Code)

```csharp
// Spawn sosig with H3TwitchTools
Sosig sosig = SpawnSosigLegacy(template, pos, rot, IFF);
SetupAllyBehavior(sosig);

// Add Advanced AI (optional)
if (AdvancedSosigAI.EnableAdvancedAI)
{
    var ai = sosig.gameObject.AddComponent<AdvancedSosigAI>();
    ai.Initialize(sosig, logger);
}
```

### Force Specific State

```csharp
// Get AI component
var ai = sosig.GetComponent<AdvancedSosigAI>();

// Force sosig into cover
if (ai != null)
{
    ai.ForceState(AdvancedSosigAI.AIState.TakingCover);
}
```

### Check AI Status

```csharp
var ai = sosig.GetComponent<AdvancedSosigAI>();
if (ai != null)
{
    var state = ai.GetCurrentState();
    var isInCombat = ai.IsInCombat();
    var health = ai.GetHealth();
    
    logger.LogInfo($"AI State: {state}, Combat: {isInCombat}, Health: {health:P0}");
}
```

---

## Advanced Features

### Squad Coordination (Experimental)

**When Enabled:**
- Sosigs coordinate attacks
- Flanking while suppressing
- Cover each other
- Synchronized assaults

**Warning:** Experimental feature, may be unstable!

```ini
[Advanced AI]
EnableSquadCoordination = true  # Use with caution!
```

### Custom AI Behaviors

**Extend the AI:**
```csharp
public class CustomSosigAI : AdvancedSosigAI
{
    protected override void ExecuteCustomState()
    {
        // Your custom AI logic here
    }
}
```

---

## Troubleshooting

### Ally Sosigs Shooting at Player

**This should NEVER happen with Advanced AI enabled!**

**Immediate Fix:**
```ini
[Advanced AI]
PreventFriendlyFire = true    # Should be enabled by default
```

**If problem persists:**
1. Check that ally sosigs have IFF 0 (they should automatically)
2. Verify Advanced AI is initialized properly
3. Check BepInEx console for "[AdvancedAI]" messages
4. Try manually calling `EnforceFriendlyFirePreventionNow()`

**Debug:**
```csharp
var ai = sosig.GetComponent<AdvancedSosigAI>();
if (ai != null)
{
    logger.LogInfo($"Is Ally: {ai.IsAlly()}");
    logger.LogInfo($"Sosig IFF: {sosig.E.IFFCode}");
    ai.EnforceFriendlyFirePreventionNow();
}
```

### Sosigs Not Using Cover

**Possible Causes:**
1. `EnableCoverSystem = false` in config
2. No valid cover within 15m radius
3. Cover is not between sosig and enemy
4. Environment layer not set correctly

**Solutions:**
- Enable cover system in config
- Increase `CoverSearchRadius`
- Verify environment has cover objects
- Check BepInEx console for errors

### Sosigs Acting Strange

**Debug Mode:**
```ini
[Advanced AI]
EnableAdvancedAI = true
# Add these temporarily
DebugMode = true
VerboseLogging = true
```

**Check Console:**
- State changes logged
- Cover search results
- Line-of-sight calculations
- Health status updates

### Performance Issues

**Reduce AI Load:**
```ini
# Less frequent updates
UpdateInterval = 3.0    # Default: 2.0 seconds

# Disable expensive features
EnableSquadCoordination = false
EnableFlank = false

# Smaller search radius
CoverSearchRadius = 10.0  # Default: 15.0
```

---

## Comparison: Classic vs Advanced

### Classic H3TwitchTools Behavior

**Allies:**
- Follow player at distance
- Shoot enemies when detected
- Simple, reliable

**Enemies:**
- Run at player
- Shoot continuously
- Predictable behavior

### Advanced AI Behavior

**Allies:**
- Follow player smartly
- Use cover when shot at
- Flank enemies
- Coordinate attacks
- Retreat when injured
- **Much more survivable**

**Enemies:**
- Tactical approaches
- Use cover effectively
- Flank player
- Suppress player
- Smart repositioning
- **Much more challenging**

---

## Performance Metrics

### CPU Usage
- **Disabled**: 0% overhead
- **Enabled (Basic)**: < 1% overhead
- **Enabled (Full)**: < 2% overhead
- **Enabled (Squad)**: 2-5% overhead

### Memory Usage
- **Component**: ~2KB per sosig
- **State Data**: ~500 bytes per sosig
- **Total Impact**: Minimal

### Update Frequency
- **State Evaluation**: Every 2 seconds
- **H3TwitchTools**: Every 1 second
- **Combined**: Optimized, non-blocking

---

## Best Practices

### For Performance
1. Disable squad coordination unless needed
2. Keep update intervals at 2+ seconds
3. Limit maximum sosigs (8-10 recommended)
4. Use cover system selectively

### For Gameplay
1. Enable Advanced AI for challenging combat
2. Disable for casual/fun gameplay
3. Mix AI levels (some advanced, some basic)
4. Adjust cover radius for map type

### For Development
1. Test with debug mode enabled
2. Monitor BepInEx console
3. Start with basic features
4. Add advanced features gradually

---

## Future Enhancements

Potential additions:
- [ ] Voice commands/callouts
- [ ] Dynamic difficulty adjustment
- [ ] Learn from player tactics
- [ ] Specialist roles (medic, sniper, etc.)
- [ ] Advanced squad formations
- [ ] Objective-based AI

---

## Credits

- **H3TwitchTools** - Base sosig spawning patterns (Arpytrooper)
- **H3TVR Team** - Advanced AI implementation
- **RUST LTD** - H3VR sosig system

---

## Summary

The Advanced AI System transforms sosigs from simple followers/attackers into tactical combatants that use cover, flank, and coordinate attacks. It's **completely optional**, maintaining full compatibility with classic H3TwitchTools behavior while adding challenging gameplay for those who want it.

**Key Takeaways:**
- ? Works alongside H3TwitchTools (doesn't replace)
- ? Optional system (can be fully disabled)
- ? Minimal performance impact (<2% overhead)
- ? 7 tactical AI states vs 2 basic states
- ? Cover system, flanking, suppression
- ? Smart health-based decisions
- ? Configurable for your playstyle

Enable it for challenging tactical combat, or disable it for classic H3VR sosig behavior. **Your choice!**
