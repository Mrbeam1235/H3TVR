using System;
using BepInEx.Logging;
using FistVR;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Core sosig spawning logic for Update 120 TNH system
    /// </summary>
    public class SosigSpawner
    {
        private ManualLogSource logger;
        private SosigTemplateCache templateCache;

        public void Initialize(ManualLogSource logSource, SosigTemplateCache cache)
        {
            logger = logSource;
            templateCache = cache;
            logger?.LogInfo("[SosigSpawner] Initialized");
        }

        public Sosig SpawnModern(SosigEnemyID enemyID, Vector3 pos, Quaternion rot, int IFF)
        {
            try
            {
                logger?.LogInfo($"[SosigSpawner] SpawnModern called - ID: {enemyID}, Pos: {pos}, IFF: {IFF}");

                if (templateCache == null)
                {
                    logger?.LogError("[SosigSpawner] templateCache is null!");
                    return null;
                }

                var template = templateCache.GetTemplate(enemyID);
                if (template == null)
                {
                    logger?.LogError($"[SosigSpawner] Could not find template for SosigEnemyID: {enemyID}");
                    return null;
                }

                logger?.LogInfo($"[SosigSpawner] Got template: {template.DisplayName ?? template.SosigEnemyID.ToString()}");

                // Spawn sosig using SpawnLegacy (handles all config, weapons, armor, outfit)
                Sosig sosig = SpawnLegacy(template, pos, rot, IFF);

                if (sosig == null)
                {
                    logger?.LogError("[SosigSpawner] SpawnLegacy returned null");
                    return null;
                }

                logger?.LogInfo($"[SosigSpawner] SpawnModern completed successfully");
                return sosig;
            }
            catch (Exception ex)
            {
                logger?.LogError($"[SosigSpawner] SpawnModern failed: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        public Sosig SpawnLegacy(SosigEnemyTemplate template, Vector3 pos, Quaternion rot, int IFF)
        {
            try
            {
                logger?.LogInfo($"[SosigSpawner] SpawnLegacy called - Template: {template?.SosigEnemyID}, Pos: {pos}");

                if (template == null || template.SosigPrefabs == null || template.SosigPrefabs.Count == 0)
                {
                    logger?.LogError("[SosigSpawner] Invalid template or no prefabs available");
                    return null;
                }
                
                var prefab = template.SosigPrefabs[UnityEngine.Random.Range(0, template.SosigPrefabs.Count)];
                if (prefab == null)
                {
                    logger?.LogError("[SosigSpawner] Prefab is null");
                    return null;
                }

                var prefabGO = prefab.GetGameObject();
                if (prefabGO == null)
                {
                    logger?.LogError("[SosigSpawner] prefab.GetGameObject() returned null");
                    return null;
                }

                logger?.LogInfo($"[SosigSpawner] Instantiating prefab at {pos}");
                GameObject sosigGO = GameObject.Instantiate(prefabGO, pos, rot);
                Sosig sosig = sosigGO.GetComponentInChildren<Sosig>();

                if (sosig == null)
                {
                    logger?.LogError("[SosigSpawner] No Sosig component found on instantiated object");
                    GameObject.Destroy(sosigGO);
                    return null;
                }

                logger?.LogInfo("[SosigSpawner] Sosig component found, configuring...");

                if (template.ConfigTemplates != null && template.ConfigTemplates.Count > 0)
                {
                    var config = template.ConfigTemplates[UnityEngine.Random.Range(0, template.ConfigTemplates.Count)];
                    if (config != null)
                    {
                        sosig.Configure(config);
                    }
                }

                sosig.E.IFFCode = IFF;
                sosig.SetIFF(IFF);

                // Configure IFF chart so sosig knows who to attack
                if (sosig.Priority.IFFChart != null)
                {
                    for (int i = 0; i < sosig.Priority.IFFChart.Length; i++)
                    {
                        // Sosigs are hostile to all IFFs except their own
                        sosig.Priority.IFFChart[i] = (i != IFF);
                    }
                }

                // Apply armor based on IFF code
                int armorLevel = IFF == 0 ? SosigCustomizationUI.AllyArmor.Value : SosigCustomizationUI.EnemyArmor.Value;
                SosigArmorManager.ApplyArmorToSosig(sosig, armorLevel);

                EquipWeapons(sosig, template, pos, rot);

                // Fill ammo after weapons are equipped
                try
                {
                    sosig.Inventory.FillAllAmmo();
                }
                catch (Exception ammoEx)
                {
                    logger?.LogWarning($"[SosigSpawner] Failed to fill ammo: {ammoEx.Message}");
                }

                if (template.OutfitConfig != null && template.OutfitConfig.Count > 0)
                {
                    ApplyOutfit(sosig, template.OutfitConfig[UnityEngine.Random.Range(0, template.OutfitConfig.Count)]);
                }

                logger?.LogInfo("[SosigSpawner] SpawnLegacy completed successfully");
                return sosig;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Legacy sosig spawn failed: {ex.Message}");
                return null;
            }
        }
        
        private void EquipWeapons(Sosig sosig, SosigEnemyTemplate template, Vector3 pos, Quaternion rot)
        {
            if (!SosigCustomizationUI.EnableGuns.Value) return;
            
            try
            {
                if (template.WeaponOptions != null && template.WeaponOptions.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions[UnityEngine.Random.Range(0, template.WeaponOptions.Count)], pos, rot);
                }
                
                if (template.WeaponOptions_Secondary != null && template.WeaponOptions_Secondary.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions_Secondary[UnityEngine.Random.Range(0, template.WeaponOptions_Secondary.Count)], pos, rot);
                }
                
                if (template.WeaponOptions_Tertiary != null && template.WeaponOptions_Tertiary.Count > 0)
                {
                    EquipWeapon(sosig, template.WeaponOptions_Tertiary[UnityEngine.Random.Range(0, template.WeaponOptions_Tertiary.Count)], pos, rot);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Weapon equip failed: {ex.Message}");
            }
        }
        
        private void EquipWeapon(Sosig sosig, FVRObject weaponObj, Vector3 pos, Quaternion rot)
        {
            try
            {
                if (weaponObj?.GetGameObject() == null) return;
                
                GameObject weaponGO = GameObject.Instantiate(weaponObj.GetGameObject(), pos + Vector3.up * 0.1f, rot);
                SosigWeapon weapon = weaponGO.GetComponent<SosigWeapon>();
                
                if (weapon != null)
                {
                    weapon.SetAutoDestroy(true);
                    weapon.O.SpawnLockable = false;
                    weapon.SetAmmoClamping(true);
                    weapon.IsShakeReloadable = false;
                    
                    if (weapon.Type == SosigWeapon.SosigWeaponType.Gun)
                    {
                        sosig.Inventory.FillAmmoWithType(weapon.AmmoType);
                    }
                    
                    sosig.Inventory.Init();
                    sosig.Inventory.FillAllAmmo();
                    sosig.InitHands();
                    sosig.ForceEquip(weapon);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Weapon equip error: {ex.Message}");
            }
        }
        
        private void ApplyOutfit(Sosig sosig, SosigOutfitConfig outfit)
        {
            try
            {
                if (outfit == null || sosig.Links.Count < 4) return;
                
                if (UnityEngine.Random.value < outfit.Chance_Headwear)
                    SpawnAccessory(outfit.Headwear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Facewear)
                    SpawnAccessory(outfit.Facewear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Eyewear)
                    SpawnAccessory(outfit.Eyewear, sosig.Links[0]);
                if (UnityEngine.Random.value < outfit.Chance_Torsowear)
                    SpawnAccessory(outfit.Torsowear, sosig.Links[1]);
                if (UnityEngine.Random.value < outfit.Chance_Pantswear)
                    SpawnAccessory(outfit.Pantswear, sosig.Links[2]);
                if (sosig.Links.Count > 3 && UnityEngine.Random.value < outfit.Chance_Pantswear_Lower)
                    SpawnAccessory(outfit.Pantswear_Lower, sosig.Links[3]);
                if (UnityEngine.Random.value < outfit.Chance_Backpacks)
                    SpawnAccessory(outfit.Backpacks, sosig.Links[1]);
                if (UnityEngine.Random.value < outfit.Chance_TorosDecoration)
                    SpawnAccessory(outfit.TorosDecoration, sosig.Links[1]);
            }
            catch (Exception ex)
            {
                logger?.LogError($"Outfit apply failed: {ex.Message}");
            }
        }
        
        private void SpawnAccessory(System.Collections.Generic.List<FVRObject> accessories, SosigLink link)
        {
            if (accessories == null || accessories.Count == 0 || link == null) return;
            
            try
            {
                var accessory = accessories[UnityEngine.Random.Range(0, accessories.Count)];
                if (accessory?.GetGameObject() == null) return;
                
                GameObject accessoryGO = GameObject.Instantiate(accessory.GetGameObject(), link.transform.position, link.transform.rotation);
                accessoryGO.transform.SetParent(link.transform);
                
                var wearable = accessoryGO.GetComponent<SosigWearable>();
                if (wearable != null)
                {
                    wearable.RegisterWearable(link);
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Accessory spawn failed: {ex.Message}");
            }
        }
    }
}
