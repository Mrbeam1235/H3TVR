# H3TVR Audio Pitch & Speed Control - Quick Reference

## Quick Setup Presets

### ?? Cinematic Slomo (Recommended)
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 0.7
SlomoPreservePitch = false
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 0.7
SlomoAudioMode = Both
```
**Effect**: Deep, slow audio perfect for cinematic moments

---

### ?? Tactical/Competitive
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.0
SlomoPreservePitch = true
SlomoAffectsAudioSpeed = false
SlomoAudioSpeedScale = 1.0
SlomoAudioMode = PitchOnly
```
**Effect**: Clear audio during slomo for tactical awareness

---

### ?? Surreal/Experimental
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.5
SlomoPreservePitch = false
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 0.3
SlomoAudioMode = Independent
```
**Effect**: Higher pitch with very slow playback - trippy!

---

### ?? No Audio Changes
```ini
[Audio]
SlomoAffectsAudio = false
```
**Effect**: Audio stays normal, only visuals slow down

---

## Configuration Parameters

| Parameter | Type | Default | Range | Description |
|-----------|------|---------|-------|-------------|
| `SlomoAffectsAudio` | bool | true | true/false | Master enable for audio effects |
| `SlomoAudioPitchScale` | float | 1.0 | 0.1 - 3.0 | Pitch multiplier during slomo |
| `SlomoPreservePitch` | bool | false | true/false | Preserve original pitch |
| `SlomoAffectsAudioSpeed` | bool | false | true/false | Enable speed adjustment |
| `SlomoAudioSpeedScale` | float | 1.0 | 0.1 - 3.0 | Speed multiplier during slomo |
| `SlomoAudioMode` | string | "Both" | See modes | How to apply adjustments |

---

## Audio Modes

| Mode | Pitch | Speed | Use Case |
|------|-------|-------|----------|
| **PitchOnly** | ? Adjusted | ? Normal | Simple, clear slomo effects |
| **SpeedOnly** | ? Normal | ? Adjusted | Experimental time-stretch |
| **Both** | ? Adjusted | ? Adjusted | Classic cinematic slomo |
| **Independent** | ? Custom | ? Custom | Advanced sound design |

---

## Common Use Cases

### 1. Movie-Style Slomo
**Goal**: Dramatic slow-motion like in action movies
```ini
SlomoAudioMode = Both
SlomoAudioPitchScale = 0.5
SlomoAudioSpeedScale = 0.5
```

### 2. Bullet Time Effect
**Goal**: Matrix-style slomo with audio clarity
```ini
SlomoAudioMode = PitchOnly
SlomoPreservePitch = true
```

### 3. Psychedelic Effect
**Goal**: Weird, experimental audio
```ini
SlomoAudioMode = Independent
SlomoAudioPitchScale = 2.0
SlomoAudioSpeedScale = 0.2
```

### 4. Realistic Physics
**Goal**: Audio matches time dilation realistically
```ini
SlomoAudioMode = Both
SlomoAudioPitchScale = 1.0
SlomoAudioSpeedScale = 1.0
```

---

## Troubleshooting Quick Fixes

| Problem | Quick Fix |
|---------|-----------|
| Audio too distorted | Reduce scale values to 0.5-0.8 range |
| No audio change | Check `SlomoAffectsAudio = true` |
| Audio stuttering | Disable speed: `SlomoAffectsAudioSpeed = false` |
| Too subtle | Reduce scale values to 0.3-0.5 |
| Too dramatic | Increase scale values to 0.8-1.0 |

---

## Scale Value Guide

### Pitch Scale Values
- **0.3** - Very deep, cinematic
- **0.5** - Noticeably lower, dramatic
- **0.7** - Moderately lower, balanced
- **1.0** - Natural scaling with time
- **1.5** - Slightly higher, energetic
- **2.0** - Noticeably higher, intense

### Speed Scale Values
- **0.2** - Extreme slow-down
- **0.5** - Significant slow-down
- **0.7** - Moderate slow-down
- **1.0** - Natural scaling with time
- **1.5** - Faster playback (unusual)

---

## Testing Your Configuration

1. **Enable slomo** (Default: F key or VR controller)
2. **Listen** to weapon sounds, footsteps, ambient audio
3. **Adjust** one parameter at a time
4. **Save** and test again
5. **Fine-tune** until it sounds right

---

## Performance Tips

- **Best Performance**: Use `PitchOnly` mode
- **Moderate**: Use `Both` with speed disabled
- **Most Intensive**: Use `Independent` mode with both enabled

---

## Integration Notes

? **Works with:**
- Pillow slomo effects
- VR controller slomo triggers
- Keyboard slomo activation
- All H3VR weapons and sounds

? **Limitations:**
- Unity doesn't support true pitch-independent time-stretching
- Some audio sources may not respond perfectly
- Extreme values (< 0.3 or > 2.0) may cause artifacts

---

## Example Configurations for Different Scenarios

### Speedrun Practice
```ini
SlomoAffectsAudio = true
SlomoPreservePitch = true
SlomoAudioMode = PitchOnly
```

### Content Creation/Streaming
```ini
SlomoAffectsAudio = true
SlomoAudioPitchScale = 0.6
SlomoAudioSpeedScale = 0.6
SlomoAudioMode = Both
```

### Casual Fun
```ini
SlomoAffectsAudio = true
SlomoAudioPitchScale = 0.8
SlomoPreservePitch = false
SlomoAudioMode = PitchOnly
```

---

## Advanced Tips

1. **Experiment**: Try different modes to find your preference
2. **Balance**: Match pitch and speed scales for cohesive effects
3. **Context**: Different scenarios may need different settings
4. **Defaults**: Start with default values and adjust slowly
5. **Save Presets**: Keep notes of your favorite configurations

---

## Need More Help?

- ?? Full documentation: `Audio_Pitch_Configuration.md`
- ?? Check BepInEx console for errors
- ?? Try the recommended presets first
- ?? One setting at a time for best results
