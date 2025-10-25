# Advanced Chat Sosig Spawner - Custom Paths Quick Reference

## ? NEW FEATURE: Files Can Be Located ANYWHERE!

Your name INI files can now be located **anywhere on your computer** - not just in the BepInEx folder!

## ?? Quick Examples

### Desktop Files (Easiest!)
```ini
[Chat Spawner Advanced]
AllyNamesFile = C:\Users\YourName\Desktop\ally_names.txt
EnemyNamesFile = C:\Users\YourName\Desktop\enemy_names.txt
```

### Documents Folder
```ini
AllyNamesFile = C:\Users\YourName\Documents\H3VR\ally_names.txt
EnemyNamesFile = C:\Users\YourName\Documents\H3VR\enemy_names.txt
```

### Downloads Folder
```ini
AllyNamesFile = C:\Users\YourName\Downloads\allies.txt
EnemyNamesFile = C:\Users\YourName\Downloads\enemies.txt
```

### Another Drive
```ini
AllyNamesFile = D:\My Files\H3VR Names\allies.ini
EnemyNamesFile = D:\My Files\H3VR Names\enemies.ini
```

### Network/Cloud Storage
```ini
AllyNamesFile = C:\Users\YourName\Dropbox\H3VR\names\allies.txt
EnemyNamesFile = \\NetworkShare\Games\H3VR\names\enemies.txt
```

## ?? Important Path Rules

### Windows Paths - Use One of These Formats:

**? GOOD - Forward slashes:**
```ini
AllyNamesFile = C:/My Files/allies.txt
```

**? GOOD - Escaped backslashes:**
```ini
AllyNamesFile = C:\\My Files\\allies.txt
```

**? MAY FAIL - Single backslashes:**
```ini
AllyNamesFile = C:\My Files\allies.txt  # Might not work!
```

## ?? Path Types Supported

### 1. Absolute Paths (Anywhere!)
- ? `C:\Users\You\Desktop\names.txt`
- ? `D:\Game Files\H3VR\allies.ini`
- ? `E:\Downloads\sosig_names.txt`
- ? `\\NetworkShare\H3VR\names.ini`

### 2. Relative Paths (BepInEx folder)
- ? `BepInEx/config/H3TVR_AllyNames.ini` (default)
- ? `config/names.txt`
- ? `plugins/H3TVR/names.txt`

## ?? Quick Setup Steps

### Step 1: Create Your Name Files
Create text files anywhere you want:

**Example: `C:\Users\YourName\Desktop\ally_names.txt`**
```
# My Ally Names
Alpha Squad
Bravo Team
Charlie Unit
Delta Force
```

**Example: `C:\Users\YourName\Desktop\enemy_names.txt`**
```
# My Enemy Names
Hostile Alpha
Enemy Bravo
Threat Charlie
Danger Delta
```

### Step 2: Update Config
Edit `BepInEx/config/com.h3tvr.improved.cfg`:

```ini
[Chat Spawner Advanced]
AllyNamesFile = C:/Users/YourName/Desktop/ally_names.txt
EnemyNamesFile = C:/Users/YourName/Desktop/enemy_names.txt
UseRandomNames = true
```

### Step 3: Start Game
Names will be loaded automatically!

Check the BepInEx log for confirmation:
```
[Info: H3TVR] Loaded 4 ally names from C:\Users\YourName\Desktop\ally_names.txt
[Info: H3TVR] Loaded 4 enemy names from C:\Users\YourName\Desktop\enemy_names.txt
```

## ?? Creative Uses

### For Streamers
```ini
# Keep with stream files
AllyNamesFile = C:/Stream Files/H3VR/subscriber_names.txt
EnemyNamesFile = C:/Stream Files/H3VR/donor_names.txt
```

### For Testing
```ini
# Quick access on desktop
AllyNamesFile = C:/Users/You/Desktop/test_allies.txt
EnemyNamesFile = C:/Users/You/Desktop/test_enemies.txt
```

### For Organization
```ini
# Organized in Documents
AllyNamesFile = C:/Users/You/Documents/Gaming/H3VR/Names/allies.txt
EnemyNamesFile = C:/Users/You/Documents/Gaming/H3VR/Names/enemies.txt
```

### For Sharing
```ini
# Network share for multiplayer/shared setup
AllyNamesFile = \\GameServer\H3VR\Shared\allies.txt
EnemyNamesFile = \\GameServer\H3VR\Shared\enemies.txt
```

## ?? Troubleshooting

### "File not found"
1. Check path is correct (copy-paste recommended)
2. Check file actually exists
3. Use forward slashes `/` or escaped backslashes `\\`
4. Check for typos

### "Permission denied"
1. Ensure file is not read-only
2. Try copying file to different location
3. Run game as administrator if needed

### Names not loading
1. Verify `UseRandomNames = true` in config
2. Check file has content (not empty)
3. Check log for "Loaded X names from [path]"
4. Ensure no encoding issues (use plain text/UTF-8)

## ?? Pro Tips

1. **Desktop = Easy Access**
   - Keep files on desktop while testing
   - Easy to edit, quick to find

2. **Documents = Organized**
   - Better long-term storage
   - Won't clutter desktop

3. **Cloud Storage = Backup**
   - Dropbox, OneDrive, etc.
   - Auto-backup and sync across PCs

4. **Copy Full Path**
   - Right-click file ? "Copy as path"
   - Paste directly into config
   - Change `\` to `/` or `\\`

5. **Test First**
   - Start with short file on desktop
   - Verify it works before moving

## ?? File Format Reminder

```
# Comments start with # or ;
; This is also a comment

Name One
Name Two
Name Three

# Blank lines ignored
Name Four
```

**Rules:**
- One name per line
- `#` or `;` for comments
- Blank lines ignored
- Any file extension works (`.txt`, `.ini`, `.cfg`)

## ? Benefits

| Before | After |
|--------|-------|
| ? Stuck in BepInEx folder | ? **Anywhere** on computer |
| ? Hard to find/edit | ? Desktop, Documents, etc. |
| ? No easy backup | ? Cloud storage friendly |
| ? Can't share easily | ? Network drives work |

---

**Full Documentation:** [AdvancedChatSosigSpawner_Custom_Paths_Guide.md](AdvancedChatSosigSpawner_Custom_Paths_Guide.md)

**Related:** [QuickStart Guide](AdvancedChatSosigSpawner_QuickStart.md) | [Names Guide](EnhancedChatSpawner_Names_Armor_Guide.md)
