# H3TVR Error Fix - Quick Reference

## What Was Fixed?
Three initialization errors that caused crashes when H3VR systems weren't ready yet:
1. ? Jedit Tippy Toy detection error
2. ? Template cache build error  
3. ? Armor loading crash

## Do I Need to Do Anything?
**No!** The errors are now fixed automatically. Just install and play.

## What Changed for Users?

### Before (Had Errors):
```
[Error] Error checking Jedit Tippy Toy availability: Object reference not set...
[Error] Cannot build template cache - IM.Instance or odicSosigObjsByID is null
[Error] Failed to load armor: Object reference not set...
```

### After (Clean Startup):
```
[Info] H3TVR Enhanced Edition is loading...
[Info] Jedit Tippy Toy: ? Available
[Info] Template cache built: 6/6 templates loaded
[Info] Loaded 150 armor pieces from ItemManager
[Info] H3TVR Enhanced Edition loaded successfully!
```

## Troubleshooting

### If You Still See Warnings (Not Errors):
Some warnings during startup are **normal** and expected:

```
[Warning] IM.OD not initialized yet during detection
[Warning] Cannot build template cache - H3VR not ready
```

These just mean H3TVR is waiting for H3VR to finish loading. The plugin will retry automatically after a few seconds.

### If Something Doesn't Work:

**Chat Sosigs not spawning?**
- Wait 5-10 seconds after map loads for H3VR to fully initialize
- Check console for "Template cache built" message
- Try spawning again with P or O keys

**Armor not applying?**
- Wait for "Delayed armor system initialization completed" message
- Open armor menu with F6 to check status
- Check if armor pieces were loaded in console

**Jedit Tippy Toy not spawning?**
- Make sure the mod is installed: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/
- Check console for "Jedit Tippy Toy: ? Available" message
- Press Keypad2 to spawn

## Expected Startup Sequence
```
1. [0-1s]  H3TVR starts loading
2. [1-2s]  Detecting optional dependencies
3. [2-3s]  Initializing sosig spawner
4. [3-5s]  Building template cache (may retry)
5. [5-7s]  Loading armor assets (may retry)
6. [7s]    "H3TVR Enhanced Edition loaded successfully!"
```

Total startup time: **~7 seconds**

## Support
If you still encounter errors after these fixes:
1. Check you have the latest version
2. Verify BepInEx is up to date
3. Check for mod conflicts
4. Share full console log for help

---

**These fixes ensure H3TVR works reliably even when H3VR takes time to initialize!**
