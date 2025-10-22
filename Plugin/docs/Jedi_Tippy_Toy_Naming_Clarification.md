# Jedi Tippy Toy Naming Convention - Clarification

## Overview
The **Jedi Tippy Toy** mod has an inconsistent naming convention between its package name and its display name. This document clarifies the correct usage.

## Correct Naming

### Package/Technical Names (DO NOT CHANGE)
These are hardcoded in the mod itself and on Thunderstore:
- **Package Name**: `Jeditippytoy` (lowercase, no spaces)
- **GUID**: `PutterMyBancakes.Jeditippytoy`  
- **Thunderstore URL**: `https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/`
- **manifest.json dependency**: `"PutterMyBancakes-Jeditippytoy-1.0.1"`

### User-Facing Names (SHOULD BE "Jedi")
These are what users see in logs, comments, and documentation:
- **Display Name**: `Jedi Tippy Toy` ? (correct spelling)
- **Comments/Logs**: `Jedi Tippy Toy` ?
- **Documentation**: `Jedi Tippy Toy` ?

## Current Status

The codebase currently uses "**Jedit**" (incorrect spelling) in many user-facing areas. This should be "**Jedi**" for clarity.

### What Needs to Change

#### ? Keep As-Is (Technical)
```csharp
// These MUST remain "Jedit" to match the package
private const string JEDIT_TIPPY_TOY_GUID = "PutterMyBancakes.Jeditippytoy";
availableDependencies["JeditTippyToy"] = IsJediTippyToyAvailable;  // Internal key
```

#### ? Should Be "Jedi" (User-Facing)
```csharp
// Property names and method names
public static bool IsJediTippyToyAvailable { get; private set; }  // Change from "Jedit"
public static bool ValidateJediTippyToy()  // Change from "Jedit"
public static string GetJediToyObjectID()  // Change from "Jedit"

// Log messages
logger.LogInfo("[OptionalDependencies] Jedi Tippy Toy detected via BepInEx");
logger.LogError("Jedi Tippy Toy mod not detected!");
logger.LogInfo($"Successfully spawned Jedi Tippy Toy (ID: {jediToyID})");

// Comments
/// <summary>
/// Check if Jedi Tippy Toy mod is available
/// </summary>

// Method names
public void SpawnJediToy()  // Change from "SpawnJeditToy"
```

## Files That Need Updates

### Code Files
1. **src/OptionalDependencyManager.cs**
   - Properties: `IsJeditTippyToyAvailable` ? `IsJediTippyToyAvailable`
   - Methods: `DetectJeditTippyToy()` ? `DetectJediTippyToy()`
   - Methods: `IsJeditToySpawnable()` ? `IsJediToySpawnable()`
   - Methods: `GetJeditToyObjectID()` ? `GetJediToyObjectID()`
   - Methods: `ValidateJeditTippyToy()` ? `ValidateJediTippyToy()`
   - All log messages and comments

2. **src/SpawnManager.cs**
   - Method: `SpawnJeditToy()` ? `SpawnJediToy()`
   - Variable: `jeditToyID` ? `jediToyID`
   - All log messages and comments

3. **src/InputHandler.cs**
   - Keybind reference: `"JeditToy"` ? `"JediToy"`
   - Method call: `SpawnJeditToy()` ? `SpawnJediToy()`

### Documentation Files
1. **docs/Jedit_Tippy_Toy_Integration_Guide.md**
   - Rename to: `docs/Jedi_Tippy_Toy_Integration_Guide.md`
   - Update all internal references

2. **docs/Jedit_Tippy_Toy_Integration_Summary.md**
   - Rename to: `docs/Jedi_Tippy_Toy_Integration_Summary.md`
   - Update all internal references

### Configuration Files
- No changes needed (doesn't reference the mod name directly)

## Why This Matters

### User Experience
- **Clarity**: "Jedi" is the correct Star Wars term (Jedi Knight)
- **Consistency**: Matches the mod's purpose (lightsaber toy)
- **Professionalism**: Shows attention to detail

### Technical Correctness
- **Package Name**: Must stay "Jeditippytoy" to match Thunderstore
- **GUID**: Must stay "PutterMyBancakes.Jeditippytoy" for detection
- **Detection**: Uses GUID/package name internally
- **Display**: Should use proper spelling "Jedi" for users

## Migration Plan

### Phase 1: Update Property and Method Names (Breaking Changes)
```csharp
// OLD (incorrect)
public static bool IsJeditTippyToyAvailable
public static bool ValidateJeditTippyToy()
public void SpawnJeditToy()

// NEW (correct)
public static bool IsJediTippyToyAvailable
public static bool ValidateJediTippyToy()
public void SpawnJediToy()
```

### Phase 2: Update All Log Messages
```csharp
// OLD
logger.LogInfo("[OptionalDependencies] Jedit Tippy Toy detected");

// NEW
logger.LogInfo("[OptionalDependencies] Jedi Tippy Toy detected");
```

### Phase 3: Update Documentation
- Rename documentation files
- Update all references to "Jedit" ? "Jedi"
- Keep technical references to package name as-is

### Phase 4: Update Comments and XML Documentation
```csharp
// OLD
/// <summary>
/// Check if Jedit Tippy Toy mod is available
/// </summary>

// NEW
/// <summary>
/// Check if Jedi Tippy Toy mod is available
/// </summary>
```

## Testing Checklist

After migration, verify:
- [ ] Mod still detects "Jeditippytoy" package ?
- [ ] GUID detection works (`PutterMyBancakes.Jeditippytoy`) ?
- [ ] Thunderstore link is correct ?
- [ ] User-facing logs say "Jedi Tippy Toy" ?
- [ ] Method calls compile successfully ?
- [ ] Keybinds work correctly ?
- [ ] Documentation is consistent ?

## Summary

**Rule of Thumb**:
- **Technical/Internal**: Use "Jedit" (package name)
- **User-Facing**: Use "Jedi" (correct spelling)

**Exception**: 
- The GUID constant `JEDIT_TIPPY_TOY_GUID` and detection code must use the exact package name to work.

---

**Status**: Documentation Complete - Ready for Implementation  
**Impact**: Medium - Method name changes require updates to callers  
**Risk**: Low - Only affects cosmetic display, not functionality
