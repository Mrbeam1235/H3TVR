# H3TVR Slomo Ramp Speed Feature

## Overview

The **Ramp Speed** feature adds cinematic, smooth transitions to the slomo effect, making it feel more polished and natural compared to the original linear time scaling.

## What It Does

Instead of instantly jumping to slow motion or using basic linear interpolation, ramp speed provides:
- **Smooth acceleration curves** when entering slomo
- **Smooth deceleration curves** when returning to normal speed
- **Multiple easing options** for different feels
- **Configurable timing** for perfect control

## Configuration

### Enable/Disable
```ini
[Slomo.Ramp]
UseRampSpeed = true  # true = smooth ramp, false = original linear
```

### Curve Types
```ini
RampCurve = "EaseInOut"  # Options: Linear, EaseIn, EaseOut, EaseInOut, Smooth, Cinematic
```

#### Curve Type Comparison

| Curve Type | Description | Best For |
|------------|-------------|----------|
| **Linear** | Constant speed transition | Technical/mechanical feel |
| **EaseIn** | Slow start, fast end | Sudden impact moments |
| **EaseOut** | Fast start, slow end | Graceful slowdowns |
| **EaseInOut** | Slow start AND end | Balanced, professional |
| **Smooth** | Cubic hermite interpolation | Natural, organic feel |
| **Cinematic** | Quintic smoothing (smoothest) | Maximum movie-like quality |

### Timing Configuration
```ini
RampDuration = 0.5  # Time in seconds to ramp DOWN to slow motion
ReturnRampDuration = 0.8  # Time in seconds to ramp UP to normal speed
```

## Example Configurations

### Max Cinematicness (Recommended)
```ini
[Slomo.Ramp]
UseRampSpeed = true
RampCurve = "Cinematic"
RampDuration = 0.6
ReturnRampDuration = 1.0
```
**Result**: Buttery smooth, movie-quality transitions

### Snappy Action
```ini
[Slomo.Ramp]
UseRampSpeed = true
RampCurve = "EaseInOut"
RampDuration = 0.3
ReturnRampDuration = 0.4
```
**Result**: Quick but smooth, arcade-style

### Dramatic Entry
```ini
[Slomo.Ramp]
UseRampSpeed = true
RampCurve = "EaseIn"
RampDuration = 0.8
ReturnRampDuration = 0.5
```
**Result**: Builds tension before hitting peak slow motion

### Classic (Original Behavior)
```ini
[Slomo.Ramp]
UseRampSpeed = false
```
**Result**: Original linear scaling (backward compatible)

## Technical Details

### How It Works

1. **Ramp Down Phase**
   - When you press the slomo button (F by default)
   - Time.timeScale smoothly transitions from 1.0 ? configured max slomo value
   - Uses `Time.unscaledDeltaTime` for frame-independent animation
- Applies selected easing curve for smooth feel

2. **Hold Phase**
   - Stays at max slomo for configured wait time
   - Original behavior unchanged

3. **Ramp Up Phase**
   - After wait time expires
- Time.timeScale smoothly returns from max slomo ? 1.0
   - Can use different duration than ramp down
   - Same easing curve applied

### Mathematical Curves

#### Linear
```csharp
return t;  // Simple 1:1 mapping
```

#### EaseIn (Quadratic)
```csharp
return t * t;  // Accelerates over time
```

#### EaseOut (Quadratic)
```csharp
return t * (2f - t);  // Decelerates over time
```

#### EaseInOut (Quadratic)
```csharp
return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
```

#### Smooth (Cubic Hermite)
```csharp
return t * t * (3f - 2f * t);  // Smooth S-curve
```

#### Cinematic (Quintic)
```csharp
return t * t * t * (t * (t * 6f - 15f) + 10f);  // Ultra-smooth
```

## Performance Impact

- **Minimal CPU usage** - Simple math calculations per frame
- **No allocations** - All calculations done with value types
- **VR-safe** - No GC pressure or frame drops
- **Frame-independent** - Works consistently at any framerate

## Compatibility

### Works With
- ? All existing slomo features
- ? VR controller slomo activation
- ? Audio pitch scaling
- ? Movement speed scaling
- ? Pillow slomo effects
- ? Keyboard slomo trigger

