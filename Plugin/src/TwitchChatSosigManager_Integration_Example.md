# Twitch Chat Sosig Manager - Integration Examples

## Overview
This document provides examples of how to integrate the Twitch Chat Sosig Manager with various streaming tools and chat systems.

## OBS/Streamlabs Integration

### Using Streamlabs Chatbot
1. **Create Text Files**: Set up Streamlabs Chatbot to write usernames to text files
2. **Configure File Paths**: Point the system to these files
3. **Example Streamlabs Script**:

```python
# Streamlabs Chatbot Script Example
import codecs
import json
import os

def Execute(data):
    if data.IsChatMessage():
        username = data.UserName
        
        # Write to ally file (example)
        ally_file = "C:/H3VRChat/ally_username.txt"
        with codecs.open(ally_file, encoding='utf-8-sig', mode='w') as f:
            f.write(json.dumps({"username": username}))
        
        # Optionally write to enemy file based on conditions
        if ShouldBeEnemy(username):
            enemy_file = "C:/H3VRChat/enemy_username.txt"
            with codecs.open(enemy_file, encoding='utf-8-sig', mode='w') as f:
                f.write(json.dumps({"username": username}))

def ShouldBeEnemy(username):
    # Custom logic to determine enemy assignment
    return username.lower().startswith("enemy_")
```

### Using OBS Scripts
```lua
-- OBS Lua Script Example
obs = obslua

function script_description()
    return "H3VR Twitch Chat Integration"
end

function on_chat_message(username, message)
    -- Write username to file for H3VR pickup
    local file_path = "C:/H3VRChat/ally_username.txt"
    local file = io.open(file_path, "w")
    if file then
        file:write('{"username": "' .. username .. '"}')
        file:close()
    end
end
```

## Twitch Bot Integration

### Using TwitchLib (C#)
```csharp
using TwitchLib.Client;
using TwitchLib.Client.Events;
using System.IO;
using Newtonsoft.Json;

public class H3VRTwitchBot
{
    private TwitchClient client;
    private string allyFilePath = @"C:\H3VRChat\ally_username.txt";
    private string enemyFilePath = @"C:\H3VRChat\enemy_username.txt";
    
    public void Initialize()
    {
        client = new TwitchClient();
        client.OnMessageReceived += OnMessageReceived;
        // Configure and connect...
    }
    
    private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
    {
        var username = e.ChatMessage.Username;
        var message = e.ChatMessage.Message;
        
        // Determine ally or enemy based on message content
        string targetFile = message.ToLower().Contains("enemy") ? 
            enemyFilePath : allyFilePath;
            
        var data = new { username = username };
        File.WriteAllText(targetFile, JsonConvert.SerializeObject(data));
    }
}
```

### Using Python (TwitchIO)
```python
import twitchio
import json
import asyncio
from twitchio.ext import commands

class H3VRBot(commands.Bot):
    def __init__(self):
        super().__init__(token='YOUR_TOKEN', prefix='!', initial_channels=['your_channel'])
        self.ally_file = "C:/H3VRChat/ally_username.txt"
        self.enemy_file = "C:/H3VRChat/enemy_username.txt"
    
    async def event_message(self, message):
        if message.echo:
            return
            
        username = message.author.name
        
        # Write to ally file by default
        with open(self.ally_file, 'w') as f:
            json.dump({"username": username}, f)
        
        # Handle commands for enemy assignment
        if message.content.startswith('!enemy'):
            with open(self.enemy_file, 'w') as f:
                json.dump({"username": username}, f)

bot = H3VRBot()
bot.run()
```

## File Format Examples

### Standard JSON Format
```json
{"username": "StreamerFan123"}
```

### Extended Format (Optional)
```json
{
    "username": "StreamerFan123",
    "displayName": "StreamerFan123",
    "timestamp": "2024-01-15T10:30:00Z",
    "isSubscriber": true,
    "isModerator": false
}
```

## Configuration Examples

### BepInEx Config Example
```ini
[Twitch Chat Sosig]

# File paths (configure these to match your setup)
AllyFilePath = C:\H3VRChat\ally_username.txt
EnemyFilePath = C:\H3VRChat\enemy_username.txt

# Keyboard controls
SpawnAllyKey = F1
SpawnEnemyKey = F2
ToggleModeKey = F3
ShowStatusKey = F4
ClearQueuesKey = F5

# Behavior settings
EnableAutoMode = true
SpawnDistance = 3.0
MaxQueueSize = 50
EnableDebugLogging = true

# Bot filtering
FilterBots = true
BotFilterKeywords = bot,nightbot,streamlabs,moobot,streamelements,fossabot,commanderroot
```

## Advanced Integration Scenarios

### Command-Based Spawning
```python
# Example: Chat commands for specific spawning
async def event_message(self, message):
    content = message.content.lower()
    username = message.author.name
    
    if content.startswith('!spawn ally'):
        write_to_file(self.ally_file, username)
    elif content.startswith('!spawn enemy'):
        write_to_file(self.enemy_file, username)
    elif content.startswith('!spawn boss'):
        write_to_file(self.boss_file, username)  # Custom boss spawning
```

### Subscriber/VIP Priority
```python
def determine_spawn_type(user):
    if user.is_subscriber:
        return "ally"  # Subscribers spawn as allies
    elif user.is_vip:
        return "ally"  # VIPs spawn as allies
    else:
        return "enemy"  # Regular viewers spawn as enemies
```

### Channel Point Rewards Integration
```csharp
// Example for Channel Point Rewards
private void OnChannelPointRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
{
    var username = e.RewardRedeemed.Redemption.User.DisplayName;
    var rewardTitle = e.RewardRedeemed.Redemption.Reward.Title;
    
    string targetFile = rewardTitle.Contains("Enemy") ? 
        enemyFilePath : allyFilePath;
        
    WriteUsernameToFile(targetFile, username);
}
```

## Troubleshooting Integration

### Common File Issues
1. **File Permissions**: Ensure H3VR can read the files
2. **File Encoding**: Use UTF-8 encoding for international characters
3. **File Locking**: Don't keep files open between writes
4. **Path Separators**: Use forward slashes or escape backslashes

### Testing Your Integration
1. **Manual File Creation**: Create test files manually to verify H3VR pickup
2. **Debug Logging**: Enable debug logging to see file monitoring activity
3. **Queue Status**: Use F4 to check if usernames are being queued
4. **File Monitoring**: Watch files with `tail -f` on Linux/Mac or PowerShell on Windows

### Performance Considerations
1. **Write Frequency**: Don't write files too frequently (max 1-2 times per second)
2. **Queue Management**: Use F5 to clear queues if they become too large
3. **Bot Filtering**: Configure bot keywords to reduce noise

## Custom Extensions

### API Integration Example
```csharp
// Example of extending the system programmatically
public class CustomTwitchIntegration : MonoBehaviour
{
    private TwitchChatSosigManager sosigManager;
    
    void Start()
    {
        sosigManager = FindObjectOfType<TwitchChatSosigManager>();
    }
    
    public void OnCustomEvent(string username, bool isAlly)
    {
        if (isAlly)
            sosigManager.AddUsernameToAllyQueue(username);
        else
            sosigManager.AddUsernameToEnemyQueue(username);
    }
}
```

This integration approach ensures maximum compatibility with existing streaming setups while providing the flexibility to customize behavior based on your specific needs.