using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Shared armor level parsing utility. Used by ChatWatcher, LioranBoardIntegration,
    /// and any other systems that need to parse armor levels from strings.
    /// </summary>
    public static class ArmorLevelParser
    {
        /// <summary>
        /// Try to parse an armor level from a string (name or number).
        /// Returns true if parsing succeeded.
        /// </summary>
        public static bool TryParse(string value, out int level, out string name)
        {
            level = 0;
            name = "None";

            if (string.IsNullOrEmpty(value)) return false;

            // Try parse as number first
            if (int.TryParse(value, out level))
            {
                level = Mathf.Clamp(level, 0, 5);
                name = SosigArmorManager.GetArmorName(level);
                return true;
            }

            // Parse by name
            switch (value.ToLower())
            {
                case "none": case "off": case "naked": case "n":
                    level = 0; name = "None"; return true;
                case "light": case "l":
                    level = 1; name = "Light"; return true;
                case "medium": case "med": case "m":
                    level = 2; name = "Medium"; return true;
                case "heavy": case "h":
                    level = 3; name = "Heavy"; return true;
                case "tank": case "juggernaut": case "jug": case "t":
                    level = 4; name = "Tank"; return true;
                case "god": case "godmode": case "immortal": case "g":
                    level = 5; name = "God"; return true;
                default:
                    return false;
            }
        }
    }
}
