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
        private EnhancedChatSpawner enhancedChatSpawner;

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            logger = logSource;
            
            logger.LogInfo("SpawnManager initialized successfully!");
        }

        public void SetEnhancedChatSpawner(EnhancedChatSpawner chatSpawner)
        {
            enhancedChatSpawner = chatSpawner;
            logger?.LogInfo("Enhanced Chat Spawner reference set in SpawnManager");
        }

        // H3TVR legacy spawn methods required by InputHandler
        public void SpawnWonderfulToy()
        {
            SpawnObject("TippyToyAnton", "WonderToy");
        }

        public void SpawnJeditToy()
        {
            SpawnObject("JediTippyToy", "JeditToy");
        }

        public void SpawnHydration()
        {
            SpawnObject("SuppressorBottle", "Hydration");
        }

        public void SpawnPillow()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                int minCount, maxCount;
                plugin.GetPillowConfig(out minCount, out maxCount);
                int pillowCount = UnityEngine.Random.Range(minCount, maxCount + 1);
                logger.LogInfo($"Spawning {pillowCount} pillow(s)");

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

                // Handle pillow effects
                HandlePillowEffects();
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnPillow failed: {ex.Message}");
            }
        }

        public void SpawnFlash()
        {
            SpawnGrenade("PinnedGrenadeXM84", "Flash", 500f, true);
        }

        public void SpawnFlash2()
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("PinnedGrenadeXM84")) return;

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

                logger.LogInfo("Spawned Flash2 (4 flashbangs)");
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnFlash2 failed: {ex.Message}");
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

                if (!IM.OD.ContainsKey("Shuriken"))
                {
                    logger.LogError("Shuriken not found in ObjectDictionary");
                    return;
                }

                FVRObject obj = IM.OD["Shuriken"];
                Vector3 shuriPosition = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 0.02f);
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
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnShuri failed: {ex.Message}");
            }
        }

        public void SpawnNadeRain()
        {
            try
            {
                // 10% spawn chance
                if (UnityEngine.Random.Range(1, 11) != 1) return;
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("PinnedGrenadeM67")) return;

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

        public void SpawnSkittySubGun()
        {
            var weaponManager = plugin.GetWeaponManager();
            weaponManager?.SpawnRandomGun(false);
        }

        public void SpawnSkittyBigGun()
        {
            var weaponManager = plugin.GetWeaponManager();
            weaponManager?.SpawnRandomGun(true);
        }

        public void DangerCloseBarrage()
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("Cartridge50mmFlareDangerClose")) return;

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
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"DangerCloseBarrage failed: {ex.Message}");
            }
        }

        public void DestroyHeld()
        {
            try
            {
                var hands = GM.CurrentMovementManager?.Hands;
                if (hands == null || hands.Length < 2) return;

                var rightHand = hands[1];
                if (rightHand?.CurrentInteractable != null && rightHand.CurrentInteractable is FVRPhysicalObject)
                {
                    Destroy(rightHand.CurrentInteractable.gameObject);

                    // Spawn celebratory shell
                    SpawnCelebratoryShell();
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"DestroyHeld failed: {ex.Message}");
            }
        }

        public void DestroyQuickbelt()
        {
            try
            {
                FVRQuickBeltSlot[] allSlots = UnityEngine.Object.FindObjectsOfType<FVRQuickBeltSlot>();
                if (allSlots == null || allSlots.Length == 0)
                {
                    logger.LogInfo("No quickbelt slots found in scene.");
                    return;
                }

                int droppedCount = 0;
                foreach (var slot in allSlots)
                {
                    var obj = slot?.CurObject;
                    if (obj == null) continue;

                    // Detach from slot
                    obj.SetQuickBeltSlot(null);

                    // Enable / adjust physics so it actually drops
                    var rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false; // ensure physics
                        rb.velocity = GM.CurrentPlayerBody.Head.forward * 1.5f + UnityEngine.Random.insideUnitSphere * 0.25f;
                        rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 2f;
                    }
                    droppedCount++;
                }

                logger.LogInfo($"Dropped {droppedCount} quickbelt object(s).");
            }
            catch (Exception ex)
            {
                logger.LogError($"DestroyQuickbelt failed: {ex.Message}");
            }
        }

        // Chat Sosig methods - using EnhancedChatSpawner
        public void SpawnChatSosigFriendly()
        {
            if (enhancedChatSpawner != null)
            {
                enhancedChatSpawner.SpawningSequence("FriendlyUser");
            }
            else
            {
                logger?.LogWarning("Enhanced chat spawner not initialized");
            }
        }

        public void SpawnChatSosigEnemy()
        {
            if (enhancedChatSpawner != null)
            {
                enhancedChatSpawner.SpawningSequenceEnemy(1, "EnemyUser");
            }
            else
            {
                logger?.LogWarning("Enhanced chat spawner not initialized");
            }
        }

        public void ClearAllChatSosigs()
        {
            if (enhancedChatSpawner != null)
            {
                enhancedChatSpawner.ClearSosigs(true, true);
            }
            else
            {
                logger?.LogWarning("Enhanced chat spawner not initialized");
            }
        }

        public ChatSosigStats GetChatSosigStats()
        {
            if (enhancedChatSpawner != null)
            {
                var stats = enhancedChatSpawner.GetStats();
                return new ChatSosigStats
                {
                    activeSosigCount = stats.ActiveAllies + stats.ActiveEnemies,
                    friendlyCount = stats.ActiveAllies,
                    enemyCount = stats.ActiveEnemies,
                    queuedSpawns = stats.QueueLength,
                    totalSpawned = stats.TotalSpawned
                };
            }
            else
            {
                return new ChatSosigStats
                {
                    activeSosigCount = 0,
                    friendlyCount = 0,
                    enemyCount = 0,
                    queuedSpawns = 0,
                    totalSpawned = 0
                };
            }
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
                SpawnPillowGrenade(grenadeArmedChance);
            }

            if (zeroGEnabled && UnityEngine.Random.value < zeroGChance)
            {
                logger.LogInfo($"Pillow zero gravity triggered! Duration: {zeroGDuration}s");
                var effectsManager = plugin.GetEffectsManager();
                effectsManager?.StartCoroutine(effectsManager.ActivatePillowZeroGravity(zeroGDuration));
            }

            if (slomoEnabled && UnityEngine.Random.value < slomoChance)
            {
                logger.LogInfo($"Pillow slow motion triggered! Duration: {slomoDuration}s");
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

        private void SpawnGrenade(string grenadeID, string grenadeName, float force, bool shouldArm)
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey(grenadeID)) return;

                FVRObject obj = IM.OD[grenadeID];
                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
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

                logger.LogInfo($"Spawned {grenadeName}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Spawn{grenadeName} failed: {ex.Message}");
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