### Does NOT Affect
- ? Zero gravity system
- ? Weapon mechanics
- ? Enemy AI
- ? Physics (other than time scale)

## Usage Tips

### For Maximum Cinematic Feel
1. Use **"Cinematic"** curve
2. Set RampDuration to **0.5-0.7 seconds**
3. Set ReturnRampDuration to **0.8-1.2 seconds**
4. Enable audio pitch preservation for even better effect

### For Arcade/Snappy Feel
1. Use **"EaseInOut"** curve
2. Set RampDuration to **0.2-0.3 seconds**
3. Set ReturnRampDuration to **0.3-0.5 seconds**

### For Dramatic Tension
1. Use **"EaseIn"** curve
2. Set RampDuration to **0.8-1.2 seconds**
3. Set ReturnRampDuration to **0.4-0.6 seconds**

## Comparison

### Before (Linear)
```
Normal ? [straight line] ? Slomo ? [straight line] ? Normal
Speed: Predictable but mechanical
Feel: Functional but not cinematic
```

### After (Ramp with Cinematic)
```
Normal ? [smooth S-curve] ? Slomo ? [smooth S-curve] ? Normal
Speed: Accelerates smoothly, feels natural
Feel: Movie-quality transitions
```

## Advanced Configuration

### Different Curves for Down/Up
While not currently configurable separately, you could modify the code to use:
- **EaseIn** for ramping down (builds anticipation)
- **EaseOut** for ramping up (graceful return)

### Fine-Tuning for Your Playstyle

| Playstyle | RampDuration | ReturnRampDuration | Curve |
|-----------|--------------|---------------------|-------|
| Tactical Shooter | 0.4s | 0.6s | EaseInOut |
| Action Hero | 0.3s | 0.5s | Smooth |
| Cinematic | 0.7s | 1.0s | Cinematic |
| Arcade | 0.2s | 0.3s | EaseIn |
| Dramatic | 1.0s | 0.5s | EaseIn |

## Troubleshooting

### Slomo Feels Too Slow to Start
- **Decrease** `RampDuration`
- Try **"EaseOut"** curve for faster initial transition

### Return Feels Too Abrupt
- **Increase** `ReturnRampDuration`
- Try **"EaseOut"** curve for gentler return

### Want Original Behavior
- Set `UseRampSpeed = false`

### Feels "Floaty" or Imprecise
- Try **"Smooth"** instead of **"Cinematic"**
- **Decrease** both duration values
- Consider **"EaseInOut"** for more predictability

## Code Example (for Modders)

```csharp
// Get ramp configuration
bool useRamp;
string curve;
float rampDuration, returnDuration;
plugin.GetSlomoRampConfig(out useRamp, out curve, out rampDuration, out returnDuration);

if (useRamp)
{
    // Calculate easing
    float t = Mathf.Clamp01(elapsed / rampDuration);
    float easedT = ApplyEasingCurve(t, curve);
  
    // Smooth interpolation
    Time.timeScale = Mathf.Lerp(startValue, targetValue, easedT);
}
```

## Future Enhancements

Potential additions:
- [ ] Separate curves for ramp down vs ramp up
- [ ] Custom animation curve editor support
- [ ] Preset configurations (Cinematic, Action, Tactical, etc.)
- [ ] Visual preview of curve shapes
- [ ] Per-activation randomization for variety

## Credits

- **Easing functions** based on Robert Penner's easing equations
- **Cinematic curve** uses smoothstep quintic formula
- **Implementation** by H3TVR Enhanced Edition team

## Summary

The Ramp Speed feature transforms slomo from a functional effect into a cinematic experience. By using smooth easing curves instead of linear interpolation, every slow motion moment feels more polished and professional.

**Recommended Settings**:
```ini
[Slomo.Ramp]
UseRampSpeed = true
RampCurve = "Cinematic"
RampDuration = 0.5
ReturnRampDuration = 0.8
```

Try it out and feel the difference! ??

---

**Version**: H3TVR 1.3.0+  
**Status**: ? Complete and Tested  
**Performance**: Minimal Impact  
**VR Compatible**: Yes
