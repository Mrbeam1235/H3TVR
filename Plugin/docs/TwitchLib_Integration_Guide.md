# H3TVR TwitchLib Integration Guide

## Overview
H3TVR Enhanced Edition now includes **real-time Twitch chat integration** using TwitchLib, replacing the old file-based system. This provides immediate response to chat commands, better user tracking, and enhanced streamer interaction.

## Quick Setup

### 1. **Initial Configuration**
- Launch H3VR with H3TVR Enhanced Edition installed
- Press **F8** to open the Twitch Integration GUI
- The system will automatically detect that TwitchLib is available

### 2. **Twitch Authentication (One-Time Setup)**
1. **Get OAuth Token:**
   - In the Twitch GUI, click "Get Token" button
   - This opens https://twitchapps.com/tmi/ in your browser
   - Click "Connect" and authorize the application
   - Copy the OAuth token (includes "oauth:" prefix)

2. **Configure Authentication:**
   - Enter your **Twitch username** in the GUI
   - Paste the **OAuth token** you copied
   - Enter the **channel name** to monitor (usually your own channel)
   - Click "Save & Login"

3. **Connect:**
   - Click "Connect" to establish real-time chat connection
   - Status indicator will turn green when connected
   - Test with chat commands like `!ally` or `!enemy`

## Chat Commands

### **Basic Commands**
- `!ally` - Spawn a friendly sosig with your Twitch username
- `!enemy` - Spawn an enemy sosig with your Twitch username  
- `!clear` - Clear all sosigs (moderators/broadcaster only)
- `!help` - Show available commands
- `!stats` - Display current sosig statistics

### **Advanced Commands**
- `!ally <armor_preset>` - Spawn ally with specific armor (e.g., `!ally Heavy`)
- `!enemy <armor_preset>` - Spawn enemy with specific armor (e.g., `!enemy Stealth`)

### **Available Armor Presets**
- `Standard` - Basic military gear
- `Heavy` - Maximum protection armor
- `Stealth` - Lightweight stealth gear
- `Riot` - Riot control equipment
- `Tactical` - Elite tactical equipment
- `Berserker` - Minimal armor for mobility
- `Juggernaut` - Maximum protection heavy armor

## Features

### **Real-Time Integration**
- ? **Instant Response** - Commands processed immediately from chat
- ? **User Tracking** - Each viewer can have their own sosigs
- ? **Cooldown Management** - Prevents spam with per-user cooldowns
- ? **Permission System** - Configure who can use commands

### **Nameplate System**
- **Twitch Usernames** - Sosigs display the spawning user's Twitch name
- **Color Coding** - Green for allies, red for enemies
- **Always Visible** - Nameplates face the camera automatically

### **Advanced Features**
- **Subscriber/Moderator Perks** - Configure special permissions
- **Per-User Limits** - Control how many sosigs each user can have
- **Auto-Cleanup** - Automatically remove old sosigs
- **Performance Mode** - Reduces spawning when FPS drops

## Configuration Options

### **Connection Settings**
```ini
[Twitch Integration]
EnableTwitchIntegration = true
AutoConnectOnStartup = false      # Auto-connect when H3VR starts
TwitchUsername = YourUsername     # Auto-filled after OAuth
TwitchChannel = YourChannel       # Channel to monitor
```

### **Permission Settings**
```ini
RequireModeratorForCommands = false    # Only mods can spawn
RequireSubscriberForCommands = false   # Only subs can spawn
AllowViewersToSpawn = true             # Regular viewers can spawn
```

### **Rate Limiting**
```ini
CommandCooldownSeconds = 30            # Cooldown between user commands
MaxSosigsPerTwitchUser = 2            # Max sosigs per user
MaxAllySosigs = 8                     # Total ally limit
MaxEnemySosigs = 8                    # Total enemy limit
```

### **Chat Commands**
```ini
SosigSpawnCommand = !ally             # Command for ally spawning
EnemySosigSpawnCommand = !enemy       # Command for enemy spawning  
ClearSosigsCommand = !clear           # Command to clear sosigs
AllowedCommands = !ally,!enemy,!clear,!help,!stats
```

## GUI Controls

### **Twitch Integration Window (F8)**
- **Connection Status** - Shows if connected to Twitch
- **Authentication** - Set up OAuth login
- **Settings** - Configure permissions and limits
- **Recent Chat** - View recent chat messages
- **Statistics** - Monitor active sosigs and users

