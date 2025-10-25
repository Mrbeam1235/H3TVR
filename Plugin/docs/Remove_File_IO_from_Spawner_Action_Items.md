# REMOVE File I/O from AdvancedChatSosigSpawner - Action Items

## Problem
`AdvancedChatSosigSpawner.cs` currently uses `System.IO` to load name list INI files (`H3TVR_AllyNames.ini` / `H3TVR_EnemyNames.ini`). This functionality should be moved to `ChatWatcher.cs` for complete separation of concerns.

---

## Required Changes to AdvancedChatSosigSpawner.cs

### 1. Remove `using System.IO;` Import
**Line**: ~4  
**Action**: Remove the import - it's only used for name lists now

```csharp
// REMOVE THIS:
using System.IO;
```

---

### 2. Remove LoadNameLists() Method
**Lines**: ~452-495  
**Action**: Delete entire method - ChatWatcher will handle this

```csharp
// DELETE THIS ENTIRE METHOD:
private void LoadNameLists() { ... }
```

---

### 3. Remove CreateDefaultNameFile() Method  
**Lines**: ~497-534  
**Action**: Delete entire method - ChatWatcher will handle file creation

```csharp
// DELETE THIS ENTIRE METHOD:
private void CreateDefaultNameFile(string path, bool isAlly) { ... }
```

---

### 4. Remove LoadNameLists() Call from Initialize()
**Line**: ~138  
**Action**: Remove the call since the method will be deleted

```csharp
// In Initialize() method:
InitializeConfiguration();
InitializeSosigTemplates();
// LoadNameLists(); // REMOVE THIS LINE

logger?.LogInfo("Advanced Chat Sosig Spawner initialized...");
```

---

### 5. Comment Out Name File Path Config Entries
**Lines**: ~103-108  
**Action**: Comment out - ChatWatcher will manage these paths

```csharp
// Advanced features configuration
private ConfigEntry<bool> enableArmorCustomization;
// COMMENT OUT THESE TWO:
// private ConfigEntry<string> allyNamesFilePath;
// private ConfigEntry<string> enemyNamesFilePath;
private ConfigEntry<bool> useRandomNames;
```

---

### 6. Remove Name File Path Bindings from InitializeConfiguration()
**Lines**: ~267-273  
**Action**: Comment out the config bindings

```csharp
enableArmorCustomization = plugin.Config.Bind("Chat Spawner Advanced", "EnableArmorCustomization", true,
    "Enable armor customization system");
    
// COMMENT OUT THESE:
// allyNamesFilePath = plugin.Config.Bind("Chat Spawner Advanced", "AllyNamesFile", 
//     "BepInEx/config/H3TVR_AllyNames.ini",
//     "Path to ally names file");
// enemyNamesFilePath = plugin.Config.Bind("Chat Spawner Advanced", "EnemyNamesFile",
//     "BepInEx/config/H3TVR_EnemyNames.ini",
//     "Path to enemy names file");
    
useRandomNames = plugin.Config.Bind("Chat Spawner Advanced", "UseRandomNames", true,
    "Use random names from name lists");
```

---

### 7. ADD New Public Method to Receive Names from ChatWatcher
**Location**: In `#region Public API` section  
**Action**: Add this new method

```csharp
/// <summary>
/// Receive and set name lists from ChatWatcher
/// CHATWATCHER COMPATIBLE - Called by ChatWatcher after loading names from INI files
/// </summary>
public void SetNameLists(List<string> allyList, List<string> enemyList)
{
    try
    {
        if (allyList != null)
        {
            allyNames = allyList;
            logger?.LogInfo($"Received {allyNames.Count} ally names from ChatWatcher");
        }
        
        if (enemyList != null)
        {
            enemyNames = enemyList;
            logger?.LogInfo($"Received {enemyNames.Count} enemy names from ChatWatcher");
        }
    }
    catch (Exception ex)
    {
        logger?.LogError($"Failed to set name lists: {ex.Message}");
    }
}
```

---

## Changes to ChatWatcher.cs

### ADD Name List Loading to ChatWatcher
ChatWatcher should load the INI files and pass names to the spawner:

