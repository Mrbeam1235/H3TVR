using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using FistVR;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// TNH Customizer Integration for H3TVR
    /// Provides custom character creation, equipment pools, and progression modification
    /// Compatible with Nicole's TNH_Customizer: https://thunderstore.io/c/h3vr/p/Nicole/TNH_Customizer/
    /// </summary>
    public class TNHCustomizerIntegration : MonoBehaviour
    {
        #region Singleton
        public static TNHCustomizerIntegration Instance { get; private set; }
        #endregion

        #region Components
        private ManualLogSource logger;
        private H3TVRImproved plugin;
        #endregion

        #region Configuration
        private ConfigEntry<bool> enableCustomCharacters;
        private ConfigEntry<bool> enableCustomPools;
        private ConfigEntry<bool> enableProgressionMods;
        private ConfigEntry<bool> enableSpawnMods;
        private ConfigEntry<string> customCharacterName;
        private ConfigEntry<int> customStartingTokens;
        private ConfigEntry<int> customMaxHealth;
        private ConfigEntry<bool> unlimitedAmmo;
        private ConfigEntry<bool> unlimitedTokens;
        private ConfigEntry<float> healthMultiplier;
        private ConfigEntry<float> sosigHealthMultiplier;
        private ConfigEntry<float> sosigSpeedMultiplier;
        private ConfigEntry<int> customHoldCount;
        #endregion

        #region Custom Character Data
        public class CustomTNHCharacter
        {
            public string CharacterName { get; set; }
            public string DisplayName { get; set; }
            public string Description { get; set; }
            
            // Starting equipment
            public List<string> StartingEquipment { get; set; }
            public int StartingTokens { get; set; }
            public int StartingHealth { get; set; }
            
            // Progression settings
            public int RequiredHolds { get; set; }
            public List<string> PrimaryWeaponPool { get; set; }
            public List<string> SecondaryWeaponPool { get; set; }
            public List<string> TertiaryWeaponPool { get; set; }
            public List<string> ShieldPool { get; set; }
            public List<string> ConsumablePool { get; set; }
            
            // Enemy settings
            public List<SosigEnemyID> EnemyPool { get; set; }
            public float EnemyHealthMultiplier { get; set; }
            public float EnemySpeedMultiplier { get; set; }
            
            // Modifiers
            public bool UnlimitedAmmo { get; set; }
            public bool UnlimitedTokens { get; set; }
            public float HealthMultiplier { get; set; }
            
            public CustomTNHCharacter()
            {
                StartingEquipment = new List<string>();
                PrimaryWeaponPool = new List<string>();
                SecondaryWeaponPool = new List<string>();
                TertiaryWeaponPool = new List<string>();
                ShieldPool = new List<string>();
                ConsumablePool = new List<string>();
                EnemyPool = new List<SosigEnemyID>();
                RequiredHolds = 5;
                StartingTokens = 3;
                StartingHealth = 1000;
                EnemyHealthMultiplier = 1.0f;
                EnemySpeedMultiplier = 1.0f;
                HealthMultiplier = 1.0f;
            }
        }
        
        private Dictionary<string, CustomTNHCharacter> customCharacters = new Dictionary<string, CustomTNHCharacter>();
        private CustomTNHCharacter activeCharacter;
        #endregion

        #region Initialization
        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            plugin = pluginInstance;
            logger = logSource;

            InitializeConfiguration();
            LoadDefaultCharacters();
            LoadCustomCharacters();

            logger?.LogInfo("TNH Customizer Integration initialized");
        }

        private void InitializeConfiguration()
        {
            if (plugin?.Config == null) return;

            try
            {
                enableCustomCharacters = plugin.Config.Bind("TNH Customizer", "EnableCustomCharacters", true,
                    "Enable custom TNH character creation");
                
                enableCustomPools = plugin.Config.Bind("TNH Customizer", "EnableCustomPools", true,
                    "Enable custom weapon/equipment pools");
                
                enableProgressionMods = plugin.Config.Bind("TNH Customizer", "EnableProgressionMods", true,
                    "Enable progression modifications (hold count, tokens, etc.)");
                
                enableSpawnMods = plugin.Config.Bind("TNH Customizer", "EnableSpawnMods", true,
                    "Enable spawn modifications (enemy types, health, speed)");
                
                customCharacterName = plugin.Config.Bind("TNH Customizer", "ActiveCharacter", "Default",
                    "Name of the active custom character to use");
                
                customStartingTokens = plugin.Config.Bind("TNH Customizer", "StartingTokens", 3,
                    "Starting tokens for custom characters");
                
                customMaxHealth = plugin.Config.Bind("TNH Customizer", "MaxHealth", 1000,
                    "Maximum health for custom characters");
                
                unlimitedAmmo = plugin.Config.Bind("TNH Customizer", "UnlimitedAmmo", false,
                    "Enable unlimited ammo for custom characters");
                
                unlimitedTokens = plugin.Config.Bind("TNH Customizer", "UnlimitedTokens", false,
                    "Enable unlimited tokens for custom characters");
                
                healthMultiplier = plugin.Config.Bind("TNH Customizer", "HealthMultiplier", 1.0f,
                    "Player health multiplier");
                
                sosigHealthMultiplier = plugin.Config.Bind("TNH Customizer", "SosigHealthMultiplier", 1.0f,
                    "Enemy sosig health multiplier");
                
                sosigSpeedMultiplier = plugin.Config.Bind("TNH Customizer", "SosigSpeedMultiplier", 1.0f,
                    "Enemy sosig speed multiplier");
                
                customHoldCount = plugin.Config.Bind("TNH Customizer", "RequiredHolds", 5,
                    "Number of holds required to complete custom TNH");

                logger?.LogInfo("TNH Customizer configuration initialized");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Config init failed: {ex.Message}");
            }
        }

        private void LoadDefaultCharacters()
        {
            try
            {
                // Easy Mode Character
                var easyMode = new CustomTNHCharacter
                {
                    CharacterName = "EasyMode",
                    DisplayName = "Easy Mode",
                    Description = "For beginners - More health, tokens, and easier enemies",
                    StartingTokens = 10,
                    StartingHealth = 2000,
                    RequiredHolds = 3,
                    UnlimitedAmmo = false,
                    UnlimitedTokens = false,
                    HealthMultiplier = 2.0f,
                    EnemyHealthMultiplier = 0.5f,
                    EnemySpeedMultiplier = 0.8f
                };
                customCharacters["EasyMode"] = easyMode;

                // Hard Mode Character
                var hardMode = new CustomTNHCharacter
                {
                    CharacterName = "HardMode",
                    DisplayName = "Hard Mode",
                    Description = "For veterans - Less health, tokens, tougher enemies",
                    StartingTokens = 1,
                    StartingHealth = 500,
                    RequiredHolds = 7,
                    UnlimitedAmmo = false,
                    UnlimitedTokens = false,
                    HealthMultiplier = 0.5f,
                    EnemyHealthMultiplier = 2.0f,
                    EnemySpeedMultiplier = 1.5f
                };
                customCharacters["HardMode"] = hardMode;

                // Infinite Resources Character
                var infiniteMode = new CustomTNHCharacter
                {
                    CharacterName = "InfiniteMode",
                    DisplayName = "Infinite Resources",
                    Description = "Unlimited ammo and tokens for sandbox play",
                    StartingTokens = 999,
                    StartingHealth = 1000,
                    RequiredHolds = 5,
                    UnlimitedAmmo = true,
                    UnlimitedTokens = true,
                    HealthMultiplier = 1.0f,
                    EnemyHealthMultiplier = 1.0f,
                    EnemySpeedMultiplier = 1.0f
                };
                customCharacters["InfiniteMode"] = infiniteMode;

                // Speed Run Character
                var speedRun = new CustomTNHCharacter
                {
                    CharacterName = "SpeedRun",
                    DisplayName = "Speed Runner",
                    Description = "Fast-paced mode - Quick holds, no encryption",
                    StartingTokens = 5,
                    StartingHealth = 1000,
                    RequiredHolds = 5,
                    UnlimitedAmmo = false,
                    UnlimitedTokens = false,
                    HealthMultiplier = 1.0f,
                    EnemyHealthMultiplier = 0.8f,
                    EnemySpeedMultiplier = 1.2f
                };
                customCharacters["SpeedRun"] = speedRun;

                // Realistic Mode Character
                var realisticMode = new CustomTNHCharacter
                {
                    CharacterName = "RealisticMode",
                    DisplayName = "Realistic",
                    Description = "Realistic combat - Low health, realistic enemy behavior",
                    StartingTokens = 2,
                    StartingHealth = 100,
                    RequiredHolds = 5,
                    UnlimitedAmmo = false,
                    UnlimitedTokens = false,
                    HealthMultiplier = 0.1f,
                    EnemyHealthMultiplier = 0.3f,
                    EnemySpeedMultiplier = 1.0f
                };
                customCharacters["RealisticMode"] = realisticMode;

                logger?.LogInfo($"Loaded {customCharacters.Count} default TNH characters");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load default characters: {ex.Message}");
            }
        }

        private void LoadCustomCharacters()
        {
            // TODO: Load custom characters from INI files
            // Format: BepInEx/config/H3TVR_TNH_Characters/[CharacterName].ini
            try
            {
                // This will be implemented to load from config files
                logger?.LogInfo("Custom character loading from files not yet implemented");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to load custom characters: {ex.Message}");
            }
        }
        #endregion

        #region TNH Hooks
        private void Update()
        {
            if (!enableCustomCharacters.Value || GM.TNH_Manager == null) return;

            try
            {
                ApplyCharacterModifications();
            }
            catch (Exception ex)
            {
                logger?.LogError($"Error applying TNH modifications: {ex.Message}");
            }
        }

        private void ApplyCharacterModifications()
        {
            if (activeCharacter == null) return;

            var tnhManager = GM.TNH_Manager;
            if (tnhManager == null) return;

            // Apply unlimited tokens
            if (activeCharacter.UnlimitedTokens || unlimitedTokens.Value)
            {
                tnhManager.m_numTokens = 999;
            }

            // Apply custom health multiplier
            if (activeCharacter.HealthMultiplier != 1.0f)
            {
                // TODO: Apply health multiplier to player
            }

            // Apply enemy modifications
            ApplyEnemyModifications();
        }

        private void ApplyEnemyModifications()
        {
            if (!enableSpawnMods.Value) return;

            try
            {
                var tnhManager = GM.TNH_Manager;
                if (tnhManager == null || tnhManager.m_curHoldPoint == null) return;

                // Modify sosig health/speed for newly spawned sosigs
                var holdPoint = tnhManager.m_curHoldPoint;
                if (holdPoint.m_activeSosigs != null)
                {
                    foreach (var sosig in holdPoint.m_activeSosigs)
                    {
                        if (sosig == null) continue;

                        // Apply health multiplier
                        float healthMult = activeCharacter?.EnemyHealthMultiplier ?? sosigHealthMultiplier.Value;
                        if (healthMult != 1.0f)
                        {
                            foreach (var link in sosig.Links)
                            {
                                if (link != null)
                                {
                                    link.m_integrity *= healthMult;
                                }
                            }
                        }

                        // Apply speed multiplier
                        float speedMult = activeCharacter?.EnemySpeedMultiplier ?? sosigSpeedMultiplier.Value;
                        if (speedMult != 1.0f)
                        {
                            sosig.Speed_Walk *= speedMult;
                            sosig.Speed_Run *= speedMult;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to apply enemy modifications: {ex.Message}");
            }
        }
        #endregion

        #region Public API
        /// <summary>
        /// Get available custom characters
        /// </summary>
        public List<string> GetAvailableCharacters()
        {
            return customCharacters.Keys.ToList();
        }

        /// <summary>
        /// Get custom character by name
        /// </summary>
        public CustomTNHCharacter GetCharacter(string characterName)
        {
            if (customCharacters.ContainsKey(characterName))
                return customCharacters[characterName];
            return null;
        }

        /// <summary>
        /// Set active custom character
        /// </summary>
        public bool SetActiveCharacter(string characterName)
        {
            if (customCharacters.ContainsKey(characterName))
            {
                activeCharacter = customCharacters[characterName];
                logger?.LogInfo($"Activated TNH character: {activeCharacter.DisplayName}");
                return true;
            }
            
            logger?.LogWarning($"Character not found: {characterName}");
            return false;
        }

        /// <summary>
        /// Create new custom character
        /// </summary>
        public bool CreateCustomCharacter(CustomTNHCharacter character)
        {
            if (string.IsNullOrEmpty(character.CharacterName))
            {
                logger?.LogError("Cannot create character with empty name");
                return false;
            }

            customCharacters[character.CharacterName] = character;
            logger?.LogInfo($"Created custom TNH character: {character.DisplayName}");
            return true;
        }

        /// <summary>
        /// Remove custom character
        /// </summary>
        public bool RemoveCustomCharacter(string characterName)
        {
            if (customCharacters.ContainsKey(characterName))
            {
                customCharacters.Remove(characterName);
                logger?.LogInfo($"Removed custom TNH character: {characterName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get current active character
        /// </summary>
        public CustomTNHCharacter GetActiveCharacter()
        {
            return activeCharacter;
        }

        /// <summary>
        /// Check if TNH Customizer is enabled
        /// </summary>
        public bool IsEnabled()
        {
            return enableCustomCharacters.Value;
        }
        #endregion
    }
}
