# Audio Pitch and Speed Configuration for Slomo Effects

## Overview
The H3TVR plugin now supports advanced configurable audio pitch and speed control during slow motion (slomo) effects. This allows you to customize how audio sounds during time dilation events with independent control over pitch and playback speed.

## Configuration Options

### Audio Section in Config

#### `SlomoAffectsAudio` (bool, default: true)
- **Description**: Master switch - whether slomo affects audio at all
- **Values**: 
  - `true`: Audio will be modified during slomo based on other settings
  - `false`: Audio remains completely unchanged during slomo

#### `SlomoAudioPitchScale` (float, default: 1.0)
- **Description**: Audio pitch multiplier during slomo
- **Values**: 
  - `1.0`: Normal pitch scaling (matches time scale)
  - `0.5`: Half pitch (deeper sound)
  - `2.0`: Double pitch (higher sound)
  - Any positive value between 0.1 and 3.0

#### `SlomoPreservePitch` (bool, default: false)
- **Description**: Controls pitch preservation behavior
- **Values**:
  - `true`: Preserves original pitch by compensating for time scale (sounds normal speed)
  - `false`: Uses custom pitch scaling based on `SlomoAudioPitchScale`

#### `SlomoAffectsAudioSpeed` (bool, default: false)
- **Description**: Whether slomo affects audio playback speed (time stretching)
- **Values**:
  - `true`: Audio playback speed will be adjusted
  - `false`: Audio speed remains normal

#### `SlomoAudioSpeedScale` (float, default: 1.0)
- **Description**: Audio speed multiplier during slomo
- **Values**:
  - `1.0`: Normal speed scaling (matches time scale)
  - `0.5`: Half speed playback
  - `2.0`: Double speed playback
  - Any positive value between 0.1 and 3.0

#### `SlomoAudioMode` (string, default: "Both")
- **Description**: Controls how pitch and speed adjustments are applied
- **Values**:
  - `"PitchOnly"`: Only pitch changes, speed remains normal
  - `"SpeedOnly"`: Only speed changes, pitch remains normal
  - `"Both"`: Both pitch and speed scale together with time
  - `"Independent"`: Pitch and speed scale independently based on their respective settings

## Usage Examples

### Example 1: Default Behavior (Audio scales naturally)
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.0
SlomoPreservePitch = false
SlomoAffectsAudioSpeed = false
SlomoAudioSpeedScale = 1.0
SlomoAudioMode = Both
```
This will make audio pitch scale naturally with the time dilation (slower time = lower pitch), classic slomo effect.

### Example 2: Preserve Normal Audio Pitch
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.0
SlomoPreservePitch = true
SlomoAffectsAudioSpeed = false
SlomoAudioSpeedScale = 1.0
SlomoAudioMode = PitchOnly
```
This will keep audio sounding normal during slomo by compensating for the time scale.

### Example 3: Custom Pitch with Time Stretching
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 0.5
SlomoPreservePitch = false
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 1.0
SlomoAudioMode = Both
```
This creates a dramatic slomo effect with both pitch and speed reduced, creating a deep, slow audio experience.

### Example 4: Independent Pitch and Speed Control
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.5
SlomoPreservePitch = false
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 0.3
SlomoAudioMode = Independent
```
This keeps pitch slightly higher while slowing down playback significantly, useful for surreal effects.

### Example 5: Speed Only (Experimental)
```ini
[Audio]
SlomoAffectsAudio = true
SlomoAudioPitchScale = 1.0
SlomoPreservePitch = true
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 1.0
SlomoAudioMode = SpeedOnly
```
This attempts to slow down audio playback while maintaining normal pitch (time-stretching).

### Example 6: Disable Audio Changes
```ini
[Audio]
SlomoAffectsAudio = false
```
This will keep audio completely unchanged during slomo.

## Audio Modes Explained

### PitchOnly Mode
- **Use Case**: When you want classic pitch shifting without time stretching
- **Effect**: Audio pitch changes with time scale, playback speed remains normal
- **Best For**: Simple slomo effects, maintaining audio clarity

