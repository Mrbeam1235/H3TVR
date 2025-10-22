# Jedit Tippy Toy Integration Guide

## Overview
H3TVR Enhanced Edition now includes full integration with the **Jedit Tippy Toy** mod by PutterMyBancakes. This allows you to spawn the iconic Jedi lightsaber-style tippy toy directly from your head using a simple keybind.

## Installation

### Step 1: Install the Jedit Tippy Toy Mod
1. Download the mod from Thunderstore:
   - **Link**: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/
2. Install using r2modman or Thunderstore Mod Manager (recommended)
3. Alternatively, manually extract to your H3VR BepInEx plugins folder

### Step 2: Verify H3TVR Enhanced Edition
Make sure you have H3TVR Enhanced Edition installed and configured.

### Step 3: Configure Keybind (Optional)
The Jedit Tippy Toy spawn is mapped by default. Check your H3TVR config file:
```
[KeyBindings]
SpawnJeditToy = K
```

## How It Works

### Automatic Detection
When H3TVR Enhanced Edition starts, it automatically:
1. Detects if Jedit Tippy Toy mod is installed
2. Validates the mod is properly loaded
3. Confirms the `TippyToy_Set2` item is available in the game's ItemManager
4. Logs the detection status in the BepInEx console

### Spawning the Jedit Tippy Toy
Press your configured keybind (default: `K`) to spawn the Jedit Tippy Toy:
- Spawns directly above your head
- Applies forward momentum and spin
- Plays custom audio effects (if configured)
- Logs successful spawn to console

### Validation & Error Handling
The system includes robust error handling:
- **Mod Not Installed**: Shows clear error message with download link
- **Mod Not Loaded**: Indicates the mod may not be properly installed
- **Item Not Found**: Reports if `TippyToy_Set2` is missing from ItemManager

## Technical Details

### Integration Architecture
The Jedit Tippy Toy integration uses H3TVR's **OptionalDependencyManager** system:

```csharp
// Detection (automatic on startup)
OptionalDependencyManager.IsJeditTippyToyAvailable

// Validation (before spawning)
OptionalDependencyManager.ValidateJeditTippyToy()

// Get Object ID
OptionalDependencyManager.GetJeditToyObjectID() // Returns "TippyToy_Set2"

// Check if spawnable
OptionalDependencyManager.IsJeditToySpawnable()
```

### Spawn Method
Located in `SpawnManager.cs`:
```csharp
public void SpawnJeditToy()
{
    // 1. Validate Jedit Tippy Toy mod is available
    // 2. Get correct object ID ("TippyToy_Set2")
    // 3. Spawn from head position with physics
    // 4. Play audio effects
    // 5. Log success/failure
}
```

### Object ID
- **Primary ID**: `TippyToy_Set2`
- This is the correct identifier used by the Jedit Tippy Toy mod
- Validated through OptionalDependencyManager

## Audio Integration

### Custom Sound Effects
You can configure custom audio for Jedit Tippy Toy spawning:

**Before Spawn (Ignite Sound)**:
```ini
[Audio.Wondertoy]
before_activate = wondertoy/jedi_ignite.wav
```

**After Spawn (Ready Sound)**:
```ini
[Audio.Wondertoy]
after_activate = wondertoy/jedi_ready.wav
```

### Example Audio Files
Create these files in your H3TVR audio folder:
- `wondertoy/jedi_ignite.wav` - Lightsaber ignition sound
- `wondertoy/jedi_ready.wav` - Ready/hum sound

## Troubleshooting

### Mod Not Detected
**Error**: "Jedit Tippy Toy mod not detected!"

**Solutions**:
1. Verify the mod is installed via r2modman
2. Check BepInEx console for mod loading errors
3. Ensure the mod GUID matches: `PutterMyBancakes.Jeditippytoy`
4. Restart the game after installation

### Item Not Found
**Error**: "Jedit Tippy Toy ID 'TippyToy_Set2' not found in ObjectDictionary!"

**Solutions**:
1. Verify the Jedit Tippy Toy mod is properly loaded
2. Check if other custom items are loading correctly
3. Try reinstalling the Jedit Tippy Toy mod
4. Check BepInEx logs for ItemManager errors

### Validation Failed
**Error**: "Jedit Tippy Toy validation failed!"

**Solutions**:
1. The mod is detected but the item isn't in ItemManager
2. This may indicate a partial/corrupted installation
3. Reinstall the Jedit Tippy Toy mod
4. Verify no conflicting mods are present

## Debug Information

### Console Output (Successful)
When working correctly, you'll see:
```
[Info   : H3TVR] [OptionalDependencies] Jedit Tippy Toy detected via ItemManager (TippyToy_Set2 found)
[Info   : H3TVR] [OptionalDependencies] Jedit Tippy Toy validated and ready
[Info   : H3TVR] Successfully spawned Jedit Tippy Toy (ID: TippyToy_Set2)
```

### Console Output (Not Installed)
If the mod isn't installed:
```
[Error  : H3TVR] Jedit Tippy Toy mod not detected!
[Error  : H3TVR] Install from: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/
```

## Advanced Configuration

### Multiple Jedit Toys
You can spawn multiple Jedit Tippy Toys by pressing the keybind multiple times. Each spawn is independent with its own physics.

### Custom Spawn Position
The spawn position is calculated as:
```csharp
Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
```
- Spawns 0.25 units above your head
- Inherits your head rotation
- Gets forward momentum from your view direction

### Physics Properties
Spawned Jedit Tippy Toys receive:
- **Torque**: `Vector3(0.25f, 0.25f, 0.25f)` - Slight spin
- **Force**: `GM.CurrentPlayerBody.Head.forward * 25` - Forward momentum

## Integration Benefits

### Why Use OptionalDependencyManager?
1. **Automatic Detection**: No manual configuration needed
2. **Graceful Degradation**: Works without the mod installed (just won't spawn)
3. **Clear Error Messages**: Users know exactly what to install
4. **Centralized Management**: All optional mods managed in one place
5. **Easy Updates**: Changes to object IDs handled in one location

### Future Compatibility
The OptionalDependencyManager system makes it easy to:
- Support updates to Jedit Tippy Toy mod
- Add support for other tippy toy variants
- Integrate with future weapon/toy mods

## Related Systems

### Other Optional Dependencies
H3TVR Enhanced Edition also integrates with:
- **Magazine Patcher**: Enhanced magazine compatibility
- **Meatyceiver 2**: Weapon transformations
- **Stovepipe**: Realistic weapon malfunctions

See `docs/Optional_Dependencies_Integration.md` for details.

### Input Handler
The Jedit Tippy Toy spawn is managed through the centralized InputHandler system. See `InputHandler.cs` for keybind configuration.

## Credits
- **Jedit Tippy Toy Mod**: PutterMyBancakes
- **Mod Link**: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/
- **H3TVR Integration**: Enhanced Optional Dependency System

## Support
If you encounter issues with Jedit Tippy Toy integration:
1. Check the BepInEx console for error messages
2. Verify the mod is installed correctly via r2modman
3. Test spawning the regular WonderToy to verify H3TVR is working
4. Report issues with full console logs

---

**May the Force be with you!** ??
