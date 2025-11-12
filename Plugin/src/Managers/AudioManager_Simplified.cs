using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using FistVR;
using BepInEx.Logging;
using BepInEx.Configuration;

namespace H3TVR
{
    /// <summary>
    /// AudioManager - Simple audio system for H3TVR Enhanced Edition
    /// One sound file per effect - fully configurable through BepInEx config
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Constants
        private const string AUDIO_FOLDER = "H3TVR_Audio";
        private const string AUDIO_PATHS_CONFIG = "H3TVR_AudioPaths.ini";
        private const float DEFAULT_VOLUME = 0.7f;
        private const float DEFAULT_PITCH = 1.0f;
        private const float SPATIAL_BLEND_2D = 0.0f;
        private const float SPATIAL_BLEND_3D = 1.0f;
        private const int CLEANUP_FRAME_THRESHOLD = 5;
        private const float SYNC_LOAD_TIMEOUT = 30f;
        #endregion

        #region Core Fields
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private bool isInitialized = false;
        private string audioFolderPath;
        private string audioPathsConfigFile;
        
        // Single file per effect - can be ANYWHERE on computer
        private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioSource> activeSources = new Dictionary<string, AudioSource>();
        private Dictionary<string, string> effectNameToFile = new Dictionary<string, string>(); // Maps effect key to actual file path (can be anywhere!)
        private List<string> audioSearchPaths = new List<string>(); // All directories to search
        #endregion

        #region Configuration
        private ConfigEntry<bool> enableAudioEffects;
        private ConfigEntry<float> masterVolume;
        private ConfigEntry<float> effectsVolume;
        private ConfigEntry<float> weaponSoundsVolume;
        private ConfigEntry<float> ambientSoundsVolume;
        
        private ConfigEntry<bool> enableSpatialAudio;
        private ConfigEntry<bool> enable3DAudio;
        private ConfigEntry<float> maxAudioDistance;
        private ConfigEntry<int> maxSimultaneousSounds;
        
        private ConfigEntry<float> shurikenVolume;
        private ConfigEntry<float> hydrationVolume;
        private ConfigEntry<float> slomoVolume;
        private ConfigEntry<float> dangerCloseVolume;
        private ConfigEntry<float> skittySubGunVolume;
        private ConfigEntry<float> destroyQuickbeltVolume;
        private ConfigEntry<float> wondertoyVolume;
        
        // Custom audio path configurations
        private ConfigEntry<string> customAudioDirectory1;
        private ConfigEntry<string> customAudioDirectory2;
        private ConfigEntry<string> customAudioDirectory3;
        
        // Individual audio file path configurations - FULL CONTROL
        private ConfigEntry<string> shurikenThrowPath;
        private ConfigEntry<string> shurikenSpawnPath;
        private ConfigEntry<string> hydrationDrinkPath;
        private ConfigEntry<string> hydrationSpawnPath;
        private ConfigEntry<string> slomoStartPath;
        private ConfigEntry<string> slomoEndPath;
        private ConfigEntry<string> slomoActivePath;
        private ConfigEntry<string> dangerClosePath;
        private ConfigEntry<string> explosionPath;
        private ConfigEntry<string> gunSpawnPath;
        private ConfigEntry<string> destroyQuickbeltPath;
        private ConfigEntry<string> itemDestroyPath;
        private ConfigEntry<string> wondertoySpawnPath;
        private ConfigEntry<string> wondertoyActivatePath;
        private ConfigEntry<string> uiConfirmPath;
        private ConfigEntry<string> uiErrorPath;
        private ConfigEntry<string> systemReadyPath;
        
        // Stovepipe paths
        private ConfigEntry<string> stovepipeJamPath;
        private ConfigEntry<string> stovepipeDoubleFeedPath;
        private ConfigEntry<string> stovepipeFailureToFeedPath;
        private ConfigEntry<string> stovepipeFailureToEjectPath;
        private ConfigEntry<string> stovepipeFailureToFirePath;
        private ConfigEntry<string> stovepipeHangFirePath;
        private ConfigEntry<string> stovepipeClearJamPath;
        private ConfigEntry<string> stovepipeCyclingPath;
        private ConfigEntry<string> stovepipeGenericPath;
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            if (isInitialized) return;

            plugin = pluginInstance;
            logger = logSource;
            
