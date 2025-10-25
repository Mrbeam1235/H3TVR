using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx.Logging;
using System;

namespace H3TVR
{
    /// <summary>
    /// Simple chat sosig statistics class
    /// </summary>
    public class ChatSosigStats
    {
        public int activeSosigCount { get; set; }
        public int friendlyCount { get; set; }
        public int enemyCount { get; set; }
        public int queuedSpawns { get; set; }
        public int totalSpawned { get; set; }
    }

    public class SpawnManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;
        private AdvancedChatSosigSpawner advancedChatSpawner;
        private AudioManager audioManager;

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource, AdvancedChatSosigSpawner chatSpawnerInstance, AudioManager audioManagerInstance)
        {
            plugin = pluginInstance;
            logger = logSource;
            advancedChatSpawner = chatSpawnerInstance;
            audioManager = audioManagerInstance;

            // Initialize dependency-aware systems
            OptionalDependencyManager.Initialize(logger);
            SosigWeaponEnhancer.Initialize(logger);

            logger.LogInfo("[SpawnManager] Spawn manager initialized successfully");
            
            // Log enhancement status
            if (OptionalDependencyManager.HasAnyDependencies())
            {
                logger.LogInfo($"[SpawnManager] Enhanced sosig spawning active with {OptionalDependencyManager.GetAvailableDependencyCount()} dependencies");
            }
        }

        // H3TVR legacy spawn methods required by InputHandler
        public void SpawnWonderfulToy()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            // Play before-action sound
            audioManager?.PlayWondertoySound("before_spawn", spawnPos, true, "wondertoy/wondertoy_appear.wav");
            
            SpawnObject("TippyToyAnton", "WonderToy");
            
            // Play after-action sound
            audioManager?.PlayWondertoySound("after_spawn", spawnPos, true, "wondertoy/wondertoy_ready.wav");
        }

        public void SpawnJeditToy()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            audioManager?.PlayWondertoySound("before_activate", spawnPos, true, "wondertoy/jedi_ignite.wav");
            
            try
            {
                if (!ValidateSpawnConditions()) return;

                // Correct Item ID for Jedit Tippy Toy mod
                string jeditToyID = "ftw.JediTippyToy";
                
                if (!IM.OD.ContainsKey(jeditToyID))
                {
                    logger.LogWarning("Jedit Tippy Toy not available. Install: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/");
                    logger.LogInfo($"Expected Item ID: {jeditToyID}");
                    
                    // List all tippy toy items for debugging
                    logger.LogInfo("Available Tippy Toy items:");
                    foreach (var kvp in IM.OD)
                    {
                        if (kvp.Key.ToLower().Contains("tippy") || kvp.Key.ToLower().Contains("jedi"))
                            logger.LogInfo($"  - {kvp.Key}");
                    }
                    return;
                }

                FVRObject obj = IM.OD[jeditToyID];
                GameObject go = Instantiate(obj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);

                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                    rb.AddForce(GM.CurrentPlayerBody.Head.forward * 25);
                }

                logger.LogInfo($"Successfully spawned Jedit Tippy Toy (ID: {jeditToyID})");
                audioManager?.PlayWondertoySound("after_activate", spawnPos, true, "wondertoy/jedi_ready.wav");
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnJeditToy failed: {ex.Message}");
            }
        }


        public void SpawnHydration()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            // Play before-action sound
            audioManager?.PlayHydrationSound("before_spawn", spawnPos, true, "hydration/bottle_materialize.wav");
            
            SpawnObject("SuppressorBottle", "Hydration");
            
            // Play after-action sound
            audioManager?.PlayHydrationSound("after_spawn", spawnPos, true, "hydration/bottle_ready.wav");
        }

        public void SpawnPillow()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                
                int minCount, maxCount;
                plugin.GetPillowConfig(out minCount, out maxCount);
                int pillowCount = UnityEngine.Random.Range(minCount, maxCount + 1);
                logger.LogInfo($"Spawning {pillowCount} pillow(s)");

                // Play before-action sound with custom volume
                audioManager?.PlayWondertoySound("before_pillow", spawnPos, true, "pillow/pillow_summon.wav", 0.8f);

                for (int i = 0; i < pillowCount; i++)
                {
                    if (!IM.OD.ContainsKey("BodyPillow"))
                    {
                        logger.LogError("BodyPillow not found in ObjectDictionary");
                        return;
                    }

                    FVRObject obj = IM.OD["BodyPillow"];
                    Vector3 spawnPosition = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                    GameObject go = Instantiate(obj.GetGameObject(), spawnPosition, GM.CurrentPlayerBody.Head.rotation);

                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(GM.CurrentPlayerBody.Head.forward * 4000f);
                    }
                }

                // Play after-action sound
                audioManager?.PlayWondertoySound("after_pillow", spawnPos, true, "pillow/pillow_launched.wav", 0.6f);

                // Handle pillow effects
                HandlePillowEffects();
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnPillow failed: {ex.Message}");
            }
        }

        public void SpawnShuri()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                int minCount, maxCount;
                plugin.GetShurikenConfig(out minCount, out maxCount);
                float scale = plugin.GetShurikenScale();
                
                int shurikenCount = UnityEngine.Random.Range(minCount, maxCount + 1);
                logger.LogInfo($"Spawning {shurikenCount} shuriken(s)");

                Vector3 shuriPosition = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 0.02f);
                
                // Play before-action sound
                audioManager?.PlayShurikenSound("before_throw", shuriPosition, true, "shuriken/shuriken_prepare.wav", 0.9f);

                if (!IM.OD.ContainsKey("Shuriken"))
                {
                    logger.LogError("Shuriken not found in ObjectDictionary");
                    return;
                }

                FVRObject obj = IM.OD["Shuriken"];
                Quaternion shuriRotation = Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward);

                for (int i = 0; i < shurikenCount; i++)
                {
                    GameObject go = Instantiate(obj.GetGameObject(), shuriPosition, shuriRotation);
                    go.transform.localScale = new Vector3(scale, scale, scale);
                    
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = GM.CurrentPlayerBody.Head.forward * 30.0f;
                    }

                    Destroy(go, 60f);
                }

                // Play after-action sound
                audioManager?.PlayShurikenSound("after_throw", shuriPosition, true, "shuriken/shuriken_thrown.wav", 0.7f);
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnShuri failed: {ex.Message}");
            }
        }

        public void SpawnSkittySubGun()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            // Play before-action sound
            audioManager?.PlayWeaponSpawnSound("before_spawn", spawnPos, true, "weapons/weapon_materializing.wav", 0.8f);
            
            SpawnObject("SkittySubGun", "SkittySubGun");

            // Play after-action sound
            audioManager?.PlayWeaponSpawnSound("after_spawn", spawnPos, true, "weapons/weapon_ready.wav", 0.7f);
        }

        public void SpawnSkittyBigGun()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            // Play before-action sound
            audioManager?.PlayWeaponSpawnSound("before_big_spawn", spawnPos, true, "weapons/big_gun_materializing.wav", 0.9f);
            
            var weaponManager = plugin.GetWeaponManager();
            if (weaponManager != null)
            {
                weaponManager.SpawnRandomGun(true);
            }

            // Play after-action sound
            audioManager?.PlayWeaponSpawnSound("after_big_spawn", spawnPos, true, "weapons/big_gun_ready.wav", 0.8f);
        }

        /// <summary>
        /// Spawn Air Strike Smoke Grenade from JerryAr
        /// Spawns from player head forward
        /// Mod: https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/
        /// </summary>
        public void SpawnAirStrikeGrenade()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                
                // Play before-action sound
                audioManager?.PlayDangerCloseSound("before_airstrike", spawnPos, true, "danger_close/airstrike_call.wav", 0.9f);

                // Air Strike Smoke Grenade Item ID
                string airStrikeID = "JerryAr_AirStrikeSmokeGrenade";
                
                if (!IM.OD.ContainsKey(airStrikeID))
                {
                    logger.LogWarning("Air Strike Smoke Grenade not available. Install: https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/");
                    logger.LogInfo($"Expected Item ID: {airStrikeID}");
                    
                    // List all grenade items for debugging
                    logger.LogInfo("Available grenade items:");
                    foreach (var kvp in IM.OD)
                    {
                        if (kvp.Key.ToLower().Contains("grenade") || kvp.Key.ToLower().Contains("smoke") || kvp.Key.ToLower().Contains("airstrike"))
                            logger.LogInfo($"  - {kvp.Key}");
                    }
                    return;
                }

                FVRObject obj = IM.OD[airStrikeID];
                GameObject go = Instantiate(obj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);

                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(GM.CurrentPlayerBody.Head.forward * 500f);
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f);
                }

                logger.LogInfo($"Successfully spawned Air Strike Smoke Grenade (ID: {airStrikeID})");
                
                // Play after-action sound
                audioManager?.PlayDangerCloseSound("after_airstrike", spawnPos, true, "danger_close/airstrike_deployed.wav", 0.8f);
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnAirStrikeGrenade failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn Titan Machine as AI enemy
        /// Spawns in front of player as hostile sosig-like entity
        /// Mod: https://thunderstore.io/c/h3vr/p/JerryAr/TitanMachine/
        /// </summary>
        public void SpawnTitanMachine()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 5f);
                
                // Play before-action sound
                audioManager?.PlayWeaponSpawnSound("before_titan", spawnPos, true, "weapons/titan_materializing.wav", 1.0f);

                // Titan Machine Item ID
                string titanID = "JerryAr_TitanMachine";
                
                if (!IM.OD.ContainsKey(titanID))
                {
                    logger.LogWarning("Titan Machine not available. Install: https://thunderstore.io/c/h3vr/p/JerryAr/TitanMachine/");
                    logger.LogInfo($"Expected Item ID: {titanID}");
                    
                    // List all titan/machine items for debugging
                    logger.LogInfo("Available machine/titan items:");
                    foreach (var kvp in IM.OD)
                    {
                        if (kvp.Key.ToLower().Contains("titan") || kvp.Key.ToLower().Contains("machine") || kvp.Key.ToLower().Contains("robot"))
                            logger.LogInfo($"  - {kvp.Key}");
                    }
                    return;
                }

                FVRObject obj = IM.OD[titanID];
                
                // Spawn at ground level in front of player
                Quaternion spawnRot = Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward);
                GameObject go = Instantiate(obj.GetGameObject(), spawnPos, spawnRot);

                // Try to configure as hostile AI if it has sosig-like components
                var sosig = go.GetComponent<Sosig>();
                if (sosig != null)
                {
                    // Set as enemy
                    sosig.SetIFF(1); // Enemy team
                    sosig.SetAssaultSpeed(Sosig.SosigMoveSpeed.Running);
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                    
                    logger.LogInfo("Titan Machine configured as hostile AI");
                }
                else
                {
                    logger.LogInfo("Titan Machine spawned (no sosig component detected - may have custom AI)");
                }

                logger.LogInfo($"Successfully spawned Titan Machine (ID: {titanID})");
                
                // Play after-action sound
                audioManager?.PlayWeaponSpawnSound("after_titan", spawnPos, true, "weapons/titan_active.wav", 0.9f);
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnTitanMachine failed: {ex.Message}");
            }
        }

        public void DangerCloseBarrage()
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("Cartridge50mmFlareDangerClose")) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                
                // Play before-action danger close warning
                audioManager?.PlayDangerCloseSound("before_barrage", spawnPos, true, "danger_close/incoming_artillery.wav", 1.0f);

                int minCount, maxCount;
                plugin.GetDangerCloseConfig(out minCount, out maxCount);
                int dangerCloseCount = UnityEngine.Random.Range(minCount, maxCount + 1);
                logger.LogInfo($"Spawning {dangerCloseCount} danger close round(s)");

                FVRObject obj = IM.OD["Cartridge50mmFlareDangerClose"];

                for (int i = 0; i < dangerCloseCount; i++)
                {
                    float howFast = 30.0f;
                    float maxAngle = 2.0f;
                    Vector2 randRot = UnityEngine.Random.insideUnitCircle;

                    Vector3 dangerClosePosition = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 0.02f);
                    GameObject go = Instantiate(obj.GetGameObject(), dangerClosePosition, Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward));

                    go.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = go.transform.forward * howFast;
                    }

                    FVRFireArmRound cartridge = go.GetComponent<FVRFireArmRound>();
                    if (cartridge != null)
                    {
                        TryExplodeCartridge(cartridge, 0.5f);
                        
                        // Play explosion sound with delay
                        StartCoroutine(PlayDelayedExplosionSound(dangerClosePosition, 0.5f));
                    }
                }

                // Play after-action sound
                audioManager?.PlayDangerCloseSound("after_barrage", spawnPos, true, "danger_close/barrage_complete.wav", 0.8f);
            }
            catch (Exception ex)
            {
                logger.LogError($"DangerCloseBarrage failed: {ex.Message}");
            }
        }

        private IEnumerator PlayDelayedExplosionSound(Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);
            audioManager?.PlayDangerCloseSound("explosion", position, true, "danger_close/explosion_impact.wav", 0.9f);
        }

        public void DestroyQuickbelt()
        {
            try
            {
                Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
                
                // Play before-action destruction sound
                audioManager?.PlayDestructionSound("before_destroy", playerPos, false, "destruction/quickbelt_clearing.wav", 0.7f);

                FVRQuickBeltSlot[] allSlots = UnityEngine.Object.FindObjectsOfType<FVRQuickBeltSlot>();
                if (allSlots == null || allSlots.Length == 0)
                {
                    logger.LogInfo("No quickbelt slots found in scene.");
                    return;
                }

                int destroyedCount = 0;
                foreach (var slot in allSlots)
                {
                    var obj = slot?.CurObject;
                    if (obj == null) continue;

                    // Skip if the object is a magazine - preserve magazines
                    if (obj is FVRFireArmMagazine)
                    {
                        continue;
                    }

                    // Detach from slot first
                    obj.SetQuickBeltSlot(null);

                    // Destroy the object completely
                    Destroy(obj.gameObject);
                    
                    destroyedCount++;
                }

                // Spawn celebratory shell if items were destroyed
                if (destroyedCount > 0)
                {
                    SpawnCelebratoryShell();
                    
                    // Play after-action sound
                    audioManager?.PlayDestructionSound("after_destroy", playerPos, false, "destruction/quickbelt_cleared.wav", 0.6f);
                    
                    logger.LogInfo($"Destroyed {destroyedCount} quickbelt object(s) (magazines preserved).");
                }
                else
                {
                    logger.LogInfo("No items in quickbelt to destroy (magazines excluded).");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"DestroyQuickbelt failed: {ex.Message}");
            }
        }

        // Chat Sosig methods - using Advanced Chat Spawner
        public void SpawnChatSosigFriendly()
        {
            var advancedSpawner = AdvancedChatSosigSpawner.Instance;
            if (advancedSpawner != null)
            {
                advancedSpawner.QueueSpawn("Player", "Test Ally", true);
                logger?.LogInfo("Queued friendly chat sosig spawn");
            }
            else
            {
                logger?.LogWarning("Advanced Chat Spawner not available");
            }
        }

        public void SpawnChatSosigEnemy()
        {
            var advancedSpawner = AdvancedChatSosigSpawner.Instance;
            if (advancedSpawner != null)
            {
                advancedSpawner.QueueSpawn("Player", "Test Enemy", false);
                logger?.LogInfo("Queued enemy chat sosig spawn");
            }
            else
            {
                logger?.LogWarning("Advanced Chat Spawner not available");
            }
        }

        public void ClearAllChatSosigs()
        {
            var advancedSpawner = AdvancedChatSosigSpawner.Instance;
            if (advancedSpawner != null)
            {
                advancedSpawner.ClearAllSosigs();
                logger?.LogInfo("Cleared all chat sosigs");
            }
            else
            {
                logger?.LogWarning("Advanced Chat Spawner not available");
            }
        }

        public ChatSosigStats GetChatSosigStats()
        {
            var advancedSpawner = AdvancedChatSosigSpawner.Instance;
            if (advancedSpawner != null)
            {
                var stats = advancedSpawner.GetStats();
                return new ChatSosigStats
                {
                    friendlyCount = stats.Allies
                };
            }
            return new ChatSosigStats { friendlyCount = 0 };
        }

        // Helper methods
        private void SpawnObject(string itemID, string objectName)
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                if (!IM.OD.ContainsKey(itemID))
                {
                    logger.LogError($"Item '{itemID}' not found in ObjectDictionary for {objectName}");
                    return;
                }

                FVRObject obj = IM.OD[itemID];
                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                Quaternion spawnRot = GM.CurrentPlayerBody.Head.rotation;
                
                GameObject go = Instantiate(obj.GetGameObject(), spawnPos, spawnRot);
                
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                    rb.AddForce(GM.CurrentPlayerBody.Head.forward * 25f);
                }

                logger.LogInfo($"Successfully spawned {objectName}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to spawn {objectName}: {ex.Message}");
            }
        }

        private void SpawnGrenade(string grenadeID, string grenadeName, float force, bool shouldArm)
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey(grenadeID)) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                
                // Play before-action sound
                audioManager?.PlayDangerCloseSound("before_grenade", spawnPos, true, "grenades/pin_pull.wav", 0.8f);

                FVRObject obj = IM.OD[grenadeID];
                GameObject go = Instantiate(obj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);

                if (shouldArm)
                {
                    PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
                    if (grenade != null)
                    {
                        grenade.ReleaseLever();
                    }
                }

                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(GM.CurrentPlayerBody.Head.forward * force);
                }

                // Play after-action sound
                audioManager?.PlayDangerCloseSound("after_grenade", spawnPos, true, "grenades/grenade_thrown.wav", 0.7f);

                logger.LogInfo($"Spawned {grenadeName}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Spawn{grenadeName} failed: {ex.Message}");
            }
        }

        public void SpawnFlash()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            audioManager?.PlayDangerCloseSound("before_flash", spawnPos, true, "grenades/flashbang_prepare.wav", 0.9f);
            SpawnGrenade("PinnedGrenadeXM84", "Flash", 500f, true);
        }

        public void SpawnFlash2()
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("PinnedGrenadeXM84")) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                audioManager?.PlayDangerCloseSound("before_multiflash", spawnPos, true, "grenades/multiple_flashbang.wav", 1.0f);

                FVRObject obj = IM.OD["PinnedGrenadeXM84"];

                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f;
                    Vector3 offsetDirection = new Vector3(
                        Mathf.Sin(angle * Mathf.Deg2Rad) * 0.3f,
                        UnityEngine.Random.Range(-0.1f, 0.2f),
                        Mathf.Cos(angle * Mathf.Deg2Rad) * 0.3f
                    );

                    Vector3 spawnPosition = GM.CurrentPlayerBody.Head.position + 
                                          GM.CurrentPlayerBody.Head.TransformDirection(offsetDirection) + 
                                          new Vector3(0f, 0.25f, 0f);

                    GameObject go = Instantiate(obj.GetGameObject(), spawnPosition, GM.CurrentPlayerBody.Head.rotation);
                    go.transform.Rotate(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(-15f, 15f), 0f);

                    PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
                    if (grenade != null)
                    {
                        grenade.ReleaseLever();
                    }

                    Vector3 forceDirection = GM.CurrentPlayerBody.Head.forward + 
                                           new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 
                                                      UnityEngine.Random.Range(-0.1f, 0.3f), 
                                                      UnityEngine.Random.Range(-0.2f, 0.2f));
                    
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.AddForce(forceDirection * UnityEngine.Random.Range(400f, 600f));
                    }
                }

                // Play after-action sound
                audioManager?.PlayDangerCloseSound("after_multiflash", spawnPos, true, "grenades/flashbangs_thrown.wav", 0.8f);

                logger.LogInfo("Spawned Flash2 (4 flashbangs)");
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnFlash2 failed: {ex.Message}");
            }
        }

        public void SpawnNadeRain()
        {
            try
            {
                // 10% spawn chance
                if (UnityEngine.Random.Range(1, 11) != 1) return;
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("PinnedGrenadeM67")) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                audioManager?.PlayDangerCloseSound("before_nade_rain", spawnPos, true, "grenades/grenade_incoming.wav", 0.8f);

                FVRObject obj = IM.OD["PinnedGrenadeM67"];
                float howFast = 15.0f;
                float maxAngle = 4.0f;
                Vector2 randRot = UnityEngine.Random.insideUnitCircle;
                int pullChance = UnityEngine.Random.Range(1, 20);

                Vector3 grenadePosition = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.up * 0.02f);
                GameObject go = Instantiate(obj.GetGameObject(), grenadePosition, Quaternion.LookRotation(GM.CurrentPlayerBody.Head.up));

                go.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);
                
                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = go.transform.forward * howFast;
                }

                if (pullChance == 10)
                {
                    PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
                    if (grenade != null)
                    {
                        grenade.ReleaseLever();
                    }
                }

                logger.LogInfo("Spawned NadeRain grenade");
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnNadeRain failed: {ex.Message}");
            }
        }

        public void DestroyHeld()
        {
            try
            {
                Vector3 handPos = GM.CurrentPlayerBody.RightHand.position;
                
                // Play before-action sound
                audioManager?.PlayDestructionSound("before_destroy_held", handPos, true, "destruction/item_dissolving.wav", 0.8f);

                var hands = GM.CurrentMovementManager?.Hands;
                if (hands == null || hands.Length < 2)
                {
                    logger.LogInfo("No hands found or hand system not available.");
                    return;
                }

                var rightHand = hands[1];
                if (rightHand?.CurrentInteractable != null && rightHand.CurrentInteractable is FVRPhysicalObject)
                {
                    Destroy(rightHand.CurrentInteractable.gameObject);

                    // Spawn celebratory shell
                    SpawnCelebratoryShell();
                    
                    // Play after-action sound
                    audioManager?.PlayDestructionSound("after_destroy_held", handPos, true, "destruction/item_destroyed.wav", 0.7f);
                    
                    logger.LogInfo("Destroyed held item in right hand.");
                }
                else
                {
                    logger.LogInfo("No item held in right hand to destroy.");
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"DestroyHeld failed: {ex.Message}");
            }
        }

        private void SpawnCelebratoryShell()
        {
            try
            {
                if (!IM.OD.ContainsKey("12GaugeShellFreedomfetti")) return;

                FVRObject obj = IM.OD["12GaugeShellFreedomfetti"];
                float maxAngle = 4.0f;
                Vector2 randRot = UnityEngine.Random.insideUnitCircle;

                Vector3 shellPosition = GM.CurrentPlayerBody.RightHand.position + 
                                      (GM.CurrentPlayerBody.RightHand.forward + GM.CurrentPlayerBody.RightHand.up * 0.5f) * 0.02f;

                GameObject go = Instantiate(obj.GetGameObject(), shellPosition, 
                                          Quaternion.LookRotation(GM.CurrentPlayerBody.RightHand.forward));

                go.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

                FVRFireArmRound cartridge = go.GetComponent<FVRFireArmRound>();
                if (cartridge != null)
                {
                    TryExplodeCartridge(cartridge, 0.01f);
                }

                // Play celebration sound
                audioManager?.PlayUISound("celebration", shellPosition, "ui/celebration.wav", 0.6f);
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnCelebratoryShell failed: {ex.Message}");
            }
        }

        private void TryExplodeCartridge(FVRFireArmRound cartridge, float delay)
        {
            try
            {
                // Try different method names for exploding/sploding cartridges
                var cartridgeType = cartridge.GetType();
                
                // Try common method names
                string[] methodNames = { "Splode", "Explode", "Detonate", "Fire", "Ignite" };
                
                foreach (var methodName in methodNames)
                {
                    var method = cartridgeType.GetMethod(methodName, new[] { typeof(float), typeof(bool), typeof(bool) });
                    if (method != null)
                    {
                        method.Invoke(cartridge, new object[] { delay, false, true });
                        return;
                    }
                    
                    // Try with different parameter signatures
                    method = cartridgeType.GetMethod(methodName, new[] { typeof(float) });
                    if (method != null)
                    {
                        method.Invoke(cartridge, new object[] { delay });
                        return;
                    }
                    
                    method = cartridgeType.GetMethod(methodName, new Type[0]);
                    if (method != null)
                    {
                        method.Invoke(cartridge, null);
                        return;
                    }
                }
                
                logger.LogWarning($"Could not find explosion method for FVRFireArmRound");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"TryExplodeCartridge failed: {ex.Message}");
            }
        }

        private bool ValidateSpawnConditions()
        {
            if (GM.CurrentPlayerBody?.Head == null)
            {
                logger.LogWarning("Cannot spawn: Player head reference is null");
                return false;
            }

            if (IM.OD == null)
            {
                logger.LogWarning("Cannot spawn: ItemManager ObjectDictionary is null");
                return false;
            }

            return true;
        }

        private void HandlePillowEffects()
        {
            bool grenadeEnabled;
            float grenadeChance, grenadeArmedChance;
            plugin.GetPillowGrenadeConfig(out grenadeEnabled, out grenadeChance, out grenadeArmedChance);

            bool zeroGEnabled;
            float zeroGChance, zeroGDuration;
            plugin.GetPillowZeroGravityConfig(out zeroGEnabled, out zeroGChance, out zeroGDuration);

            bool slomoEnabled;
            float slomoChance, slomoDuration;
            plugin.GetPillowSlomoConfig(out slomoEnabled, out slomoChance, out slomoDuration);

            if (grenadeEnabled && UnityEngine.Random.value < grenadeChance)
            {
                logger.LogInfo("Pillow grenade spawn triggered!");
                audioManager?.PlayDangerCloseSound("pillow_grenade", GM.CurrentPlayerBody.Head.position, true, "pillow/grenade_surprise.wav", 0.8f);
                SpawnPillowGrenade(grenadeArmedChance);
            }

            if (zeroGEnabled && UnityEngine.Random.value < zeroGChance)
            {
                logger.LogInfo($"Pillow zero gravity triggered! Duration: {zeroGDuration}s");
                audioManager?.PlaySlomoSound("zerog_start", GM.CurrentPlayerBody.Head.position, false, "effects/zero_gravity.wav", 0.7f);
                var effectsManager = plugin.GetEffectsManager();
                effectsManager?.StartCoroutine(effectsManager.ActivatePillowZeroGravity(zeroGDuration));
            }

            if (slomoEnabled && UnityEngine.Random.value < slomoChance)
            {
                logger.LogInfo($"Pillow slow motion triggered! Duration: {slomoDuration}s");
                audioManager?.PlaySlomoSound("start", GM.CurrentPlayerBody.Head.position, false, "effects/slomo_pillow.wav", 0.8f);
                var effectsManager = plugin.GetEffectsManager();
                effectsManager?.StartCoroutine(effectsManager.ActivatePillowSlomo(slomoDuration));
            }
        }

        private void SpawnPillowGrenade(float armedChance)
        {
            try
            {
                if (!IM.OD.ContainsKey("PinnedGrenadeM67"))
                {
                    logger.LogError("PinnedGrenadeM67 not found for pillow grenade");
                    return;
                }

                FVRObject grenadeObj = IM.OD["PinnedGrenadeM67"];
                Vector3 grenadeSpawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                GameObject grenadeGO = Instantiate(grenadeObj.GetGameObject(), grenadeSpawnPos, GM.CurrentPlayerBody.Head.rotation);

                bool shouldArmGrenade = UnityEngine.Random.value < armedChance;
                
                if (shouldArmGrenade)
                {
                    PinnedGrenade grenade = grenadeGO.GetComponentInChildren<PinnedGrenade>();
                    if (grenade != null)
                    {
                        grenade.ReleaseLever();
                        logger.LogInfo($"Pillow grenade armed and released! ({armedChance * 100}% chance triggered)");
                    }
                }
                else
                {
                    logger.LogInfo("Pillow grenade spawned but not armed (safe)");
                }

                var grenadeRB = grenadeGO.GetComponent<Rigidbody>();
                if (grenadeRB != null)
                {
                    grenadeRB.AddForce(GM.CurrentPlayerBody.Head.forward * 4000f);
                    grenadeRB.AddTorque(UnityEngine.Random.insideUnitSphere * 5f);
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnPillowGrenade failed: {ex.Message}");
            }
        }
    }

    // Pillow effect components
    public class PillowGrenade : MonoBehaviour
    {
        private bool isArmed;
        private float fuseTime = 3f;

        public void Initialize(bool armed)
        {
            isArmed = armed;
            if (isArmed)
            {
                StartCoroutine(FuseCoroutine());
            }
        }

        private IEnumerator FuseCoroutine()
        {
            yield return new WaitForSeconds(fuseTime);
            Explode();
        }

        private void Explode()
        {
            // Create explosion effect
            Vector3 pos = transform.position;
            
            // Find nearby objects and apply force
            Collider[] nearbyObjects = Physics.OverlapSphere(pos, 5f);
            foreach (Collider col in nearbyObjects)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    Vector3 direction = (col.transform.position - pos).normalized;
                    float distance = Vector3.Distance(pos, col.transform.position);
                    float force = Mathf.Lerp(10f, 0f, distance / 5f);
                    rb.AddForce(direction * force, ForceMode.Impulse);
                }
            }

            Destroy(gameObject);
        }
    }

    public class PillowZeroGravity : MonoBehaviour
    {
        private float duration;
        private float originalGravity;

        public void Initialize(float dur)
        {
            duration = dur;
            originalGravity = Physics.gravity.y;
            StartCoroutine(ZeroGravityCoroutine());
        }

        private IEnumerator ZeroGravityCoroutine()
        {
            Physics.gravity = Vector3.zero;
            yield return new WaitForSeconds(duration);
            Physics.gravity = new Vector3(0, originalGravity, 0);
            Destroy(this);
        }
    }

    public class PillowSlomo : MonoBehaviour
    {
        private float duration;

        public void Initialize(float dur, EffectsManager effects)
        {
            duration = dur;
            StartCoroutine(SlomoCoroutine(effects));
        }

        private IEnumerator SlomoCoroutine(EffectsManager effectsManager)
        {
            if (effectsManager != null)
            {
                // Use the pillow slomo activation method instead
                yield return effectsManager.StartCoroutine(effectsManager.ActivatePillowSlomo(duration));
            }
            Destroy(this);
        }
    }
}