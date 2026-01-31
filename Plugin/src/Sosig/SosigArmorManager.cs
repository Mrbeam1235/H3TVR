using System.Collections.Generic;
using FistVR;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Lightweight sosig armor manager with chat command support
    /// Armor is applied once at spawn time - no continuous overhead
    /// 
    /// CHAT COMMANDS (case-insensitive):
    ///   !armor none    - No armor (level 0)
    ///   !armor light   - Light armor (level 1)
    ///   !armor medium  - Medium armor (level 2)
    ///   !armor heavy   - Heavy armor (level 3)
    ///   !armor tank    - Juggernaut armor (level 4)
    ///   !armor god     - God mode (level 5)
    ///   !armor 0-5     - Set by number directly
    /// </summary>
    public static class SosigArmorManager
    {
        #region Armor Presets - Cached for performance
        // Health multipliers applied at spawn time only
        private static readonly float[] ArmorMultipliers = new float[]
        {
            1.0f,   // 0: None
            1.5f,   // 1: Light
            2.0f,   // 2: Medium
            3.0f,   // 3: Heavy
            5.0f,   // 4: Juggernaut/Tank
            100.0f  // 5: God Mode
        };

        // Friendly name lookup for chat feedback
        private static readonly string[] ArmorNames = new string[]
        {
            "None", "Light", "Medium", "Heavy", "Tank", "God"
        };
        #endregion

        #region Per-User Armor Preferences
        // Cached user preferences - O(1) lookup
        private static Dictionary<string, int> userArmorPreferences = new Dictionary<string, int>();
        
        // Global default when user has no preference
        private static int globalAllyDefault = 0;
        private static int globalEnemyDefault = 0;
        #endregion

        #region Chat Command Parsing
        /// <summary>
        /// Parse armor command from chat message
        /// Returns true if message was an armor command
        /// </summary>
        public static bool TryParseArmorCommand(string message, string username, out int armorLevel, out string armorName)
        {
            armorLevel = 0;
            armorName = "None";

            if (string.IsNullOrEmpty(message)) return false;

            string lower = message.ToLower().Trim();
            
            // Check for !armor command
            if (!lower.StartsWith("!armor")) return false;

            // Extract parameter
            string param = lower.Length > 6 ? lower.Substring(6).Trim() : "";

            // Parse armor level
            if (TryParseArmorParam(param, out armorLevel))
            {
                armorName = GetArmorName(armorLevel);
                
                // Store user preference
                if (!string.IsNullOrEmpty(username))
                {
                    SetUserArmorPreference(username, armorLevel);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Parse armor parameter (name or number)
        /// </summary>
        private static bool TryParseArmorParam(string param, out int level)
        {
            level = 0;

            // Try parse as number first (fastest)
            if (int.TryParse(param, out level))
            {
                level = Mathf.Clamp(level, 0, 5);
                return true;
            }

            // Parse by name
            switch (param)
            {
                case "none":
                case "off":
                case "naked":
                    level = 0; return true;
                case "light":
                case "l":
                    level = 1; return true;
                case "medium":
                case "med":
                case "m":
                    level = 2; return true;
                case "heavy":
                case "h":
                    level = 3; return true;
                case "tank":
                case "juggernaut":
                case "jug":
                case "t":
                    level = 4; return true;
                case "god":
                case "godmode":
                case "immortal":
                case "g":
                    level = 5; return true;
                default:
                    return false;
            }
        }
        #endregion

        #region User Preferences
        /// <summary>
        /// Set armor preference for a specific user
        /// </summary>
        public static void SetUserArmorPreference(string username, int armorLevel)
        {
            if (string.IsNullOrEmpty(username)) return;
            
            string key = username.ToLower();
            armorLevel = Mathf.Clamp(armorLevel, 0, 5);
            
            userArmorPreferences[key] = armorLevel;
        }

        /// <summary>
        /// Get armor preference for a user (returns global default if no preference)
        /// </summary>
        public static int GetUserArmorPreference(string username, bool isAlly)
        {
            if (!string.IsNullOrEmpty(username))
            {
                string key = username.ToLower();
                if (userArmorPreferences.TryGetValue(key, out int level))
                {
                    return level;
                }
            }
            
            return isAlly ? globalAllyDefault : globalEnemyDefault;
        }

        /// <summary>
        /// Clear a user's armor preference
        /// </summary>
        public static void ClearUserArmorPreference(string username)
        {
            if (!string.IsNullOrEmpty(username))
            {
                userArmorPreferences.Remove(username.ToLower());
            }
        }

        /// <summary>
        /// Clear all user preferences
        /// </summary>
        public static void ClearAllPreferences()
        {
            userArmorPreferences.Clear();
        }

        /// <summary>
        /// Set global default armor levels
        /// </summary>
        public static void SetGlobalDefaults(int allyDefault, int enemyDefault)
        {
            globalAllyDefault = Mathf.Clamp(allyDefault, 0, 5);
            globalEnemyDefault = Mathf.Clamp(enemyDefault, 0, 5);
        }
        #endregion

        #region Armor Application
        /// <summary>
        /// Apply armor to sosig - called once at spawn time
        /// </summary>
        public static void ApplyArmorToSosig(Sosig sosig, int armorLevel)
        {
            if (sosig == null) return;

            armorLevel = Mathf.Clamp(armorLevel, 0, 5);
            float multiplier = ArmorMultipliers[armorLevel];

            // Skip if no armor modification needed
            if (armorLevel == 0) return;

            try
            {
                // Apply health multiplier to all links
                if (sosig.Links != null)
                {
                    for (int i = 0; i < sosig.Links.Count; i++)
                    {
                        var link = sosig.Links[i];
                        if (link != null)
                        {
                            link.m_integrity *= multiplier;
                        }
                    }
                }
            }
            catch
            {
                // Silently fail - sosig may have unusual structure
            }
        }

        /// <summary>
        /// Apply armor based on username preference
        /// </summary>
        public static void ApplyArmorForUser(Sosig sosig, string username, bool isAlly)
        {
            int armorLevel = GetUserArmorPreference(username, isAlly);
            ApplyArmorToSosig(sosig, armorLevel);
        }
        #endregion

        #region Utility
        /// <summary>
        /// Get friendly armor name
        /// </summary>
        public static string GetArmorName(int level)
        {
            level = Mathf.Clamp(level, 0, 5);
            return ArmorNames[level];
        }

        /// <summary>
        /// Get armor multiplier for level
        /// </summary>
        public static float GetArmorMultiplier(int level)
        {
            level = Mathf.Clamp(level, 0, 5);
            return ArmorMultipliers[level];
        }

        /// <summary>
        /// Get total registered user preferences count
        /// </summary>
        public static int GetPreferenceCount()
        {
            return userArmorPreferences.Count;
        }
        #endregion
    }
}
