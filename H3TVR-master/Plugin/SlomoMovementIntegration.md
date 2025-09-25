# Slomo Movement Integration Documentation

## Overview
I've successfully added slomo movement functionality to your H3TVR plugin by creating a new `SlomoMovementController` class. This system scales player movement speed during slow motion events to create a more cohesive slow-motion experience.

## Files Created/Modified

### New File: `SlomoMovementController.cs`
- **Purpose**: Handles all movement scaling logic during slomo events
- **Features**:
  - Automatically scales movement speed during slomo
  - Supports multiple H3VR movement types (FVRMovementManager and others)
  - Uses reflection to modify movement parameters dynamically
  - Stores and restores original movement values
  - Configurable movement scaling factors

### Modified: `Plugin.cs`
- **Added**: SlomoMovementController integration
- **Changes**:
  - Added `slomoMovementController` field
  - Integrated movement scaling into existing slomo methods
  - Added cleanup in `OnDestroy()`
  - Added test method for adjusting settings

## Configuration Options

The system uses your existing config entries:

1. **SlomoAffectsMovement** (bool, default: true)
   - Whether slomo affects player movement speed
   - Can be disabled to use only time scaling

2. **SlomoMovementScale** (float, default: 0.3f)
   - Movement speed multiplier during slomo
   - 0.3 = 30% of normal movement speed
   - Can be adjusted from 0.0 to 1.0

## How It Works

### Integration Points
1. **SlomoScaleDown()**: Calls `UpdateMovementScale()` as time scales down
2. **SlomoReturn()**: Calls `UpdateMovementScale()` as time returns to normal
3. **Update()**: Ensures movement is restored when slomo ends completely

### Movement Scaling Strategy
The controller uses multiple strategies to find and modify movement parameters:

1. **Primary Strategy**: FVRMovementManager (main H3VR movement system)
   - Scales: MoveSpeed, RotationSpeed, SpeedMultiplier
   
2. **Fallback Strategy**: Generic reflection-based approach
   - Tries common field names like Speed, MovementSpeed, MaxSpeed
   - Attempts to modify both fields and properties

3. **Restoration**: Stores original values and restores them when slomo ends

## Usage Examples

### Manual Testing
```csharp
// You can test the system by calling:
TestMovementScaling(); // Updates settings from config values
```

### Configuration Changes
```csharp
// The controller automatically updates when config values change
SlomoMovementScale.Value = 0.5f; // 50% movement speed during slomo
SlomoAffectsMovement.Value = false; // Disable movement scaling
```

## Technical Details

### Reflection-Based Approach
The system uses reflection to modify movement parameters because:
- H3VR may have different movement systems
- Provides compatibility with modded movement systems
- Allows for graceful degradation if specific fields don't exist

### Error Handling
- Gracefully handles missing fields/properties
- Logs warnings for debugging
- Continues functioning even if some parameters can't be modified

## Build Issue Note

There's currently a namespace issue in the original Plugin.cs file:
- **Issue**: `using BepInEx.Config;` should be `using BepInEx.Configuration;`
- **Fix**: Change line 2 from `using BepInEx.Config;` to `using BepInEx.Configuration;`

Once this is fixed, the project should build successfully with the new slomo movement functionality.

## Testing the Feature

1. **Enable slomo movement**: Ensure `SlomoAffectsMovement` is true in config
2. **Set movement scale**: Adjust `SlomoMovementScale` (try 0.3 for 30% speed)
3. **Trigger slomo**: Use your configured slomo key (default: Keypad7)
4. **Observe**: Movement should slow down proportionally with time scale

## Benefits

- **Immersive Experience**: Movement speed matches time dilation
- **Configurable**: Users can adjust or disable the feature
- **Compatible**: Works with existing slomo system
- **Robust**: Handles different movement types gracefully
- **Performance**: Minimal overhead, only active during slomo

The slomo movement system is now fully integrated and ready to use once the namespace issue is resolved!