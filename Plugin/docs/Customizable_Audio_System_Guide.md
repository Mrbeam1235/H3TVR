# H3TVR Enhanced Edition - Customizable Audio System Guide

## Overview

The H3TVR Enhanced Edition audio system is **fully customizable**. Every single sound effect can be replaced with your own audio files through simple configuration. No code changes needed!

## Features

? **Every Effect Customizable** - All 30+ sound effects can use custom audio  
? **Multiple File Support** - Specify multiple files per effect for variety  
? **Random Selection** - Plugin randomly picks from your files  
? **Hot-Reload Ready** - Changes take effect on game restart  
? **Flexible Paths** - Use relative or absolute file paths  
? **Multiple Formats** - Supports WAV, OGG, MP3, AIFF  
? **No Coding Required** - Pure configuration-based customization  

## Quick Start

### 1. Find Your Audio Folder

The audio folder is located at:
```
[H3VR Install]/BepInEx/plugins/H3TVR_Audio/
```

This folder is created automatically when the mod runs for the first time.

### 2. Add Your Audio Files

Place your custom audio files in the `H3TVR_Audio` folder. You can organize them in subfolders if you want:

```
H3TVR_Audio/
??? my_explosion.wav
??? explosions/
?   ??? boom1.wav
?   ??? boom2.wav
?   ??? boom3.wav
??? weapons/
?   ??? gun_spawn.wav
??? custom_sounds/
    ??? slomo_effect.wav
```

### 3. Configure Your Audio

Edit the configuration file at:
```
[H3VR Install]/BepInEx/config/H3TVR_AudioConfig.ini
```

Find the effect you want to customize and update the file path:

```ini
# Single custom file
ExplosionSounds=my_explosion.wav

# Multiple files for variety (plugin picks randomly)
ExplosionSounds=explosions/boom1.wav,explosions/boom2.wav,explosions/boom3.wav

# Use files from a subfolder
SlomoStartSounds=custom_sounds/slomo_effect.wav
```

### 4. Restart H3VR

Changes take effect when you restart the game!

## Supported Audio Formats

- **.wav** - Recommended, best compatibility
- **.ogg** - Good for smaller file sizes
- **.mp3** - Common format, works well
- **.aif / .aiff** - Apple audio format
- **.mod / .it / .s3m / .xm** - Tracker formats (advanced)

## All Customizable Effects

### Combat & Action Sounds

| Effect | Config Key | Default Files | When It Plays |
|--------|-----------|---------------|---------------|
| **Shuriken Throw** | `ShurikenThrowSounds` | shuriken_throw.wav, shuriken_whoosh.wav | When throwing shuriken |
| **Shuriken Spawn** | `ShurikenSpawnSounds` | shuriken_spawn.wav | When shuriken appears |
| **Explosion** | `ExplosionSounds` | explosion_large.wav, explosion_medium.wav | During explosions |
| **Danger Close** | `DangerCloseSounds` | danger_close.wav | Danger close warning |

### Time Manipulation Sounds

| Effect | Config Key | Default Files | When It Plays |
|--------|-----------|---------------|---------------|
| **Slomo Start** | `SlomoStartSounds` | slomo_start.wav, time_slow.wav | When slow-mo begins |
| **Slomo End** | `SlomoEndSounds` | slomo_end.wav, time_normal.wav | When slow-mo ends |
| **Slomo Active** | `SlomoActiveSounds` | slomo_ambient.wav | During slow-mo |

### Item & Spawning Sounds

| Effect | Config Key | Default Files | When It Plays |
|--------|-----------|---------------|---------------|
| **Hydration Drink** | `HydrationDrinkSounds` | hydration_drink.wav, water_pour.wav | Drinking water |
| **Hydration Spawn** | `HydrationSpawnSounds` | bottle_spawn.wav | Water bottle spawns |
| **Weapon Spawn** | `SkittySubGunSounds` | gun_spawn.wav, weapon_materialize.wav | Skitty Sub Gun spawns |
| **Generic Gun** | `GenericGunSpawnSounds` | gun_appear.wav | Generic weapon spawn |
| **Wondertoy Spawn** | `WondertoySpawnSounds` | wondertoy_spawn.wav, toy_appear.wav | Wondertoy appears |
| **Wondertoy Activate** | `WondertoyActivateSounds` | toy_activate.wav | Wondertoy activation |

### Destruction Sounds

| Effect | Config Key | Default Files | When It Plays |
|--------|-----------|---------------|---------------|
| **Destroy Quickbelt** | `DestroyQuickbeltSounds` | items_destroy.wav, quickbelt_clear.wav | Clearing quickbelt |
| **Item Destroy** | `ItemDestroySounds` | item_vanish.wav | Single item destroyed |

### UI & System Sounds

