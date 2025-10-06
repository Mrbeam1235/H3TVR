# H3TVR Enhanced Edition - Custom Audio Quick Reference

## ?? Quick Start Guide

### Step 1: Locate Audio Folder
```
[H3VR Install]/BepInEx/plugins/H3TVR_Audio/
```

### Step 2: Add Your Audio Files
Drop your `.wav`, `.ogg`, or `.mp3` files into the folder

### Step 3: Configure
Edit: `BepInEx/config/H3TVR_AudioConfig.ini`

### Step 4: Restart H3VR
Your custom audio is now active!

---

## ?? Configuration Format

### Single File
```ini
ExplosionSounds=my_explosion.wav
```

### Multiple Files (Random Selection)
```ini
ExplosionSounds=boom1.wav,boom2.wav,boom3.wav
```

### With Subfolders
```ini
ExplosionSounds=explosions/large.wav,explosions/medium.wav
```

### Absolute Path
```ini
ExplosionSounds=C:\MyAudio\explosion.wav
```

---

## ?? All Effect Configuration Keys

### Combat Effects
| What | Config Key | Example |
|------|-----------|---------|
| Shuriken throw | `ShurikenThrowSounds` | `shuriken.wav` |
| Shuriken spawn | `ShurikenSpawnSounds` | `spawn.wav` |
| Explosions | `ExplosionSounds` | `boom1.wav,boom2.wav` |
| Danger close | `DangerCloseSounds` | `warning.wav` |

### Time Effects
| What | Config Key | Example |
|------|-----------|---------|
| Slomo start | `SlomoStartSounds` | `slow_start.wav` |
| Slomo end | `SlomoEndSounds` | `slow_end.wav` |
| Slomo active | `SlomoActiveSounds` | `ambient.wav` |

### Items
| What | Config Key | Example |
|------|-----------|---------|
| Drink water | `HydrationDrinkSounds` | `drink.wav` |
| Water spawn | `HydrationSpawnSounds` | `bottle_spawn.wav` |
| Gun spawn | `SkittySubGunSounds` | `gun_materialize.wav` |
| Wondertoy | `WondertoySpawnSounds` | `toy.wav` |
| Destroy items | `DestroyQuickbeltSounds` | `destroy.wav` |

### UI
| What | Config Key | Example |
|------|-----------|---------|
| Confirm | `UIConfirmSounds` | `beep.wav` |
| Error | `UIErrorSounds` | `error.wav` |
| System ready | `SystemReadySounds` | `startup.wav` |

### Weapon Malfunctions (Stovepipe)
| What | Config Key | Example |
|------|-----------|---------|
| Jam | `StovepipeJamSounds` | `jam.wav` |
| Double feed | `StovepipeDoubleFeedSounds` | `double_feed.wav` |
| Fail to feed | `StovepipeFailureToFeedSounds` | `ftf.wav` |
| Fail to eject | `StovepipeFailureToEjectSounds` | `fte.wav` |
| Fail to fire | `StovepipeFailureToFireSounds` | `click.wav` |
| Hang fire | `StovepipeHangFireSounds` | `hangfire.wav` |
| Clear jam | `StovepipeClearJamSounds` | `clear.wav` |
| Cycling | `StovepipeCyclingSounds` | `cycle.wav` |

---

## ??? Volume Controls

```ini
[Volume Levels]
MasterVolume=1.0          # Overall volume (0.0-1.0)
EffectsVolume=0.8         # Effects category
WeaponSoundsVolume=0.9    # Weapons category
AmbientSoundsVolume=0.6   # Ambient category

# Individual effects
ShurikenVolume=0.8
HydrationVolume=0.7
SlomoVolume=0.9
DangerCloseVolume=1.0
SkittySubGunVolume=0.8
DestroyQuickbeltVolume=0.6
WondertoyVolume=0.7
```

---

## ? Best Practices

### Audio File Specs
- **Format**: WAV (recommended), OGG, MP3
- **Sample Rate**: 44100 Hz or 48000 Hz
- **Bit Depth**: 16-bit
- **Duration**: 0.5-3 seconds for most effects
- **Volume**: Peak at -6 dB, average at -12 dB

### File Organization
```
H3TVR_Audio/
??? explosions/
?   ??? boom1.wav
?   ??? boom2.wav
?   ??? boom3.wav
??? weapons/
?   ??? spawn.wav
?   ??? ready.wav
??? effects/
    ??? slomo_start.wav
    ??? slomo_end.wav
```

### Configuration
```ini
ExplosionSounds=explosions/boom1.wav,explosions/boom2.wav,explosions/boom3.wav
SkittySubGunSounds=weapons/spawn.wav
SlomoStartSounds=effects/slomo_start.wav
```

---

## ?? Troubleshooting

### Sound Not Playing?
1. ? File exists in `H3TVR_Audio` folder
2. ? File name matches config exactly
3. ? Format is supported (WAV, OGG, MP3)
4. ? No typos in config file
5. ? Restarted H3VR after changes

### Still Not Working?
Check BepInEx logs:
```
BepInEx/LogOutput.log
```
Search for `[AudioManager]` entries

### Volume Issues?
Adjust volumes in config:
```ini
MasterVolume=0.5      # Lower overall volume
ExplosionVolume=0.3   # Lower specific effect
```

---

## ?? Example Audio Pack

### Sci-Fi Theme
```ini
[Custom Audio Files]
ExplosionSounds=scifi/energy_blast1.wav,scifi/energy_blast2.wav
SkittySubGunSounds=scifi/weapon_materialize.wav
SlomoStartSounds=scifi/time_warp.wav
SlomoEndSounds=scifi/time_normal.wav
UIConfirmSounds=scifi/beep_confirm.wav
UIErrorSounds=scifi/beep_error.wav
```

### Horror Theme
```ini
[Custom Audio Files]
ExplosionSounds=horror/thunder1.wav,horror/thunder2.wav
SkittySubGunSounds=horror/creepy_spawn.wav
SlomoStartSounds=horror/distortion.wav
UIConfirmSounds=horror/eerie_click.wav
UIErrorSounds=horror/scream.wav
```

### Comedy Theme
```ini
[Custom Audio Files]
ExplosionSounds=comedy/cartoon_boom.wav,comedy/spring_boing.wav
SkittySubGunSounds=comedy/pop.wav
SlomoStartSounds=comedy/record_scratch.wav
UIConfirmSounds=comedy/ding.wav
UIErrorSounds=comedy/sad_trombone.wav
```

---

## ?? Pro Tips

1. **Variety**: Use 3-5 files per effect for variety
2. **Consistency**: Keep volumes similar across files
3. **Testing**: Test in-game after each change
4. **Backup**: Save your config before major changes
5. **Share**: Create audio packs for the community!

---

## ?? More Information

For detailed documentation, see:
- `docs/Customizable_Audio_System_Guide.md` - Complete guide
- `docs/AudioManager_Documentation.md` - Technical reference
- `docs/Custom_Audio_Examples.md` - Code examples

---

**Make H3TVR sound exactly how you want it!** ??
