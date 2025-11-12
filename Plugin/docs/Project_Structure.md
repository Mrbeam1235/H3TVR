# H3TVR Project Structure

This document outlines the organized folder structure of the H3TVR plugin project.

## Directory Structure

```
Plugin/
??? src/
??? Core/
    ?   ??? H3TVRImproved.cs- Main plugin entry point
    ? ??? PluginInfo.cs        - Plugin metadata
  ?
    ??? Managers/
    ?   ??? AudioManager_Simplified.cs - Audio system management
    ?   ??? EffectsManager.cs - Visual/audio effects
    ?   ??? SpawnManager.cs      - Item spawn management
    ?   ??? WeaponManager.cs      - Weapon spawning & manipulation
    ?
    ??? Integration/
    ?   ??? MeatyceiverIntegrationManager.cs      - Meatyceiver mod integration
    ?   ??? SosigArmorWristMenuIntegration.cs     - Wrist menu integration
    ?   ??? SteamFriendsIntegration.cs  - Steam friends features
    ?   ??? StovepipeIntegrationManager.cs        - Stovepipe mod integration
    ?   ??? TNHCustomizerIntegration.cs        - Take & Hold customizer
    ?
  ??? Sosig/
    ?   ??? AdvancedChatSosigSpawner.cs   - Chat-based sosig spawning
    ?   ??? AdvancedSosigAI.cs  - Enhanced sosig AI behavior
    ?   ??? BossSosigSystem.cs      - Boss sosig mechanics
    ?   ??? SosigArmorWristMenuComplete.cs - Sosig armor UI system
  ?   ??? SosigWeaponEnhancer.cs         - Sosig weapon enhancements
    ?
    ??? Chat/
  ?   ??? ChatWatcher.cs               - File-based chat monitoring
    ?
    ??? ChatSpawner/
    ?   ??? SosigBehaviorController.cs    - Sosig behavior logic
    ?   ??? SosigNameManager.cs       - Name generation & tracking
    ???? SosigNameplateManager.cs    - Nameplate display system
    ?   ??? SosigSpawnConfig.cs      - Spawn configuration
    ?   ??? SosigSpawner.cs      - Core spawning logic
    ?   ??? SosigSpawnPositionCalculator.cs   - Position calculation
    ?   ??? SosigTemplateCache.cs- Template caching
    ?   ??? SpawnPriority.cs - Spawn priority enum
    ?
    ??? Utilities/
        ??? H3VRDelayedInitializer.cs - Delayed initialization helper
     ??? Hooks.cs      - Game hooks
        ??? InputHandler.cs   - Input processing
      ??? OptionalDependencyManager.cs   - Dependency management
   ??? SlomoMovementController.cs     - Slow-motion movement
```

## Folder Purposes

### Core/
Contains the main plugin entry point and core initialization code. These files are the foundation of the plugin.

### Managers/
System managers that handle specific aspects of functionality:
- **AudioManager_Simplified**: Centralized audio playback and management
- **EffectsManager**: Visual effects, particle systems, and audio effects coordination
- **SpawnManager**: General item spawning and lifecycle management
- **WeaponManager**: Weapon-specific spawning, randomization, and manipulation

### Integration/
Integration with other mods and external systems:
- **MeatyceiverIntegrationManager**: Meatyceiver mod support
- **SosigArmorWristMenuIntegration**: Integration with wrist menu system
- **SteamFriendsIntegration**: Steam friends list features
- **StovepipeIntegrationManager**: Stovepipe mod compatibility
- **TNHCustomizerIntegration**: Take & Hold Customizer integration

### Sosig/
All sosig (AI character) related functionality:
- **AdvancedChatSosigSpawner**: Main spawner for chat-triggered sosigs
- **AdvancedSosigAI**: Enhanced AI behaviors and decision making
- **BossSosigSystem**: Special boss sosig mechanics
- **SosigArmorWristMenuComplete**: Full sosig armor UI system
- **SosigWeaponEnhancer**: Weapon enhancements for sosigs

### Chat/
Chat integration and monitoring:
- **ChatWatcher**: File-based chat monitoring for Twitch Channel Points and streaming software

### ChatSpawner/
Modular chat sosig spawning system components:
- **SosigBehaviorController**: Controls sosig behavior states
- **SosigNameManager**: Manages username tracking and display
- **SosigNameplateManager**: Handles nameplate rendering
- **SosigSpawnConfig**: Configuration for spawn parameters
- **SosigSpawner**: Core spawning implementation
- **SosigSpawnPositionCalculator**: Safe spawn position calculation
- **SosigTemplateCache**: Performance optimization for templates
- **SpawnPriority**: Priority levels for spawn requests

### Utilities/
Utility classes and helpers:
- **H3VRDelayedInitializer**: Handles delayed initialization after H3VR loads
- **Hooks**: Game event hooks
- **InputHandler**: Keyboard and controller input processing
- **OptionalDependencyManager**: Manages optional mod dependencies
- **SlomoMovementController**: Slow-motion movement mechanics

## Benefits of This Structure

1. **Better Organization**: Related files are grouped together
2. **Easier Navigation**: Find files by their purpose, not alphabetically
3. **Clearer Dependencies**: See which systems depend on others
4. **Maintainability**: Easier to understand and modify code
5. **Scalability**: Easy to add new features in appropriate folders

## All Files Use Single Namespace

All C# files use the `H3TVR` namespace, regardless of folder location. This maintains backward compatibility and simplifies references.

## Build Verification

? Project builds successfully after reorganization
? All namespace references intact
? No breaking changes to existing functionality
