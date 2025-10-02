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
            if (GM.CurrentPlayerBody?.Head == null)
            {
                logger.LogWarning("Cannot spawn weapon: Player head reference is null");
                return false;
            }

            if (IM.OD == null)
            {
                logger.LogWarning("Cannot spawn weapon: ItemManager ObjectDictionary is null");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parse configuration list strings (used by Skitty gun spawning)
        /// </summary>
        private string[] ParseConfigList(string listString)
        {
            return listString
                .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .Where(g => g.Length > 0)
                .ToArray();
        }

        /// <summary>
        /// Get a random weapon from ItemManager
        /// </summary>
        private FVRObject GetRandomWeapon()
        {
            try
            {
                var allFirearms = IM.OD.Values
                    .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Firearm)
                    .ToArray();

                if (allFirearms.Length > 0)
                {
                    return allFirearms[UnityEngine.Random.Range(0, allFirearms.Length)];
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError($"GetRandomWeapon failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Spawn a magazine for a specific weapon
        /// </summary>
        private void SpawnMagazineForWeapon(FVRFireArm firearm, FVRObject magazineObj)
        {
            try
            {
                if (firearm.MagazineEjectPos == null || magazineObj == null)
                    return;

                Vector3 spawnPos = firearm.MagazineEjectPos.position + Vector3.up * 0.1f;
                GameObject magGO = Instantiate(magazineObj.GetGameObject(), spawnPos, firearm.transform.rotation);
                
                var magRB = magGO.GetComponent<Rigidbody>();
                if (magRB != null)
                {
                    magRB.AddForce(Vector3.up * 50f);
                }

                logger.LogInfo($"Spawned magazine {magazineObj.ItemID} for weapon {firearm.name}");
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnMagazineForWeapon failed: {ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// Spawn a random weapon with enhanced functionality using optional dependencies
        /// </summary>
        public void SpawnRandomWeapon(Vector3 position, Quaternion rotation, bool forceRandomMagazine = false)
        {
            try
            {
                if (IM.OD == null || IM.OD.Count == 0)
                {
                    logger.LogError("ItemManager not ready for weapon spawning");
                    return;
                }

                FVRObject weaponObj = GetRandomWeapon();
                if (weaponObj?.GetGameObject() == null)
                {
                    logger.LogError("Failed to get valid weapon object");
                    return;
                }

                // Spawn the weapon
                GameObject weaponGO = UnityEngine.Object.Instantiate(weaponObj.GetGameObject(), position, rotation);
                if (weaponGO == null)
                {
                    logger.LogError("Failed to instantiate weapon");
                    return;
                }

                logger.LogInfo($"[WeaponManager] Spawned weapon: {weaponObj.ItemID}");

                var firearm = weaponGO.GetComponent<FVRFireArm>();
                if (firearm != null)
                {
                    // STOVEPIPE INTEGRATION: Check if weapon can jam
                    if (OptionalDependencyManager.IsStovepipeAvailable && OptionalDependencyManager.CanFirearmJam(firearm))
                    {
                        logger.LogInfo($"[WeaponManager] Weapon {weaponObj.ItemID} is compatible with Stovepipe jamming");
                        
                        // Randomly apply jamming (10% chance)
                        if (UnityEngine.Random.value < 0.1f)
                        {
                            if (OptionalDependencyManager.TryTriggerStovepipeJam(firearm))
                            {
                                logger.LogInfo($"[WeaponManager] Applied Stovepipe jam to {weaponObj.ItemID}");
                            }
                        }
                    }

                    // MEATYCEIVER 2 INTEGRATION: Enhanced transformation system
                    if (MeatyceiverIntegrationManager.IsIntegrationEnabled())
                    {
                        logger.LogInfo($"[WeaponManager] Weapon {weaponObj.ItemID} is compatible with Meatyceiver 2");
                        
                        // Apply transformation with player context and 5% chance
                        if (MeatyceiverIntegrationManager.TryTransformWeapon(firearm, "Player"))
                        {
                            logger.LogInfo($"[WeaponManager] Applied Meatyceiver 2 transformation to {weaponObj.ItemID}");
                        }
                    }

                    // MAGAZINE PATCHER INTEGRATION: Spawn compatible magazine
                    if (firearm.MagazineEjectPos != null)
                    {
                        FVRObject magazineObj = FindBestMagazineMatchAdvanced(weaponObj);
                        if (magazineObj != null)
                        {
                            SpawnMagazineForWeapon(firearm, magazineObj);
                        }
                        else if (OptionalDependencyManager.IsMagazinePatcherAvailable)
                        {
                            logger.LogWarning($"[WeaponManager] Magazine Patcher available but no compatible magazine found for {weaponObj.ItemID}");
                        }
                    }
                }

                // Play spawn sound effect
                audioManager?.PlayWeaponSpawnSound("spawn", position, true);

                weaponSpawnCount++;
                lastWeaponSpawnTime = Time.time;

                logger.LogInfo($"[WeaponManager] Successfully spawned weapon with enhanced features: {weaponObj.ItemID}");
            }
            catch (Exception ex)
            {
                logger.LogError($"[WeaponManager] Error in enhanced weapon spawning: {ex.Message}");
                logger.LogError($"Stack trace: {ex.StackTrace}");
            }
        }

        #region Stovepipe Integration
        /// <summary>
        /// Try to jam a held weapon using Stovepipe
        /// </summary>
        public void JamHeldWeapon(string context = "Player", StovepipeIntegrationManager.MalfunctionType jamType = StovepipeIntegrationManager.MalfunctionType.None)
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null)
                {
                    logger.LogWarning("JamHeldWeapon: No firearm found in hands");
                    audioManager?.PlayUISound("error");
                    return;
                }

                if (!OptionalDependencyManager.IsStovepipeAvailable)
                {
                    logger.LogWarning("JamHeldWeapon: Stovepipe not available");
                    audioManager?.PlayUISound("error");
                    return;
                }

                bool success;
                if (jamType != StovepipeIntegrationManager.MalfunctionType.None)
                {
                    // Force specific jam type
                    success = OptionalDependencyManager.ForceStovepipeMalfunction(firearm, jamType, context);
                }
                else
                {
                    // Let Stovepipe determine jam type
                    success = OptionalDependencyManager.TryTriggerStovepipeJam(firearm, context, 1.0f); // 100% chance
                }

                if (success)
                {
                    logger.LogInfo($"JamHeldWeapon: Successfully jammed {firearm.name} with {jamType} malfunction");
                    audioManager?.PlayUISound("confirm");
                }
                else
                {
                    logger.LogWarning($"JamHeldWeapon: Failed to jam {firearm.name}");
                    audioManager?.PlayUISound("error");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"JamHeldWeapon failed: {ex.Message}");
                audioManager?.PlayUISound("error");
            }
        }

        /// <summary>
        /// Clear jam from held weapon
        /// </summary>
        public void ClearHeldWeaponJam()
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null)
                {
                    logger.LogWarning("ClearHeldWeaponJam: No firearm found in hands");
                    audioManager?.PlayUISound("error");
                    return;
                }

                if (!OptionalDependencyManager.IsStovepipeAvailable)
                {
                    logger.LogWarning("ClearHeldWeaponJam: Stovepipe not available");
                    audioManager?.PlayUISound("error");
                    return;
                }

                if (!OptionalDependencyManager.IsFirearmJammed(firearm))
                {
                    logger.LogInfo("ClearHeldWeaponJam: Weapon is not jammed");
                    audioManager?.PlayUISound("confirm");
                    return;
                }

                bool success = OptionalDependencyManager.ClearFirearmJam(firearm);
                if (success)
                {
                    logger.LogInfo($"ClearHeldWeaponJam: Successfully cleared jam from {firearm.name}");
                    audioManager?.PlayUISound("confirm");
                }
                else
                {
                    logger.LogWarning($"ClearHeldWeaponJam: Failed to clear jam from {firearm.name}");
                    audioManager?.PlayUISound("error");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ClearHeldWeaponJam failed: {ex.Message}");
                audioManager?.PlayUISound("error");
            }
        }

        /// <summary>
        /// Check if held weapon is jammed
        /// </summary>
        public bool IsHeldWeaponJammed()
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null || !OptionalDependencyManager.IsStovepipeAvailable)
                    return false;

                return OptionalDependencyManager.IsFirearmJammed(firearm);
            }
            catch (Exception ex)
            {
                logger.LogError($"IsHeldWeaponJammed failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Apply random malfunction to held weapon (for testing/fun)
        /// </summary>
        public void ApplyRandomMalfunction()
        {
            try
            {
                var malfunctionTypes = Enum.GetValues(typeof(StovepipeIntegrationManager.MalfunctionType))
                    .Cast<StovepipeIntegrationManager.MalfunctionType>()
                    .Where(t => t != StovepipeIntegrationManager.MalfunctionType.None)
                    .ToArray();

                if (malfunctionTypes.Length > 0)
                {
                    var randomType = malfunctionTypes[UnityEngine.Random.Range(0, malfunctionTypes.Length)];
                    JamHeldWeapon("Random", randomType);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ApplyRandomMalfunction failed: {ex.Message}");
            }
        }
        #endregion
    }
}