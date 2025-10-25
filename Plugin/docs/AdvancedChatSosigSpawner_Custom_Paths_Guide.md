# Advanced Chat Sosig Spawner - Custom Name File Paths Guide

## ?? Overview

The Advanced Chat Sosig Spawner now supports **name files located ANYWHERE on your computer!** You're no longer limited to the BepInEx folder.

## ?? Supported Path Types

### 1. **Absolute Paths** (Files Anywhere!)

You can point to files on **any drive, any folder** on your computer:

```ini
[Chat Spawner Advanced]
AllyNamesFile = C:\My Documents\H3VR Stuff\ally_names.txt
EnemyNamesFile = D:\Game Files\H3VR\enemies.ini
```

**Examples:**
- `C:\Users\YourName\Desktop\sosig_names.txt`
- `D:\Steam Games\H3VR\Custom Names\allies.ini`
- `E:\Downloads\cool_names.txt`
- `C:\My Files\Gaming\H3VR\sosig_names\allies_2024.ini`

### 2. **Relative Paths** (BepInEx Folder)

Traditional relative paths still work:

```ini
AllyNamesFile = BepInEx/config/H3TVR_AllyNames.ini
EnemyNamesFile = BepInEx/config/H3TVR_EnemyNames.ini
```

**Path Resolution Order:**
1. Relative to plugin folder (`BepInEx/plugins/H3TVR/`)
2. Relative to BepInEx folder (`BepInEx/`)
3. Relative to game root folder

## ?? Quick Setup Examples

### Example 1: Desktop Files
Store your name files on your desktop for easy editing:

**Config (`BepInEx/config/com.h3tvr.improved.cfg`):**
```ini
[Chat Spawner Advanced]
AllyNamesFile = C:\Users\YourName\Desktop\ally_names.txt
EnemyNamesFile = C:\Users\YourName\Desktop\enemy_names.txt
```

**File (`C:\Users\YourName\Desktop\ally_names.txt`):**
```
# My Awesome Ally Names
Alpha Squad
Bravo Team
Charlie Unit
Delta Force
Echo Platoon
```

### Example 2: Shared Network Drive
Share name files across multiple PCs:

```ini
[Chat Spawner Advanced]
AllyNamesFile = Z:\Shared\H3VR\Names\allies.ini
EnemyNamesFile = Z:\Shared\H3VR\Names\enemies.ini
```

### Example 3: Dropbox/Cloud Storage
Sync names across devices:

```ini
[Chat Spawner Advanced]
AllyNamesFile = C:\Users\YourName\Dropbox\H3VR\ally_names.txt
EnemyNamesFile = C:\Users\YourName\Dropbox\H3VR\enemy_names.txt
```

### Example 4: Organized Folder Structure
Keep everything organized in your documents:

```ini
[Chat Spawner Advanced]
AllyNamesFile = C:\Users\YourName\Documents\Gaming\H3VR\Names\allies.ini
EnemyNamesFile = C:\Users\YourName\Documents\Gaming\H3VR\Names\enemies.ini
```

## ?? File Format

Name files support **any text file format** (`.txt`, `.ini`, `.cfg`, etc.):

```
# Comments start with # or ;
; This is also a comment

# One name per line
Friendly Bot
Guardian Angel
Protector
Defender
Support Squad

# Blank lines are ignored

Helper Unit
Medic
Scout
```

**Rules:**
- ? One name per line
- ? Comments with `#` or `;`
- ? Blank lines ignored
- ? Whitespace trimmed automatically
- ? Any file extension (`.txt`, `.ini`, `.cfg`)

## ?? Use Cases

### 1. **Easy Editing**
Keep files on desktop for quick access and editing without digging through game folders.

### 2. **Version Control**
Store in Git repository or cloud storage for backup and version history.

### 3. **Shared Configurations**
Multiple people can use the same name files from a network drive.

### 4. **Stream Integration**
Point to files that your stream tools can also access/modify.

### 5. **Multiple Profiles**
Easily switch between different name lists:
```ini
# Military theme
AllyNamesFile = C:\Names\military_allies.txt

# Fantasy theme
#AllyNamesFile = C:\Names\fantasy_allies.txt

# Sci-fi theme
#AllyNamesFile = C:\Names\scifi_allies.txt
```

## ?? Configuration Location

Edit your paths in the BepInEx config file:

**File:** `BepInEx/config/com.h3tvr.improved.cfg`

**Section:**
```ini
[Chat Spawner Advanced]

## Path to ally names file
## SUPPORTS ABSOLUTE PATHS ANYWHERE ON YOUR COMPUTER!
## Examples:
##   Relative: BepInEx/config/H3TVR_AllyNames.ini
##   Absolute: C:\My Files\ally_names.txt
##   Absolute: D:\Game Stuff\H3VR\ally_sosig_names.ini
# Setting type: String
# Default value: BepInEx/config/H3TVR_AllyNames.ini
AllyNamesFile = C:\Your\Custom\Path\allies.txt

## Path to enemy names file
## SUPPORTS ABSOLUTE PATHS ANYWHERE ON YOUR COMPUTER!
## Examples:
##   Relative: BepInEx/config/H3TVR_EnemyNames.ini
##   Absolute: C:\My Files\enemy_names.txt
##   Absolute: D:\Game Stuff\H3VR\enemy_sosig_names.ini
# Setting type: String
# Default value: BepInEx/config/H3TVR_EnemyNames.ini
EnemyNamesFile = C:\Your\Custom\Path\enemies.txt

## Use random names from name lists
# Setting type: Boolean
# Default value: true
UseRandomNames = true
```

