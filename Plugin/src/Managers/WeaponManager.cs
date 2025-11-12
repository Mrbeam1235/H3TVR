using UnityEngine;
using FistVR;
using System.Reflection;
using System.Linq;
using System.IO;
using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Helper class for magazine compatibility scoring
    /// </summary>
    public class MagazineCompatibilityScore
    {
        public FVRObject magazine;
        public int score;
    }

    /// <summary>
    /// Manages all weapon-related functionality including gun randomization, fire mode toggling, and malfunction boosts
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private AudioManager audioManager;

        // Weapon statistics
        private int weaponSpawnCount = 0;
        private float lastWeaponSpawnTime = 0f;

        // Gun scale modifier tracking
        private Dictionary<FVRFireArm, ScaleModifierData> activeScaleModifiers = new Dictionary<FVRFireArm, ScaleModifierData>();
        
        /// <summary>
        /// Data class for tracking scale modifications
        /// </summary>
        private class ScaleModifierData
        {
            public Vector3 originalScale;
            public float endTime;
            public float targetScale;
        }

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource, AudioManager audioManagerInstance)
        {
            plugin = pluginInstance;
            logger = logSource;
            audioManager = audioManagerInstance;

            // Initialize optional dependency manager first
            OptionalDependencyManager.Initialize(logger);

            InitializeWeaponData();

            logger.LogInfo("[WeaponManager] Weapon manager initialized successfully");
            
            // Log dependency status for weapons
            if (OptionalDependencyManager.HasAnyDependencies())
            {
                logger.LogInfo($"[WeaponManager] Enhanced with {OptionalDependencyManager.GetAvailableDependencyCount()} optional dependencies");
            }
            else
            {
                logger.LogInfo("[WeaponManager] Running in standard mode - install optional dependencies for enhanced features");
            }
        }

        /// <summary>
        /// Original Skitty gun spawning method from H3TVR
        /// </summary>
        private void SpawnSkittyGunFromLists(bool isBigGun)
        {
            try
            {
                string gunListValue, magListValue;
                plugin.GetGunLists(out gunListValue, out magListValue);
                
                string gunListString = File.Exists(gunListValue) ? File.ReadAllText(gunListValue) : gunListValue;
                string[] gunList = ParseConfigList(gunListString);

                if (gunList.Length == 0)
                {
                    logger.LogError("Gun list is empty after parsing.");
                    return;
                }

                string selectedGun = isBigGun ? gunList[0] : gunList[UnityEngine.Random.Range(0, gunList.Length)];
                
                if (!IM.OD.ContainsKey(selectedGun))
                {
                    logger.LogError($"Gun key '{selectedGun}' not found in IM.OD dictionary.");
                    return;
                }

                FVRObject gunObj = IM.OD[selectedGun];
                SpawnGunAndMagazine(gunObj, isBigGun);
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnSkittyGunFromLists failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Initialize weapon data and caches
        /// </summary>
        private void InitializeWeaponData()
        {
            try
            {
                // Initialize any weapon-specific data here
                weaponSpawnCount = 0;
                lastWeaponSpawnTime = 0f;
                
                logger.LogInfo("[WeaponManager] Weapon data initialized");
            }
            catch (Exception ex)
            {
                logger.LogError($"[WeaponManager] Error initializing weapon data: {ex.Message}");
            }
        }

        #region Gun Spawning
        public void SpawnRandomGun(bool isBigGun)
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                
                // Play weapon spawn sound
                audioManager?.PlayWeaponSpawnSound(isBigGun ? "gun_spawn" : "skitty_sub_gun", spawnPos, true);

                if (plugin.UseItemManagerForGuns())
                {
                    SpawnFromItemManager(isBigGun);
                }
                else
                {
                    SpawnFromConfigLists(isBigGun);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnRandomGun failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawns Skitty Sub Gun using original list-based system (preserves legacy functionality)
        /// </summary>
        public void SpawnSkittySubGun()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                logger.LogInfo("SpawnSkittySubGun: Using original list-based system");
                SpawnSkittyGunFromLists(false);
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnSkittySubGun failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawns Skitty Big Gun using original list-based system (preserves legacy functionality)
        /// </summary>
        public void SpawnSkittyBigGun()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                logger.LogInfo("SpawnSkittyBigGun: Using original list-based system");
                SpawnSkittyGunFromLists(true);
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnSkittyBigGun failed: {ex.Message}");
            }
        }

        private void SpawnFromItemManager(bool isBigGun)
        {
            // Get all firearms from ItemManager (includes all H3VR and modded guns with OdEaK patches)
            var allFirearms = IM.OD.Values
                .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Firearm)
                .ToArray();

            if (allFirearms.Length == 0)
            {
                logger.LogError("No firearms found in ItemManager.");
                return;
            }

            FVRObject selectedFirearm;
            if (isBigGun && allFirearms.Length > 0)
            {
                // For "big gun" mode, select the first gun and scale it up
                selectedFirearm = allFirearms[0];
                logger.LogInfo($"Selected first gun for big gun mode: {selectedFirearm.DisplayName}");
            }
            else
            {
                // Random selection from all available firearms
                selectedFirearm = allFirearms[UnityEngine.Random.Range(0, allFirearms.Length)];
                logger.LogInfo($"Randomly selected gun: {selectedFirearm.DisplayName}");
            }

            SpawnGunAndMagazine(selectedFirearm, isBigGun);
        }

        private void SpawnFromConfigLists(bool isBigGun)
        {
            string gunListValue, magListValue;
            plugin.GetGunLists(out gunListValue, out magListValue);
            
            string gunListString = File.Exists(gunListValue) ? File.ReadAllText(gunListValue) : gunListValue;
            string[] gunList = ParseConfigList(gunListString);

            if (gunList.Length == 0)
            {
                logger.LogError("Gun list is empty after parsing.");
                return;
            }

            string selectedGun = isBigGun ? gunList[0] : gunList[UnityEngine.Random.Range(0, gunList.Length)];
            
            if (!IM.OD.ContainsKey(selectedGun))
            {
                logger.LogError($"Gun key '{selectedGun}' not found in IM.OD dictionary.");
                return;
            }

            FVRObject gunObj = IM.OD[selectedGun];
            SpawnGunAndMagazine(gunObj, isBigGun);
        }

        private void SpawnGunAndMagazine(FVRObject gunObj, bool isBigGun)
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            GameObject gunGO = Instantiate(gunObj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);

            var gunRB = gunGO.GetComponent<Rigidbody>();
            if (gunRB != null)
            {
                gunRB.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                gunRB.AddForce(GM.CurrentPlayerBody.Head.forward * 100f);
            }

            if (isBigGun)
            {
                gunGO.transform.localScale = new Vector3(5, 5, 5);
            }

            // Try to spawn matching magazine
            TrySpawnMatchingMagazine(gunObj, spawnPos, isBigGun);

            logger.LogInfo($"Spawned {(isBigGun ? "big" : "normal")} gun: {gunObj.DisplayName}");
        }

        private void TrySpawnMatchingMagazine(FVRObject gunObj, Vector3 spawnPos, bool isBigGun)
        {
            try
            {
                FVRObject selectedMagazine = null;

                // Try to get firearm component to check for compatible magazines
                var firearmComponent = gunObj.GetGameObject()?.GetComponent<FVRFireArm>();
                
                // Strategy 1: Use H3VR's built-in compatible magazines if available
                if (firearmComponent != null && HasCompatibleMagazines(firearmComponent))
                {
                    var compatibleMagazines = GetCompatibleMagazines(firearmComponent);
                    if (compatibleMagazines.Count > 0)
                    {
                        var compatibleMag = compatibleMagazines[UnityEngine.Random.Range(0, compatibleMagazines.Count)];
                        selectedMagazine = compatibleMag;
                        logger.LogInfo($"Using built-in compatible magazine: {compatibleMag.DisplayName}");
                    }
                }

                // Strategy 2: Advanced magazine matching using MagazinePatcher-style compatibility
                if (selectedMagazine == null)
                {
                    selectedMagazine = FindBestMagazineMatchAdvanced(gunObj);
                    if (selectedMagazine != null)
                    {
                        logger.LogInfo($"Using advanced MagazinePatcher compatibility: {selectedMagazine.DisplayName}");
                    }
                }

                // Strategy 3: Config file magazine matching (original H3TVR system)
                if (selectedMagazine == null && !plugin.UseItemManagerForGuns())
                {
                    selectedMagazine = FindMagazineFromConfigLists(gunObj);
                    if (selectedMagazine != null)
                    {
                        logger.LogInfo($"Using config file magazine matching: {selectedMagazine.DisplayName}");
                    }
                }

                // Strategy 4: Random magazine fallback
                if (selectedMagazine == null)
                {
                    var allMagazines = IM.OD.Values
                        .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Magazine)
                        .ToArray();

                    if (allMagazines.Length > 0)
                    {
                        selectedMagazine = allMagazines[UnityEngine.Random.Range(0, allMagazines.Length)];
                        logger.LogInfo($"Using random magazine fallback: {selectedMagazine.DisplayName}");
                    }
                }

                // Spawn the selected magazine
                if (selectedMagazine != null)
                {
                    SpawnMagazine(selectedMagazine, spawnPos, isBigGun);
                    logger.LogInfo($"Successfully spawned magazine: {selectedMagazine.DisplayName} for gun: {gunObj.DisplayName}");
                }
                else
                {
                    logger.LogWarning($"Could not find any compatible magazine for gun: {gunObj.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Magazine spawn failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if firearm has compatible magazines (safe check)
        /// </summary>
        private bool HasCompatibleMagazines(FVRFireArm firearm)
        {
            try
            {
                // Use reflection to safely check for CompatibleMagazines property
                var compatibleMagsProperty = firearm.GetType().GetProperty("CompatibleMagazines");
                if (compatibleMagsProperty != null)
                {
                    var compatibleMags = compatibleMagsProperty.GetValue(firearm, null) as System.Collections.IList;
                    return compatibleMags != null && compatibleMags.Count > 0;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get compatible magazines list (safe getter)
        /// </summary>
        private List<FVRObject> GetCompatibleMagazines(FVRFireArm firearm)
        {
            try
            {
                // Use reflection to safely get CompatibleMagazines
                var compatibleMagsProperty = firearm.GetType().GetProperty("CompatibleMagazines");
                if (compatibleMagsProperty != null)
                {
                    var compatibleMags = compatibleMagsProperty.GetValue(firearm, null) as System.Collections.IList;
                    if (compatibleMags != null)
                    {
                        return compatibleMags.Cast<FVRObject>().ToList();
                    }
                }
                return new List<FVRObject>();
            }
            catch
            {
                return new List<FVRObject>();
            }
        }

        /// <summary>
        /// Advanced magazine matching using MagazinePatcher-style compatibility scoring
        /// This ensures we use OdEaK's magazine patch system effectively
        /// </summary>
        private FVRObject FindBestMagazineMatchAdvanced(FVRObject gunObj)
        {
            // FIRST PRIORITY: Use Magazine Patcher if available
            if (OptionalDependencyManager.IsMagazinePatcherAvailable)
            {
                try
                {
                    var patcherMagazine = OptionalDependencyManager.FindCompatibleMagazine(gunObj);
                    if (patcherMagazine != null)
                    {
                        logger.LogInfo($"[WeaponManager] Found magazine via Magazine Patcher: {patcherMagazine.ItemID}");
                        return patcherMagazine;
                    }
                    
                    // Try to get enhanced compatibility list
                    var enhancedMagazines = OptionalDependencyManager.GetEnhancedMagazineCompatibility(gunObj);
                    if (enhancedMagazines.Count > 0)
                    {
                        var selectedMag = enhancedMagazines[UnityEngine.Random.Range(0, enhancedMagazines.Count)];
                        logger.LogInfo($"[WeaponManager] Found magazine from enhanced compatibility: {selectedMag.ItemID}");
                        return selectedMag;
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError($"[WeaponManager] Error using Magazine Patcher: {ex.Message}");
                }
            }

            // SECOND PRIORITY: H3VR's built-in CompatibleMagazines list
            try
            {
                var firearmComponent = gunObj.GetGameObject()?.GetComponent<FVRFireArm>();
                if (firearmComponent != null && HasCompatibleMagazines(firearmComponent))
                {
                    var compatibleMagazines = GetCompatibleMagazines(firearmComponent);
                    if (compatibleMagazines.Count > 0)
                    {
                        var magazine = compatibleMagazines[UnityEngine.Random.Range(0, compatibleMagazines.Count)];
                        logger.LogInfo($"[WeaponManager] Found magazine via H3VR CompatibleMagazines: {magazine.ItemID}");
                        return magazine;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"[WeaponManager] Could not access H3VR CompatibleMagazines: {ex.Message}");
            }

            // THIRD PRIORITY: Advanced compatibility scoring (existing logic)
            if (IM.OD == null) return null;

            FVRObject bestMatch = null;
            float bestScore = 0f;
            string gunId = gunObj.ItemID.ToLower();

            // Extract gun characteristics for matching
            string gunBase = ExtractGunBaseName(gunId);
            string gunCaliber = ExtractCaliber(gunId);

            foreach (var kvp in IM.OD)
            {
                var obj = kvp.Value;
                if (obj.Category != FVRObject.ObjectCategory.Magazine) continue;

                string magId = obj.ItemID.ToLower();
                float score = CalculateCompatibilityScore(gunId, magId, gunBase, gunCaliber);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = obj;
                }
            }

            if (bestMatch != null)
            {
                logger.LogInfo($"[WeaponManager] Found magazine via advanced scoring: {bestMatch.ItemID} (score: {bestScore:F2})");
            }

            return bestMatch;
        }

        /// <summary>
        /// Extract gun base name for compatibility matching
        /// </summary>
        private string ExtractGunBaseName(string gunId)
        {
            try
            {
                // Remove common suffixes and numbers
                string baseName = gunId.ToLower();
                baseName = System.Text.RegularExpressions.Regex.Replace(baseName, @"[0-9]+", "");
                baseName = baseName.Replace("_", "").Replace("-", "");
                
                // Take first significant portion
                if (baseName.Length > 5)
                    baseName = baseName.Substring(0, 5);
                    
                return baseName;
            }
            catch
            {
                return gunId.Length > 3 ? gunId.Substring(0, 3) : gunId;
            }
        }

        /// <summary>
        /// Extract caliber information from gun ID
        /// </summary>
        private string ExtractCaliber(string gunId)
        {
            try
            {
                // Look for caliber patterns in the ID
                var caliberPatterns = new[] { "9mm", "45acp", "762", "556", "308", "50bmg", "12ga", "20ga" };
                
                foreach (var pattern in caliberPatterns)
                {
                    if (gunId.ToLower().Contains(pattern))
                        return pattern;
                }
                
                return "";
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        /// Calculate compatibility score between gun and magazine
        /// </summary>
        private float CalculateCompatibilityScore(string gunId, string magId, string gunBase, string gunCaliber)
        {
            try
            {
                float score = 0f;
                
                // Base name matching
                if (!string.IsNullOrEmpty(gunBase) && magId.Contains(gunBase))
                    score += 50f;
                
                // Caliber matching
                if (!string.IsNullOrEmpty(gunCaliber) && magId.Contains(gunCaliber))
                    score += 30f;
                
                // Prefix matching
                if (gunId.Length >= 3 && magId.StartsWith(gunId.Substring(0, 3)))
                    score += 20f;
                
                return score;
            }
            catch
            {
                return 0f;
            }
        }
        #endregion

        #region Held Gun Management
        public void RandomizeHeldGun()
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null)
                {
                    logger.LogWarning("RandomizeHeldGun: No firearm found in hands");
                    return;
                }

                Vector3 pos = firearm.transform.position;
                Quaternion rot = firearm.transform.rotation;
                string currentKey = firearm.ObjectWrapper?.ItemID;

                Destroy(firearm.gameObject);

                // Get random replacement firearm
                var allFirearms = IM.OD.Values
                    .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Firearm && obj.ItemID != currentKey)
                    .ToArray();

                if (allFirearms.Length > 0)
                {
                    var newFirearm = allFirearms[UnityEngine.Random.Range(0, allFirearms.Length)];
                    GameObject newGunGO = Instantiate(newFirearm.GetGameObject(), pos, rot);
                    
                    var gunRB = newGunGO.GetComponent<Rigidbody>();
                    if (gunRB != null)
                    {
                        gunRB.velocity = Vector3.zero;
                        gunRB.angularVelocity = Vector3.zero;
                    }

                    logger.LogInfo($"Randomized held gun to: {newFirearm.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"RandomizeHeldGun failed: {ex.Message}");
            }
        }

        public void ToggleHeldGunFireMode()
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null)
                {
                    logger.LogWarning("ToggleHeldGunFireMode: No firearm found in hands");
                    audioManager?.PlayUISound("error"); // Play error sound
                    return;
                }

                string gunType = firearm.GetType().Name;
                logger.LogInfo($"ToggleHeldGunFireMode: Attempting to toggle fire mode on {gunType}");

                // Play fire mode toggle sound
                audioManager?.PlayWeaponSpawnSound("weapon_ready", firearm.transform.position, true);

                // Try various method names for fire mode cycling
                string[] methodNames = { 
                    "CycleFireMode", "CycleFireSelector", "ToggleFireMode", "NextFireMode",
                    "CycleSelectorMode", "AdvanceFireSelector", "SwitchFireMode"
                };

                foreach (var methodName in methodNames)
                {
                    MethodInfo mi = firearm.GetType().GetMethod(methodName, 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (mi != null && mi.GetParameters().Length == 0)
                    {
                        mi.Invoke(firearm, null);
                        logger.LogInfo($"ToggleHeldGunFireMode: Successfully toggled via method '{methodName}'");
                        audioManager?.PlayUISound("confirm"); // Play confirmation sound
                        return;
                    }
                }

                // Try field-based approach for fire selectors
                TryToggleFireSelectorField(firearm);

            }
            catch (Exception ex)
            {
                logger.LogError($"ToggleHeldGunFireMode failed: {ex.Message}");
                audioManager?.PlayUISound("error"); // Play error sound
            }
        }

        private void TryToggleFireSelectorField(FVRFireArm firearm)
        {
            string[] selectorFieldNames = { 
                "m_fireSelector", "FireSelector", "m_selector", "fireSelector", 
                "m_FireSelector", "m_fireSelectorMode", "FireSelectorMode"
            };

            foreach (var fieldName in selectorFieldNames)
            {
                FieldInfo selectorField = firearm.GetType().GetField(fieldName, 
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                
                if (selectorField != null && selectorField.FieldType.IsEnum)
                {
                    if (TryToggleEnumField(firearm, selectorField, fieldName))
                    {
                        logger.LogInfo($"ToggleHeldGunFireMode: Successfully toggled via field '{fieldName}'");
                        return;
                    }
                }
            }

            logger.LogWarning($"ToggleHeldGunFireMode: Could not find fire mode control for {firearm.GetType().Name}");
        }

        private bool TryToggleEnumField(FVRFireArm firearm, FieldInfo field, string fieldName)
        {
            try
            {
                object currentVal = field.GetValue(firearm);
                if (currentVal == null) return false;

                Array enumValues = Enum.GetValues(currentVal.GetType());
                if (enumValues.Length <= 1) return false;

                int currentIndex = Array.IndexOf(enumValues, currentVal);
                int nextIndex = (currentIndex + 1) % enumValues.Length;
                object nextVal = enumValues.GetValue(nextIndex);
                
                field.SetValue(firearm, nextVal);
                logger.LogInfo($"ToggleHeldGunFireMode: Changed {fieldName} from {currentVal} to {nextVal}");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"TryToggleEnumField failed for {fieldName}: {ex.Message}");
                return false;
            }
        }

        public void EmptyHeldGunChamber()
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null)
                {
                    logger.LogWarning("EmptyHeldGunChamber: No firearm found in hands");
                    return;
                }

                string gunType = firearm.GetType().Name;
                logger.LogInfo($"EmptyHeldGunChamber: Attempting to empty chamber on {gunType}");

                string[] methodNames = { 
                    "EjectChamberedRound", "EjectRound", "EjectChambered", "Eject", 
                    "ExtractRound", "DumpChamber", "ClearChamber", "EmptyChamber"
                };

                foreach (var methodName in methodNames)
                {
                    MethodInfo mi = firearm.GetType().GetMethod(methodName, 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (mi != null && mi.GetParameters().Length == 0) 
                    { 
                        mi.Invoke(firearm, null); 
                        logger.LogInfo($"EmptyHeldGunChamber: Successfully ejected via method '{methodName}'");
                        return; 
                    }
                }

                logger.LogWarning($"EmptyHeldGunChamber: Could not find chamber eject method for {gunType}");
            }
            catch (Exception ex)
            {
                logger.LogError($"EmptyHeldGunChamber failed: {ex.Message}");
            }
        }

        private FVRFireArm GetHeldFirearm()
        {
            var hands = GM.CurrentMovementManager?.Hands;
            if (hands == null || hands.Length == 0) return null;

            // Check right hand first, then left
            for (int handIndex = hands.Length - 1; handIndex >= 0; handIndex--)
            {
                var hand = hands[handIndex];
                if (hand?.CurrentInteractable == null) continue;

                var firearm = hand.CurrentInteractable as FVRFireArm;
                if (firearm != null) return firearm;

                // Check if it's a subclass of FVRFireArm
                if (hand.CurrentInteractable.GetType().IsSubclassOf(typeof(FVRFireArm)))
                    return (FVRFireArm)hand.CurrentInteractable;
            }
            return null;
        }
        #endregion

        #region Malfunction System
        public void ActivateMalfunctionBoost(ref bool isActive, ref float endTime)
        {
            isActive = true;
            endTime = Time.time + 120f; // MalfunctionBoostDuration constant
            logger.LogInfo("Meatyceiver malfunction boost activated for 120 seconds.");
        }

        public void ApplyMalfunctionLogic()
        {
            try
            {
                var hands = GM.CurrentMovementManager?.Hands;
                if (hands == null) return;

                foreach (var hand in hands)
                {
                    if (hand?.CurrentInteractable == null) continue;

                    var firearm = hand.CurrentInteractable as FVRFireArm;
                    if (firearm == null && hand.CurrentInteractable.GetType().IsSubclassOf(typeof(FVRFireArm))) 
                        firearm = (FVRFireArm)hand.CurrentInteractable;
                    if (firearm == null) continue;

                    // Check if it's a "meaty" weapon
                    bool isMeaty = IsMeatyWeapon(firearm);
                    if (!isMeaty) continue;

                    if (hand.Input.TriggerDown && UnityEngine.Random.value < 0.75f) // ForcedMalfunctionChance
                    {
                        ForceMalfunction(firearm);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ApplyMalfunctionLogic failed: {ex.Message}");
            }
        }

        private bool IsMeatyWeapon(FVRFireArm firearm)
        {
            try
            {
                string id = firearm.ObjectWrapper?.ItemID ?? string.Empty;
                string name = firearm.gameObject?.name ?? string.Empty;
                
                return id.IndexOf("meaty", StringComparison.OrdinalIgnoreCase) >= 0 ||
                       name.IndexOf("meaty", StringComparison.OrdinalIgnoreCase) >= 0;
            }
            catch
            {
                return false;
            }
        }

        private void ForceMalfunction(FVRFireArm firearm)
        {
            try
            {
                // Try to call malfunction methods
                string[] methods = { "ForceMalfunction", "DoMalfunction", "AttemptMalfunction", "Jam", "CauseMalfunction" };
                foreach (var methodName in methods)
                {
                    var mi = firearm.GetType().GetMethod(methodName, 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (mi != null && mi.GetParameters().Length == 0) 
                    { 
                        mi.Invoke(firearm, null); 
                        logger.LogInfo($"Forced malfunction via method: {methodName}"); 
                        return; 
                    }
                }

                // Try to set malfunction chance fields
                string[] fields = { "MalfunctionChance", "m_malfunctionChance", "JamChance", "m_jamChance" };
                foreach (var fieldName in fields)
                {
                    var fi = firearm.GetType().GetField(fieldName, 
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    
                    if (fi != null && (fi.FieldType == typeof(float) || fi.FieldType == typeof(double)))
                    {
                        if (fi.FieldType == typeof(float)) 
                            fi.SetValue(firearm, 1f); 
                        else 
                            fi.SetValue(firearm, 1.0);
                        
                        logger.LogInfo($"Set high malfunction/jam chance via field: {fieldName}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ForceMalfunction reflection failed: {ex.Message}");
            }
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Find magazine from config lists using original H3TVR method
        /// </summary>
        private FVRObject FindMagazineFromConfigLists(FVRObject gunObj)
        {
            try
            {
                string gunListValue, magListValue;
                plugin.GetGunLists(out gunListValue, out magListValue);
                
                string magazineListString = File.Exists(magListValue) ? File.ReadAllText(magListValue) : magListValue;
                string[] magazineList = ParseConfigList(magazineListString);

                if (magazineList.Length == 0) return null;

                // Use 5-character truncation method like original
                string gunTruncated = new string(gunObj.ItemID.Take(5).ToArray());
                var matchingMagazines = magazineList.Where(m => m.Contains(gunTruncated)).ToArray();

                if (matchingMagazines.Length > 0)
                {
                    string selectedMag = matchingMagazines[UnityEngine.Random.Range(0, matchingMagazines.Length)];
                    if (IM.OD.ContainsKey(selectedMag))
                    {
                        return IM.OD[selectedMag];
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"FindMagazineFromConfigLists failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Spawn a magazine at the specified position
        /// </summary>
        private void SpawnMagazine(FVRObject magObj, Vector3 spawnPos, bool isBigGun)
        {
            try
            {
                GameObject magGO = Instantiate(magObj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);
                var magRB = magGO.GetComponent<Rigidbody>();
                if (magRB != null)
                {
                    magRB.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                    magRB.AddForce(GM.CurrentPlayerBody.Head.forward * 100f);
                }

                if (isBigGun)
                {
                    magGO.transform.localScale = new Vector3(5, 5, 5);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnMagazine failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate spawn conditions
        /// </summary>
        private bool ValidateSpawnConditions()
        {
            // Implement any necessary validation logic here
            return true;
        }

        /// <summary>
        /// Parse a config list string into an array of trimmed strings
        /// </summary>
        private string[] ParseConfigList(string listString)
        {
            if (string.IsNullOrEmpty(listString)) return new string[0];

            return listString
                .Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
        }
        #endregion

        #region Gun Scale Modifier
        /// <summary>
  /// Scale held weapon to specified size for a duration
        /// </summary>
        public void ScaleHeldWeapon(float scaleFactor, float duration = 30f)
        {
     try
    {
    FVRFireArm firearm = GetHeldFirearm();
            if (firearm == null)
          {
    logger.LogWarning("ScaleHeldWeapon: No firearm found in hands");
        audioManager?.PlayUISound("error");
  return;
             }

              // Remove existing scale modifier if present
    if (activeScaleModifiers.ContainsKey(firearm))
   {
       RestoreOriginalScale(firearm);
       }

 // Store original scale
      ScaleModifierData scaleData = new ScaleModifierData
      {
            originalScale = firearm.transform.localScale,
           endTime = Time.time + duration,
  targetScale = scaleFactor
      };

  activeScaleModifiers[firearm] = scaleData;

      // Apply new scale
                firearm.transform.localScale = scaleData.originalScale * scaleFactor;

       logger.LogInfo($"ScaleHeldWeapon: Scaled {firearm.name} to {scaleFactor}x for {duration} seconds");
           audioManager?.PlayWeaponSpawnSound("weapon_ready", firearm.transform.position, true);
 audioManager?.PlayUISound("confirm");
      }
 catch (Exception ex)
            {
       logger.LogError($"ScaleHeldWeapon failed: {ex.Message}");
 audioManager?.PlayUISound("error");
          }
        }

        /// <summary>
        /// Scale held weapon to random size (0.25x to 3x)
        /// </summary>
  public void RandomScaleHeldWeapon(float duration = 30f)
        {
         try
          {
       // Generate random scale: 25% to 300%
            float[] possibleScales = { 0.25f, 0.5f, 0.75f, 1.5f, 2f, 2.5f, 3f };
float randomScale = possibleScales[UnityEngine.Random.Range(0, possibleScales.Length)];
    
    ScaleHeldWeapon(randomScale, duration);
          
   string scaleDescription = randomScale < 1f ? "tiny" : randomScale > 2f ? "giant" : "enlarged";
              logger.LogInfo($"RandomScaleHeldWeapon: Applied {scaleDescription} scale ({randomScale}x)");
  }
            catch (Exception ex)
            {
     logger.LogError($"RandomScaleHeldWeapon failed: {ex.Message}");
      }
      }

        /// <summary>
        /// Make held weapon tiny (0.25x scale)
        /// </summary>
        public void MakeHeldWeaponTiny(float duration = 30f)
        {
       ScaleHeldWeapon(0.25f, duration);
        }

     /// <summary>
        /// Make held weapon giant (3x scale)
        /// </summary>
  public void MakeHeldWeaponGiant(float duration = 30f)
        {
            ScaleHeldWeapon(3f, duration);
        }

        /// <summary>
/// Restore held weapon to original scale
        /// </summary>
        public void RestoreHeldWeaponScale()
        {
try
         {
                FVRFireArm firearm = GetHeldFirearm();
           if (firearm == null)
       {
          logger.LogWarning("RestoreHeldWeaponScale: No firearm found in hands");
          return;
 }

     if (RestoreOriginalScale(firearm))
        {
   logger.LogInfo($"RestoreHeldWeaponScale: Restored original scale for {firearm.name}");
     audioManager?.PlayUISound("confirm");
 }
      else
       {
        logger.LogWarning("RestoreHeldWeaponScale: No active scale modifier found");
        }
    }
   catch (Exception ex)
        {
       logger.LogError($"RestoreHeldWeaponScale failed: {ex.Message}");
            }
        }

 /// <summary>
        /// Update active scale modifiers (call from Update loop)
        /// </summary>
        public void UpdateScaleModifiers()
    {
            try
     {
        if (activeScaleModifiers.Count == 0) return;

            List<FVRFireArm> expiredModifiers = new List<FVRFireArm>();

       foreach (var kvp in activeScaleModifiers)
 {
            FVRFireArm firearm = kvp.Key;
         ScaleModifierData data = kvp.Value;

            // Check if modifier has expired
              if (Time.time >= data.endTime)
             {
            expiredModifiers.Add(firearm);
}
 // Check if weapon was destroyed
    else if (firearm == null || firearm.gameObject == null)
         {
  expiredModifiers.Add(firearm);
         }
  }

        // Restore expired modifiers
    foreach (var firearm in expiredModifiers)
    {
      if (firearm != null && firearm.gameObject != null)
      {
                  RestoreOriginalScale(firearm);
  logger.LogInfo($"UpdateScaleModifiers: Scale modifier expired for {firearm.name}");
           audioManager?.PlayWeaponSpawnSound("weapon_ready", firearm.transform.position, false);
         }
             else
        {
     activeScaleModifiers.Remove(firearm);
           }
           }
            }
            catch (Exception ex)
         {
        logger.LogError($"UpdateScaleModifiers failed: {ex.Message}");
 }
        }

        /// <summary>
      /// Restore original scale for a specific firearm
    /// </summary>
        private bool RestoreOriginalScale(FVRFireArm firearm)
        {
     try
            {
 if (firearm == null || !activeScaleModifiers.ContainsKey(firearm))
       return false;

                ScaleModifierData data = activeScaleModifiers[firearm];
    firearm.transform.localScale = data.originalScale;
          activeScaleModifiers.Remove(firearm);
       
                return true;
            }
        catch (Exception ex)
     {
         logger.LogError($"RestoreOriginalScale failed: {ex.Message}");
     return false;
            }
        }

        /// <summary>
        /// Clear all active scale modifiers
        /// </summary>
        public void ClearAllScaleModifiers()
    {
     try
     {
          List<FVRFireArm> firearms = new List<FVRFireArm>(activeScaleModifiers.Keys);
        
                foreach (var firearm in firearms)
                {
 if (firearm != null && firearm.gameObject != null)
{
      RestoreOriginalScale(firearm);
     }
        }

           activeScaleModifiers.Clear();
     logger.LogInfo("ClearAllScaleModifiers: Cleared all active scale modifiers");
          }
   catch (Exception ex)
            {
           logger.LogError($"ClearAllScaleModifiers failed: {ex.Message}");
            }
        }

        /// <summary>
   /// Get remaining time for held weapon scale modifier
   /// </summary>
  public float GetHeldWeaponScaleRemainingTime()
 {
          try
 {
         FVRFireArm firearm = GetHeldFirearm();
          if (firearm == null || !activeScaleModifiers.ContainsKey(firearm))
   return 0f;

     ScaleModifierData data = activeScaleModifiers[firearm];
       return Mathf.Max(0f, data.endTime - Time.time);
       }
    catch
            {
           return 0f;
      }
        }

        /// <summary>
        /// Check if held weapon has active scale modifier
      /// </summary>
        public bool IsHeldWeaponScaled()
        {
            try
         {
    FVRFireArm firearm = GetHeldFirearm();
        return firearm != null && activeScaleModifiers.ContainsKey(firearm);
            }
            catch
            {
           return false;
            }
      }
        #endregion
    }
}