            try
            {
                SetupConfiguration();
                SetupAudioFolders();
                LoadCustomPathsConfig();
                LoadConfiguredPaths();
                ScanForAudioFiles();
                LoadAudioClips();
                
                isInitialized = true;
                logger.LogInfo($"[AudioManager] Initialized - Found {effectNameToFile.Count} audio files across {audioSearchPaths.Count} locations");
                
                PlayEffect("system_ready", Vector3.zero, false);
            }
            catch (Exception ex)
            {
                logger.LogError($"[AudioManager] Init failed: {ex.Message}");
            }
        }

        private void SetupConfiguration()
        {
            enableAudioEffects = plugin.Config.Bind("Audio", "EnableAudioEffects", true, "Enable all audio effects");
            masterVolume = plugin.Config.Bind("Audio", "MasterVolume", 1.0f, "Master volume (0.0-1.0)");
            effectsVolume = plugin.Config.Bind("Audio", "EffectsVolume", 0.8f, "Effects volume (0.0-1.0)");
            weaponSoundsVolume = plugin.Config.Bind("Audio", "WeaponSoundsVolume", 0.9f, "Weapon sounds volume (0.0-1.0)");
            ambientSoundsVolume = plugin.Config.Bind("Audio", "AmbientSoundsVolume", 0.6f, "Ambient sounds volume (0.0-1.0)");
            
            enableSpatialAudio = plugin.Config.Bind("Audio", "EnableSpatialAudio", true, "Enable 3D positional audio");
            enable3DAudio = plugin.Config.Bind("Audio", "Enable3DAudio", true, "Enable full 3D audio processing");
            maxAudioDistance = plugin.Config.Bind("Audio", "MaxAudioDistance", 50f, "Max distance for 3D audio");
            maxSimultaneousSounds = plugin.Config.Bind("Audio", "MaxSimultaneousSounds", 10, "Max simultaneous sounds");
            
            shurikenVolume = plugin.Config.Bind("Audio.Effects", "ShurikenVolume", 0.8f, "Shuriken sounds volume");
            hydrationVolume = plugin.Config.Bind("Audio.Effects", "HydrationVolume", 0.7f, "Hydration sounds volume");
            slomoVolume = plugin.Config.Bind("Audio.Effects", "SlomoVolume", 0.9f, "Slomo effects volume");
            dangerCloseVolume = plugin.Config.Bind("Audio.Effects", "DangerCloseVolume", 1.0f, "Danger close volume");
            skittySubGunVolume = plugin.Config.Bind("Audio.Effects", "SkittySubGunVolume", 0.8f, "Weapon spawn volume");
            destroyQuickbeltVolume = plugin.Config.Bind("Audio.Effects", "DestroyQuickbeltVolume", 0.6f, "Destruction volume");
            wondertoyVolume = plugin.Config.Bind("Audio.Effects", "WondertoyVolume", 0.7f, "Wondertoy volume");
            
            // Custom directories - can point ANYWHERE on your computer!
            customAudioDirectory1 = plugin.Config.Bind("Audio.CustomPaths", "CustomDirectory1", "", 
                "Additional audio directory (can be anywhere on your computer, e.g., C:\\My Sounds\\Game Audio)");
            customAudioDirectory2 = plugin.Config.Bind("Audio.CustomPaths", "CustomDirectory2", "", 
                "Additional audio directory (can be anywhere on your computer)");
            customAudioDirectory3 = plugin.Config.Bind("Audio.CustomPaths", "CustomDirectory3", "", 
                "Additional audio directory (can be anywhere on your computer)");
            
            // Individual file paths - FULL CONTROL FOR EACH EFFECT
            shurikenThrowPath = plugin.Config.Bind("Audio.FilePaths", "ShurikenThrow", "", 
                "Full path to shuriken throw sound (e.g., C:\\My Audio\\shuriken.wav). Leave empty for auto-detection.");
            shurikenSpawnPath = plugin.Config.Bind("Audio.FilePaths", "ShurikenSpawn", "", 
                "Full path to shuriken spawn sound. Leave empty for auto-detection.");
            
            hydrationDrinkPath = plugin.Config.Bind("Audio.FilePaths", "HydrationDrink", "", 
                "Full path to hydration drink sound. Leave empty for auto-detection.");
            hydrationSpawnPath = plugin.Config.Bind("Audio.FilePaths", "HydrationSpawn", "", 
                "Full path to hydration spawn sound. Leave empty for auto-detection.");
            
            slomoStartPath = plugin.Config.Bind("Audio.FilePaths", "SlomoStart", "", 
                "Full path to slomo start sound. Leave empty for auto-detection.");
            slomoEndPath = plugin.Config.Bind("Audio.FilePaths", "SlomoEnd", "", 
                "Full path to slomo end sound. Leave empty for auto-detection.");
            slomoActivePath = plugin.Config.Bind("Audio.FilePaths", "SlomoActive", "", 
                "Full path to slomo active loop sound. Leave empty for auto-detection.");
            
            dangerClosePath = plugin.Config.Bind("Audio.FilePaths", "DangerClose", "", 
                "Full path to danger close sound. Leave empty for auto-detection.");
            explosionPath = plugin.Config.Bind("Audio.FilePaths", "Explosion", "", 
                "Full path to explosion sound. Leave empty for auto-detection.");
            
            gunSpawnPath = plugin.Config.Bind("Audio.FilePaths", "GunSpawn", "", 
                "Full path to gun spawn sound. Leave empty for auto-detection.");
            
            destroyQuickbeltPath = plugin.Config.Bind("Audio.FilePaths", "DestroyQuickbelt", "", 
                "Full path to destroy quickbelt sound. Leave empty for auto-detection.");
            itemDestroyPath = plugin.Config.Bind("Audio.FilePaths", "ItemDestroy", "", 
                "Full path to item destroy sound. Leave empty for auto-detection.");
            
            wondertoySpawnPath = plugin.Config.Bind("Audio.FilePaths", "WondertoySpawn", "", 
                "Full path to wondertoy spawn sound. Leave empty for auto-detection.");
            wondertoyActivatePath = plugin.Config.Bind("Audio.FilePaths", "WondertoyActivate", "", 
                "Full path to wondertoy activate sound. Leave empty for auto-detection.");
            
            uiConfirmPath = plugin.Config.Bind("Audio.FilePaths", "UIConfirm", "", 
                "Full path to UI confirm sound. Leave empty for auto-detection.");
            uiErrorPath = plugin.Config.Bind("Audio.FilePaths", "UIError", "", 
                "Full path to UI error sound. Leave empty for auto-detection.");
            
            systemReadyPath = plugin.Config.Bind("Audio.FilePaths", "SystemReady", "", 
                "Full path to system ready sound. Leave empty for auto-detection.");
            
            // Stovepipe paths
            stovepipeJamPath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "WeaponJam", "", 
                "Full path to weapon jam sound. Leave empty for auto-detection.");
            stovepipeDoubleFeedPath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "DoubleFeed", "", 
                "Full path to double feed sound. Leave empty for auto-detection.");
            stovepipeFailureToFeedPath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "FailureToFeed", "", 
                "Full path to failure to feed sound. Leave empty for auto-detection.");
            stovepipeFailureToEjectPath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "FailureToEject", "", 
                "Full path to failure to eject sound. Leave empty for auto-detection.");
            stovepipeFailureToFirePath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "FailureToFire", "", 
                "Full path to failure to fire sound. Leave empty for auto-detection.");
            stovepipeHangFirePath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "HangFire", "", 
                "Full path to hang fire sound. Leave empty for auto-detection.");
            stovepipeClearJamPath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "ClearJam", "", 
                "Full path to clear jam sound. Leave empty for auto-detection.");
            stovepipeCyclingPath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "Cycling", "", 
                "Full path to cycling sound. Leave empty for auto-detection.");
            stovepipeGenericPath = plugin.Config.Bind("Audio.FilePaths.Stovepipe", "GenericMalfunction", "", 
                "Full path to generic malfunction sound. Leave empty for auto-detection.");
        }

        /// <summary>
        /// Load all configured file paths from BepInEx config
        /// </summary>
        private void LoadConfiguredPaths()
        {
            RegisterConfigPath("shuriken", shurikenThrowPath.Value);
            RegisterConfigPath("shuriken_spawn", shurikenSpawnPath.Value);
            RegisterConfigPath("hydration", hydrationDrinkPath.Value);
            RegisterConfigPath("hydration_spawn", hydrationSpawnPath.Value);
            RegisterConfigPath("slomo_start", slomoStartPath.Value);
            RegisterConfigPath("slomo_end", slomoEndPath.Value);
            RegisterConfigPath("slomo_active", slomoActivePath.Value);
            RegisterConfigPath("danger_close", dangerClosePath.Value);
            RegisterConfigPath("explosion", explosionPath.Value);
            RegisterConfigPath("gun_spawn", gunSpawnPath.Value);
            RegisterConfigPath("skitty_sub_gun", gunSpawnPath.Value); // Same as gun_spawn
            RegisterConfigPath("destroy_quickbelt", destroyQuickbeltPath.Value);
            RegisterConfigPath("item_destroy", itemDestroyPath.Value);
            RegisterConfigPath("wondertoy", wondertoySpawnPath.Value);
            RegisterConfigPath("wondertoy_activate", wondertoyActivatePath.Value);
            RegisterConfigPath("ui_confirm", uiConfirmPath.Value);
            RegisterConfigPath("ui_error", uiErrorPath.Value);
            RegisterConfigPath("system_ready", systemReadyPath.Value);
            
            // Stovepipe
            RegisterConfigPath("stovepipe_jam", stovepipeJamPath.Value);
            RegisterConfigPath("stovepipe_malfunction", stovepipeJamPath.Value);
            RegisterConfigPath("stovepipe_double_feed", stovepipeDoubleFeedPath.Value);
            RegisterConfigPath("stovepipe_failure_to_feed", stovepipeFailureToFeedPath.Value);
            RegisterConfigPath("stovepipe_failure_to_eject", stovepipeFailureToEjectPath.Value);
            RegisterConfigPath("stovepipe_failure_to_fire", stovepipeFailureToFirePath.Value);
            RegisterConfigPath("stovepipe_hang_fire", stovepipeHangFirePath.Value);
            RegisterConfigPath("stovepipe_clear_jam", stovepipeClearJamPath.Value);
            RegisterConfigPath("stovepipe_cycling", stovepipeCyclingPath.Value);
            RegisterConfigPath("stovepipe_generic", stovepipeGenericPath.Value);
        }

        private void RegisterConfigPath(string effectKey, string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            
            if (File.Exists(filePath))
            {
                effectNameToFile[effectKey] = filePath;
                logger.LogInfo($"[AudioManager] Config path registered: {effectKey} -> {filePath}");
            }
            else
            {
                logger.LogWarning($"[AudioManager] Config path not found for {effectKey}: {filePath}");
            }
        }

        private void SetupAudioFolders()
        {
            try
            {
                string pluginFolder = Path.GetDirectoryName(plugin.Info.Location);
                audioFolderPath = Path.Combine(pluginFolder, AUDIO_FOLDER);
                audioPathsConfigFile = Path.Combine(pluginFolder, AUDIO_PATHS_CONFIG);
                
                if (!Directory.Exists(audioFolderPath))
                {
                    Directory.CreateDirectory(audioFolderPath);
                    CreateReadme();
                }
                
                // Add main audio folder
                audioSearchPaths.Add(audioFolderPath);
                
                // Add all subdirectories recursively
                AddSubdirectoriesRecursive(audioFolderPath);
                
                // Add custom directories from config
                AddCustomDirectories();
                
                logger.LogInfo($"[AudioManager] Scanning {audioSearchPaths.Count} directories for audio files");
            }
            catch (Exception ex)
            {
                logger.LogError($"[AudioManager] Folder setup failed: {ex.Message}");
            }
        }

        private void AddCustomDirectories()
        {
            // Add custom directories from BepInEx config
            AddCustomDirectory(customAudioDirectory1.Value);
            AddCustomDirectory(customAudioDirectory2.Value);
            AddCustomDirectory(customAudioDirectory3.Value);
        }

        private void AddCustomDirectory(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            
            try
            {
                if (Directory.Exists(path))
                {
                    if (!audioSearchPaths.Contains(path))
                    {
                        audioSearchPaths.Add(path);
                        AddSubdirectoriesRecursive(path);
                        logger.LogInfo($"[AudioManager] Added custom directory: {path}");
                    }
                }
                else
                {
                    logger.LogWarning($"[AudioManager] Custom directory not found: {path}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[AudioManager] Error adding custom directory {path}: {ex.Message}");
            }
        }

        private void AddSubdirectoriesRecursive(string directory)
        {
            try
            {
                string[] subdirs = Directory.GetDirectories(directory);
                foreach (string subdir in subdirs)
                {
                    if (!audioSearchPaths.Contains(subdir))
                    {
                        audioSearchPaths.Add(subdir);
                    }
                    AddSubdirectoriesRecursive(subdir); // Recursive
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[AudioManager] Could not scan subdirectory: {ex.Message}");
            }
        }

        /// <summary>
        /// Load custom audio paths from INI file
        /// Format: effectName=C:\Full\Path\To\File.wav
        /// </summary>
        private void LoadCustomPathsConfig()
        {
            if (!File.Exists(audioPathsConfigFile))
            {
                CreatePathsConfigTemplate();
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(audioPathsConfigFile);
                int loadedCount = 0;

                foreach (string line in lines)
                {
                    if (line == null || line.Trim().Length == 0 || line.TrimStart().StartsWith("#") || line.TrimStart().StartsWith(";"))
                        continue;

                    string[] parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string effectKey = parts[0].Trim();
                        string filePath = parts[1].Trim();

                        if (File.Exists(filePath))
                        {
                            // Only use INI if not already set by BepInEx config
                            if (!effectNameToFile.ContainsKey(effectKey))
                            {
                                effectNameToFile[effectKey] = filePath;
                                loadedCount++;
                                logger.LogDebug($"[AudioManager] INI path loaded: {effectKey} -> {filePath}");
                            }
                        }
                        else
                        {
                            logger.LogWarning($"[AudioManager] File not found for {effectKey}: {filePath}");
                        }
                    }
                }

                if (loadedCount > 0)
                {
                    logger.LogInfo($"[AudioManager] Loaded {loadedCount} custom audio paths from INI config");
                }
             }
            catch (Exception ex)
            {
                logger.LogError($"[AudioManager] Error loading custom paths config: {ex.Message}");
            }
        }

        private void CreatePathsConfigTemplate()
        {
            string template = @"# H3TVR Enhanced Edition - Custom Audio Paths Configuration
# ============================================================
# 
# NOTE: You can now configure audio paths directly in BepInEx config!
# This INI file is still supported for backwards compatibility.
# BepInEx config paths take priority over this file.
#
# Use this file to point to audio files ANYWHERE on your computer!
# Format: effectName=C:\Full\Path\To\Your\Audio\File.wav
#
# Supported formats: .wav, .ogg, .mp3, .aif, .aiff
#
# EXAMPLES:
# shuriken=C:\Users\YourName\Music\Sound Effects\shuriken_throw.wav
# slomo_start=D:\Game Audio\slomo_start.ogg
# explosion=E:\Downloads\explosion.mp3
#
# Available effect names:
# -----------------------
# Shuriken: shuriken, shuriken_spawn
# Hydration: hydration, hydration_spawn
# Slomo: slomo_start, slomo_end, slomo_active
# Danger Close: danger_close, explosion
# Weapons: gun_spawn, skitty_sub_gun
# Destruction: destroy_quickbelt, item_destroy
# Wondertoy: wondertoy, wondertoy_activate
# UI: ui_confirm, ui_error
# System: system_ready
# Stovepipe: stovepipe_jam, stovepipe_malfunction, stovepipe_double_feed,
#            stovepipe_failure_to_feed, stovepipe_failure_to_eject,
#            stovepipe_failure_to_fire, stovepipe_hang_fire,
#            stovepipe_clear_jam, stovepipe_cycling, stovepipe_generic
#
# YOUR CUSTOM PATHS (uncomment and edit):
# ========================================

# shuriken=C:\Path\To\Your\shuriken_throw.wav
# explosion=C:\Path\To\Your\explosion.wav
# slomo_start=C:\Path\To\Your\slomo_start.ogg

";

            try
            {
                File.WriteAllText(audioPathsConfigFile, template);
                logger.LogInfo($"[AudioManager] Created custom paths config template: {audioPathsConfigFile}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[AudioManager] Could not create paths config template: {ex.Message}");
            }
        }

        private void CreateReadme()
        {
            string readme = @"H3TVR Enhanced Edition - Audio Files
====================================

ULTIMATE FLEXIBILITY - Configure audio files DIRECTLY in BepInEx config!

METHOD 1: BepInEx Config (RECOMMENDED - easiest!)
  Open BepInEx\config\com.h3tvr.improved.cfg
  Find [Audio.FilePaths] section
  Set full paths for each effect:
    ShurikenThrow = C:\My Sounds\shuriken.wav
    Explosion = D:\Downloads\boom.mp3

METHOD 2: Use H3TVR_AudioPaths.ini to point to files anywhere

METHOD 3: Place files in this folder (auto-detected by name)

METHOD 4: Add custom directories in BepInEx config

SUPPORTED FORMATS: .wav, .ogg, .mp3, .aif, .aiff

EFFECT FILE NAMES (case-insensitive, auto-detected):
----------------------------------------------------

SHURIKEN:
  - shuriken_throw.*
  - shuriken_spawn.*

HYDRATION:
  - hydration_drink.*
  - hydration_spawn.*

SLOMO:
  - slomo_start.*
  - slomo_end.*
  - slomo_active.*

DANGER CLOSE:
  - danger_close.*
  - explosion.*

WEAPONS:
  - gun_spawn.*

DESTRUCTION:
  - destroy_quickbelt.*
  - item_destroy.*

WONDERTOY:
  - wondertoy_spawn.*
  - wondertoy_activate.*

UI SOUNDS:
  - ui_confirm.*
  - ui_error.*

SYSTEM:
  - system_ready.*

STOVEPIPE (weapon malfunctions):
  - weapon_jam.*
  - double_feed.*
  - failure_to_feed.*
  - failure_to_eject.*
  - failure_to_fire.*
  - hang_fire.*
  - clear_jam.*
  - cycling.*
  - generic_malfunction.*

PRIORITY ORDER:
--------------
1. BepInEx config paths (highest priority)
2. H3TVR_AudioPaths.ini
3. Auto-detected files in folders
";
            
            try
            {
                File.WriteAllText(Path.Combine(audioFolderPath, "README.txt"), readme);
            }
            catch { }
        }

        private void ScanForAudioFiles()
        {
            // Don't clear - we want to keep custom paths loaded from config
            // effectNameToFile.Clear(); 
            
            // Define all possible effect names (without extensions)
            Dictionary<string, string> effectPatterns = new Dictionary<string, string>
            {
                // Shuriken
                { "shuriken", "shuriken_throw" },
                { "shuriken_spawn", "shuriken_spawn" },
                
                // Hydration
                { "hydration", "hydration_drink" },
                { "hydration_spawn", "hydration_spawn" },
                
                // Slomo
                { "slomo_start", "slomo_start" },
                { "slomo_end", "slomo_end" },
                { "slomo_active", "slomo_active" },
                
                // Danger Close
                { "danger_close", "danger_close" },
                { "explosion", "explosion" },
                
                // Weapons
                { "skitty_sub_gun", "gun_spawn" },
                { "gun_spawn", "gun_spawn" },
                
                // Destruction
                { "destroy_quickbelt", "destroy_quickbelt" },
                { "item_destroy", "item_destroy" },
                
                // Wondertoy
                { "wondertoy", "wondertoy_spawn" },
                { "wondertoy_activate", "wondertoy_activate" },
                
                // UI
                { "ui_confirm", "ui_confirm" },
                { "ui_error", "ui_error" },
                
                // System
                { "system_ready", "system_ready" },
                
                // Stovepipe
                { "stovepipe_jam", "weapon_jam" },
                { "stovepipe_malfunction", "weapon_jam" },
                { "stovepipe_double_feed", "double_feed" },
                { "stovepipe_failure_to_feed", "failure_to_feed" },
                { "stovepipe_failure_to_eject", "failure_to_eject" },
                { "stovepipe_failure_to_fire", "failure_to_fire" },
                { "stovepipe_hang_fire", "hang_fire" },
                { "stovepipe_clear_jam", "clear_jam" },
                { "stovepipe_cycling", "cycling" },
                { "stovepipe_generic", "generic_malfunction" }
            };

            string[] supportedExtensions = { ".wav", ".ogg", ".mp3", ".aif", ".aiff" };

            // Scan all search paths
            foreach (string searchPath in audioSearchPaths)
            {
                if (!Directory.Exists(searchPath)) continue;

                try
                {
                    string[] files = Directory.GetFiles(searchPath);
                    
                    foreach (string filePath in files)
                    {
                        string fileName = Path.GetFileNameWithoutExtension(filePath);
                        string extension = Path.GetExtension(filePath).ToLower();
                        
                        // Check if this is a supported audio file
                        if (Array.IndexOf(supportedExtensions, extension) == -1) continue;
                        
                        // Match against effect patterns (case-insensitive)
                        foreach (var pattern in effectPatterns)
                        {
                            if (fileName.Equals(pattern.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                // Don't overwrite if already found (BepInEx config, INI, or first found wins)
                                if (!effectNameToFile.ContainsKey(pattern.Key))
                                {
                                    effectNameToFile[pattern.Key] = filePath;
                                    logger.LogDebug($"[AudioManager] Auto-detected: {pattern.Key} -> {Path.GetFileName(filePath)}");
                                }
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"[AudioManager] Error scanning {searchPath}: {ex.Message}");
                }
            }
            
            logger.LogInfo($"[AudioManager] Discovered {effectNameToFile.Count} audio files");
        }
        #endregion

        #region Audio Loading
        private void LoadAudioClips()
        {
            foreach (var mapping in effectNameToFile)
            {
                LoadAudioClip(mapping.Value, mapping.Key);
            }
        }

        private void LoadAudioClip(string filePath, string effectKey)
        {
            if (File.Exists(filePath))
            {
                StartCoroutine(LoadAudioClipCoroutine(filePath, effectKey));
            }
        }

        private IEnumerator LoadAudioClipCoroutine(string filePath, string effectKey)
        {
            string url = "file://" + filePath;
            
            using (WWW www = new WWW(url))
            {
                yield return www;
                
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = www.GetAudioClip(false, false, GetAudioType(filePath));
                    if (clip != null)
                    {
                        clip.name = effectKey;
                        audioClips[effectKey] = clip;
                        logger.LogDebug($"[AudioManager] Loaded: {effectKey} from {filePath}");
                    }
                }
                else
                {
                    logger.LogWarning($"[AudioManager] Failed to load {Path.GetFileName(filePath)}: {www.error}");
                }
            }
        }

        private AudioType GetAudioType(string filePath)
        {
            string ext = Path.GetExtension(filePath).ToLower();
            switch (ext)
            {
                case ".wav": return AudioType.WAV;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".mp3": return AudioType.MPEG;
                case ".aif":
                case ".aiff": return AudioType.AIFF;
                default: return AudioType.WAV;
            }
        }
        #endregion

        #region Public APIs
        public void PlayShurikenSound(string action = "throw", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = action == "throw" ? "shuriken" : $"shuriken_{action}";
            float volume = customVolume >= 0 ? customVolume : shurikenVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayHydrationSound(string action = "drink", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = action == "drink" ? "hydration" : $"hydration_{action}";
            float volume = customVolume >= 0 ? customVolume : hydrationVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlaySlomoSound(string phase = "start", Vector3 position = default, bool is3D = false, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = $"slomo_{phase}";
            float volume = customVolume >= 0 ? customVolume : slomoVolume.Value * ambientSoundsVolume.Value * masterVolume.Value;
            float pitch = phase == "active" ? Time.timeScale : DEFAULT_PITCH;
            
            PlayEffect(soundKey, position, is3D, volume, pitch, customFilePath);
        }

        public void PlayDangerCloseSound(string type = "danger_close", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            float volume = customVolume >= 0 ? customVolume : dangerCloseVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayWeaponSpawnSound(string type = "skitty_sub_gun", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            float volume = customVolume >= 0 ? customVolume : skittySubGunVolume.Value * weaponSoundsVolume.Value * masterVolume.Value;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayDestructionSound(string type = "destroy_quickbelt", Vector3 position = default, bool is3D = false, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            float volume = customVolume >= 0 ? customVolume : destroyQuickbeltVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayWondertoySound(string action = "spawn", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = action == "spawn" ? "wondertoy" : $"wondertoy_{action}";
            float volume = customVolume >= 0 ? customVolume : wondertoyVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayUISound(string type = "confirm", Vector3 position = default, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = $"ui_{type}";
            float volume = customVolume >= 0 ? customVolume : effectsVolume.Value * masterVolume.Value * 0.5f;
            PlayEffect(soundKey, position, false, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayStovepipeSound(string action, Vector3 position, bool is3D = true, string customSound = null, float volume = 1.0f)
        {
            if (!isInitialized || !enableAudioEffects.Value) return;

            string soundKey = GetStovepipeSoundKey(action);
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customSound);
        }

        public void PlayStovepipeEffect(Vector3 position, string effectType)
        {
            PlayStovepipeSound(effectType, position, true);
        }

        /// <summary>
        /// Rescan audio folders for new files
        /// </summary>
        public void RescanAudioFiles()
        {
            logger.LogInfo("[AudioManager] Rescanning for audio files...");
            LoadConfiguredPaths();
            LoadCustomPathsConfig();
            ScanForAudioFiles();
            LoadAudioClips();
        }

        /// <summary>
        /// Manually register an audio file from anywhere on your computer
        /// </summary>
        public bool RegisterCustomAudioFile(string effectKey, string absoluteFilePath)
        {
            if (string.IsNullOrEmpty(effectKey) || string.IsNullOrEmpty(absoluteFilePath))
            {
                logger.LogWarning("[AudioManager] Cannot register: empty key or path");
                return false;
            }

            if (!File.Exists(absoluteFilePath))
            {
                logger.LogWarning($"[AudioManager] File not found: {absoluteFilePath}");
                return false;
            }

            effectNameToFile[effectKey] = absoluteFilePath;
            LoadAudioClip(absoluteFilePath, effectKey);
            logger.LogInfo($"[AudioManager] Registered custom audio: {effectKey} -> {absoluteFilePath}");
            return true;
        }
        #endregion

        #region Core Playback
        private void PlayEffect(string effectKey, Vector3 position, bool is3D, float volume = DEFAULT_VOLUME, float pitch = DEFAULT_PITCH, string customFilePath = null)
        {
            if (!enableAudioEffects.Value || !isInitialized) return;
            if (string.IsNullOrEmpty(effectKey)) return;

            volume = Mathf.Clamp(volume, 0f, 2f);
            pitch = Mathf.Clamp(pitch, 0.1f, 10f);

            if (activeSources.Count >= maxSimultaneousSounds.Value)
            {
                CleanupFinishedSources();
                if (activeSources.Count >= maxSimultaneousSounds.Value) return;
            }

            AudioClip clip = GetAudioClip(effectKey, customFilePath);
            if (clip == null) return;

            CreateAndPlayAudioSource(effectKey, clip, position, is3D, volume, pitch);
        }

        private AudioClip GetAudioClip(string effectKey, string customFilePath)
        {
            // Priority 1: Custom file path provided directly
            if (!string.IsNullOrEmpty(customFilePath))
            {
                AudioClip customClip = LoadCustomAudioFileSync(customFilePath);
                if (customClip != null) return customClip;
            }

            // Priority 2: Pre-loaded effect
            if (audioClips.ContainsKey(effectKey))
            {
                return audioClips[effectKey];
            }
            
            return null;
        }

        private void CreateAndPlayAudioSource(string effectKey, AudioClip clip, Vector3 position, bool is3D, float volume, float pitch)
        {
            GameObject audioObject = new GameObject($"H3TVR_Audio_{effectKey}");
            AudioSource source = audioObject.AddComponent<AudioSource>();
            
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3.0f);
            source.playOnAwake = false;
            source.loop = false;
            
            if (is3D && enableSpatialAudio.Value)
            {
                source.spatialBlend = enable3DAudio.Value ? SPATIAL_BLEND_3D : 0.5f;
                source.maxDistance = maxAudioDistance.Value;
                source.rolloffMode = AudioRolloffMode.Logarithmic;
                source.dopplerLevel = 0.1f;
                source.transform.position = position;
            }
            else
            {
                source.spatialBlend = SPATIAL_BLEND_2D;
            }
            
            string sourceKey = $"{effectKey}_{Time.time}_{UnityEngine.Random.Range(1000, 9999)}";
            activeSources[sourceKey] = source;
            
            source.Play();
            StartCoroutine(CleanupAudioSource(sourceKey, clip.length + 0.1f));
        }
        #endregion

        #region Audio Control
        public void StopEffectSounds(string effectKey)
        {
            if (string.IsNullOrEmpty(effectKey)) return;

            List<string> keysToRemove = new List<string>();
            
            foreach (var kvp in activeSources)
            {
                if (kvp.Key.StartsWith(effectKey + "_"))
                {
                    if (kvp.Value != null)
                    {
                        kvp.Value.Stop();
                        if (kvp.Value.gameObject != null)
                        {
                            Destroy(kvp.Value.gameObject);
                        }
                    }
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (string key in keysToRemove)
            {
                activeSources.Remove(key);
            }
        }

        public void StopAllAudio()
        {
            foreach (var kvp in activeSources)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.Stop();
                    if (kvp.Value.gameObject != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                }
            }
            
            activeSources.Clear();
        }
        #endregion

        #region Custom Audio
        public bool LoadCustomAudioFile(string filePath, string effectKey, bool replaceExisting = true)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(effectKey)) return false;
            if (!File.Exists(filePath)) return false;
            if (effectNameToFile.ContainsKey(effectKey) && !replaceExisting) return false;

            StartCoroutine(LoadCustomAudioFileCoroutine(filePath, effectKey));
            return true;
        }

        private IEnumerator LoadCustomAudioFileCoroutine(string filePath, string effectKey)
        {
            string url = "file://" + filePath;
            
            using (WWW www = new WWW(url))
            {
                yield return www;
                
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = www.GetAudioClip(false, false, GetAudioType(filePath));
                    if (clip != null)
                    {
                        clip.name = effectKey;
                        audioClips[effectKey] = clip;
                        effectNameToFile[effectKey] = filePath;
                        
                        logger.LogInfo($"[AudioManager] Loaded custom: {effectKey}");
                    }
                }
            }
        }

        private AudioClip LoadCustomAudioFileSync(string filePath)
        {
            try
            {
                string fullPath = filePath;
                
                // If it's not an absolute path, try to find it in search paths
                if (!Path.IsPathRooted(filePath))
                {
                    fullPath = null;
                    foreach (string searchPath in audioSearchPaths)
                    {
                        string testPath = Path.Combine(searchPath, filePath);
                        if (File.Exists(testPath))
                        {
                            fullPath = testPath;
                            break;
                        }
                    }
                    
                    if (fullPath == null) return null;
                }
                
                if (!File.Exists(fullPath)) return null;

                string cacheKey = $"temp_{Path.GetFileName(fullPath)}_{fullPath.GetHashCode()}";
                if (audioClips.ContainsKey(cacheKey)) return audioClips[cacheKey];

                string url = "file://" + fullPath;
                using (WWW www = new WWW(url))
                {
                    float timeoutTime = Time.realtimeSinceStartup + SYNC_LOAD_TIMEOUT;
                    while (!www.isDone && string.IsNullOrEmpty(www.error))
                    {
                        if (Time.realtimeSinceStartup > timeoutTime) return null;
                        System.Threading.Thread.Sleep(10);
                    }

                    if (string.IsNullOrEmpty(www.error))
                    {
                        AudioClip clip = www.GetAudioClip(false, false, GetAudioType(fullPath));
                        if (clip != null)
                        {
                            clip.name = cacheKey;
                            audioClips[cacheKey] = clip;
                            return clip;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        public bool PlayLoadedEffect(string effectName, Vector3 position = default, bool is3D = true, float volume = 0.8f, float pitch = 1.0f)
        {
            if (!enableAudioEffects.Value || !isInitialized) return false;
            if (!HasEffect(effectName)) return false;

            float finalVolume = volume * effectsVolume.Value * masterVolume.Value;
            PlayEffect(effectName, position, is3D, finalVolume, pitch);
            
            return true;
        }
        #endregion

        #region Utility
        public bool HasEffect(string effectName)
        {
            return !string.IsNullOrEmpty(effectName) && audioClips.ContainsKey(effectName);
        }

        public bool IsInitialized()
        {
            return isInitialized;
        }

        public void LogConfiguration()
        {
            logger.LogInfo("[AudioManager] Configuration:");
            logger.LogInfo($"  Enabled: {enableAudioEffects.Value}");
            logger.LogInfo($"  Master Volume: {masterVolume.Value}");
            logger.LogInfo($"  Loaded Clips: {audioClips.Count}");
            logger.LogInfo($"  Active Sources: {activeSources.Count}");
            logger.LogInfo($"  Search Paths: {audioSearchPaths.Count}");
            logger.LogInfo("  Loaded Effects:");
            foreach (var effect in effectNameToFile)
            {
                logger.LogInfo($"    {effect.Key} -> {effect.Value}");
            }
        }

        private string GetStovepipeSoundKey(string action)
        {
            string normalized = action.ToLower().Replace(" ", "_");
            if (normalized == "jam" || normalized == "malfunction") return "stovepipe_jam";
            return $"stovepipe_{normalized}";
        }
        #endregion

        #region Cleanup
        private void CleanupFinishedSources()
        {
            List<string> keysToRemove = new List<string>();
            
            foreach (var kvp in activeSources)
            {
                if (kvp.Value == null || !kvp.Value.isPlaying)
                {
                    keysToRemove.Add(kvp.Key);
                    if (kvp.Value != null && kvp.Value.gameObject != null)
                    {
                        try
                        {
                            Destroy(kvp.Value.gameObject);
                        }
                        catch { }
                    }
                }
            }
            
            foreach (string key in keysToRemove)
            {
                activeSources.Remove(key);
            }
        }

        private IEnumerator CleanupAudioSource(string sourceKey, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (activeSources.ContainsKey(sourceKey))
            {
                if (activeSources[sourceKey] != null && activeSources[sourceKey].gameObject != null)
                {
                    Destroy(activeSources[sourceKey].gameObject);
                }
                activeSources.Remove(sourceKey);
            }
        }

        private void OnDestroy()
        {
            StopAllAudio();
            
            foreach (var clip in audioClips.Values)
            {
                if (clip != null) Destroy(clip);
            }
            
            audioClips.Clear();
            effectNameToFile.Clear();
            audioSearchPaths.Clear();
            
            logger?.LogInfo("[AudioManager] Destroyed and cleaned up");
        }
        #endregion
    }
}
