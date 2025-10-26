# ? ChatWatcher Username Extraction - Complete

## What Was Changed

The ChatWatcher system has been updated to extract usernames from **simple text files** without requiring JSON. This makes it perfect for Twitch Channel Points redemptions where you just want to write the redeemer's username to a file.

## Files Modified

### `src\ChatWatcher.cs`
- ? Removed JSON parsing completely
- ? Added `ExtractUsername()` method with smart format detection
- ? Enhanced `ParseUsernames()` to handle multiple text formats
- ? Updated configuration descriptions for channel points
- ? Added detailed logging for username extraction

## New Features

### 1. Multiple Text Format Support
The system now accepts **three different formats**:

**Format 1: Plain Username** (Simplest)
```
ViewerName
```

**Format 2: Key=Value** (Recommended)
```
username=ViewerName
user=ViewerName
redeemer=ViewerName
```

**Format 3: First Word** (Automatic)
```
ViewerName redeemed your reward!
```

### 2. Smart Username Extraction
The system tries formats in this priority order:
1. **Key=Value format** - Looks for `username=`, `user=`, `redeemer=`, etc.
2. **First word** - Extracts username from text before first space
3. **Plain text** - Uses entire line as username

### 3. Automatic Cleanup
- Removes comments (lines starting with `#` or `;`)
- Strips whitespace
- Removes inline comments after `#`
- Prevents duplicate spawns with username cache

## How to Use with Channel Points

### Step 1: Configure File Paths
In your BepInEx config:
```ini
[Chat Watcher - File Mode]
AllyChatFilePath = C:\StreamFiles\ally_redeem.txt
EnemyChatFilePath = C:\StreamFiles\enemy_redeem.txt
ClearFileAfterRead = true
FileCheckInterval = 0.5
```

### Step 2: Set Up Channel Point Reward

**In Streamer.bot or similar:**
- Create reward: "Spawn Ally Sosig"
- Set cost: 250 points (or whatever you want)
- Action: Write to file `C:\StreamFiles\ally_redeem.txt`
- Content: `username={user}`

**In OBS/Stream Elements:**
- Write to file: `C:\StreamFiles\ally_redeem.txt`
- Content: `{user}` or `username={user}`

### Step 3: Test It!
1. Write `TestUser` to your configured file
2. Watch the BepInEx console
3. Look for: `Channel Point Redemption: Spawned ally for TestUser`

## Example Configurations

### Streamer.bot Action
```yaml
Trigger: Channel Point Redemption "Spawn Ally"
Action: Write File
  File Path: C:\StreamFiles\ally_redeem.txt
  Content: username={user}
  Overwrite: true
```

### PowerShell Script
```powershell
# Write username to file
param([string]$username)
"username=$username" | Set-Content "C:\StreamFiles\ally_redeem.txt"
```

### Batch File
```batch
@echo off
echo username=%1 > "C:\StreamFiles\ally_redeem.txt"
```

## Documentation Created

### ?? Comprehensive Guide
**`docs\Channel_Points_Username_Extraction_Guide.md`**
- Complete format documentation
- Setup instructions
- Examples for all formats
- Troubleshooting guide
- Advanced usage tips

### ?? Quick Reference
**`docs\Channel_Points_Quick_Reference.md`**
- 3-step setup guide
- Format comparison table
- Common configurations
- Quick troubleshooting
- Pro tips

### ?? Implementation Summary
**`docs\Channel_Points_Implementation_Summary.md`**
- Technical changes made
- Code modifications
- Integration examples
- Testing procedures
- Log message reference

### ?? Example Files
**`config\H3TVR_AllyChat_Example.txt`**
**`config\H3TVR_EnemyChat_Example.txt`**
- Ready-to-use example files
- Format documentation
- Usage instructions

## Testing

### Quick Test
1. Create file `C:\test\ally.txt`
2. Add line: `TestUser123`
3. Point config to this file
4. Watch for spawn!

### Format Tests
Test each format to find what works best:

```
# Test 1: Plain
ViewerName

# Test 2: Key=Value
username=ViewerName

# Test 3: First Word
ViewerName redeemed!
```

All three should extract `ViewerName` and spawn a sosig.

## Supported Keys

These keys all work in key=value format:
- `username=`
- `user=`
- `redeemer=`
- `name=`
- `viewer=`
- `chatter=`

Use whichever makes sense for your setup!

## Log Messages

### Successful Operation
```
[Info] Chat Watcher initialized (Channel Points ready)
[Info] Extracted username: 'ViewerName' from line: 'username=ViewerName'
[Info] Channel Point Redemption: Spawned ally for ViewerName
```

### File Monitoring
```
[Info] File watching initialized (Channel Points ready)
[Info]   Ally file: C:\StreamFiles\ally_redeem.txt
[Info]   Enemy file: C:\StreamFiles\enemy_redeem.txt
```

## Benefits

? **No JSON Required** - Simple text files only
? **Multiple Formats** - Works with any text format
? **Channel Points Ready** - Perfect for redemptions
? **Flexible** - Supports various key names
? **Smart Extraction** - Automatic format detection
? **Duplicate Prevention** - Username caching
? **Well Documented** - Comprehensive guides
? **Easy Testing** - Manual keyboard controls
? **Backward Compatible** - Works with existing setups

## Troubleshooting

### Username Not Detected
**Check:**
- File exists at configured path
- File contains valid format
- Username on its own line
- No special characters

**Solution:**
- Try simplest format first: just the username
- Check BepInEx console for errors
- Verify file path is correct

### Duplicate Spawns
**Check:**
- `ClearFileAfterRead = true`
- Spawn cooldown setting
- File not being written multiple times

**Solution:**
- Enable file clearing
- Increase spawn cooldown
- Check redemption tool isn't writing twice

### No Spawns
**Check:**
- `EnableFileWatching = true`
- File path matches config
- Max sosigs not reached
- File actually being written to

**Solution:**
- Check BepInEx console logs
- Test with manual key (P for ally)
- Verify file path is accessible

## Manual Controls

While channel points are running, you can still manually spawn:
- **P** - Spawn random ally sosig
- **O** - Spawn random enemy sosig
- **Delete** - Clear all sosigs

## Summary

The ChatWatcher system now **perfectly supports Twitch Channel Points** with simple, flexible username extraction:

### For Channel Points:
**Recommended:** `username={user}`

**Simplest:** `{user}`

**Both work perfectly!**

### The system will:
1. ? Detect username in file
2. ? Extract it automatically
3. ? Spawn sosig for redeemer
4. ? Clear file (if configured)
5. ? Log the spawn
6. ? Prevent duplicates

**No JSON, no complex parsing, just simple text files - perfect for channel points integration!**

## Next Steps

1. ? Configure file paths in BepInEx config
2. ? Set up channel point reward
3. ? Test with example username
4. ? Watch sosigs spawn for your viewers!

Enjoy your viewer-spawned sosigs! ????
