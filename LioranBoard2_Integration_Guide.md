# LioranBoard 2 Integration Guide

This guide explains how to set up and use the LioranBoard 2 integration system for H3VR sosig spawning with Twitch usernames.

## Overview

The LioranBoard 2 integration allows you to:
- Spawn ally and enemy sosigs with Twitch usernames from your stream's chat
- Manage username queues for organized spawning
- Use both deck commands and keyboard shortcuts
- Receive JSON responses for successful/failed operations

## Configuration

The integration adds several configuration options in your H3TVR config file:

```ini
[LioranBoard2]
HttpPort = 8080
MaxQueueSize = 50
SpawnAllyWithUsernameKey = F1
SpawnEnemyWithUsernameKey = F2
EnableIntegration = true
LogHttpRequests = false
```

## HTTP API Endpoints

All commands are sent via HTTP POST requests to `http://localhost:8080/` (or your configured port).

### Available Commands

#### 1. Spawn Ally
Spawns an ally sosig with a username from the ally queue or recent chatters.

**Request:**
```json
{
  "command": "spawn_ally",
  "username": "optional_specific_username"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Spawning ally sosig with username: TwitchViewer123",
  "username": "TwitchViewer123",
  "type": "ally"
}
```

#### 2. Spawn Enemy
Spawns an enemy sosig with a username from the enemy queue or recent chatters.

**Request:**
```json
{
  "command": "spawn_enemy",
  "username": "optional_specific_username"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Spawning enemy sosig with username: EnemyViewer456",
  "username": "EnemyViewer456",
  "type": "enemy"
}
```

#### 3. Add to Ally Queue
Adds a username to the ally spawning queue.

**Request:**
```json
{
  "command": "add_to_ally_queue",
  "username": "NewAllyViewer"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Added NewAllyViewer to ally queue",
  "queueSize": 5
}
```

#### 4. Add to Enemy Queue
Adds a username to the enemy spawning queue.

**Request:**
```json
{
  "command": "add_to_enemy_queue",
  "username": "NewEnemyViewer"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Added NewEnemyViewer to enemy queue",
  "queueSize": 3
}
```

#### 5. Get Queue Status
Returns the current status of all queues.

**Request:**
```json
{
  "command": "get_queue_status"
}
```

**Response:**
```json
{
  "success": true,
  "allyQueueSize": 5,
  "enemyQueueSize": 3,
  "recentChattersCount": 15,
  "maxQueueSize": 50
}
```

#### 6. Clear Queues
Clears all username queues.

**Request:**
```json
{
  "command": "clear_queues"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Cleared 5 ally and 3 enemy usernames from queues"
}
```

## LioranBoard 2 Deck Setup

### Basic Spawn Commands

1. **Spawn Ally Button:**
   - Action: HTTP Request
   - Method: POST
   - URL: `http://localhost:8080/`
   - Body: `{"command": "spawn_ally"}`

2. **Spawn Enemy Button:**
   - Action: HTTP Request
   - Method: POST
   - URL: `http://localhost:8080/`
   - Body: `{"command": "spawn_enemy"}`

### Queue Management Commands

3. **Add Viewer to Ally Queue:**
   - Action: HTTP Request
   - Method: POST
   - URL: `http://localhost:8080/`
   - Body: `{"command": "add_to_ally_queue", "username": "ViewerName"}`

4. **Add Viewer to Enemy Queue:**
   - Action: HTTP Request
   - Method: POST
   - URL: `http://localhost:8080/`
   - Body: `{"command": "add_to_enemy_queue", "username": "ViewerName"}`

### Advanced Commands

5. **Queue Status Check:**
   - Action: HTTP Request
   - Method: POST
   - URL: `http://localhost:8080/`
   - Body: `{"command": "get_queue_status"}`

6. **Clear All Queues:**
   - Action: HTTP Request
   - Method: POST
   - URL: `http://localhost:8080/`
   - Body: `{"command": "clear_queues"}`

## Keyboard Shortcuts

- **F1** (default): Spawn ally sosig with username from queue
- **F2** (default): Spawn enemy sosig with username from queue

These can be customized in the configuration file.

## Usage Workflow

### Typical Streaming Setup:

1. **During Stream Setup:**
   - Start H3VR with the mod loaded
   - Verify HTTP server is running (check console logs)
   - Test deck commands to ensure connectivity

2. **During Gameplay:**
   - Use "Add to Ally Queue" when viewers want to be allies
   - Use "Add to Enemy Queue" when viewers want to be enemies
   - Use "Spawn Ally" and "Spawn Enemy" commands during appropriate moments
   - Monitor queue status to manage viewer expectations

3. **Queue Management:**
   - Queues automatically maintain a maximum size (default: 50)
   - Recent chatters are tracked separately (up to 100)
   - If queues are empty, spawning will use random recent chatters

## Error Handling

The system handles several edge cases:

- **Empty Queues:** Falls back to recent chatters
- **No Recent Chatters:** Returns error message
- **Invalid Commands:** Returns descriptive error
- **Server Issues:** Logged to H3VR console

## Troubleshooting

### Common Issues:

1. **Connection Failed:**
   - Check if HTTP port is available (default: 8080)
   - Verify EnableIntegration is set to true
   - Check firewall settings

2. **No Sosigs Spawning:**
   - Ensure SosigSpawnerManager or ChatWatcher is active
   - Check H3VR console for error messages
   - Verify you're in a compatible game mode

3. **Usernames Not Appearing:**
   - Check if ChatWatcher integration is working
   - Verify nameplate system is functional
   - Look for integration-specific error messages

### Debug Settings:

Enable `LogHttpRequests = true` in config to see all incoming requests in the H3VR console.

## Examples

### LioranBoard 2 Command Examples:

```javascript
// Spawn ally with specific username
{
  "command": "spawn_ally",
  "username": "SpecificViewer"
}

// Spawn enemy from queue
{
  "command": "spawn_enemy"
}

// Add multiple viewers to ally queue
{
  "command": "add_to_ally_queue",
  "username": "Viewer1"
}
```

## Testing the Integration

### Using the Test Client

A Python test client is provided at `/tmp/test_lioranboard_api.py`:

```bash
python3 test_lioranboard_api.py
```

This will test all API endpoints and show the expected responses. The client gracefully handles connection failures if the server isn't running.

### Manual Testing with curl

You can also test manually with curl:

```bash
# Test queue status
curl -X POST -H "Content-Type: application/json" \
     -d '{"command":"get_queue_status"}' \
     http://localhost:8080/

# Add user to ally queue  
curl -X POST -H "Content-Type: application/json" \
     -d '{"command":"add_to_ally_queue","username":"TestUser"}' \
     http://localhost:8080/

# Spawn ally sosig
curl -X POST -H "Content-Type: application/json" \
     -d '{"command":"spawn_ally"}' \
     http://localhost:8080/
```

## Integration Architecture

The system consists of several key components:

1. **HTTP Server**: Runs in a separate thread to handle LioranBoard 2 requests
2. **Queue Manager**: Maintains separate queues for ally and enemy usernames  
3. **Spawning Integration**: Interfaces with existing ChatWatcher and SosigSpawnerManager
4. **Username Assignment**: Assigns usernames to spawned sosigs via nameplate system
5. **Public API**: Provides static methods for external mod integration

## Compatibility Notes

- **File-based Integration**: Writes usernames to ChatWatcher's configured file paths for compatibility
- **Direct Integration**: Also sets usernames directly in ChatWatcher.SpawnerName
- **Fallback System**: Falls back to ChatWatcher spawning if SosigSpawnerManager isn't available
- **Error Recovery**: Handles missing components gracefully with informative error messages