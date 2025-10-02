# H3TVR Custom Audio Examples

Here are simple examples of how to use your own audio files with the H3TVR AudioManager:

## Example 1: Play a Single Audio File

```csharp
// Get the audio manager
var audioManager = plugin.GetAudioManager(); // Replace 'plugin' with your H3TVR plugin instance

// Play any audio file immediately
audioManager.PlayAudioFile(@"C:\MyMusic\explosion.wav");

// Play a file from the H3TVR_Audio folder
audioManager.PlayAudioFile("my_custom_sound.wav");

// Play with 3D positioning and custom volume
audioManager.PlayAudioFile("explosion.wav", 
    position: transform.position, 
    volume: 1.0f, 
    is3D: true);
```

## Example 2: Load Multiple Files and Play Them

```csharp
// Load a bunch of custom sounds
var myAudioFiles = new Dictionary<string, string>
{
    ["my_explosion"] = @"C:\Audio\big_boom.wav",
    ["my_gunshot"] = @"D:\Sounds\rifle.ogg", 
    ["footsteps"] = "walking.mp3",  // This one is in H3TVR_Audio folder
    ["victory"] = "victory_sound.wav"
};

// Load them all at once
audioManager.LoadAudioFiles(myAudioFiles);

// Now you can play them anytime by name
audioManager.PlayLoadedEffect("my_explosion", explosionPosition);
audioManager.PlayLoadedEffect("footsteps", playerPosition, true, 0.5f);
audioManager.PlayLoadedEffect("victory", Vector3.zero, false); // 2D UI sound
```

## Example 3: Integration with H3TVR Effects

```csharp
// Replace the default shuriken sound with your own
audioManager.PlayAudioFile("my_shuriken_whoosh.wav", 
    position: shurikenPosition,
    effectKey: "custom_shuriken"); // Register it for future use

// Later, you can play your custom shuriken sound
audioManager.PlayLoadedEffect("custom_shuriken", throwPosition);

// Or directly play custom sounds for existing effects
audioManager.PlayShurikenSound("throw", position, customFilePath: "ninja_star.wav");
```

## Example 4: Managing Your Custom Sounds

```csharp
// Check what effects you have loaded
var myEffects = audioManager.GetMyCustomEffects();
foreach(string effect in myEffects)
{
    Debug.Log($"I have loaded: {effect}");
}

// Check if a specific effect is ready to play
if (audioManager.HasEffect("my_explosion"))
{
    audioManager.PlayLoadedEffect("my_explosion", bombPosition);
}

// Remove an effect you don't need anymore
audioManager.RemoveEffect("old_sound");

// Stop all sounds of a specific type
audioManager.StopEffectSounds("my_explosion");
```

## Audio File Placement Options

### Option 1: H3TVR_Audio Folder (Recommended)
Put your files in: `[BepInEx Plugin Folder]/H3TVR_Audio/`

- Example: `BepInEx/plugins/H3TVR/H3TVR_Audio/my_sound.wav`
- Usage: `audioManager.PlayAudioFile("my_sound.wav")`

### Option 2: Anywhere on Your Computer
Use full file paths:

- Example: `C:\MyAudio\explosion.wav`
- Usage: `audioManager.PlayAudioFile(@"C:\MyAudio\explosion.wav")`

## Supported Audio Formats

- **.wav** - Best compatibility (recommended)
- **.ogg** - Good compression
- **.mp3** - Widely supported

## Quick Integration Examples

### For Weapon Sounds
```csharp
// Custom weapon firing sound
audioManager.PlayAudioFile("custom_ak47.wav", gunPosition, true, 0.9f);
```

### For UI Feedback
```csharp
// Button click sound (2D, no positioning)
audioManager.PlayAudioFile("button_click.wav", Vector3.zero, false, 0.3f);
```

### For Environmental Audio
```csharp
// Ambient forest sounds
audioManager.PlayAudioFile("forest_ambient.ogg", environmentPos, true, 0.4f);
```

### For Explosion Effects
```csharp
// Multiple explosion variations
var explosions = new Dictionary<string, string>
{
    ["small_boom"] = "small_explosion.wav",
    ["big_boom"] = "large_explosion.wav", 
    ["nuke"] = "nuclear_blast.wav"
};

audioManager.LoadAudioFiles(explosions);

// Use them in your code
audioManager.PlayLoadedEffect("big_boom", explosionPos, true, 1.0f);
```

That's it! The AudioManager will handle all the loading, caching, volume control, and 3D positioning for you.