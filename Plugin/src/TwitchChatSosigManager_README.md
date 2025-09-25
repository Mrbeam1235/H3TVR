# Twitch Chat Sosig Manager

## Overview

The **Twitch Chat Sosig Manager** is a standalone C# mod for H3VR that assigns Twitch usernames to ally and enemy sosigs without requiring external tools like LioranBoard 2. The system is completely self-contained and works entirely within the game.

## Features

### Core Functionality
- **Direct Keyboard Controls**: Simple keyboard shortcuts to spawn ally/enemy sosigs with Twitch usernames
- **Automatic Username Assignment**: Pull usernames from Twitch chat and assign them to spawned sosigs  
- **Smart Queue Management**: Maintain separate queues for allies and enemies with automatic rotation
- **In-Game UI**: Simple on-screen display showing queue status and controls
- **Configurable Hotkeys**: Allow users to customize keyboard shortcuts
- **Chat Integration**: Seamlessly integrate with existing ChatSpawner.cs system

### Key Controls (Default)
- **F1**: Spawn ally sosig with next username from ally queue
- **F2**: Spawn enemy sosig with next username from enemy queue
- **F3**: Toggle between ally/enemy assignment for new chatters
- **F4**: Show current queue status and help
- **F5**: Clear all queues

### Modes
- **Automatic Mode**: New chatters automatically added to current queue (ally/enemy)
- **Manual Mode**: Chatters added to both queues, used randomly
- **Queue Rotation**: Prevents same usernames from being used repeatedly

## Installation

1. Install BepInEx for H3VR if not already installed
2. Copy the H3TVR plugin files to your BepInEx/plugins folder
3. Launch H3VR - the system will initialize automatically
4. Configure file paths for Twitch username monitoring (see Configuration section)

## Configuration

The system integrates with existing ChatWatcher configuration:

### Required Setup
1. **Ally Username File**: Configure the file path where ally usernames are written by your streaming software
2. **Enemy Username File**: Configure the file path where enemy usernames are written by your streaming software

### BepInEx Configuration
The mod adds the following configuration options to your BepInEx config file:

```ini
[Twitch Chat Sosig]

# Keyboard Controls
SpawnAllyKey = F1
SpawnEnemyKey = F2  
ToggleModeKey = F3
ShowStatusKey = F4
ClearQueuesKey = F5

# Behavior Settings
EnableAutoMode = true
SpawnDistance = 3.0
MaxQueueSize = 50
EnableDebugLogging = true

# Username Filtering
FilterBots = true
BotFilterKeywords = bot,nightbot,streamlabs,moobot,streamelements,fossabot
```

## Usage

### Basic Operation
1. **Start the game** - The system initializes automatically
2. **Configure username files** - Set up your streaming software to write usernames to text files
3. **Use keyboard controls** - Press F1/F2 to spawn sosigs with queued usernames
4. **Monitor status** - Press F4 to see queue status and current mode

### Advanced Features

#### Mode Switching
- **Press F3** to toggle between Ally Mode and Enemy Mode
- In **Ally Mode**: New chatters are added to the ally queue
- In **Enemy Mode**: New chatters are added to the enemy queue

#### Queue Management
- **Automatic rotation** prevents the same username from being used repeatedly
- **Smart filtering** removes bot usernames automatically
- **Queue size limits** prevent memory issues with very active chats

#### Integration with ChatSpawner
- Uses existing ChatSpawner.cs logic for reliable sosig creation
- Maintains compatibility with existing sosig templates and configurations
- Preserves all existing sosig behavior and AI

## Technical Details

### Architecture
- **Single MonoBehaviour**: `TwitchChatSosigManager` handles all functionality
- **No External Dependencies**: No HTTP servers, no LioranBoard 2, no network requirements
- **File-based Integration**: Monitors text files written by streaming software
- **Performance Optimized**: Lightweight system that doesn't impact game performance

### Integration Points
- **ChatWatcher.cs**: Uses existing file monitoring and username extraction
- **ChatSpawner.cs**: Leverages existing sosig spawning and configuration
- **SosigSpawnerManager.cs**: Compatible with advanced sosig features

### Username Processing
1. **File Monitoring**: Continuously monitors configured username files
2. **Username Extraction**: Parses JSON-formatted username data
3. **Bot Filtering**: Removes usernames matching bot keywords
4. **Queue Assignment**: Adds valid usernames to appropriate queues
5. **Rotation Logic**: Prevents duplicate usage of same usernames

### Sosig Spawning
1. **Queue Processing**: Retrieves next username from selected queue
2. **ChatSpawner Integration**: Uses existing spawning logic for consistency
3. **Name Assignment**: Assigns username to spawned sosig nameplate
4. **Behavior Configuration**: Applies appropriate AI behavior (ally/enemy)
5. **Tracking**: Maintains lists of active sosigs for cleanup

## Troubleshooting

### Common Issues

#### No Sosigs Spawning
- **Check file paths**: Ensure username files exist and are being written to
- **Verify ChatSpawner**: Make sure ChatSpawner.cs is working independently
- **Check queues**: Press F4 to see if usernames are being added to queues

#### Wrong Username Assignment
- **Mode check**: Press F3 to verify current mode (Ally/Enemy)
- **Bot filtering**: Check if usernames are being filtered as bots
- **File content**: Verify username files contain valid JSON data

#### Performance Issues
- **Queue size**: Reduce MaxQueueSize if experiencing lag
- **Debug logging**: Disable EnableDebugLogging for better performance
- **Clear queues**: Use F5 to clear overflowing queues

### Debug Information
Enable debug logging in the configuration to see detailed information about:
- Username file monitoring
- Queue operations
- Sosig spawning attempts
- Bot filtering results
- Integration status

## API Reference

### Public Methods
```csharp
// Manual queue management
public void AddUsernameToAllyQueue(string username)
public void AddUsernameToEnemyQueue(string username)

// Status queries
public int GetAllyQueueCount()
public int GetEnemyQueueCount()
public bool IsInAllyMode()
public int GetActiveAlliesCount()
public int GetActiveEnemiesCount()
public string GetSystemStatus()
```

### Events and Integration
The system provides hooks for other mods to integrate:
- Queue status monitoring
- Custom username processing
- Sosig spawn event handling

## Changelog

### Version 1.0.0
- Initial release
- Basic queue management
- ChatSpawner integration
- Configurable controls
- Bot filtering
- In-game UI
- Performance optimization

## Support

For issues, feature requests, or contributions, please refer to the main H3TVR repository.

## License

Part of the H3TVR mod package. Please refer to the main project license.