| Effect | Config Key | Default Files | When It Plays |
|--------|-----------|---------------|---------------|
| **UI Confirm** | `UIConfirmSounds` | ui_confirm.wav, beep_confirm.wav | Confirming action |
| **UI Error** | `UIErrorSounds` | ui_error.wav | Error/invalid action |
| **System Ready** | `SystemReadySounds` | system_ready.wav | System startup |

### Weapon Malfunction Sounds (Stovepipe Integration)

| Effect | Config Key | Default Files | When It Plays |
|--------|-----------|---------------|---------------|
| **Weapon Jam** | `StovepipeJamSounds` | weapon_jam.wav, stovepipe_jam.wav | General weapon jam |
| **Double Feed** | `StovepipeDoubleFeedSounds` | double_feed.wav | Double feed malfunction |
| **Failure to Feed** | `StovepipeFailureToFeedSounds` | failure_to_feed.wav | Ammo feeding fails |
| **Failure to Eject** | `StovepipeFailureToEjectSounds` | failure_to_eject.wav | Casing fails to eject |
| **Failure to Fire** | `StovepipeFailureToFireSounds` | failure_to_fire.wav | Weapon fails to fire |
| **Hang Fire** | `StovepipeHangFireSounds` | hang_fire.wav | Delayed ignition |
| **Clear Jam** | `StovepipeClearJamSounds` | jam_cleared.wav | Jam cleared |
| **Cycling** | `StovepipeCyclingSounds` | action_cycling.wav | Weapon cycling |
| **Generic Malfunction** | `StovepipeGenericMalfunctionSounds` | generic_malfunction.wav | Any malfunction |

## Advanced Configuration Examples

### Example 1: Single Custom Explosion

Replace all explosion sounds with your custom file:

```ini
ExplosionSounds=MyBigBoom.wav
```

### Example 2: Multiple Explosions for Variety

Have the plugin randomly select from 5 different explosion sounds:

```ini
ExplosionSounds=boom1.wav,boom2.wav,boom3.wav,boom4.wav,boom5.wav
```

### Example 3: Organized in Subfolders

Keep your audio organized in themed folders:

```ini
# Explosion category
ExplosionSounds=explosions/large.wav,explosions/medium.wav,explosions/small.wav

# Weapon sounds category
SkittySubGunSounds=weapons/spawn.wav,weapons/materialize.wav

# Time effects category
SlomoStartSounds=time_effects/slow_down.wav
SlomoEndSounds=time_effects/speed_up.wav
```

### Example 4: Absolute Paths

Use files from anywhere on your computer:

```ini
ExplosionSounds=C:\MyAudioLibrary\Explosions\nuclear_blast.wav
SlomoStartSounds=D:\SoundEffects\TimeWarp\slowmo.wav
```

### Example 5: Mix and Match

Combine relative and absolute paths, single and multiple files:

```ini
# Local file
ExplosionSounds=my_explosion.wav

# Multiple local files
DangerCloseSounds=dc1.wav,dc2.wav,dc3.wav

# Files in subfolder
SlomoStartSounds=effects/slomo_start.wav,effects/time_slow.wav

# Absolute path
SystemReadySounds=C:\CoolSounds\startup.wav
```

## Audio File Guidelines

### Recommended Specifications

- **Format**: WAV (highest compatibility)
- **Sample Rate**: 44100 Hz or 48000 Hz
- **Bit Depth**: 16-bit or 24-bit
- **Duration**: 
  - Impact sounds: 0.5-2 seconds
  - Ambient sounds: 2-10 seconds
  - UI sounds: 0.1-0.5 seconds
  - Malfunction sounds: 0.5-3 seconds

### 3D Audio Considerations

For effects that use 3D positional audio (explosions, weapon spawns, etc.):
- **Mono files** work best for 3D spatial audio
- **Stereo files** work but may sound less accurate positionally

For UI and non-positional sounds:
- **Stereo files** work perfectly
- Slomo, UI, system sounds use 2D audio

### Volume Levels

Keep your source audio at moderate levels:
- **Peak**: -6 dB to -3 dB
- **Average**: -12 dB to -9 dB
- The plugin has volume controls, so don't normalize to 0 dB

### File Sizes

Recommendations for performance:
- **Short effects** (< 2 sec): Keep under 1 MB
- **Medium effects** (2-5 sec): Keep under 3 MB
- **Long ambient** (5-10 sec): Keep under 5 MB

## Troubleshooting

### Sound Not Playing?

1. **Check file exists**: Verify the audio file is in the correct location
2. **Check file name**: Ensure spelling matches exactly (case-sensitive on some systems)
3. **Check format**: Make sure it's a supported format (WAV, OGG, MP3)
4. **Check config**: Verify the config entry is correct
5. **Check logs**: Look at BepInEx logs for loading errors

