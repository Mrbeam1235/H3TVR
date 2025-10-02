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
    /// AudioManager - Handles all sound effects for H3TVR Enhanced Edition
    /// Manages audio playback for shuriken, hydration, slomo, danger close, skitty sub guns, destroy quickbelt, and wondertoy
    /// Supports both streamed and loaded audio with volume, pitch, and spatial audio controls
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Constants and Configuration
        private const string AUDIO_FOLDER = "H3TVR_Audio";
        private const float DEFAULT_VOLUME = 0.7f;
        private const float DEFAULT_PITCH = 1.0f;
        private const float SPATIAL_BLEND_2D = 0.0f;
        private const float SPATIAL_BLEND_3D = 1.0f;
        #endregion

        #region Private Fields
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();
        private Dictionary<string, AudioSource> activeSources = new Dictionary<string, AudioSource>();
        private bool isInitialized = false;
        private string audioFolderPath;
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
        
        // Individual effect volumes
        private ConfigEntry<float> shurikenVolume;
        private ConfigEntry<float> hydrationVolume;
        private ConfigEntry<float> slomoVolume;
        private ConfigEntry<float> dangerCloseVolume;
        private ConfigEntry<float> skittySubGunVolume;
        private ConfigEntry<float> destroyQuickbeltVolume;
        private ConfigEntry<float> wondertoyVolume;
        #endregion

        #region Audio File Mappings
        private readonly Dictionary<string, string[]> audioFileMapping = new Dictionary<string, string[]>
        {
            // Shuriken sounds - throwing, impact, whoosh effects
            ["shuriken"] = new[] { "shuriken_throw.wav", "shuriken_whoosh.wav", "shuriken_impact.wav" },
            ["shuriken_spawn"] = new[] { "shuriken_spawn.wav", "metal_clink.wav" },
            
            // Hydration sounds - drinking, water effects
            ["hydration"] = new[] { "hydration_drink.wav", "water_pour.wav", "bottle_open.wav" },
            ["hydration_spawn"] = new[] { "bottle_spawn.wav", "water_splash.wav" },
            
            // Slomo sounds - time effects, whoosh
            ["slomo_start"] = new[] { "slomo_start.wav", "time_slow.wav", "whoosh_slow.wav" },
            ["slomo_end"] = new[] { "slomo_end.wav", "time_normal.wav", "whoosh_fast.wav" },
            ["slomo_active"] = new[] { "slomo_ambient.wav", "time_distortion.wav" },
            
            // Danger Close sounds - explosions, military effects
            ["danger_close"] = new[] { "danger_close.wav", "explosion_distant.wav", "artillery_incoming.wav" },
            ["explosion"] = new[] { "explosion_large.wav", "explosion_medium.wav", "explosion_small.wav" },
            
            // Skitty Sub Gun sounds - weapon spawn, mechanical
            ["skitty_sub_gun"] = new[] { "gun_spawn.wav", "weapon_materialize.wav", "metal_clank.wav" },
            ["gun_spawn"] = new[] { "gun_appear.wav", "weapon_ready.wav" },
            
            // Destroy Quickbelt sounds - destruction, item removal
            ["destroy_quickbelt"] = new[] { "items_destroy.wav", "quickbelt_clear.wav", "magic_poof.wav" },
            ["item_destroy"] = new[] { "item_vanish.wav", "destroy_sound.wav" },
            
            // Wondertoy sounds - magical, toy-like effects
            ["wondertoy"] = new[] { "wondertoy_spawn.wav", "toy_appear.wav", "magic_sparkle.wav" },
            ["wondertoy_activate"] = new[] { "toy_activate.wav", "magic_chime.wav" },
            
            // General UI and system sounds
            ["ui_confirm"] = new[] { "ui_confirm.wav", "beep_confirm.wav" },
            ["ui_error"] = new[] { "ui_error.wav", "beep_error.wav" },
            ["system_ready"] = new[] { "system_ready.wav", "startup_chime.wav" }
        };
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            if (isInitialized) return;

            plugin = pluginInstance;
            logger = logSource;
            
            SetupConfiguration();
            SetupAudioFolder();
            LoadAudioClips();
            
            isInitialized = true;
            logger.LogInfo("[AudioManager] Audio system initialized successfully");
            
            // Play system ready sound
            PlayEffect("system_ready", Vector3.zero, false);
        }

        private void SetupConfiguration()
        {
            // Main audio settings
            enableAudioEffects = plugin.Config.Bind("Audio", "EnableAudioEffects", true, "Enable all audio effects for H3TVR");
            masterVolume = plugin.Config.Bind("Audio", "MasterVolume", 1.0f, "Master volume for all H3TVR audio (0.0 - 1.0)");
            effectsVolume = plugin.Config.Bind("Audio", "EffectsVolume", 0.8f, "Volume for effect sounds (0.0 - 1.0)");
            weaponSoundsVolume = plugin.Config.Bind("Audio", "WeaponSoundsVolume", 0.9f, "Volume for weapon-related sounds (0.0 - 1.0)");
            ambientSoundsVolume = plugin.Config.Bind("Audio", "AmbientSoundsVolume", 0.6f, "Volume for ambient/background sounds (0.0 - 1.0)");
            
            // 3D Audio settings
            enableSpatialAudio = plugin.Config.Bind("Audio", "EnableSpatialAudio", true, "Enable positional 3D audio effects");
            enable3DAudio = plugin.Config.Bind("Audio", "Enable3DAudio", true, "Enable full 3D audio processing");
            maxAudioDistance = plugin.Config.Bind("Audio", "MaxAudioDistance", 50f, "Maximum distance for 3D audio effects");
            maxSimultaneousSounds = plugin.Config.Bind("Audio", "MaxSimultaneousSounds", 10, "Maximum number of simultaneous audio sources");
            
            // Individual effect volumes
            shurikenVolume = plugin.Config.Bind("Audio.Effects", "ShurikenVolume", 0.8f, "Volume for shuriken sounds");
            hydrationVolume = plugin.Config.Bind("Audio.Effects", "HydrationVolume", 0.7f, "Volume for hydration sounds");
            slomoVolume = plugin.Config.Bind("Audio.Effects", "SlomoVolume", 0.9f, "Volume for slomo effects");
            dangerCloseVolume = plugin.Config.Bind("Audio.Effects", "DangerCloseVolume", 1.0f, "Volume for danger close explosions");
            skittySubGunVolume = plugin.Config.Bind("Audio.Effects", "SkittySubGunVolume", 0.8f, "Volume for weapon spawn sounds");
            destroyQuickbeltVolume = plugin.Config.Bind("Audio.Effects", "DestroyQuickbeltVolume", 0.6f, "Volume for item destruction sounds");
            wondertoyVolume = plugin.Config.Bind("Audio.Effects", "WondertoyVolume", 0.7f, "Volume for wondertoy sounds");
        }

        private void SetupAudioFolder()
        {
            string pluginFolder = Path.GetDirectoryName(plugin.Info.Location);
            audioFolderPath = Path.Combine(pluginFolder, AUDIO_FOLDER);
            
            if (!Directory.Exists(audioFolderPath))
            {
                Directory.CreateDirectory(audioFolderPath);
                logger.LogInfo($"[AudioManager] Created audio folder: {audioFolderPath}");
                CreateAudioReadme();
            }
        }

        private void CreateAudioReadme()
        {
            string readmePath = Path.Combine(audioFolderPath, "README.txt");
            string readmeContent = @"H3TVR Enhanced Edition - Audio Files

Place your custom audio files in this folder to override default sounds.
Supported formats: .wav, .ogg, .mp3

Audio File Categories:
===================

SHURIKEN SOUNDS:
- shuriken_throw.wav - Sound when throwing shuriken
- shuriken_whoosh.wav - Shuriken flying through air
- shuriken_impact.wav - Shuriken hitting target
- shuriken_spawn.wav - Shuriken appearing/spawning

HYDRATION SOUNDS:
- hydration_drink.wav - Drinking sound
- water_pour.wav - Water pouring sound
- bottle_open.wav - Opening bottle sound
- bottle_spawn.wav - Bottle spawning sound

SLOMO SOUNDS:
- slomo_start.wav - Time slowdown beginning
- slomo_end.wav - Time returning to normal
- slomo_ambient.wav - Background sound during slomo
- time_distortion.wav - Time effect sound

DANGER CLOSE SOUNDS:
- danger_close.wav - Danger close warning
- explosion_large.wav - Large explosion
- explosion_medium.wav - Medium explosion
- explosion_small.wav - Small explosion
- artillery_incoming.wav - Incoming artillery sound

WEAPON SOUNDS:
- gun_spawn.wav - Gun spawning sound
- weapon_materialize.wav - Weapon appearing
- weapon_ready.wav - Weapon ready sound

DESTRUCTION SOUNDS:
- items_destroy.wav - Items being destroyed
- quickbelt_clear.wav - Quickbelt clearing sound
- item_vanish.wav - Single item vanishing

WONDERTOY SOUNDS:
- wondertoy_spawn.wav - Wondertoy appearing
- toy_activate.wav - Wondertoy activation
- magic_sparkle.wav - Magical effect sound

SYSTEM SOUNDS:
- ui_confirm.wav - UI confirmation
- ui_error.wav - UI error sound
- system_ready.wav - System startup sound

Notes:
- Files should be relatively short (under 10 seconds for most effects)
- Use moderate volumes in your audio files (H3TVR has volume controls)
- 3D positional audio is supported for most effects
";
            
            try
            {
                File.WriteAllText(readmePath, readmeContent);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[AudioManager] Could not create README: {ex.Message}");
            }
        }

        private void LoadAudioClips()
        {
            foreach (var category in audioFileMapping)
            {
                foreach (string fileName in category.Value)
                {
                    LoadAudioClip(fileName, category.Key);
                }
            }
            
            logger.LogInfo($"[AudioManager] Loaded {audioClips.Count} audio clips");
        }

        private void LoadAudioClip(string fileName, string category)
        {
            string filePath = Path.Combine(audioFolderPath, fileName);
            
            if (File.Exists(filePath))
            {
                StartCoroutine(LoadAudioClipCoroutine(filePath, fileName, category));
            }
            else
            {
                // Create a silent placeholder or use a default sound
                logger.LogDebug($"[AudioManager] Audio file not found: {fileName} (this is normal if using default sounds)");
            }
        }

        private IEnumerator LoadAudioClipCoroutine(string filePath, string fileName, string category)
        {
            string url = "file://" + filePath;
            
            // Use WWW class for .NET Framework 3.5 compatibility
            using (WWW www = new WWW(url))
            {
                yield return www;
                
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = www.GetAudioClip(false, false, GetAudioType(filePath));
                    if (clip != null)
                    {
                        clip.name = fileName;
                        audioClips[fileName] = clip;
                        logger.LogDebug($"[AudioManager] Loaded audio clip: {fileName}");
                    }
                    else
                    {
                        logger.LogWarning($"[AudioManager] Audio clip is null for file: {fileName}");
                    }
                }
                else
                {
                    logger.LogWarning($"[AudioManager] Failed to load audio file {fileName}: {www.error}");
                }
            }
        }

        private AudioType GetAudioType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".wav": return AudioType.WAV;
                case ".ogg": return AudioType.OGGVORBIS;
                case ".mp3": return AudioType.MPEG;
                case ".aif":
                case ".aiff": return AudioType.AIFF;
                case ".mod": return AudioType.MOD;
                case ".it": return AudioType.IT;
                case ".s3m": return AudioType.S3M;
                case ".xm": return AudioType.XM;
                default: 
                    logger.LogWarning($"[AudioManager] Unknown audio format: {extension}, defaulting to WAV");
                    return AudioType.WAV;
            }
        }
        #endregion

        #region Public API - Effect Sounds
        
        /// <summary>
        /// Play shuriken-related sound effects
        /// </summary>
        public void PlayShurikenSound(string action = "throw", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = action == "throw" ? "shuriken" : $"shuriken_{action}";
            float volume = customVolume >= 0 ? customVolume : shurikenVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        /// <summary>
        /// Play hydration-related sound effects
        /// </summary>
        public void PlayHydrationSound(string action = "drink", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = action == "drink" ? "hydration" : $"hydration_{action}";
            float volume = customVolume >= 0 ? customVolume : hydrationVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        /// <summary>
        /// Play slomo-related sound effects
        /// </summary>
        public void PlaySlomoSound(string phase = "start", Vector3 position = default, bool is3D = false, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = $"slomo_{phase}";
            float volume = customVolume >= 0 ? customVolume : slomoVolume.Value * ambientSoundsVolume.Value * masterVolume.Value;
            float pitch = phase == "active" ? Time.timeScale : DEFAULT_PITCH;
            
            PlayEffect(soundKey, position, is3D, volume, pitch, customFilePath);
        }

        /// <summary>
        /// Play danger close explosion sounds
        /// </summary>
        public void PlayDangerCloseSound(string type = "danger_close", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            float volume = customVolume >= 0 ? customVolume : dangerCloseVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        /// <summary>
        /// Play weapon spawn sounds (Skitty Sub Gun)
        /// </summary>
        public void PlayWeaponSpawnSound(string type = "skitty_sub_gun", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            float volume = customVolume >= 0 ? customVolume : skittySubGunVolume.Value * weaponSoundsVolume.Value * masterVolume.Value;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        /// <summary>
        /// Play destruction sounds (Destroy Quickbelt)
        /// </summary>
        public void PlayDestructionSound(string type = "destroy_quickbelt", Vector3 position = default, bool is3D = false, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            float volume = customVolume >= 0 ? customVolume : destroyQuickbeltVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(type, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        /// <summary>
        /// Play wondertoy sounds
        /// </summary>
        public void PlayWondertoySound(string action = "spawn", Vector3 position = default, bool is3D = true, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = action == "spawn" ? "wondertoy" : $"wondertoy_{action}";
            float volume = customVolume >= 0 ? customVolume : wondertoyVolume.Value * effectsVolume.Value * masterVolume.Value;
            PlayEffect(soundKey, position, is3D, volume, DEFAULT_PITCH, customFilePath);
        }

        /// <summary>
        /// Play UI and system sounds
        /// </summary>
        public void PlayUISound(string type = "confirm", Vector3 position = default, string customFilePath = null, float customVolume = -1f)
        {
            if (!enableAudioEffects.Value) return;
            
            string soundKey = $"ui_{type}";
            float volume = customVolume >= 0 ? customVolume : effectsVolume.Value * masterVolume.Value * 0.5f; // UI sounds are quieter
            PlayEffect(soundKey, position, false, volume, DEFAULT_PITCH, customFilePath);
        }
        #endregion

        #region Core Audio Playback
        /// <summary>
        /// Play effect with enhanced error handling and validation
        /// </summary>
        private void PlayEffect(string effectKey, Vector3 position, bool is3D, float volume = DEFAULT_VOLUME, float pitch = DEFAULT_PITCH, string customFilePath = null)
        {
            if (!enableAudioEffects.Value || !isInitialized) return;

            // Validate parameters
            if (string.IsNullOrEmpty(effectKey))
            {
                logger.LogWarning("[AudioManager] PlayEffect called with null or empty effectKey");
                return;
            }

            if (volume < 0f || volume > 2f)
            {
                logger.LogWarning($"[AudioManager] Volume {volume} out of reasonable range (0-2), clamping");
                volume = Mathf.Clamp(volume, 0f, 2f);
            }

            if (pitch < 0.1f || pitch > 10f)
            {
                logger.LogWarning($"[AudioManager] Pitch {pitch} out of reasonable range (0.1-10), clamping");
                pitch = Mathf.Clamp(pitch, 0.1f, 10f);
            }

            // Limit simultaneous sounds
            if (activeSources.Count >= maxSimultaneousSounds.Value)
            {
                CleanupFinishedSources();
                if (activeSources.Count >= maxSimultaneousSounds.Value)
                {
                    logger.LogDebug("[AudioManager] Maximum simultaneous sounds reached, skipping playback");
                    return;
                }
            }

            AudioClip selectedClip = null;

            // If custom file path is provided, try to load it
            if (!string.IsNullOrEmpty(customFilePath))
            {
                selectedClip = LoadCustomAudioFileSync(customFilePath);
                if (selectedClip != null)
                {
                    logger.LogDebug($"[AudioManager] Using custom audio file: {customFilePath}");
                }
            }

            // If no custom clip loaded, get default clips for this effect
            if (selectedClip == null)
            {
                var clips = GetAudioClipsForEffect(effectKey);
                if (clips.Count == 0)
                {
                    // Use default system sound or silence
                    logger.LogDebug($"[AudioManager] No audio clips found for effect: {effectKey}");
                    return;
                }

                // Select random clip from available options
                selectedClip = clips[UnityEngine.Random.Range(0, clips.Count)];
            }
            
            // Validate final audio clip
            if (selectedClip == null)
            {
                logger.LogWarning($"[AudioManager] Failed to get audio clip for effect: {effectKey}");
                return;
            }

            // Create and configure audio source
            GameObject audioObject = new GameObject($"H3TVR_Audio_{effectKey}_{Time.time}");
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            
            ConfigureAudioSource(audioSource, selectedClip, position, is3D, volume, pitch);
            
            // Track active source
            string sourceKey = $"{effectKey}_{Time.time}_{UnityEngine.Random.Range(1000, 9999)}";
            activeSources[sourceKey] = audioSource;
            
            // Start playback and cleanup
            audioSource.Play();
            StartCoroutine(CleanupAudioSource(sourceKey, selectedClip.length + 0.1f));
            
            logger.LogDebug($"[AudioManager] Playing effect: {effectKey} at position: {position} (Volume: {volume}, Custom: {!string.IsNullOrEmpty(customFilePath)})");
        }

        /// <summary>
        /// Stop all currently playing sounds for a specific effect
        /// </summary>
        public void StopEffectSounds(string effectKey)
        {
            if (string.IsNullOrEmpty(effectKey)) return;

            List<string> keysToRemove = new List<string>();
            
            foreach (var kvp in activeSources)
            {
                if (kvp.Key.StartsWith(effectKey + "_") && kvp.Value != null)
                {
                    kvp.Value.Stop();
                    if (kvp.Value.gameObject != null)
                    {
                        Destroy(kvp.Value.gameObject);
                    }
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (string key in keysToRemove)
            {
                activeSources.Remove(key);
            }
            
            if (keysToRemove.Count > 0)
            {
                logger.LogDebug($"[AudioManager] Stopped {keysToRemove.Count} sounds for effect: {effectKey}");
            }
        }

        /// <summary>
        /// Stop all currently playing audio
        /// </summary>
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
            logger.LogInfo("[AudioManager] Stopped all audio");
        }

        private void ConfigureAudioSource(AudioSource source, AudioClip clip, Vector3 position, bool is3D, float volume, float pitch)
        {
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(pitch, 0.1f, 3.0f);
            
            // 3D Audio configuration
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
            
            // Additional audio settings
            source.playOnAwake = false;
            source.loop = false;
        }

        private List<AudioClip> GetAudioClipsForEffect(string effectKey)
        {
            List<AudioClip> clips = new List<AudioClip>();
            
            if (audioFileMapping.ContainsKey(effectKey))
            {
                foreach (string fileName in audioFileMapping[effectKey])
                {
                    if (audioClips.ContainsKey(fileName))
                    {
                        clips.Add(audioClips[fileName]);
                    }
                }
            }
            
            return clips;
        }

        private IEnumerator CleanupAudioSource(string sourceKey, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            if (activeSources.ContainsKey(sourceKey))
            {
                if (activeSources[sourceKey] != null)
                {
                    Destroy(activeSources[sourceKey].gameObject);
                }
                activeSources.Remove(sourceKey);
            }
        }

        /// <summary>
        /// Enhanced cleanup with memory management
        /// </summary>
        private void CleanupFinishedSources()
        {
            List<string> keysToRemove = new List<string>();
            
            foreach (var kvp in activeSources)
            {
                if (kvp.Value == null || !kvp.Value.isPlaying)
                {
                    keysToRemove.Add(kvp.Key);
                    if (kvp.Value != null)
                    {
                        // Ensure proper cleanup
                        try
                        {
                            if (kvp.Value.gameObject != null)
                            {
                                Destroy(kvp.Value.gameObject);
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning($"[AudioManager] Error cleaning up audio source: {ex.Message}");
                        }
                    }
                }
            }
            
            foreach (string key in keysToRemove)
            {
                activeSources.Remove(key);
            }
            
            // Log cleanup if significant
            if (keysToRemove.Count > 5)
            {
                logger.LogDebug($"[AudioManager] Cleaned up {keysToRemove.Count} finished audio sources");
            }
        }
        #endregion

        #region Enhanced Custom Audio Loading
        
        /// <summary>
        /// Load a custom audio file from any file path and add it to the audio system
        /// </summary>
        /// <param name="filePath">Full path to the audio file</param>
        /// <param name="effectKey">Key to register this audio under (e.g., "my_custom_explosion")</param>
        /// <param name="replaceExisting">Whether to replace existing audio with the same key</param>
        /// <returns>True if loaded successfully</returns>
        public bool LoadCustomAudioFile(string filePath, string effectKey, bool replaceExisting = true)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(effectKey))
            {
                logger.LogWarning("[AudioManager] LoadCustomAudioFile: File path and effect key cannot be empty");
                return false;
            }

            if (!File.Exists(filePath))
            {
                logger.LogWarning($"[AudioManager] Custom audio file not found: {filePath}");
                return false;
            }

            // Check if effect key already exists
            if (audioFileMapping.ContainsKey(effectKey) && !replaceExisting)
            {
                logger.LogWarning($"[AudioManager] Effect key '{effectKey}' already exists. Use replaceExisting=true to override");
                return false;
            }

            try
            {
                StartCoroutine(LoadCustomAudioFileCoroutine(filePath, effectKey, replaceExisting));
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError($"[AudioManager] Failed to start loading custom audio file: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Load multiple custom audio files from file paths
        /// </summary>
        /// <param name="audioFiles">Dictionary of effect keys and file paths</param>
        /// <param name="replaceExisting">Whether to replace existing audio with the same keys</param>
        /// <returns>Number of files successfully queued for loading</returns>
        public int LoadMultipleCustomAudioFiles(Dictionary<string, string> audioFiles, bool replaceExisting = true)
        {
            int successCount = 0;
            foreach (var kvp in audioFiles)
            {
                if (LoadCustomAudioFile(kvp.Value, kvp.Key, replaceExisting))
                {
                    successCount++;
                }
            }
            logger.LogInfo($"[AudioManager] Queued {successCount}/{audioFiles.Count} custom audio files for loading");
            return successCount;
        }

        /// <summary>
        /// Load custom audio files from a directory with automatic effect key generation
        /// </summary>
        /// <param name="directoryPath">Path to directory containing audio files</param>
        /// <param name="prefix">Prefix for generated effect keys (e.g., "custom_")</param>
        /// <param name="replaceExisting">Whether to replace existing audio</param>
        /// <returns>Number of files successfully queued for loading</returns>
        public int LoadAudioFilesFromDirectory(string directoryPath, string prefix = "custom_", bool replaceExisting = true)
        {
            if (!Directory.Exists(directoryPath))
            {
                logger.LogWarning($"[AudioManager] Directory not found: {directoryPath}");
                return 0;
            }

            var supportedExtensions = new[] { ".wav", ".ogg", ".mp3", ".aif", ".aiff", ".mod", ".it", ".s3m", ".xm" };
            var audioFiles = new List<string>();

            foreach (var extension in supportedExtensions)
            {
                audioFiles.AddRange(Directory.GetFiles(directoryPath, "*" + extension, SearchOption.TopDirectoryOnly));
            }

            int successCount = 0;
            foreach (var filePath in audioFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string effectKey = prefix + fileName.ToLower().Replace(" ", "_");
                
                if (LoadCustomAudioFile(filePath, effectKey, replaceExisting))
                {
                    successCount++;
                }
            }

            logger.LogInfo($"[AudioManager] Queued {successCount} audio files from directory: {directoryPath}");
            return successCount;
        }

        private IEnumerator LoadCustomAudioFileCoroutine(string filePath, string effectKey, bool replaceExisting)
        {
            string url = "file://" + filePath;
            string fileName = Path.GetFileName(filePath);
            
            logger.LogInfo($"[AudioManager] Loading custom audio file: {fileName} as '{effectKey}'");

            using (WWW www = new WWW(url))
            {
                yield return www;
                
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = www.GetAudioClip(false, false, GetAudioType(filePath));
                    if (clip != null)
                    {
                        clip.name = effectKey;
                        
                        // Store the clip for direct access
                        audioClips[effectKey] = clip;
                        
                        // Create or update the mapping for the effect key
                        if (audioFileMapping.ContainsKey(effectKey))
                        {
                            if (replaceExisting)
                            {
                                audioFileMapping[effectKey] = new[] { effectKey };
                                logger.LogInfo($"[AudioManager] Replaced existing audio for effect '{effectKey}'");
                            }
                        }
                        else
                        {
                            audioFileMapping[effectKey] = new[] { effectKey };
                        }
                        
                        logger.LogInfo($"[AudioManager] Successfully loaded custom audio: {fileName} as '{effectKey}'");
                    }
                    else
                    {
                        logger.LogWarning($"[AudioManager] Failed to create audio clip from file: {fileName}");
                    }
                }
                else
                {
                    logger.LogWarning($"[AudioManager] Failed to load custom audio file {fileName}: {www.error}");
                }
            }
        }

        /// <summary>
        /// Synchronously load a custom audio file for immediate playback
        /// This is a temporary loading method for PlayEffect compatibility
        /// </summary>
        private AudioClip LoadCustomAudioFileSync(string filePath)
        {
            try
            {
                // Check if the file path is relative and make it absolute to the audio folder
                string fullPath;
                if (Path.IsPathRooted(filePath))
                {
                    fullPath = filePath;
                }
                else
                {
                    fullPath = Path.Combine(audioFolderPath, filePath);
                }

                // Check if file exists
                if (!File.Exists(fullPath))
                {
                    logger.LogDebug($"[AudioManager] Custom audio file not found: {fullPath}");
                    return null;
                }

                // Check if already loaded in cache
                string cacheKey = $"temp_{Path.GetFileName(fullPath)}";
                if (audioClips.ContainsKey(cacheKey))
                {
                    return audioClips[cacheKey];
                }

                // Load synchronously using WWW (Note: This blocks but is needed for immediate playback)
                string url = "file://" + fullPath;
                using (WWW www = new WWW(url))
                {
                    // Wait for loading to complete (synchronous)
                    float timeoutTime = Time.realtimeSinceStartup + 30f; // 30 second timeout
                    while (!www.isDone && string.IsNullOrEmpty(www.error))
                    {
                        if (Time.realtimeSinceStartup > timeoutTime)
                        {
                            logger.LogWarning($"[AudioManager] Timeout loading custom audio file: {fullPath}");
                            return null;
                        }
                        // Small delay to prevent complete blocking
                        System.Threading.Thread.Sleep(10);
                    }

                    if (string.IsNullOrEmpty(www.error))
                    {
                        AudioClip clip = www.GetAudioClip(false, false, GetAudioType(fullPath));
                        if (clip != null)
                        {
                            clip.name = cacheKey;
                            audioClips[cacheKey] = clip; // Cache for future use
                            logger.LogDebug($"[AudioManager] Loaded custom audio clip: {Path.GetFileName(fullPath)}");
                            return clip;
                        }
                    }
                    else
                    {
                        logger.LogWarning($"[AudioManager] Failed to load custom audio file: {www.error}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"[AudioManager] Exception loading custom audio clip: {ex.Message}");
            }

            return null;
        }

        #endregion

        #region Enhanced Custom Audio Loading

        /// <summary>
        /// Play a previously loaded custom audio effect by name
        /// /// </summary>
        /// <param name="effectName">The name you gave the effect when loading it</param>
        /// <param name="position">3D position to play the sound</param>
        /// <param name="is3D">Whether to use 3D spatial audio</param>
        /// <param name="volume">Volume level 0.0-1.0</param>
        /// <param name="pitch">Pitch adjustment 0.1-3.0</param>
        /// <returns>True if the effect was found and played</returns>
        public bool PlayLoadedEffect(string effectName, Vector3 position = default, bool is3D = true, float volume = 0.8f, float pitch = 1.0f)
        {
            if (!enableAudioEffects.Value || !isInitialized)
            {
                logger.LogWarning("[AudioManager] Audio system not enabled or not initialized");
                return false;
            }

            if (string.IsNullOrEmpty(effectName))
            {
                logger.LogWarning("[AudioManager] PlayLoadedEffect: Effect name cannot be empty");
                return false;
            }

            if (!HasEffect(effectName))
            {
                logger.LogWarning($"[AudioManager] Effect '{effectName}' not found. Did you load it first with LoadAudioFiles or PlayAudioFile?");
                return false;
            }

            float finalVolume = volume * effectsVolume.Value * masterVolume.Value;
            PlayEffect(effectName, position, is3D, finalVolume, pitch);
            
            logger.LogDebug($"[AudioManager] Played loaded effect: {effectName}");
            return true;
        }

        /// <summary>
        /// Get the full path for an audio file, handling both absolute and relative paths
        /// </summary>
        private string GetFullAudioPath(string filePath)
        {
            // If it's already an absolute path, use it as-is
            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }
            
            // If it's a relative path, assume it's in the H3TVR_Audio folder
            return Path.Combine(audioFolderPath, filePath);
        }

        /// <summary>
        /// Coroutine to load and play an audio file immediately (temporary loading)
        /// </summary>
        private IEnumerator LoadAndPlayAudioFileCoroutine(string fullPath, Vector3 position, bool is3D, float volume, float pitch)
        {
            string url = "file://" + fullPath;
            string fileName = Path.GetFileName(fullPath);
            
            using (WWW www = new WWW(url))
            {
                yield return www;
                
                if (string.IsNullOrEmpty(www.error))
                {
                    AudioClip clip = www.GetAudioClip(false, false, GetAudioType(fullPath));
                    if (clip != null)
                    {
                        clip.name = fileName;
                        
                        // Play the clip immediately
                        PlayClipDirectly(clip, position, is3D, volume * effectsVolume.Value * masterVolume.Value, pitch);
                        
                        logger.LogDebug($"[AudioManager] Played temporary audio file: {fileName}");
                    }
                    else
                    {
                        logger.LogWarning($"[AudioManager] Failed to create audio clip from file: {fileName}");
                    }
                }
                else
                {
                    logger.LogWarning($"[AudioManager] Failed to load audio file {fileName}: {www.error}");
                }
            }
        }

        /// <summary>
        /// Play after effect has been loaded
        /// </summary>
        private IEnumerator PlayFileAfterLoading(string effectKey, Vector3 position, bool is3D, float volume, float pitch)
        {
            // Wait a frame for the loading to complete
            yield return null;
            
            // Try to play the effect
            int attempts = 0;
            while (attempts < 30 && !HasEffect(effectKey)) // Wait up to 30 frames (about 0.5 seconds)
            {
                yield return null;
                attempts++;
            }
            
            if (HasEffect(effectKey))
            {
                PlayLoadedEffect(effectKey, position, is3D, volume, pitch);
            }
            else
            {
                logger.LogWarning($"[AudioManager] Failed to load effect '{effectKey}' in time for playback");
            }
        }

        /// <summary>
        /// Play an audio clip directly without going through the effect system
        /// </summary>
        private void PlayClipDirectly(AudioClip clip, Vector3 position, bool is3D, float volume, float pitch)
        {
            if (clip == null) return;

            // Create and configure audio source
            GameObject audioObject = new GameObject($"H3TVR_TempAudio_{clip.name}_{Time.time}");
            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
            
            ConfigureAudioSource(audioSource, clip, position, is3D, volume, pitch);
            
            // Track active source
            string sourceKey = $"temp_{clip.name}_{Time.time}_{UnityEngine.Random.Range(1000, 9999)}";
            activeSources[sourceKey] = audioSource;
            
            // Start playback and cleanup
            audioSource.Play();
            StartCoroutine(CleanupAudioSource(sourceKey, clip.length + 0.1f));
        }

        /// <summary>
        /// Get a list of all your loaded custom audio effects
        /// </summary>
        /// <returns>List of effect names you can use with PlayLoadedEffect</returns>
        public List<string> GetMyCustomEffects()
        {
            var customEffects = new List<string>();
            
            foreach (var key in audioFileMapping.Keys)
            {
                // Skip built-in effects (they have predefined mappings with multiple files)
                if (audioFileMapping[key].Length == 1 && audioFileMapping[key][0] == key)
                {
                    customEffects.Add(key);
                }
            }
            
            return customEffects;
        }

        /// <summary>
        /// Check if you have loaded a specific audio effect
        /// </summary>
        /// <param name="effectName">The name of the effect to check</param>
        /// <returns>True if you can play this effect with PlayLoadedEffect</returns>
        public bool HasEffect(string effectName)
        {
            return !string.IsNullOrEmpty(effectName) && 
                   audioFileMapping.ContainsKey(effectName) && 
                   audioClips.ContainsKey(effectName);
        }

        /// <summary>
        /// Remove a custom audio effect you loaded
        /// </summary>
        /// <param name="effectName">The name of the effect to remove</param>
        /// <returns>True if the effect was found and removed</returns>
        public bool RemoveEffect(string effectName)
        {
            if (string.IsNullOrEmpty(effectName))
            {
                logger.LogWarning("[AudioManager] RemoveEffect: Effect name cannot be empty");
                return false;
            }

            bool removed = false;
            
            // Stop any currently playing sounds with this effect key
            StopEffectSounds(effectName);
            
            // Remove from mapping
            if (audioFileMapping.ContainsKey(effectName))
            {
                audioFileMapping.Remove(effectName);
                removed = true;
            }
            
            // Remove from clips
            if (audioClips.ContainsKey(effectName))
            {
                var clip = audioClips[effectName];
                audioClips.Remove(effectName);
                
                if (clip != null)
                {
                    Destroy(clip);
                }
                removed = true;
            }
            
            if (removed)
            {
                logger.LogInfo($"[AudioManager] Removed custom audio effect: {effectName}");
            }
            else
            {
                logger.LogWarning($"[AudioManager] Custom audio effect '{effectName}' not found");
            }
            
            return removed;
        }

        #endregion

        #region Stovepipe Sounds

        /// <summary>
        /// Play Stovepipe malfunction sound effects
        /// </summary>
        public void PlayStovepipeSound(string action, Vector3 position, bool is3D = true, string customSound = null, float volume = 1.0f)
        {
            try
            {
                if (!isInitialized) return;

                string soundPath = customSound ?? GetStovepipeSoundPath(action);
                PlaySlomoSound(action, position, is3D, soundPath, volume);

                LogSoundEvent("Stovepipe", action, soundPath, volume);
            }
            catch (Exception ex)
            {
                logger.LogError($"PlayStovepipeSound failed for action '{action}': {ex.Message}");
            }
        }

        /// <summary>
        /// Get sound path for Stovepipe actions
        /// </summary>
        private string GetStovepipeSoundPath(string action)
        {
            switch (action.ToLower())
            {
                case "jam":
                case "malfunction":
                    return "stovepipe/weapon_jam.wav";
                case "stovepipe":
                    return "stovepipe/stovepipe_jam.wav";
                case "double_feed":
                    return "stovepipe/double_feed.wav";
                case "failure_to_feed":
                    return "stovepipe/failure_to_feed.wav";
                case "failure_to_eject":
                    return "stovepipe/failure_to_eject.wav";
                case "failure_to_fire":
                    return "stovepipe/failure_to_fire.wav";
                case "hang_fire":
                    return "stovepipe/hang_fire.wav";
                case "clear_jam":
                    return "stovepipe/jam_cleared.wav";
                case "cycling":
                    return "stovepipe/action_cycling.wav";
                default:
                    return "stovepipe/generic_malfunction.wav";
            }
        }

        /// <summary>
        /// Log sound event for debugging
        /// </summary>
        private void LogSoundEvent(string system, string action, string soundPath, float volume)
        {
            logger.LogDebug($"[AudioManager] {system} sound: {action} -> {soundPath} (volume: {volume:F2})");
        }

        /// <summary>
        /// Check if audio manager is initialized
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized;
        }

        /// <summary>
        /// Play Stovepipe effect with specific malfunction type
        /// </summary>
        public void PlayStovepipeEffect(Vector3 position, string effectType)
        {
            PlayStovepipeSound(effectType, position, true);
        }

        #endregion
    }
}