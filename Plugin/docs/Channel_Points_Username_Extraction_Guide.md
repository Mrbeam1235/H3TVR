# Channel Points Username Extraction Guide

## Overview
The ChatWatcher system now extracts usernames from simple text files without using JSON. This makes it perfect for Twitch Channel Points redemptions where you just want to write the redeemer's username to a file.

## Supported File Formats

The ChatWatcher supports **three simple formats** for extracting usernames:

### Format 1: Plain Username (Simplest)
Just write the username on a line by itself:
```
ViewerName
```

### Format 2: Key=Value Format (Recommended for Channel Points)
Use a key=value pair:
```
username=ViewerName
user=ViewerName
redeemer=ViewerName
name=ViewerName
viewer=ViewerName
chatter=ViewerName
```

### Format 3: First Word Format
If you have text like "Username did something", it will extract the first word:
```
ViewerName redeemed your channel points!
```

## How It Works

### Username Extraction Priority
The system tries to extract usernames in this order:

1. **Key=Value format** (if line contains `=`)
   - Looks for keys: `username`, `user`, `redeemer`, `name`, `viewer`, `chatter`
   - Extracts the value after the `=`
   - Removes any comments (# symbol) or extra text after spaces

2. **First word format** (if line contains spaces)
   - Takes the first word before a space
   - Skips lines that look like system messages (starting with `[`, `<`, or ending with `:`)

3. **Plain username format** (entire line)
   - Uses the entire trimmed line as the username
   - Skips lines starting with `[` or `<`

### Comments and Empty Lines
- Lines starting with `#` or `;` are ignored (comments)
- Empty lines are skipped
- Whitespace is automatically trimmed

## Setup for Channel Points

### Option 1: Using Streamer.bot or Similar Tools

1. **Configure file paths in BepInEx config:**
   ```ini
   [Chat Watcher - File Mode]
   AllyChatFilePath = C:\StreamFiles\ally_redeem.txt
   EnemyChatFilePath = C:\StreamFiles\enemy_redeem.txt
   ClearFileAfterRead = true
   FileCheckInterval = 0.5
   ```

2. **Set up your Channel Point reward to write to file:**
   - **Ally Redemption:** Write to `C:\StreamFiles\ally_redeem.txt`
   - **Enemy Redemption:** Write to `C:\StreamFiles\enemy_redeem.txt`

3. **Choose your format in the redemption action:**
   
   **Simplest (Plain Username):**
   ```
   {user}
   ```
   
   **Recommended (Key=Value):**
   ```
   username={user}
   ```
   
   **Descriptive (First Word):**
   ```
   {user} redeemed ally spawn!
   ```

### Option 2: Using OBS or Stream Elements

Write the username to the configured file using any of the supported formats:

```
username={redeemerUsername}
```

or

```
{redeemerUsername}
```

## Configuration Details

### File Paths
- **Absolute paths:** `C:\StreamFiles\ally_chat.txt`
- **Relative paths:** `BepInEx/config/H3TVR_AllyChat.txt`

### Clear After Read
- **Enabled (recommended):** File is cleared after reading usernames
- **Disabled:** File keeps accumulating usernames (may cause duplicates)

### File Check Interval
- Default: `0.5` seconds (checks file twice per second)
- Faster: `0.25` seconds (checks 4 times per second)
- Slower: `1.0` seconds (checks once per second)

## Example File Contents

### Example 1: Plain Usernames
```
Viewer1
Viewer2
Viewer3
```
**Result:** Spawns sosigs for Viewer1, Viewer2, and Viewer3

### Example 2: Key=Value Format
```
username=CoolViewer
user=AwesomeGamer
redeemer=TopFan
```
**Result:** Spawns sosigs for CoolViewer, AwesomeGamer, and TopFan

### Example 3: Mixed Format
```
# Ally spawns from channel points
username=Viewer1
Viewer2
user=Viewer3
Viewer4 redeemed ally spawn!
```
**Result:** Spawns sosigs for Viewer1, Viewer2, Viewer3, and Viewer4

### Example 4: With Comments
```
# Channel point redemptions
username=Viewer1
# This is a comment - ignored
user=Viewer2
; Another comment style - also ignored

# Empty lines are skipped
redeemer=Viewer3
```
**Result:** Spawns sosigs for Viewer1, Viewer2, and Viewer3

## Duplicate Prevention

The system automatically prevents duplicate spawns:
- Each username is tracked in a cache
- If the same username appears again, it's skipped
- Cache holds the last 1000 usernames
- Clearing sosigs also clears the cache

## Testing Your Setup

### Manual Testing
1. Write a test username to your configured file:
   ```
   TestUser
   ```

2. Watch the BepInEx console for:
   ```
   [Info   :  H3TVR] Channel Point Redemption: Spawned ally for TestUser
   ```

3. If you see the message, it's working!

### File Format Testing

Test each format to see which works best for your setup:

**Test 1 - Plain:**
```
MyUsername
```

**Test 2 - Key=Value:**
```
username=MyUsername
```

**Test 3 - First Word:**
```
MyUsername redeemed!
```

All three should work and extract `MyUsername`.

## Troubleshooting

### Username Not Extracted
**Check:**
- File exists at configured path
- Username is on its own line
- No special characters in username
- File has been modified (timestamp changed)

### Duplicate Spawns
**Solutions:**
- Enable `ClearFileAfterRead = true`
- Wait for spawn cooldown to expire
- Check that file isn't being written to multiple times

### No Spawns Happening
**Check:**
- `EnableFileWatching = true` in config
- File path is correct (check BepInEx console logs)
- File contains valid username format
- Spawn limits not reached (max sosigs)

## Advanced Usage

### Multiple Redemptions Per File
You can have multiple usernames in one file:
```
username=Viewer1
username=Viewer2
username=Viewer3
```

All will be spawned in sequence (respecting spawn cooldown).

### Custom Key Names
The system recognizes these keys:
- `username=`
- `user=`
- `redeemer=`
- `name=`
- `viewer=`
- `chatter=`

Use whichever makes sense for your setup!

### Combining with Other Systems
You can still use manual keyboard spawning:
- **P key:** Spawn random ally
- **O key:** Spawn random enemy
- **Delete key:** Clear all sosigs

## Log Messages

When working correctly, you'll see:
```
[Info] Chat Watcher initialized (Channel Points ready)
[Info] Channel Point Redemption: Spawned ally for ViewerName
[Info] Extracted username: 'ViewerName' from line: 'username=ViewerName'
```

## Summary

**For Channel Points, the recommended format is:**
```
username={user}
```

**Or the simplest:**
```
{user}
```

**Both will work perfectly to extract the redeemer's username and spawn a sosig for them!**

The system is flexible and will handle various formats automatically, making integration with any channel point redemption system easy.