```csharp
// In ChatWatcher.Initialize()
private void LoadAndProvideNameLists()
{
    List<string> allyNames = new List<string>();
    List<string> enemyNames = new List<string>();
    
    // Load ally names from BepInEx/config/H3TVR_AllyNames.ini
    string allyPath = "BepInEx/config/H3TVR_AllyNames.ini";
    if (File.Exists(allyPath))
    {
        var lines = File.ReadAllLines(allyPath);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith(";"))
            {
                allyNames.Add(trimmed);
            }
        }
    }
    
    // Load enemy names from BepInEx/config/H3TVR_EnemyNames.ini
    string enemyPath = "BepInEx/config/H3TVR_EnemyNames.ini";
    if (File.Exists(enemyPath))
    {
        var lines = File.ReadAllLines(enemyPath);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith(";"))
            {
                enemyNames.Add(trimmed);
            }
        }
    }
    
    // Send to spawner
    if (sosigSpawner != null)
    {
        sosigSpawner.SetNameLists(allyNames, enemyNames);
    }
}
```

---

## Final Architecture

```
????????????????????????????????????
?         ChatWatcher.cs           ?
?  (Handles ALL File Operations)   ?
????????????????????????????????????
?  • Loads H3TVR_AllyChat.txt      ?
?  • Loads H3TVR_EnemyChat.txt     ?
?  • Loads H3TVR_AllyNames.ini  ?  ?
?  • Loads H3TVR_EnemyNames.ini ?  ?
?  • Parses usernames              ?
?  • Monitors file changes         ?
?  • Handles keyboard input        ?
?                                  ?
?  ? Calls spawner.SpawningSequence()
?  ? Calls spawner.SetNameLists() ?
????????????????????????????????????
                ?
????????????????????????????????????
?  AdvancedChatSosigSpawner.cs     ?
?  (NO File Operations - Pure API) ?
????????????????????????????????????
?  • Spawns sosigs                 ?
?  • Manages AI behavior           ?
?  • Applies armor/outfits         ?
?  • Tracks statistics             ?
?  • Stores name lists (in memory) ?
?                                  ?
?  ? NO File.Read operations      ?
?  ? NO File.Write operations     ?
?  ? NO System.IO import          ?
????????????????????????????????????
```

---

## Benefits of This Change

### 1. **Clean Separation of Concerns**
- **ChatWatcher**: All file I/O (chat files + name files)
- **AdvancedChatSosigSpawner**: Pure spawning logic (NO file operations)

### 2. **Single Responsibility**
- One component responsible for ALL file operations
- Easier to debug file-related issues
- Clearer code organization

### 3. **Testability**
- Can test spawner without file system
- Can test ChatWatcher independently
- Easier to mock dependencies

### 4. **Future-Proof**
- Easy to add new file sources (JSON, XML, database)
- Can add hot-reload to ChatWatcher only
- Clear interface between components

---

## Testing Checklist

After making these changes:

- [ ] AdvancedChatSosigSpawner compiles without `System.IO`
- [ ] No file operations in AdvancedChatSosigSpawner
- [ ] ChatWatcher loads name INI files
- [ ] ChatWatcher calls `spawner.SetNameLists()`
- [ ] Names appear correctly on nameplates
- [ ] Random names still work when `UseRandomNames = true`
- [ ] Steam Friends names still work (fallback)
- [ ] Default names ("Ally"/"Enemy") work when lists empty

---

## Summary

**REMOVE** from AdvancedChatSosigSpawner:
1. ? `using System.IO;`
2. ? `LoadNameLists()` method
3. ? `CreateDefaultNameFile()` method
4. ? `allyNamesFilePath` config entry
5. ? `enemyNamesFilePath` config entry
6. ? Call to `LoadNameLists()` in `Initialize()`

**ADD** to AdvancedChatSosigSpawner:
1. ? `SetNameLists(List<string> ally, List<string> enemy)` public method

**ADD** to ChatWatcher:
1. ? Load H3TVR_AllyNames.ini
2. ? Load H3TVR_EnemyNames.ini
3. ? Call `spawner.SetNameLists()` after loading

---

**Status**: ?? **ACTION REQUIRED**  
**Priority**: High  
**Impact**: Complete separation of file I/O from spawner logic  
**Benefit**: Cleaner architecture, better maintainability

