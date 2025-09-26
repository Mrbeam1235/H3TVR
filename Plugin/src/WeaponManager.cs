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
    /// 
    /// MAGAZINE COMPATIBILITY SYSTEM:
    /// This WeaponManager integrates with OdEaK's MagazinePatch system to provide intelligent magazine spawning.
    /// The system uses multiple strategies in order of priority:
    /// 1. H3VR's built-in CompatibleMagazines list (OdEaK's MagazinePatch integration) - HIGHEST PRIORITY
    /// 2. Advanced compatibility scoring based on MagazinePatcher methodology
    /// 3. Config file magazine matching (original H3TVR system)
    /// 4. Random magazine fallback
    /// 
    /// This ensures that spawned guns receive appropriate magazines that are compatible with OdEaK's
    /// magazine patch system while maintaining backwards compatibility with the original H3TVR configuration.
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            logger = logSource;
        }

        #region Gun Spawning
        public void SpawnRandomGun(bool isBigGun)
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

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

                // Strategy 1: Use H3VR's built-in compatible magazines (OdEaK magazine patch integration)
                if (gunObj.CompatibleMagazines != null && gunObj.CompatibleMagazines.Count > 0)
                {
                    // Use OdEaK's magazine patch system - this is the highest priority
                    var compatibleMag = gunObj.CompatibleMagazines[UnityEngine.Random.Range(0, gunObj.CompatibleMagazines.Count)];
                    selectedMagazine = compatibleMag;
                    logger.LogInfo($"Using OdEaK MagazinePatch compatible magazine: {compatibleMag.DisplayName}");
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
        /// Advanced magazine matching using MagazinePatcher-style compatibility scoring
        /// This ensures we use OdEaK's magazine patch system effectively
        /// </summary>
        private FVRObject FindBestMagazineMatchAdvanced(FVRObject gunObj)
        {
            try
            {
                // Get all magazines from ItemManager (includes OdEaK patched magazines)
                var allMagazines = IM.OD.Values
                    .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Magazine)
                    .ToArray();

                if (allMagazines.Length == 0) return null;

                var magazineScores = new List<MagazineCompatibilityScore>();

                foreach (var magazine in allMagazines)
                {
                    int score = CalculateAdvancedMagazineCompatibility(gunObj, magazine);
                    if (score > 0)
                    {
                        magazineScores.Add(new MagazineCompatibilityScore 
                        { 
                            magazine = magazine, 
                            score = score 
                        });
                    }
                }

                if (magazineScores.Count == 0) return null;

                // Sort by score and select from top tier
                magazineScores.Sort((x, y) => y.score.CompareTo(x.score));
                var bestScore = magazineScores[0].score;
                var topTierMagazines = magazineScores.Where(s => s.score >= bestScore * 0.8f).ToArray();

                var selectedMatch = topTierMagazines[UnityEngine.Random.Range(0, topTierMagazines.Length)];
                logger.LogInfo($"Advanced magazine matching - Selected: {selectedMatch.magazine.DisplayName} (Score: {selectedMatch.score})");

                return selectedMatch.magazine;
            }
            catch (Exception ex)
            {
                logger.LogError($"Advanced magazine matching failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Calculate advanced magazine compatibility using OdEaK MagazinePatcher-inspired scoring
        /// </summary>
        private int CalculateAdvancedMagazineCompatibility(FVRObject gunObj, FVRObject magObj)
        {
            int score = 0;

            try
            {
                // Highest Priority: MagazineType exact match (OdEaK's primary compatibility system)
                if (gunObj.MagazineType != 0 && magObj.MagazineType == gunObj.MagazineType)
                {
                    score += 200; // Highest priority - this is OdEaK's main compatibility method
                }

                // High Priority: RoundType compatibility (ammunition matching)
                if (gunObj.UsesRoundTypeFlag && magObj.UsesRoundTypeFlag && 
                    gunObj.RoundType != 0 && gunObj.RoundType == magObj.RoundType)
                {
                    score += 150;
                }

                // ItemID family matching (manufacturer/series compatibility)
                score += CalculateItemIdFamilyScore(gunObj.ItemID, magObj.ItemID);

                // FirearmAction compatibility
                if (gunObj.TagFirearmAction != FVRObject.OTagFirearmAction.None && 
                    gunObj.TagFirearmAction == magObj.TagFirearmAction)
                {
                    score += 100;
                }

                // Era compatibility (historical period matching)
                if (gunObj.TagEra != FVRObject.OTagEra.None && gunObj.TagEra == magObj.TagEra)
                {
                    score += 90;
                }

                // Country of origin compatibility
                if (gunObj.TagFirearmCountryOfOrigin != FVRObject.OTagFirearmCountryOfOrigin.None && 
                    gunObj.TagFirearmCountryOfOrigin == magObj.TagFirearmCountryOfOrigin)
                {
                    score += 80;
                }

                // Set compatibility (Real vs Fictional)
                if (gunObj.TagSet == magObj.TagSet)
                {
                    score += 70;
                }

                // Round power correlation
                if (gunObj.TagFirearmRoundPower != FVRObject.OTagFirearmRoundPower.None && 
                    magObj.TagFirearmRoundPower == gunObj.TagFirearmRoundPower)
                {
                    score += 60;
                }

                // Size and capacity correlation
                if (CorrelateFirearmSizeWithCapacity(gunObj.TagFirearmSize, magObj.MagazineCapacity))
                {
                    score += 50;
                }

                // Brand/manufacturer matching
                score += CalculateBrandCompatibility(gunObj.DisplayName, magObj.DisplayName);

                return score;
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Error calculating compatibility for {magObj.DisplayName}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Original Skitty gun spawning logic using config lists and 5-character truncation method
        /// This preserves the exact original behavior for Skitty guns
        /// </summary>
        private void SpawnSkittyGunFromLists(bool isBigGun)
        {
            string gunListValue, magListValue;
            plugin.GetGunLists(out gunListValue, out magListValue);
            
            // Parse gun list
            string gunListString = File.Exists(gunListValue) ? File.ReadAllText(gunListValue) : gunListValue;
            string[] gunList = ParseConfigList(gunListString);

            if (gunList.Length == 0)
            {
                logger.LogError("Skitty gun list is empty after parsing.");
                return;
            }

            // Parse magazine list
            string magazineListString = File.Exists(magListValue) ? File.ReadAllText(magListValue) : magListValue;
            string[] magazineList = ParseConfigList(magazineListString);

            string selectedGun;
            string selectedMagazine = string.Empty;

            if (isBigGun)
            {
                // Big gun: use first gun from list
                selectedGun = gunList[0];
                logger.LogInfo($"Skitty Big Gun selected: {selectedGun}");
            }
            else
            {
                // Sub gun: random selection
                int randomGunIndex = UnityEngine.Random.Range(0, gunList.Length);
                selectedGun = gunList[randomGunIndex];
                logger.LogInfo($"Skitty Sub Gun random selection: {selectedGun} (index {randomGunIndex}/{gunList.Length - 1})");
            }

            // Original 5-character truncation method for magazine matching
            string selectedGunTruncated = new string(selectedGun.Take(5).ToArray());
            logger.LogInfo($"Gun truncated for magazine matching: {selectedGunTruncated}");

            // Find matching magazines using original algorithm
            if (magazineList.Length > 0)
            {
                var matchingMagazines = magazineList.Where(m => m.Contains(selectedGunTruncated)).ToArray();
                if (matchingMagazines.Length > 0)
                {
                    int randomMagIndex = UnityEngine.Random.Range(0, matchingMagazines.Length);
                    selectedMagazine = matchingMagazines[randomMagIndex];
                    logger.LogInfo($"Selected magazine: {selectedMagazine} (index {randomMagIndex}/{matchingMagazines.Length - 1})");
                }
                else
                {
                    logger.LogWarning($"No matching magazines found for truncated gun key: {selectedGunTruncated}");
                }
            }
            else
            {
                logger.LogWarning("Magazine list is empty.");
            }

            // Validate objects exist in ItemManager
            if (!IM.OD.ContainsKey(selectedGun))
            {
                logger.LogError($"Gun key '{selectedGun}' not found in IM.OD dictionary.");
                return;
            }

            if (string.IsNullOrEmpty(selectedMagazine) || !IM.OD.ContainsKey(selectedMagazine))
            {
                logger.LogError($"Magazine key '{selectedMagazine}' not found in IM.OD dictionary.");
                return;
            }

            // Spawn gun and magazine
            FVRObject gunObj = IM.OD[selectedGun];
            FVRObject magObj = IM.OD[selectedMagazine];

            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            // Spawn gun
            GameObject gunGO = Instantiate(gunObj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);
            var gunRB = gunGO.GetComponent<Rigidbody>();
            if (gunRB != null)
            {
                gunRB.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                gunRB.AddForce(GM.CurrentPlayerBody.Head.forward * 100f);
            }

            // Spawn magazine
            GameObject magGO = Instantiate(magObj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);
            var magRB = magGO.GetComponent<Rigidbody>();
            if (magRB != null)
            {
                magRB.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                magRB.AddForce(GM.CurrentPlayerBody.Head.forward * 100f);
            }

            // Apply scaling for big gun
            if (isBigGun)
            {
                gunGO.transform.localScale = new Vector3(5, 5, 5);
                magGO.transform.localScale = new Vector3(5, 5, 5);
                logger.LogInfo("Applied 5x scaling for big gun mode");
            }

            logger.LogInfo($"Successfully spawned Skitty {(isBigGun ? "Big" : "Sub")} gun: {selectedGun} with magazine: {selectedMagazine}");
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
                    return;
                }

                string gunType = firearm.GetType().Name;
                logger.LogInfo($"ToggleHeldGunFireMode: Attempting to toggle fire mode on {gunType}");

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
                        return;
                    }
                }

                // Try field-based approach for fire selectors
                TryToggleFireSelectorField(firearm);

            }
            catch (Exception ex)
            {
                logger.LogError($"ToggleHeldGunFireMode failed: {ex.Message}");
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
        /// Calculate compatibility score based on ItemID similarity
        /// </summary>
        private int CalculateItemIdFamilyScore(string gunId, string magId)
        {
            try
            {
                if (string.IsNullOrEmpty(gunId) || string.IsNullOrEmpty(magId))
                    return 0;

                // Convert to lowercase for comparison
                gunId = gunId.ToLower();
                magId = magId.ToLower();

                // Exact match bonus
                if (gunId == magId) return 120;

                // Substring matching with different lengths
                for (int len = Math.Min(gunId.Length, magId.Length); len >= 3; len--)
                {
                    string gunPrefix = gunId.Substring(0, Math.Min(len, gunId.Length));
                    if (magId.Contains(gunPrefix))
                    {
                        return Math.Max(0, 40 + (len * 10)); // Higher score for longer matches
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Check if firearm size correlates with magazine capacity
        /// </summary>
        private bool CorrelateFirearmSizeWithCapacity(FVRObject.OTagFirearmSize gunSize, int magCapacity)
        {
            try
            {
                switch (gunSize)
                {
                    case FVRObject.OTagFirearmSize.Pocket:
                        return magCapacity <= 10;
                    case FVRObject.OTagFirearmSize.Pistol:
                        return magCapacity >= 5 && magCapacity <= 20;
                    case FVRObject.OTagFirearmSize.Compact:
                        return magCapacity >= 10 && magCapacity <= 30;
                    case FVRObject.OTagFirearmSize.Carbine:
                        return magCapacity >= 15 && magCapacity <= 40;
                    case FVRObject.OTagFirearmSize.FullSize:
                        return magCapacity >= 20 && magCapacity <= 50;
                    case FVRObject.OTagFirearmSize.Bulky:
                        return magCapacity >= 30;
                    case FVRObject.OTagFirearmSize.Oversize:
                        return magCapacity >= 50;
                    default:
                        return true; // Unknown size, don't penalize
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Calculate brand/manufacturer compatibility based on name similarity
        /// </summary>
        private int CalculateBrandCompatibility(string gunName, string magName)
        {
            try
            {
                if (string.IsNullOrEmpty(gunName) || string.IsNullOrEmpty(magName))
                    return 0;

                gunName = gunName.ToLower();
                magName = magName.ToLower();

                // Common brand/manufacturer keywords
                string[] brands = { "ak", "ar", "m4", "hk", "sig", "glock", "beretta", "colt", "fn", "steyr", "kel", "ruger" };

                foreach (var brand in brands)
                {
                    if (gunName.Contains(brand) && magName.Contains(brand))
                    {
                        return 40;
                    }
                }

                // Check for common word matches
                var gunWords = gunName.Split(' ', '-', '_').Where(w => w.Length > 2).ToArray();
                var magWords = magName.Split(' ', '-', '_').Where(w => w.Length > 2).ToArray();

                int commonWords = gunWords.Intersect(magWords).Count();
                return commonWords * 15;
            }
            catch
            {
                return 0;
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
        #endregion
    }
}