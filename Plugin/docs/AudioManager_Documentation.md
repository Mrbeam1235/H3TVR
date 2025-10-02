# H3TVR Enhanced Edition - AudioManager Documentation

## Overview

The AudioManager is a comprehensive audio system for H3TVR Enhanced Edition that provides immersive sound effects for all the features you requested:

- **Shuriken** - Throwing and impact sounds
- **Hydration** - Drinking and bottle sounds  
- **Slomo** - Time distortion effects
- **Danger Close** - Explosion and artillery sounds
- **Skitty Sub Guns** - Weapon spawn and materialization
- **Destroy Quickbelt** - Item destruction effects
- **Wondertoy** - Magical spawn sounds

## Features

### ?? Complete Audio Integration
- **3D Positional Audio** - Sounds originate from their spawn locations
- **Dynamic Volume Control** - Individual volume settings for each effect type
- **Multiple Audio Files** - Random selection from multiple sound files for variety
- **Spatial Audio Support** - Full 3D audio with distance falloff
- **Performance Optimized** - Limited simultaneous sounds to prevent audio overload

### ?? Easy Configuration
- **BepInEx Config Integration** - All settings available in config files
- **Audio File Management** - Simple drag-and-drop audio file system
- **Format Support** - WAV, OGG, and MP3 audio files
- **Hot Reload** - Audio files can be reloaded without restarting the game

### ?? Effect-Specific Audio

#### Shuriken Sounds
- `PlayShurikenSound("throw")` - Throwing and whoosh effects
- `PlayShurikenSound("spawn")` - Metal clinks and spawn sounds

#### Hydration Sounds  
- `PlayHydrationSound("drink")` - Drinking and pouring effects
- `PlayHydrationSound("spawn")` - Bottle spawning sounds

#### Slomo Effects
- `PlaySlomoSound("start")` - Time slowdown beginning
- `PlaySlomoSound("active")` - Background during slomo
- `PlaySlomoSound("end")` - Time returning to normal

#### Danger Close
- `PlayDangerCloseSound("danger_close")` - Warning sounds
- `PlayDangerCloseSound("explosion")` - Various explosion effects

#### Weapon Spawning
- `PlayWeaponSpawnSound("skitty_sub_gun")` - Small weapon spawn
- `PlayWeaponSpawnSound("gun_spawn")` - Large weapon spawn

#### Destruction Effects
- `PlayDestructionSound("destroy_quickbelt")` - Quickbelt clearing
- `PlayDestructionSound("item_destroy")` - Individual item destruction

#### Wondertoy Magic
- `PlayWondertoySound("spawn")` - Magical appearing sounds
- `PlayWondertoySound("activate")` - Toy activation effects

## Installation and Setup

### 1. Audio Folder
The AudioManager automatically creates an `H3TVR_Audio` folder next to your plugin DLL with:
- Comprehensive README.txt with all audio file names
- Organized categories for each effect type
- Example file naming conventions

### 2. Audio Files
Place custom audio files in the `H3TVR_Audio` folder:

```
H3TVR_Audio/
??? README.txt
??? shuriken_throw.wav
??? hydration_drink.wav  
??? slomo_start.wav
??? danger_close.wav
??? gun_spawn.wav
??? items_destroy.wav
??? wondertoy_spawn.wav
??? ... (see README.txt for complete list)
```

### 3. Configuration
Audio settings are integrated into the main H3TVR config file:

```ini
[Audio]
EnableAudioEffects=true
MasterVolume=1.0
EffectsVolume=0.8
WeaponSoundsVolume=0.9
EnableSpatialAudio=true
MaxAudioDistance=50.0
MaxSimultaneousSounds=10

[Audio.Effects]
ShurikenVolume=0.8
HydrationVolume=0.7
SlomoVolume=0.9
DangerCloseVolume=1.0
SkittySubGunVolume=0.8
DestroyQuickbeltVolume=0.6
WondertoyVolume=0.7
```

## Integration with H3TVR Systems

### Automatic Integration
The AudioManager is automatically integrated with:
- **SpawnManager** - All spawn effects have audio
- **WeaponManager** - Weapon spawning and interactions
- **EffectsManager** - Slomo and special effects
- **InputHandler** - UI feedback sounds

### Manual Usage
You can also call audio effects directly:

```csharp
// Get the audio manager
var audioManager = plugin.GetAudioManager();

// Play specific effects
audioManager.PlayShurikenSound("throw", position, true);
audioManager.PlaySlomoSound("start", Vector3.zero, false);
audioManager.PlayDangerCloseSound("explosion", explosionPos, true);
```

## Audio File Specifications

### Recommended Formats
- **WAV** - Best compatibility and quality
- **OGG** - Good compression and quality
- **MP3** - Universal compatibility