### **Armor Configuration (F6)**
- **Armor Presets** - Configure different armor sets
- **Faction Settings** - Set default armor for allies/enemies
- **Custom Configurations** - Create your own armor combinations

## Troubleshooting

### **Connection Issues**
1. **"Not authenticated"**
   - Get a new OAuth token from https://twitchapps.com/tmi/
   - Make sure to include the "oauth:" prefix
   - Verify your username matches your Twitch account

2. **"Failed to connect"**
   - Check your internet connection
   - Verify the channel name is correct
   - Try disconnecting and reconnecting

3. **"Commands not working"**
   - Check if you have permission to use commands
   - Verify cooldown settings aren't too restrictive
   - Make sure the command is in the allowed commands list

### **Performance Issues**
1. **FPS drops with many sosigs**
   - Lower `MaxAllySosigs` and `MaxEnemySosigs` values
   - Enable auto-cleanup with shorter sosig lifetimes
   - Performance mode will activate automatically

2. **Sosigs not spawning**
   - Check if at sosig limits
   - Verify templates are loaded correctly
   - Look for errors in BepInEx console

### **GUI Problems**
1. **Twitch GUI not opening**
   - Check if F8 key is bound correctly
   - Look for key conflicts with other mods
   - Try changing the key binding in config

## Migration from File-Based System

### **Automatic Migration**
- H3TVR Enhanced Edition automatically detects the new TwitchLib system
- Old file-based monitoring is disabled by default
- All existing armor and nameplate settings are preserved

### **Enable Legacy Mode (if needed)**
```ini
[Chat Sosigs]
EnableLegacyFileMode = true      # Re-enable file-based chat
EnableTwitchChatSosigs = false   # Disable TwitchLib
```

### **Benefits of Upgrading**
- **Real-time response** vs. file polling delays
- **Better user experience** with instant feedback
- **Enhanced features** like per-user tracking
- **Improved performance** with direct chat integration
- **More reliable** without file system dependencies

## Advanced Configuration

### **Custom Armor Integration**
The system integrates with the advanced armor GUI (F6):
```ini
[Enhanced Chat Spawner]
DefaultAllyArmor = Standard
DefaultEnemyArmor = Heavy Assault
EnableCustomArmorCommands = true
```

### **Nameplate Customization**
```ini
[Enhanced Chat Spawner Nameplates]  
NameplateHeight = 2.5
NameplateScale = 0.02
AllyNameplateColor = 0,1,0,1      # Green (R,G,B,A)
EnemyNameplateColor = 1,0,0,1     # Red (R,G,B,A)
```

### **Sosig Behavior**
```ini
[Enhanced Chat Spawner]
EnableAdvancedAI = true           # Enhanced sosig AI
SosigLifetime = 300.0            # Auto-cleanup after 5 minutes
EnableAutoCleanup = true         # Remove expired sosigs
```

## Security & Privacy

### **OAuth Token Security**
- Tokens are stored encrypted locally in `config/H3TVR_TwitchAuth.json`
- Never share your OAuth token with others
- Regenerate tokens if compromised

### **Data Collection**
- Only chat messages and usernames are processed
- No personal data is stored permanently
- All data is used solely for sosig spawning functionality

### **Permissions**
- The mod only requests chat read permissions
- Cannot send messages on your behalf (except status messages)
- Cannot access private information

## Support

### **Getting Help**
1. Check the BepInEx console for error messages
2. Verify all configuration settings
3. Try disabling and re-enabling the integration
4. Report issues with detailed logs

### **Known Limitations**
- Requires stable internet connection
- OAuth tokens expire (typically 60 days)
- Some corporate firewalls may block Twitch IRC
- Maximum of ~100 concurrent chat users recommended

## File Locations
- **Main Config:** `BepInEx/config/H3TVR.cfg`
- **Twitch Auth:** `BepInEx/config/H3TVR_TwitchAuth.json`
- **Armor Config:** `BepInEx/config/H3TVR_ChatSosigArmor.ini`
- **Name Files:** `BepInEx/config/H3TVR_AllyNames.ini` and `H3TVR_EnemyNames.ini`

---

The TwitchLib integration transforms H3TVR from a file-based system to a true real-time Twitch interactive experience, making your H3VR streams more engaging and responsive to your audience!