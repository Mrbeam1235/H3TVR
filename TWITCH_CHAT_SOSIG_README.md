# H3TVR Twitch Chat Sosig Integration

This system allows Twitch chat viewers to spawn sosigs (AI characters) in your H3VR game with customizable armor and weapons.

## Features

- **Twitch Chat Integration**: Viewers can spawn sosigs using chat commands
- **Configurable Armor**: Multiple armor sets with different protection levels and appearance chances
- **Friendly and Enemy Sosigs**: Support for both friendly followers and hostile enemies
- **File-based Fallback**: Works with file monitoring when direct Twitch connection isn't available
- **Queue System**: Manages spawn requests to prevent spam and performance issues
- **Auto-cleanup**: Automatically removes dead sosigs after a configurable delay

## Setup

### 1. Configuration Files

The system will create several configuration files in your BepInEx/config folder:

- `H3TVR_ChatSosigArmor.ini` - Defines armor sets and their properties
- Chat spawn trigger files (paths configurable in main config)

### 2. Key Bindings (Default)

- **P** - Spawn friendly chat sosig
- **O** - Spawn enemy chat sosig  
- **L** - Cycle through armor sets
- **Delete** - Clear all chat sosigs
- **Insert** - Show chat sosig statistics

### 3. Configuration Options

In the main H3TVR configuration file:

```ini
[Chat Sosigs]
EnableTwitchChatSosigs = true
TwitchChatFilePath = chat_spawner.txt
TwitchEnemyChatFilePath = enemy_chat_spawner.txt
EnableArmor = true
ArmorChance = 0.7
MaxChatSosigs = 10
FollowDistance = 6.0
AutoCleanup = true
```

## Usage

### Manual Spawning

1. Press **P** to spawn a friendly sosig (will follow and protect you)
2. Press **O** to spawn an enemy sosig (will attack you)
3. Press **L** to cycle through available armor sets before spawning
4. Press **Delete** to clear all spawned chat sosigs

### Twitch Chat Commands (when using SimpleTwitchChatIntegration)

Configure your Twitch bot credentials in the SimpleTwitchChatIntegration component:

```csharp
public string channel = "your_channel_name";
public string username = "your_bot_username";  
public string oauth = "oauth:your_oauth_token";
```

Available chat commands:
- `!spawnsosig [armor_set]` - Spawn a friendly sosig
- `!spawnenemy [armor_set]` - Spawn an enemy sosig
- `!armor [armor_set_name]` - Set armor for next spawn
- `!clear` - Clear all sosigs (moderator only)

Example:
- `!spawnsosig Heavy Assault` - Spawns a friendly sosig with heavy assault armor
- `!spawnenemy Stealth Ops` - Spawns an enemy sosig with stealth gear

### File-based Integration (OBS, Streamlabs, etc.)

If you can't use direct Twitch integration, you can use file monitoring:

1. Set up your streaming software to write chat usernames to text files
2. Configure the file paths in the H3TVR config
3. The system will automatically detect file changes and spawn sosigs

Example file format for `chat_spawner.txt`:
```json
{"username":"ViewerName"}
```

## Armor Sets

### Available Armor Sets:

1. **Standard** - Basic military gear (Light armor)
2. **Heavy Assault** - Heavy combat armor (Heavy armor)  
3. **Stealth Ops** - Lightweight stealth gear (Light armor)
4. **Riot Control** - Riot control equipment (Heavy armor)
5. **Civilian** - Basic civilian clothing (No armor)
6. **Tactical Elite** - Elite tactical equipment (Elite armor)
7. **Berserker** - Minimal armor for maximum mobility (Light armor)
8. **Juggernaut** - Maximum protection gear (Elite armor)

### Custom Armor Sets

You can create custom armor sets by editing `H3TVR_ChatSosigArmor.ini`:

```ini
[MyCustomSet]
description=My custom armor configuration
headwear_chance=0.9
facewear_chance=0.5
eyewear_chance=0.7
torsowear_chance=1.0
pantswear_chance=1.0
backpack_chance=0.6
armor_level=Heavy
```

## Advanced Features

### Sosig Behavior

- **Friendly Sosigs**: Follow the player, assist in combat, search for equipment
- **Enemy Sosigs**: Actively hunt and attack the player
- **Smart AI**: Sosigs use appropriate combat tactics based on their equipment

### Performance Management

- Maximum active sosig limit (configurable)
- Automatic cleanup of dead sosigs
- Queue system to prevent spawn spam
- Update interval optimization

### Statistics

Press **Insert** to view current statistics:
- Active sosig count
- Friendly vs enemy counts  
- Queued spawn requests
- Performance metrics

## Troubleshooting

### Common Issues

1. **Sosigs not spawning**
   - Check that EnableTwitchChatSosigs is true
   - Verify file paths are correct
   - Check maximum sosig limit hasn't been reached

2. **Twitch integration not working**
   - Verify OAuth token is valid
   - Check channel name is correct
   - Ensure firewall allows connections

3. **Performance issues**
   - Reduce MaxChatSosigs setting
   - Increase sosig update interval
   - Enable auto-cleanup

### Debug Information

Check the BepInEx console for detailed logging:
- Spawn events
- Configuration loading
- Error messages
- Statistics updates

## Integration with Other Mods

This system integrates with:
- TNH (Take and Hold) for spawn positioning
- Other H3TVR features (slomo, zero gravity effects)
- Weapon randomization systems
- SosigSpawner mods

## Development

The system is modular and extensible:

- `TwitchChatSosigManager` - Core sosig management
- `SimpleTwitchChatIntegration` - Basic Twitch chat integration  
- `ArmorSet` - Armor configuration system
- `ChatSosig` - Individual sosig data structure

You can extend the system by:
- Adding new armor sets
- Creating custom spawn behaviors
- Implementing additional chat commands
- Adding new sosig templates

## Security Notes

- Chat commands should be rate-limited
- Consider moderator-only commands for destructive actions
- Validate all user input from chat
- Monitor system resources with many concurrent sosigs

## Support

For issues or feature requests:
1. Check the BepInEx console for error messages
2. Verify configuration files are properly formatted  
3. Test with manual spawning first
4. Report bugs with full error logs and configuration details