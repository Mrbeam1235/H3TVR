# Slomo Ramp Speed - Quick Reference

## ?? What It Does
Adds smooth, cinematic transitions to slow motion instead of instant/linear changes.

## ?? Quick Setup

### Maximum Cinematic (Recommended)
```ini
[Slomo.Ramp]
UseRampSpeed = true
RampCurve = "Cinematic"
RampDuration = 0.5
ReturnRampDuration = 0.8
```

### Snappy Action
```ini
[Slomo.Ramp]
UseRampSpeed = true
RampCurve = "EaseInOut"
RampDuration = 0.3
ReturnRampDuration = 0.4
```

### Disable (Original)
```ini
[Slomo.Ramp]
UseRampSpeed = false
```

## ?? Curve Types Cheat Sheet

| Curve | Feel | Use When |
|-------|------|----------|
| **Cinematic** | Ultra-smooth, movie-like | Maximum polish |
| **Smooth** | Natural S-curve | General use |
| **EaseInOut** | Balanced | Versatile |
| **EaseIn** | Builds tension | Dramatic moments |
| **EaseOut** | Graceful | Gentle slowdowns |
| **Linear** | Mechanical | Technical feel |

## ?? Common Presets

### Movie Mode
```
Curve: Cinematic
Down: 0.6s
Up: 1.0s
```

### Combat Mode
```
Curve: EaseInOut
Down: 0.3s
Up: 0.5s
```

### Dramatic Mode
```
Curve: EaseIn
Down: 1.0s
Up: 0.5s
```

## ?? Tuning Guide

**Too slow?** ? Decrease durations  
**Too fast?** ? Increase durations  
**Too floaty?** ? Try "Smooth" instead of "Cinematic"  
**Too abrupt?** ? Increase return duration  
**Want precision?** ? Use "EaseInOut" with short durations

## ? Compatibility
- Works with all slomo features
- VR-safe, no performance issues
- Backward compatible (can disable)

## ?? File Location
```
BepInEx/config/com.h3tvr.improved.cfg
```

Look for the `[Slomo.Ramp]` section.

---

**Quick Tip**: Start with "Cinematic" + 0.5/0.8 durations, then adjust to taste!
