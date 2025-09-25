using System.Collections.Generic;
using UnityEngine;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Advanced configuration class for sosig loadouts and templates
    /// This provides a more structured way to define sosig spawn configurations
    /// </summary>
    [System.Serializable]
    public class AdvancedSosigLoadout
    {
        [Header("Basic Information")]
        public string loadoutName = "Custom Loadout";
        public string description = "A custom sosig loadout";
        public Sprite loadoutIcon;

        [Header("Faction Settings")]
        public int defaultIFF = 0;
        public bool isHostileToPlayer = false;
        public bool canSwitchSides = false;
        public Color factionColor = Color.white;

        [Header("Sosig Templates")]
        public List<SosigEnemyTemplate> primaryTemplates = new List<SosigEnemyTemplate>();
        public List<SosigEnemyTemplate> alternativeTemplates = new List<SosigEnemyTemplate>();
        public List<SosigOutfitConfig> outfitConfigs = new List<SosigOutfitConfig>();

        [Header("Spawn Behavior")]
        public bool followPlayer = true;
        public bool patrolArea = false;
        public float patrolRadius = 10f;
        public Sosig.SosigOrder fallbackOrder = Sosig.SosigOrder.SearchForEquipment;
        public bool enableChattering = true;

        [Header("Equipment Settings")]
        public bool useRandomWeapons = true;
        public List<FVRObject> customPrimaryWeapons = new List<FVRObject>();
        public List<FVRObject> customSecondaryWeapons = new List<FVRObject>();
        public List<FVRObject> customTertiaryWeapons = new List<FVRObject>();
        
        [Header("Armor Configuration")]
        public ArmorLoadoutConfig armorConfig = new ArmorLoadoutConfig();

        [Header("Advanced Settings")]
        public bool useCustomHealth = false;
        public float customHealthMultiplier = 1.0f;
        public bool useCustomSpeed = false;
        public float customSpeedMultiplier = 1.0f;
        public bool immuneToSlomo = false;

        public SosigEnemyTemplate GetRandomTemplate()
        {
            var allTemplates = new List<SosigEnemyTemplate>();
            allTemplates.AddRange(primaryTemplates);
            allTemplates.AddRange(alternativeTemplates);
            
            if (allTemplates.Count == 0) return null;
            return allTemplates[Random.Range(0, allTemplates.Count)];
        }

        public SosigOutfitConfig GetRandomOutfit()
        {
            if (outfitConfigs.Count == 0) return null;
            return outfitConfigs[Random.Range(0, outfitConfigs.Count)];
        }
    }

    [System.Serializable]
    public class ArmorLoadoutConfig
    {
        [Header("Armor Pieces")]
        public bool forceHeadwear = false;
        public bool forceTorsowear = false;
        public bool forcePantswear = false;
        public bool forceBackpack = false;

        [Header("Armor Chances (0-1)")]
        [Range(0f, 1f)] public float headwearChance = 0.7f;
        [Range(0f, 1f)] public float facewearChance = 0.3f;
        [Range(0f, 1f)] public float eyewearChance = 0.4f;
        [Range(0f, 1f)] public float torsowearChance = 0.8f;
        [Range(0f, 1f)] public float pantswearChance = 0.6f;
        [Range(0f, 1f)] public float pantswearLowerChance = 0.4f;
        [Range(0f, 1f)] public float backpackChance = 0.2f;
        [Range(0f, 1f)] public float decorationChance = 0.1f;

        [Header("Custom Armor Sets")]
        public List<FVRObject> customHeadwear = new List<FVRObject>();
        public List<FVRObject> customFacewear = new List<FVRObject>();
        public List<FVRObject> customEyewear = new List<FVRObject>();
        public List<FVRObject> customTorsowear = new List<FVRObject>();
        public List<FVRObject> customPantswear = new List<FVRObject>();
        public List<FVRObject> customPantswearLower = new List<FVRObject>();
        public List<FVRObject> customBackpacks = new List<FVRObject>();
        public List<FVRObject> customDecorations = new List<FVRObject>();

        public SosigOutfitConfig ToSosigOutfitConfig()
        {
            var outfitConfig = ScriptableObject.CreateInstance<SosigOutfitConfig>();
            
            outfitConfig.Chance_Headwear = forceHeadwear ? 1.0f : headwearChance;
            outfitConfig.Chance_Facewear = facewearChance;
            outfitConfig.Chance_Eyewear = eyewearChance;
            outfitConfig.Chance_Torsowear = forceTorsowear ? 1.0f : torsowearChance;
            outfitConfig.Chance_Pantswear = forcePantswear ? 1.0f : pantswearChance;
            outfitConfig.Chance_Pantswear_Lower = pantswearLowerChance;
            outfitConfig.Chance_Backpacks = forceBackpack ? 1.0f : backpackChance;
            outfitConfig.Chance_TorosDecoration = decorationChance;

            // Use custom armor if available
            if (customHeadwear.Count > 0) outfitConfig.Headwear = customHeadwear;
            if (customFacewear.Count > 0) outfitConfig.Facewear = customFacewear;
            if (customEyewear.Count > 0) outfitConfig.Eyewear = customEyewear;
            if (customTorsowear.Count > 0) outfitConfig.Torsowear = customTorsowear;
            if (customPantswear.Count > 0) outfitConfig.Pantswear = customPantswear;
            if (customPantswearLower.Count > 0) outfitConfig.Pantswear_Lower = customPantswearLower;
            if (customBackpacks.Count > 0) outfitConfig.Backpacks = customBackpacks;
            if (customDecorations.Count > 0) outfitConfig.TorosDecoration = customDecorations;

            return outfitConfig;
        }
    }

    /// <summary>
    /// Manager for loading and organizing sosig loadouts
    /// </summary>
    public static class SosigLoadoutManager
    {
        private static List<AdvancedSosigLoadout> loadouts = new List<AdvancedSosigLoadout>();
        private static bool initialized = false;

        public static void Initialize()
        {
            if (initialized) return;

            // Initialize H3VR asset loader first
            H3VRAssetLoader.Initialize();
            
            LoadDefaultLoadouts();
            LoadCustomLoadouts();
            LoadAssetsFromH3VR();
            initialized = true;
        }

        private static void LoadDefaultLoadouts()
        {
            // Friendly Soldier Loadout using H3VR assets
            var friendlyLoadout = new AdvancedSosigLoadout
            {
                loadoutName = "Friendly Soldier",
                description = "A standard friendly military unit",
                defaultIFF = 0,
                isHostileToPlayer = false,
                followPlayer = true,
                factionColor = Color.green,
                armorConfig = new ArmorLoadoutConfig
                {
                    forceTorsowear = true,
                    torsowearChance = 1.0f,
                    headwearChance = 0.8f,
                    backpackChance = 0.6f
                }
            };
            loadouts.Add(friendlyLoadout);

            // Enemy Combatant Loadout
            var enemyLoadout = new AdvancedSosigLoadout
            {
                loadoutName = "Enemy Combatant",
                description = "Hostile military unit",
                defaultIFF = 1,
                isHostileToPlayer = true,
                followPlayer = false,
                patrolArea = true,
                patrolRadius = 15f,
                factionColor = Color.red,
                fallbackOrder = Sosig.SosigOrder.Assault,
                armorConfig = new ArmorLoadoutConfig
                {
                    forceTorsowear = true,
                    forceHeadwear = true,
                    torsowearChance = 1.0f,
                    headwearChance = 1.0f,
                    backpackChance = 0.4f
                }
            };
            loadouts.Add(enemyLoadout);

            // Civilian Loadout
            var civilianLoadout = new AdvancedSosigLoadout
            {
                loadoutName = "Civilian",
                description = "Non-combatant civilian",
                defaultIFF = 2,
                isHostileToPlayer = false,
                followPlayer = false,
                patrolArea = true,
                patrolRadius = 5f,
                factionColor = Color.yellow,
                useRandomWeapons = false,
                armorConfig = new ArmorLoadoutConfig
                {
                    torsowearChance = 0.9f,
                    pantswearChance = 0.9f,
                    headwearChance = 0.2f,
                    backpackChance = 0.1f
                }
            };
            loadouts.Add(civilianLoadout);

            // Elite Soldier Loadout
            var eliteLoadout = new AdvancedSosigLoadout
            {
                loadoutName = "Elite Soldier",
                description = "High-tier military unit with advanced equipment",
                defaultIFF = 0,
                isHostileToPlayer = false,
                followPlayer = true,
                factionColor = Color.blue,
                useCustomHealth = true,
                customHealthMultiplier = 1.5f,
                useCustomSpeed = true,
                customSpeedMultiplier = 1.2f,
                armorConfig = new ArmorLoadoutConfig
                {
                    forceTorsowear = true,
                    forceHeadwear = true,
                    forceBackpack = true,
                    torsowearChance = 1.0f,
                    headwearChance = 1.0f,
                    backpackChance = 1.0f,
                    facewearChance = 0.8f,
                    eyewearChance = 0.6f
                }
            };
            loadouts.Add(eliteLoadout);
        }

        private static void LoadCustomLoadouts()
        {
            // In a real implementation, this would load loadouts from configuration files
            // or Unity asset files
        }

        public static List<AdvancedSosigLoadout> GetAllLoadouts()
        {
            if (!initialized) Initialize();
            return new List<AdvancedSosigLoadout>(loadouts);
        }

        public static AdvancedSosigLoadout GetLoadout(string name)
        {
            if (!initialized) Initialize();
            return loadouts.Find(l => l.loadoutName == name);
        }

        public static AdvancedSosigLoadout GetLoadout(int index)
        {
            if (!initialized) Initialize();
            if (index < 0 || index >= loadouts.Count) return null;
            return loadouts[index];
        }

        public static void AddCustomLoadout(AdvancedSosigLoadout loadout)
        {
            if (!initialized) Initialize();
            if (loadout != null && !loadouts.Contains(loadout))
            {
                loadouts.Add(loadout);
            }
        }

        public static List<AdvancedSosigLoadout> GetLoadoutsByFaction(int IFF)
        {
            if (!initialized) Initialize();
            return loadouts.FindAll(l => l.defaultIFF == IFF);
        }

        public static List<AdvancedSosigLoadout> GetFriendlyLoadouts()
        {
            return GetLoadoutsByFaction(0);
        }

        public static List<AdvancedSosigLoadout> GetEnemyLoadouts()
        {
            return GetLoadoutsByFaction(1);
        }

        public static List<AdvancedSosigLoadout> GetNeutralLoadouts()
        {
            return GetLoadoutsByFaction(2);
        }
    }
}