### Multiple Files Not Working?

1. **Check separators**: Use commas (`,`) between file names
2. **Check spacing**: Remove spaces around commas: `file1.wav,file2.wav` not `file1.wav , file2.wav`
3. **Check each file**: Make sure all listed files exist

### Volume Too Quiet/Loud?

Adjust in the config:
```ini
[Volume Levels]
# Master control
MasterVolume=1.0

# Category volumes
EffectsVolume=0.8
WeaponSoundsVolume=0.9
AmbientSoundsVolume=0.6

# Individual effect volumes
ExplosionVolume=1.0
ShurikenVolume=0.7
```

## Configuration File Reference

### Location
```
[H3VR Install]/BepInEx/config/H3TVR_AudioConfig.ini
```

### Structure
```ini
[General]
# Overall audio settings
EnableAudioEffects=true
MasterVolume=1.0
EnableSpatialAudio=true
MaxAudioDistance=50.0
MaxSimultaneousSounds=10

[Volume Levels]
# Volume controls for categories and individual effects
EffectsVolume=0.8
WeaponSoundsVolume=0.9
# ... individual volumes ...

[Custom Audio Files]
# File paths for every sound effect
ShurikenThrowSounds=shuriken_throw.wav,shuriken_whoosh.wav
ExplosionSounds=explosion_large.wav,explosion_medium.wav
# ... all other effects ...
```

## Creating Custom Audio Packs

Want to share your custom audio? Create an audio pack!

### 1. Organize Your Files

Create a folder structure:
```
MyH3TVRAudioPack/
??? H3TVR_Audio/
?   ??? explosions/
?   ??? weapons/
?   ??? effects/
?   ??? ui/
??? config/
    ??? H3TVR_AudioConfig.ini
```

### 2. Configure the Paths

Update `H3TVR_AudioConfig.ini` with your file paths:
```ini
[Custom Audio Files]
ExplosionSounds=explosions/boom1.wav,explosions/boom2.wav
SkittySubGunSounds=weapons/gun_spawn.wav
SlomoStartSounds=effects/slowmo.wav
```

### 3. Package and Share

1. Zip up your folder
2. Include installation instructions:
   - Extract `H3TVR_Audio` folder to `BepInEx/plugins/`
   - Extract `config` folder to `BepInEx/`
   - Overwrite when prompted (or merge configs)

### 4. Document Your Pack

Include a README with:
- Theme/style of your audio pack
- Number of custom sounds
- Audio sources/credits
- Installation instructions
- Screenshots/videos

## Examples of Audio Pack Themes

### Sci-Fi Pack
- Laser weapon spawns
- Energy explosions
- Digital UI beeps
- Futuristic time effects

### Horror Pack
- Eerie ambient sounds
- Disturbing malfunction sounds
- Creepy UI feedback
- Unsettling time distortions

### Comedy Pack
- Cartoon sound effects
- Silly weapon spawns
- Funny explosions
- Humorous UI sounds

### Realistic Military Pack
- Real gunfire recordings
- Authentic explosions
- Radio chatter
- Military sound effects

### Retro Gaming Pack
- 8-bit/16-bit sounds
- Classic game sound effects
- Arcade style audio
- Nostalgic UI beeps

## API Integration

Modders can programmatically use the audio system:

```csharp
// Get the audio manager
AudioManager audioManager = plugin.GetAudioManager();

// Play a loaded effect
audioManager.PlayLoadedEffect("explosion", transform.position, true, 0.8f, 1.0f);

// Load custom audio at runtime
audioManager.LoadCustomAudioFile("path/to/file.wav", "my_effect", true);

// Play the custom effect
audioManager.PlayLoadedEffect("my_effect", position);

// Check if effect exists
if (audioManager.HasEffect("explosion"))
{
    audioManager.PlayLoadedEffect("explosion", position);
}

// Get list of custom effects
List<string> effects = audioManager.GetMyCustomEffects();
```

## Performance Notes

- Audio files are loaded at startup
- Memory usage depends on file count and sizes
- Recommended: Keep total audio files under 100 MB
- Use compressed formats (OGG, MP3) for longer sounds
- Use WAV for short, frequently played sounds

## Future Enhancements

Planned features:
- In-game audio browser/tester
- Dynamic volume adjustment during gameplay
- Audio pack manager UI
- One-click audio pack installation
- Community audio pack repository
- Real-time audio replacement (no restart needed)

## Support & Community

- Report issues on GitHub
- Share your audio packs in the community
- Request new customizable effects
- Suggest improvements

## Credits

H3TVR Enhanced Edition Audio System by the H3TVR development team.

Special thanks to:
- Unity for the audio engine
- BepInEx for the configuration system
- The H3VR modding community for feedback and ideas

---

**Enjoy your fully customized H3TVR audio experience!** ????
