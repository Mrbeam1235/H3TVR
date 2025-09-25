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
    /// Manages all weapon-related functionality with performance optimizations
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;

        // Performance caches
        private FVRObject[] cachedFirearms;
        private FVRObject[] cachedMagazines;
        private DateTime lastCacheUpdate = DateTime.MinValue;
        private const int CACHE_LIFETIME_SECONDS = 30; // Refresh cache every 30 seconds
        
        // Reflection method cache
        private readonly Dictionary<string, MethodInfo> methodCache = new Dictionary<string, MethodInfo>();
        private readonly Dictionary<string, FieldInfo> fieldCache = new Dictionary<string, FieldInfo>();

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            logger = logSource;
            
            // Pre-populate caches on initialization
            RefreshItemCaches();
        }

        private void RefreshItemCaches()
        {
            try
            {
                if (IM.OD == null) return;
                
                cachedFirearms = IM.OD.Values
                    .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Firearm)
                    .ToArray();
                    
                cachedMagazines = IM.OD.Values
                    .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Magazine)
                    .ToArray();
                    
                lastCacheUpdate = DateTime.Now;
                logger.LogInfo($"Item caches refreshed: {cachedFirearms.Length} firearms, {cachedMagazines.Length} magazines");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to refresh item caches: {ex.Message}");
            }
        }

        private bool ShouldRefreshCache()
        {
            return (DateTime.Now - lastCacheUpdate).TotalSeconds > CACHE_LIFETIME_SECONDS;
        }

        #region Gun Spawning - Optimized
        public void SpawnRandomGun(bool isBigGun)
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                // Refresh cache if needed
                if (ShouldRefreshCache())
                {
                    RefreshItemCaches();
                }

                if (plugin.UseItemManagerForGuns())
                {
                    SpawnFromItemManagerCached(isBigGun);
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

        private void SpawnFromItemManagerCached(bool isBigGun)
        {
            if (cachedFirearms == null || cachedFirearms.Length == 0)
            {
                logger.LogError("No firearms found in cached data.");
                return;
            }

            FVRObject selectedFirearm;
            if (isBigGun && cachedFirearms.Length > 0)
            {
                selectedFirearm = cachedFirearms[0]; // First gun for "big gun"
            }
            else
            {
                selectedFirearm = cachedFirearms[UnityEngine.Random.Range(0, cachedFirearms.Length)];
            }

            SpawnGunAndMagazine(selectedFirearm, isBigGun);
        }

        // ...existing SpawnFromConfigLists method remains the same...
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
            TrySpawnMatchingMagazineCached(gunObj, spawnPos, isBigGun);

            logger.LogInfo($"Spawned {(isBigGun ? "big" : "normal")} gun: {gunObj.DisplayName}");
        }

        private void TrySpawnMatchingMagazineCached(FVRObject gunObj, Vector3 spawnPos, bool isBigGun)
        {
            try
            {
                // Strategy 1: Use H3VR's built-in compatible magazines
                if (gunObj.CompatibleMagazines != null && gunObj.CompatibleMagazines.Count > 0)
                {
                    var compatibleMag = gunObj.CompatibleMagazines[UnityEngine.Random.Range(0, gunObj.CompatibleMagazines.Count)];
                    SpawnMagazine(compatibleMag, spawnPos, isBigGun);
                    logger.LogInfo($"Spawned compatible magazine: {compatibleMag.DisplayName}");
                    return;
                }

                // Strategy 2: Use cached magazines
                if (cachedMagazines != null && cachedMagazines.Length > 0)
                {
                    var randomMag = cachedMagazines[UnityEngine.Random.Range(0, cachedMagazines.Length)];
                    SpawnMagazine(randomMag, spawnPos, isBigGun);
                    logger.LogInfo($"Spawned cached magazine: {randomMag.DisplayName}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Magazine spawn failed: {ex.Message}");
            }
        }

        private void SpawnMagazine(FVRObject magObj, Vector3 basePos, bool isBig)
        {
            Vector3 magPos = basePos + Vector3.up * 0.1f + GM.CurrentPlayerBody.Head.right * UnityEngine.Random.Range(-0.1f, 0.1f);
            GameObject magGO = Instantiate(magObj.GetGameObject(), magPos, GM.CurrentPlayerBody.Head.rotation);

            var magRB = magGO.GetComponent<Rigidbody>();
            if (magRB != null)
            {
                magRB.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                magRB.AddForce(GM.CurrentPlayerBody.Head.forward * 100f);
            }

            if (isBig)
            {
                magGO.transform.localScale = new Vector3(5, 5, 5);
            }
        }

        private string[] ParseConfigList(string listString)
        {
            return listString
                .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .Where(g => g.Length > 0)
                .ToArray();
        }
        #endregion

        #region Held Gun Management - Optimized with Reflection Caching
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

                // Use cached firearms for better performance
                if (ShouldRefreshCache())
                {
                    RefreshItemCaches();
                }
                
                var availableFirearms = cachedFirearms?.Where(obj => obj.ItemID != currentKey).ToArray();
                if (availableFirearms != null && availableFirearms.Length > 0)
                {
                    var newFirearm = availableFirearms[UnityEngine.Random.Range(0, availableFirearms.Length)];
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
                string cacheKey = $"{gunType}_FireMode";
                
                // Check method cache first
                if (methodCache.ContainsKey(cacheKey))
                {
                    var cachedMethod = methodCache[cacheKey];
                    if (cachedMethod != null)
                    {
                        cachedMethod.Invoke(firearm, null);
                        logger.LogInfo($"ToggleHeldGunFireMode: Used cached method for {gunType}");
                        return;
                    }
                }

                logger.LogInfo($"ToggleHeldGunFireMode: Attempting to toggle fire mode on {gunType}");

                // Try various method names for fire mode cycling
                string[] methodNames = { 
                    "CycleFireMode", "CycleFireSelector", "ToggleFireMode", "NextFireMode",
                    "CycleSelectorMode", "AdvanceFireSelector", "SwitchFireMode"
                };

                foreach (var methodName in methodNames)
                {
                    MethodInfo mi = GetCachedMethodInfo(firearm.GetType(), methodName);
                    
                    if (mi != null && mi.GetParameters().Length == 0)
                    {
                        mi.Invoke(firearm, null);
                        methodCache[cacheKey] = mi; // Cache successful method
                        logger.LogInfo($"ToggleHeldGunFireMode: Successfully toggled via method '{methodName}'");
                        return;
                    }
                }

                // Try field-based approach for fire selectors
                TryToggleFireSelectorFieldCached(firearm, cacheKey);

            }
            catch (Exception ex)
            {
                logger.LogError($"ToggleHeldGunFireMode failed: {ex.Message}");
            }
        }

        private MethodInfo GetCachedMethodInfo(Type type, string methodName)
        {
            string cacheKey = $"{type.Name}_{methodName}";
            
            if (methodCache.ContainsKey(cacheKey))
            {
                return methodCache[cacheKey];
            }

            MethodInfo method = type.GetMethod(methodName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            methodCache[cacheKey] = method; // Cache even if null to avoid repeated lookups
            return method;
        }

        private FieldInfo GetCachedFieldInfo(Type type, string fieldName)
        {
            string cacheKey = $"{type.Name}_{fieldName}";
            
            if (fieldCache.ContainsKey(cacheKey))
            {
                return fieldCache[cacheKey];
            }

            FieldInfo field = type.GetField(fieldName, 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            fieldCache[cacheKey] = field; // Cache even if null to avoid repeated lookups
            return field;
        }

        private void TryToggleFireSelectorFieldCached(FVRFireArm firearm, string cacheKey)
        {
            string[] selectorFieldNames = { 
                "m_fireSelector", "FireSelector", "m_selector", "fireSelector", 
                "m_FireSelector", "m_fireSelectorMode", "FireSelectorMode"
            };

            foreach (var fieldName in selectorFieldNames)
            {
                FieldInfo selectorField = GetCachedFieldInfo(firearm.GetType(), fieldName);
                
                if (selectorField != null && selectorField.FieldType.IsEnum)
                {
                    if (TryToggleEnumField(firearm, selectorField, fieldName))
                    {
                        // Cache this field as the successful method for this gun type
                        methodCache[cacheKey] = null; // Mark as field-based approach
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
                string cacheKey = $"{gunType}_EmptyChamber";
                
                // Check method cache first
                if (methodCache.ContainsKey(cacheKey))
                {
                    var cachedMethod = methodCache[cacheKey];
                    if (cachedMethod != null)
                    {
                        cachedMethod.Invoke(firearm, null);
                        logger.LogInfo($"EmptyHeldGunChamber: Used cached method for {gunType}");
                        return;
                    }
                }

                logger.LogInfo($"EmptyHeldGunChamber: Attempting to empty chamber on {gunType}");

                string[] methodNames = { 
                    "EjectChamberedRound", "EjectRound", "EjectChambered", "Eject", 
                    "ExtractRound", "DumpChamber", "ClearChamber", "EmptyChamber"
                };

                foreach (var methodName in methodNames)
                {
                    MethodInfo mi = GetCachedMethodInfo(firearm.GetType(), methodName);
                    
                    if (mi != null && mi.GetParameters().Length == 0) 
                    { 
                        mi.Invoke(firearm, null); 
                        methodCache[cacheKey] = mi; // Cache successful method
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

        #region Malfunction System - Optimized
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

                    // Check if it's a "meaty" weapon - cache results
                    bool isMeaty = IsMeatyWeaponCached(firearm);
                    if (!isMeaty) continue;

                    if (hand.Input.TriggerDown && UnityEngine.Random.value < 0.75f) // ForcedMalfunctionChance
                    {
                        ForceMalfunctionCached(firearm);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"ApplyMalfunctionLogic failed: {ex.Message}");
            }
        }

        private bool IsMeatyWeaponCached(FVRFireArm firearm)
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

        private void ForceMalfunctionCached(FVRFireArm firearm)
        {
            try
            {
                string gunType = firearm.GetType().Name;
                string cacheKey = $"{gunType}_Malfunction";
                
                // Check method cache first
                if (methodCache.ContainsKey(cacheKey))
                {
                    var cachedMethod = methodCache[cacheKey];
                    if (cachedMethod != null)
                    {
                        cachedMethod.Invoke(firearm, null);
                        logger.LogInfo($"Forced malfunction via cached method for {gunType}");
                        return;
                    }
                }

                // Try to call malfunction methods
                string[] methods = { "ForceMalfunction", "DoMalfunction", "AttemptMalfunction", "Jam", "CauseMalfunction" };
                foreach (var methodName in methods)
                {
                    var mi = GetCachedMethodInfo(firearm.GetType(), methodName);
                    
                    if (mi != null && mi.GetParameters().Length == 0) 
                    { 
                        mi.Invoke(firearm, null);
                        methodCache[cacheKey] = mi; // Cache successful method
                        logger.LogInfo($"Forced malfunction via method: {methodName}"); 
                        return; 
                    }
                }

                // Try to set malfunction chance fields
                string[] fields = { "MalfunctionChance", "m_malfunctionChance", "JamChance", "m_jamChance" };
                foreach (var fieldName in fields)
                {
                    var fi = GetCachedFieldInfo(firearm.GetType(), fieldName);
                    
                    if (fi != null && (fi.FieldType == typeof(float) || fi.FieldType == typeof(double)))
                    {
                        if (fi.FieldType == typeof(float)) 
                            fi.SetValue(firearm, 1f); 
                        else 
                            fi.SetValue(firearm, 1.0);
                        
                        logger.LogInfo($"Set high malfunction/jam chance via field: {fieldName}");
                        methodCache[cacheKey] = null; // Mark as field-based approach
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

        #region Utility Methods
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
        
        private void OnDestroy()
        {
            // Clear caches on cleanup
            methodCache.Clear();
            fieldCache.Clear();
            cachedFirearms = null;
            cachedMagazines = null;
        }
        #endregion
    }
}