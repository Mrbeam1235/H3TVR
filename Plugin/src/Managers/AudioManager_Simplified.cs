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
        // Audio settings (hardcoded defaults - [Audio] config section removed)
        private const bool enableAudioEffects = true;
        private const float masterVolume = 1.0f;
        private const float effectsVolume = 0.8f;
        private const float weaponSoundsVolume = 0.9f;
        private const float ambientSoundsVolume = 0.6f;

        private const bool enableSpatialAudio = true;
        private const bool enable3DAudio = true;
        private const float maxAudioDistance = 50f;
        private const int maxSimultaneousSounds = 10;

        // Per-effect volumes (hardcoded - [Audio.Effects] config section removed)
        private const float shurikenVolume = 0.8f;
        private const float hydrationVolume = 0.7f;
        private const float slomoVolume = 0.9f;
        private const float dangerCloseVolume = 1.0f;
        private const float skittySubGunVolume = 0.8f;
        private const float destroyQuickbeltVolume = 0.6f;
        private const float wondertoyVolume = 0.7f;
        private const float jeditoyVolume = 0.7f;
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            if (isInitialized) return;

            plugin = pluginInstance;
            logger = logSource;
            
            try
            {
                SetupAudioFolders();
                LoadCustomPathsConfig();
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
            // Custom directory config removed - only the H3TVR_Audio folder and its subdirectories are scanned
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
  - jeditoy_spawn.*

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
                { "jeditoy", "jeditoy_spawn" },
                
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
            if (!enableAudioEffects) return;
            
            string soundKey = action == "throw" ? "shuriken" : $"shuriken_{action}";
            float volume = customVolume >= 0 ? customVolume : shurikenVolume * effectsVolume * masterVolume;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayHydrationSound(string action = "drink", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;
            
            string soundKey = action == "drink" ? "hydration" : $"hydration_{action}";
            float volume = customVolume >= 0 ? customVolume : hydrationVolume * effectsVolume * masterVolume;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlaySlomoSound(string phase = "start", Vector3 position = default, bool is3D = false, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;
            
            string soundKey = $"slomo_{phase}";
            float volume = customVolume >= 0 ? customVolume : slomoVolume * ambientSoundsVolume * masterVolume;
            float pitch = phase == "active" ? Time.timeScale : DEFAULT_PITCH;
            
            PlayEffect(soundKey, position, is3D, volume, pitch, customFilePath);
        }

        public void PlayDangerCloseSound(string type = "danger_close", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;
            
            float volume = customVolume >= 0 ? customVolume : dangerCloseVolume * effectsVolume * masterVolume;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayWeaponSpawnSound(string type = "skitty_sub_gun", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;
            
            float volume = customVolume >= 0 ? customVolume : skittySubGunVolume * weaponSoundsVolume * masterVolume;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayDestructionSound(string type = "destroy_quickbelt", Vector3 position = default, bool is3D = false, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;
            
            float volume = customVolume >= 0 ? customVolume : destroyQuickbeltVolume * effectsVolume * masterVolume;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayWondertoySound(string action = "spawn", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;
            
            string soundKey = action == "spawn" ? "wondertoy" : $"wondertoy_{action}";
            float volume = customVolume >= 0 ? customVolume : wondertoyVolume * effectsVolume * masterVolume;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayJeditoySound(string action = "spawn", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;

            string soundKey = action == "spawn" ? "jeditoy" : $"jeditoy_{action}";
            float volume = customVolume >= 0 ? customVolume : jeditoyVolume * effectsVolume * masterVolume;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayUISound(string type = "confirm", Vector3 position = default, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects) return;
            
            string soundKey = $"ui_{type}";
            float volume = customVolume >= 0 ? customVolume : effectsVolume * masterVolume * 0.5f;
            PlayEffect(soundKey, position, false, volume, DEFAULT_PITCH, customFilePath);
        }

        public void PlayStovepipeSound(string action, Vector3 position, bool is3D = true, string customSound = null, float volume = 1.0f)
        {
            if (!isInitialized || !enableAudioEffects) return;

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
            if (!enableAudioEffects || !isInitialized) return;
            if (string.IsNullOrEmpty(effectKey)) return;

            volume = Mathf.Clamp(volume, 0f, 2f);
            pitch = Mathf.Clamp(pitch, 0.1f, 10f);

            if (activeSources.Count >= maxSimultaneousSounds)
            {
                CleanupFinishedSources();
                if (activeSources.Count >= maxSimultaneousSounds) return;
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
            
            if (is3D && enableSpatialAudio)
            {
                source.spatialBlend = enable3DAudio ? SPATIAL_BLEND_3D : 0.5f;
                source.maxDistance = maxAudioDistance;
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
            if (!enableAudioEffects || !isInitialized) return false;
            if (!HasEffect(effectName)) return false;

            float finalVolume = volume * effectsVolume * masterVolume;
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
            logger.LogInfo($"  Enabled: {enableAudioEffects}");
            logger.LogInfo($"  Master Volume: {masterVolume}");
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