### Audio Guidelines
- **Length**: Keep effects under 10 seconds (UI sounds under 2 seconds)
- **Volume**: Use moderate volume levels (plugin has volume controls)
- **Quality**: 44.1kHz, 16-bit minimum for good quality
- **Channels**: Mono or stereo both supported

### File Naming Convention
Audio files are organized by effect type with descriptive names:

```
shuriken_throw.wav      # Primary shuriken throwing sound
shuriken_whoosh.wav     # Alternative shuriken flight sound
shuriken_impact.wav     # Shuriken hitting target
hydration_drink.wav     # Primary drinking sound
bottle_open.wav         # Bottle opening sound
slomo_start.wav         # Time slowdown effect
explosion_large.wav     # Large explosion
gun_spawn.wav           # Weapon materialization
wondertoy_spawn.wav     # Magical toy appearing
```

## Advanced Features

### 3D Audio System
- **Positional Audio** - Sounds come from their 3D location
- **Distance Falloff** - Volume decreases with distance
- **Spatial Blend** - Mix between 2D and 3D audio
- **Doppler Effect** - Subtle doppler for moving objects

### Performance Management
- **Source Limiting** - Maximum simultaneous sounds to prevent lag
- **Automatic Cleanup** - Finished audio sources are cleaned up
- **Memory Management** - Audio clips are properly managed
- **Frame Rate Optimization** - Audio processing spread across frames

### Error Handling
- **Graceful Degradation** - Missing audio files don't break functionality
- **Logging** - Comprehensive logging for troubleshooting
- **Fallback System** - Default sounds when custom files aren't found
- **Exception Safety** - Audio errors don't crash the plugin

## API Reference

### Main Methods

```csharp
// Initialization
void Initialize(H3TVRImproved plugin, ManualLogSource logger)

// Effect Audio
void PlayShurikenSound(string action, Vector3 position, bool is3D)
void PlayHydrationSound(string action, Vector3 position, bool is3D)  
void PlaySlomoSound(string phase, Vector3 position, bool is3D)
void PlayDangerCloseSound(string type, Vector3 position, bool is3D)
void PlayWeaponSpawnSound(string type, Vector3 position, bool is3D)
void PlayDestructionSound(string type, Vector3 position, bool is3D)
void PlayWondertoySound(string action, Vector3 position, bool is3D)
void PlayUISound(string type, Vector3 position)

// Control Methods
void StopAllSounds()
void StopEffectSounds(string effectKey)
void ReloadAudioClips()
string GetAudioStatus()
```

### Configuration Properties
All audio settings are exposed through the BepInEx configuration system and can be modified in real-time.

## Troubleshooting

### No Audio Playing
1. Check `EnableAudioEffects=true` in config
2. Verify `MasterVolume` > 0
3. Check Unity Audio Listener is present
4. Look for audio loading errors in logs

### Audio Files Not Loading
1. Verify file formats (WAV, OGG, MP3)
2. Check file names match expected names (see README.txt)
3. Ensure files are in the `H3TVR_Audio` folder
4. Check file permissions and encoding

### Performance Issues
1. Reduce `MaxSimultaneousSounds` setting
2. Use shorter audio files
3. Reduce audio quality/bitrate
4. Disable 3D audio if needed

### 3D Audio Issues
1. Verify `EnableSpatialAudio=true`
2. Check `Enable3DAudio` setting
3. Ensure proper position coordinates
4. Verify Audio Listener exists in scene

## Examples

### Basic Usage
```csharp
// Play shuriken throw sound at spawn position
Vector3 shurikenPos = GM.CurrentPlayerBody.Head.position;
audioManager.PlayShurikenSound("throw", shurikenPos, true);

// Play slomo start effect (2D audio for UI feedback)
audioManager.PlaySlomoSound("start", Vector3.zero, false);

// Play explosion at specific location
audioManager.PlayDangerCloseSound("explosion", explosionPosition, true);
```

### Advanced Configuration
```csharp
// Get current audio system status
string status = audioManager.GetAudioStatus();
Debug.Log(status);

// Stop all danger close sounds
audioManager.StopEffectSounds("danger_close");

// Reload audio files after adding new ones
audioManager.ReloadAudioClips();
```

## Integration with Other Mods

The AudioManager is designed to work alongside other audio mods:
- **Non-Conflicting** - Uses separate audio sources
- **Configurable** - All effects can be disabled if needed
- **Performance Aware** - Limited resource usage
- **API Available** - Other mods can use the AudioManager API

---

*The AudioManager provides a complete audio experience for H3TVR Enhanced Edition, bringing all your requested effects to life with immersive, high-quality sound design.*