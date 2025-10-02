# H3TVR Custom Audio Usage Guide

This guide shows you how to use your own audio files with the H3TVR AudioManager.

## Quick Start - Play Any Audio File

The easiest way to play your own audio file:

```csharp
// Play a file from anywhere on your computer
audioManager.PlayAudioFile(@"C:\MyAudio\explosion.wav");

// Play a file from the H3TVR_Audio folder
audioManager.PlayAudioFile("my_custom_sound.wav");

// Play with custom settings
audioManager.PlayAudioFile("explosion.wav", 
    position: transform.position,    // 3D position
    is3D: true,                     // Use 3D spatial audio
    volume: 1.0f,                   // Volume level
    pitch: 0.8f);                   // Lower pitch = slower/deeper
```

## Loading Multiple Files for Reuse

If you want to load several files once and play them multiple times:

```csharp
// Load multiple files with custom names
var myAudioFiles = new Dictionary<string, string>
{
    ["my_explosion"] = @"C:\MyAudio\big_explosion.wav",
    ["my_gunshot"] = @"D:\Sounds\rifle_shot.ogg",
    ["custom_beep"] = "beep_sound.mp3"  // This one is in H3TVR_Audio folder
};

// Load all files
int loaded = audioManager.LoadAudioFiles(myAudioFiles);
Console.WriteLine($"Loaded {loaded} custom sounds");

// Now you can play them anytime by name
audioManager.PlayLoadedEffect("my_explosion", transform.position);
audioManager.PlayLoadedEffect("my_gunshot", weaponPosition, true, 0.8f);
audioManager.PlayLoadedEffect("custom_beep", Vector3.zero, false); // 2D sound
```

## Integration with H3TVR Effects

You can also register your files to work with existing H3TVR effects:

```csharp
// Load and register for future use
audioManager.PlayAudioFile("my_shuriken_sound.wav", 
    position: Vector3.zero, 
    effectKey: "my_shuriken");

// Later, play the registered effect
audioManager.PlayLoadedEffect("my_shuriken", throwPosition);
```

## File Locations

### Option 1: H3TVR_Audio Folder (Recommended)
Place files in: `[H3TVR Plugin Folder]/H3TVR_Audio/`

```csharp
// These files are in the H3TVR_Audio folder
audioManager.PlayAudioFile("explosion.wav");
audioManager.PlayAudioFile("my_sounds/gunshot.ogg");
```

### Option 2: Absolute Paths
Use full paths to files anywhere on your computer:

```csharp
audioManager.PlayAudioFile(@"C:\Users\YourName\Music\sound.wav");
audioManager.PlayAudioFile(@"D:\GameAudio\Effects\boom.ogg");
```

## Supported Audio Formats

- **.wav** - Best compatibility, recommended
- **.ogg** - Good compression, Unity native
- **.mp3** - Widely supported
- **.aif/.aiff** - High quality, larger files
- **.mod/.it/.s3m/.xm** - Tracker formats

## Utility Methods

```csharp
// Check what custom effects you have loaded
List<string> myEffects = audioManager.GetMyCustomEffects();
foreach(string effect in myEffects)
{
    Console.WriteLine($"I have: {effect}");
}

// Check if a specific effect is loaded
if (audioManager.HasEffect("my_explosion"))
{
    audioManager.PlayLoadedEffect("my_explosion", boomPosition);
}

// Remove an effect you don't need anymore
audioManager.RemoveEffect("old_sound");

// Stop all sounds for a specific effect
audioManager.StopEffectSounds("my_explosion");

// Stop all audio
audioManager.StopAllAudio();
```

## Example Use Cases

### Custom Weapon Sounds
```csharp
// Load custom weapon sounds
var weaponSounds = new Dictionary<string, string>
{
    ["ak47_fire"] = @"C:\WeaponSounds\ak47.wav",
    ["sniper_fire"] = @"C:\WeaponSounds\sniper.wav",
    ["reload_sound"] = "reload.ogg"
};
audioManager.LoadAudioFiles(weaponSounds);

// Use them in your weapon code
audioManager.PlayLoadedEffect("ak47_fire", gunPosition, true, 0.9f);
```

### Environmental Sounds
```csharp
// Play ambient sounds
audioManager.PlayAudioFile("forest_ambient.wav", 
    position: environmentPosition,
    is3D: true,
    volume: 0.3f);
```

### UI Feedback
```csharp
// Play UI sounds (2D, no spatial audio)
audioManager.PlayAudioFile("button_click.wav", 
    position: Vector3.zero,
    is3D: false,
    volume: 0.5f);
```

## Tips

1. **File Size**: Keep audio files under 10MB for best performance
2. **Length**: Most effect sounds should be under 10 seconds
3. **Volume**: Record your files at moderate volumes, H3TVR has volume controls
4. **3D Audio**: Position matters! Use `transform.position` for object-based sounds
5. **Format**: Use .wav files for best compatibility and fastest loading
6. **Organization**: Put files in the H3TVR_Audio folder for easy management

## Configuration

The AudioManager respects all volume settings from the H3TVR configuration:
- Master Volume
- Effects Volume  
- Individual effect volumes

Your custom sounds will automatically use these settings.