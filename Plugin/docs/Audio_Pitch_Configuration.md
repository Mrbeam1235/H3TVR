# Audio Pitch Configuration for Slomo Effects

## Overview
The H3TVR plugin now supports configurable audio pitch during slow motion (slomo) effects. This allows you to customize how audio sounds during time dilation events.

## Configuration Options

### Audio Section in Config

#### `SlomoAffectsAudio` (bool, default: true)
- **Description**: Whether slomo affects audio pitch
- **Values**: 
  - `true`: Audio pitch will be modified during slomo
  - `false`: Audio pitch remains unchanged during slomo

#### `SlomoAudioPitchScale` (float, default: 1.0)
- **Description**: Audio pitch multiplier during slomo
- **Values**: 
  - `1.0`: Normal pitch (no change)
  - `0.5`: Half pitch (deeper sound)
  - `2.0`: Double pitch (higher sound)
  - Any positive value is valid

#### `SlomoPreservePitch` (bool, default: false)
- **Description**: Controls pitch preservation behavior
- **Values**:
  - `true`: Preserves original pitch by compensating for time scale (sounds normal speed)
  - `false`: Uses custom pitch scaling based on `SlomoAudioPitchScale`

## Usage Examples

### Default Behavior (Audio scales with time)
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.0
SlomoPreservePitch = false
```
This will make audio pitch scale naturally with the time dilation (slower time = lower pitch).

### Preserve Normal Audio Pitch
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.0
SlomoPreservePitch = true
```
This will keep audio sounding normal during slomo by compensating for the time scale.

### Custom Pitch Scaling
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 0.5
SlomoPreservePitch = false
```
This will make audio pitch 50% lower during slomo, creating a dramatic effect.

### Disable Audio Changes
```ini
[Audio]
SlomoAffectsAudio = false
SlomoAudioPitchScale = 1.0
SlomoPreservePitch = false
```
This will keep audio completely unchanged during slomo.

## Technical Details

### How It Works
The system uses a Harmony patch on `AudioSource.pitch` setter to intercept and modify audio pitch values during slomo:

1. **When `SlomoAffectsAudio = false`**: No modifications are made
2. **When `SlomoPreservePitch = true`**: Pitch is adjusted to maintain normal sound: `pitch *= (1.0 / timeScale)`
3. **When `SlomoPreservePitch = false`**: Custom scaling is applied: `pitch *= (timeScale * pitchScale)`

### Performance Impact
- Minimal performance impact as the patch only executes when audio pitch is being set
- No additional processing during normal gameplay when not in slomo

### Compatibility
- Works with all Unity AudioSource components in H3VR
- Compatible with modded weapons and sound effects
- Integrates seamlessly with existing slomo system

## Configuration Tips

### For Cinematic Effects
- Use `SlomoAudioPitchScale = 0.3` with `SlomoPreservePitch = false` for dramatic slow-motion scenes
- Combine with movement scaling for full cinematic experience

### For Gameplay Focus
- Use `SlomoPreservePitch = true` to maintain audio clarity during tactical slomo
- Useful when you want time dilation without audio distraction

### For Realistic Effects
- Use default settings (`SlomoAudioPitchScale = 1.0`, `SlomoPreservePitch = false`)
- Audio pitch scales naturally with time, similar to real-world physics

## Integration with Other Systems

### Pillow Effects
The audio pitch system automatically works with pillow-triggered slomo effects. The same configuration applies to all slomo sources.

### VR Controller Triggers
Audio pitch changes apply to VR controller-triggered slomo as well as keyboard-triggered slomo.

### Movement Integration
Audio pitch works alongside the movement scaling system for a comprehensive slomo experience.

## Troubleshooting

### Audio Sounds Weird During Slomo
- Check `SlomoAudioPitchScale` value - very high or very low values can sound unnatural
- Try `SlomoPreservePitch = true` for normal-sounding audio

### No Audio Changes During Slomo
- Ensure `SlomoAffectsAudio = true`
- Check that slomo is actually activating (time scale should be less than 1.0)

### Audio Cuts Out
- Very low `SlomoAudioPitchScale` values (< 0.1) may cause audio issues
- Try values between 0.3 and 3.0 for best results