### SpeedOnly Mode (Experimental)
- **Use Case**: When you want to slow down audio without changing pitch
- **Effect**: Attempts time-stretching (limited by Unity's capabilities)
- **Best For**: Experimental effects, note that results may vary
- **Note**: Unity doesn't have native time-stretching, so this is simulated

### Both Mode
- **Use Case**: Classic slomo effect where everything slows down together
- **Effect**: Both pitch and speed scale with time
- **Best For**: Cinematic slomo, realistic time dilation

### Independent Mode
- **Use Case**: Advanced control for unique audio effects
- **Effect**: Pitch and speed can be controlled separately
- **Best For**: Creative sound design, custom effects

## Technical Details

### How It Works
The system uses Harmony patches on `AudioSource.pitch` setter to intercept and modify audio behavior:

1. **Pitch Adjustment**: Direct modification of AudioSource.pitch property
2. **Speed Adjustment**: Simulated through sample position manipulation (Unity limitation)
3. **Mode Switching**: Different algorithms applied based on selected mode
4. **State Tracking**: Original audio states are preserved and restored

### Performance Impact
- **Minimal**: Patches only execute when audio properties are being set
- **No overhead**: During normal gameplay when not in slomo
- **Optimized**: Clamping and validation prevent extreme values
- **Safe**: Error handling prevents audio system crashes

### Compatibility
- Works with all Unity AudioSource components in H3VR
- Compatible with modded weapons and sound effects
- Integrates seamlessly with existing slomo system
- Non-destructive: Original audio is preserved

## Configuration Tips

### For Cinematic Effects
```ini
SlomoAudioMode = Both
SlomoAudioPitchScale = 0.3
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 0.3
```
Creates dramatic slow-motion scenes with deep, slow audio.

### For Gameplay Focus
```ini
SlomoAudioMode = PitchOnly
SlomoPreservePitch = true
SlomoAffectsAudioSpeed = false
```
Maintains audio clarity during tactical slomo for competitive play.

### For Realistic Physics
```ini
SlomoAudioMode = Both
SlomoAudioPitchScale = 1.0
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 1.0
```
Audio scales naturally with time, similar to real-world physics.

### For Experimental Effects
```ini
SlomoAudioMode = Independent
SlomoAudioPitchScale = 2.0
SlomoAudioSpeedScale = 0.2
```
Higher pitch with very slow playback for surreal experiences.

## Integration with Other Systems

### Pillow Effects
The audio pitch and speed system automatically works with pillow-triggered slomo effects.

### VR Controller Triggers
Audio adjustments apply to VR controller-triggered slomo as well as keyboard-triggered slomo.

### Movement Integration
Audio modifications work alongside the movement scaling system for comprehensive slomo.

## Advanced Features

### Automatic Cleanup
- Audio state tracking is automatically cleaned up when sources stop playing
- Memory-efficient with automatic dictionary cleanup
- No memory leaks from long play sessions

### Error Handling
- All audio manipulations wrapped in try-catch blocks
- Graceful degradation if audio operations fail
- Debug logging for troubleshooting

### Clamping and Validation
- Pitch values clamped to 0.1 - 3.0 range
- Speed values clamped to 0.1 - 3.0 range
- Prevents audio distortion from extreme values

## Troubleshooting

### Audio Sounds Weird During Slomo
- **Issue**: Distorted or unnatural audio
- **Solution**: Reduce `SlomoAudioPitchScale` or `SlomoAudioSpeedScale` to values between 0.3 and 1.5
- **Try**: `SlomoPreservePitch = true` for normal-sounding audio

### No Audio Changes During Slomo
- **Check**: `SlomoAffectsAudio = true`
- **Verify**: Slomo is actually activating (time scale should be less than 1.0)
- **Mode**: Ensure `SlomoAudioMode` is set to valid value

### Audio Cuts Out or Stutters
- **Cause**: Very low speed or pitch values
- **Solution**: Keep values between 0.3 and 3.0 for best results
- **Try**: Set `SlomoAffectsAudioSpeed = false` if speed adjustment causes issues

### SpeedOnly Mode Not Working Well
- **Note**: This is experimental due to Unity limitations
- **Alternative**: Use `Both` mode with careful tuning
- **Limitation**: Unity doesn't support true pitch-independent time stretching

### Performance Issues
- **Check**: Disable speed adjustment with `SlomoAffectsAudioSpeed = false`
- **Reason**: Speed adjustment requires more processing
- **Solution**: Use `PitchOnly` mode for better performance

## Best Practices

1. **Start Simple**: Begin with default settings and adjust incrementally
2. **Test Modes**: Try different modes to find what sounds best for your play style
3. **Reasonable Values**: Keep scale values between 0.3 and 2.0 for best results
4. **Mode Selection**: Use `PitchOnly` for performance, `Both` for cinematic effects
5. **Preserve Pitch**: Enable for competitive play, disable for cinematic effects

## Configuration File Example

Complete example configuration:

```ini
[Audio]
# Master control
SlomoAffectsAudio = true

# Pitch control
SlomoAudioPitchScale = 0.7
SlomoPreservePitch = false

# Speed control
SlomoAffectsAudioSpeed = true
SlomoAudioSpeedScale = 0.7

# Mode selection
SlomoAudioMode = Both
```

## Development Notes

### For Modders
The audio system exposes the following:
- `GetSlomoAudioConfigComplete()` - Get all audio settings
- Harmony patches on `AudioSource.pitch` and `AudioSource.Stop`
- State tracking dictionary for audio sources

### Extension Points
You can extend the system by:
- Adding new audio modes
- Implementing custom pitch algorithms
- Creating preset configurations
- Adding audio effect chains

## Known Limitations

1. **Unity Audio Engine**: Limited time-stretching capabilities
2. **Speed Adjustment**: Simulated, not true time-stretching
3. **Some Audio Sources**: May not respond to all adjustments
4. **Extreme Values**: May cause audio artifacts

## Support

For issues or feature requests:
1. Check BepInEx console for error messages
2. Verify configuration values are within valid ranges
3. Test with `PitchOnly` mode first
4. Report bugs with full error logs and configuration