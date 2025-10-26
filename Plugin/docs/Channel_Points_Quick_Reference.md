# Channel Points Setup - Quick Reference Card

## ?? Quick Setup (3 Steps)

### 1. Configure File Paths
```ini
[Chat Watcher - File Mode]
AllyChatFilePath = C:\StreamFiles\ally_redeem.txt
EnemyChatFilePath = C:\StreamFiles\enemy_redeem.txt
ClearFileAfterRead = true
```

### 2. Set Up Channel Point Reward
- **Reward Name:** "Spawn Ally Sosig"
- **Cost:** Your choice (100-500 points recommended)
- **Action:** Write to file: `C:\StreamFiles\ally_redeem.txt`
- **Content:** `username={user}` or just `{user}`

### 3. Test It!
Write `TestUser` to your file and watch for spawn!

---

## ?? File Format Options

| Format | Example | Use Case |
|--------|---------|----------|
| **Plain** | `ViewerName` | Simplest, works everywhere |
| **Key=Value** | `username=ViewerName` | Recommended for clarity |
| **First Word** | `ViewerName redeemed!` | Automatic extraction |

---

## ?? Supported Keys

All of these work:
```
username=ViewerName
user=ViewerName
redeemer=ViewerName
name=ViewerName
viewer=ViewerName
chatter=ViewerName
```

---

## ?? Common Configurations

### Streamer.bot Action
```
Write to File: C:\StreamFiles\ally_redeem.txt
Content: username={user}
```

### OBS Browser Source Script
```javascript
fetch('file:///C:/StreamFiles/ally_redeem.txt', {
  method: 'PUT',
  body: 'username=' + redeemer
});
```

### PowerShell (Direct Write)
```powershell
"username=$env:TWITCH_USER" | Set-Content "C:\StreamFiles\ally_redeem.txt"
```

---

## ? Verification Checklist

- [ ] File path configured in BepInEx config
- [ ] Channel point reward created
- [ ] File write action set up
- [ ] Test file write works
- [ ] BepInEx console shows "Channel Point Redemption: Spawned ally for..."

---

## ?? Quick Troubleshooting

| Problem | Solution |
|---------|----------|
| No spawn | Check file path in config |
| Duplicate spawns | Enable `ClearFileAfterRead = true` |
| Wrong username | Check file format matches examples |
| Spawn delayed | Adjust `FileCheckInterval` to 0.25 |

---

## ?? Example Working Files

### Ally Redemption File (ally_redeem.txt)
```
username=CoolViewer123
```

### Enemy Redemption File (enemy_redeem.txt)
```
user=EvilViewer456
```

---

## ?? In-Game Controls

| Key | Action |
|-----|--------|
| **P** | Manual ally spawn |
| **O** | Manual enemy spawn |
| **Delete** | Clear all sosigs |

---

## ?? Pro Tips

1. **Use absolute paths** - More reliable than relative
2. **Enable file clearing** - Prevents duplicate spawns
3. **Test with manual key** - Verify system works before channel points
4. **Check BepInEx console** - Shows exactly what's happening
5. **Keep file simple** - One username per redemption works best

---

## ?? Quick Support

**Log Location:** `BepInEx/LogOutput.log`

**Look for:**
```
[Info] Chat Watcher initialized (Channel Points ready)
[Info] Extracted username: 'ViewerName'
[Info] Channel Point Redemption: Spawned ally for ViewerName
```

**If you see these messages, it's working perfectly! ??**
