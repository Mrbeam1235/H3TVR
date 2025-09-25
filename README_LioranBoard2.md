# LioranBoard 2 Integration for H3TVR

## Overview

This integration allows streamers to use LioranBoard 2 to spawn H3VR sosigs with Twitch usernames from their chat. Viewers can join ally or enemy queues, and sosigs can be spawned with their usernames displayed.

## Features

- ✅ **HTTP API Server**: Listens for LioranBoard 2 commands
- ✅ **Username Queues**: Separate queues for ally and enemy spawning
- ✅ **Keyboard Shortcuts**: F1 (ally) and F2 (enemy) by default
- ✅ **Queue Management**: Add/remove users, check status, clear queues
- ✅ **Compatibility**: Works with existing ChatWatcher system
- ✅ **Error Handling**: Graceful handling of edge cases
- ✅ **Public API**: Methods for external integrations

## Quick Start

1. **Install the mod** and ensure H3TVR is loaded
2. **Start H3VR** - the HTTP server will start automatically on port 8080
3. **Import the example deck** (`LioranBoard2_Example_Deck.json`) into LioranBoard 2
4. **Test connectivity** using the "Queue Status" button
5. **Set up chat commands** as desired (examples provided)

## Configuration

Add these settings to your H3TVR config file:

```ini
[LioranBoard2]
HttpPort = 8080                    # Port for HTTP server
MaxQueueSize = 50                  # Max usernames in each queue
SpawnAllyWithUsernameKey = F1      # Keyboard shortcut for ally spawn
SpawnEnemyWithUsernameKey = F2     # Keyboard shortcut for enemy spawn
EnableIntegration = true           # Enable/disable the integration
LogHttpRequests = false            # Log requests for debugging
```

## API Commands

### Spawn Commands
- `spawn_ally` - Spawn ally sosig with username from queue
- `spawn_enemy` - Spawn enemy sosig with username from queue

### Queue Management
- `add_to_ally_queue` - Add username to ally queue
- `add_to_enemy_queue` - Add username to enemy queue
- `get_queue_status` - Get current queue sizes
- `clear_queues` - Clear all queues

## Example Usage

```json
// Spawn ally with specific username
{
  "command": "spawn_ally",
  "username": "ViewerName"
}

// Add viewer to enemy queue
{
  "command": "add_to_enemy_queue", 
  "username": "EnemyViewer"
}

// Check queue status
{
  "command": "get_queue_status"
}
```

## Chat Commands (LioranBoard 2 Setup)

Suggested chat commands for your stream:

- `!joinally` - Viewer joins ally queue
- `!joinenemy` - Viewer joins enemy queue  
- `!spawnally` - Moderator spawns ally (from queue)
- `!spawnenemy` - Moderator spawns enemy (from queue)
- `!queuestatus` - Check current queue sizes

## Troubleshooting

### Common Issues

**"Connection Failed"**
- Check if port 8080 is available
- Verify `EnableIntegration = true` in config
- Check Windows Firewall settings

**"No Sosigs Spawning"**
- Ensure you're in a compatible H3VR game mode
- Check H3VR console for error messages
- Verify SosigSpawnerManager is active

**"Usernames Not Showing"**
- Ensure ChatWatcher integration is working
- Check if nameplate system is functional
- Look for spawning-related errors in console

### Debug Mode

Enable `LogHttpRequests = true` to see all incoming requests in the H3VR console.

## Public API for Developers

External mods can use these static methods:

```csharp
// Add users to queues
LioranBoard2IntegrationManager.AddUsernameToAllyQueue("username");
LioranBoard2IntegrationManager.AddUsernameToEnemyQueue("username");

// Trigger spawning
LioranBoard2IntegrationManager.TriggerAllySpawn("username");
LioranBoard2IntegrationManager.TriggerEnemySpawn("username");

// Get queue status
var status = LioranBoard2IntegrationManager.GetQueueStatus();
```

## Files

- `LioranBoard2IntegrationManager.cs` - Main integration class
- `LioranBoard2_Integration_Guide.md` - Detailed setup guide
- `LioranBoard2_Example_Deck.json` - Example LioranBoard 2 deck
- `test_lioranboard_api.py` - Python test client

## Requirements

- H3VR with BepInEx
- H3TVR mod installed
- LioranBoard 2 (for deck commands)
- .NET Framework 4.5+ (for HTTP server)

## Compatibility

- Works with existing ChatWatcher system
- Compatible with SosigSpawnerManager
- Supports both file-based and direct username assignment
- Maintains compatibility with existing keybinds

## Support

Check the H3VR console for detailed error messages and debug information. Enable `LogHttpRequests` for additional debugging output.