## ?? Path Resolution Logic

When you configure a path, the mod checks in this order:

### For Absolute Paths (e.g., `C:\Names\allies.txt`)
1. ? Uses the path **exactly as specified**
2. ? File can be on **any drive**
3. ? File can be in **any folder**

### For Relative Paths (e.g., `config/names.txt`)
1. Checks relative to plugin folder: `BepInEx/plugins/H3TVR/config/names.txt`
2. Checks relative to BepInEx root: `BepInEx/config/names.txt`
3. Checks relative to game root: `h3vr/config/names.txt`
4. Uses first match found

## ?? Troubleshooting

### File Not Found
**Symptom:** Log shows "names file not found"

**Solution:** Check that:
- Path uses forward slashes `/` or escaped backslashes `\\`
- File actually exists at that location
- No typos in path or filename
- File permissions allow reading

**Correct Formats:**
```ini
# ? Good - forward slashes
AllyNamesFile = C:/My Files/allies.txt

# ? Good - escaped backslashes
AllyNamesFile = C:\\My Files\\allies.txt

# ? Bad - single backslashes (may not work)
AllyNamesFile = C:\My Files\allies.txt
```

### Permission Denied
**Symptom:** Error accessing file

**Solution:**
- Ensure file is not read-only
- Check folder permissions
- Try moving file to different location
- Run game as administrator if needed

### Names Not Loading
**Symptom:** Default names used instead of custom

**Solution:** Check that:
- `UseRandomNames = true` in config
- File contains valid names (one per line)
- File is not empty
- No Unicode/encoding issues (use UTF-8 or ASCII)

## ?? Examples by Use Case

### For Streamers
```ini
# Keep names with your stream files for OBS/Streamlabs integration
AllyNamesFile = C:\Stream Files\H3VR\subscriber_names.txt
EnemyNamesFile = C:\Stream Files\H3VR\viewer_names.txt
```

### For Modders
```ini
# Keep in your mod development folder
AllyNamesFile = D:\Modding\H3VR\Test Names\allies.txt
EnemyNamesFile = D:\Modding\H3VR\Test Names\enemies.txt
```

### For Content Creators
```ini
# Organize with your recording/editing files
AllyNamesFile = E:\Videos\H3VR Content\Episode 5\names\allies.txt
EnemyNamesFile = E:\Videos\H3VR Content\Episode 5\names\enemies.txt
```

### For Network/LAN Setup
```ini
# Share across multiple computers on network
AllyNamesFile = \\SERVER\Games\H3VR\Names\allies.txt
EnemyNamesFile = \\SERVER\Games\H3VR\Names\enemies.txt
```

## ?? Pro Tips

1. **Use Descriptive Paths**
   - `C:\H3VR Names\military_allies.txt` is clearer than `C:\names.txt`

2. **Backup Your Files**
   - Custom paths make it easy to back up to cloud storage

3. **Test Your Paths**
   - Check the BepInEx log to confirm files loaded correctly
   - Look for: `[Info: H3TVR] Loaded X ally names from [path]`

4. **Organize by Theme**
   ```
   C:\H3VR Names\
   ??? military_allies.txt
   ??? military_enemies.txt
   ??? scifi_allies.txt
   ??? scifi_enemies.txt
   ??? fantasy_allies.txt
   ??? fantasy_enemies.txt
   ```

5. **Edit While Game Runs**
   - Files are loaded at startup
   - Restart the scene or reload the mod to pick up changes

## ?? Default Behavior

If no custom path is specified or file is not found:
- Default files created in: `BepInEx/config/`
- Default ally names: "Friendly Bot", "Guardian", "Protector", etc.
- Default enemy names: "Hostile Bot", "Attacker", "Enemy", etc.

## ? Benefits

### Before (Relative Paths Only)
- ? Files stuck in BepInEx folder
- ? Hard to find/edit
- ? No easy backup
- ? Can't share easily

### After (Absolute Paths Supported)
- ? Files **anywhere** on computer
- ? Easy to find/edit (desktop, documents, etc.)
- ? Easy backup (cloud storage)
- ? Easy sharing (network drives)
- ? Better organization
- ? Integration with other tools

## ?? Summary

The Advanced Chat Sosig Spawner now gives you **complete freedom** to organize your name files however you want!

**Key Features:**
- ? Absolute paths to **any location**
- ? Relative paths still work
- ? Automatic path resolution
- ? Helpful logging
- ? Fallback to defaults if needed

**Just edit your config and point to wherever you want your names stored!**

---

**Related Documentation:**
- [Advanced Chat Sosig Spawner Guide](AdvancedChatSosigSpawner_QuickStart.md)
- [Name System Implementation](AllyINI_Names_Implementation_Summary.md)
- [Configuration Guide](EnhancedChatSpawner_Names_Armor_Guide.md)
