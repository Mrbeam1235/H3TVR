using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx.Logging;
using System;

namespace H3TVR
{
    public class SpawnManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            logger = logSource;

            // Initialize dependency-aware systems
            OptionalDependencyManager.Initialize(logger);

            logger.LogInfo("[SpawnManager] Spawn manager initialized successfully");
            
            // Log optional dependency status for the remaining item/effect spawns.
            if (OptionalDependencyManager.HasAnyDependencies())
            {
                logger.LogInfo($"[SpawnManager] Optional spawn integrations active: {OptionalDependencyManager.GetAvailableDependencyCount()}");
            }
        }

        // H3TVR legacy spawn methods required by InputHandler
        public void SpawnWonderfulToy()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            // Play before-action sound
            
            SpawnObject("TippyToyAnton", "WonderToy");
            
            // Play after-action sound
        }

        public void SpawnJeditToy()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            
            try
            {
                if (!ValidateSpawnConditions()) return;

                // Item ID for Jedi Tippy Toy mod
                string jeditToyID = "ftw.JediTippyToy";
                
                if (!IM.OD.ContainsKey(jeditToyID))
                {
                    logger.LogWarning("Jedi Tippy Toy not available. Install: https://thunderstore.io/c/h3vr/p/PutterMyBancakes/Jeditippytoy/");
                    logger.LogInfo($"Expected Item ID: {jeditToyID}");
                    
                    // List available tippy toys for debugging
                    logger.LogInfo("Available Tippy Toy items:");
                    foreach (var kvp in IM.OD)
                    {
                        if (kvp.Key.ToLower().Contains("tippy") || kvp.Key.ToLower().Contains("jedi"))
                            logger.LogInfo($"  - {kvp.Key}");
                    }
                    return;
                }

                FVRObject obj = IM.OD[jeditToyID];
                
                // Spawn normally first
                GameObject go = Instantiate(obj.GetGameObject(), spawnPos, GM.CurrentPlayerBody.Head.rotation);

                // Simulate the flip motion to activate the tippy toy
                // (flip upside down, then back right-side up)
                StartCoroutine(FlipTippyToyToActivate(go));

                logger.LogInfo($"Successfully spawned Jedi Tippy Toy (ID: {jeditToyID})");
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnJeditToy failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Simulate flipping the tippy toy upside down and back to activate it
        /// </summary>
        private IEnumerator FlipTippyToyToActivate(GameObject tippyToy)
        {
            if (tippyToy == null) yield break;

            // Wait a frame for initialization
            yield return null;

            // Disable physics temporarily so we can control the rotation
            var rb = tippyToy.GetComponent<Rigidbody>();
            bool wasKinematic = false;
            if (rb != null)
            {
                wasKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            // Store original rotation
            Quaternion originalRotation = tippyToy.transform.rotation;
            
            // Flip upside down (rotate 180 degrees on X axis)
            float flipDuration = 0.15f;
            float elapsed = 0f;
            
            // Flip DOWN
            while (elapsed < flipDuration)
            {
                if (tippyToy == null) yield break;
                
                elapsed += Time.deltaTime;
                float t = elapsed / flipDuration;
                tippyToy.transform.rotation = Quaternion.Slerp(originalRotation, originalRotation * Quaternion.Euler(180f, 0f, 0f), t);
                yield return null;
            }

            // Brief pause while upside down
            yield return new WaitForSeconds(0.1f);

            // Flip back UP
            Quaternion upsideDownRotation = tippyToy.transform.rotation;
            elapsed = 0f;
            
            while (elapsed < flipDuration)
            {
                if (tippyToy == null) yield break;
                
                elapsed += Time.deltaTime;
                float t = elapsed / flipDuration;
                tippyToy.transform.rotation = Quaternion.Slerp(upsideDownRotation, originalRotation, t);
                yield return null;
            }

            // Re-enable physics and give it a push forward
            if (rb != null)
            {
                rb.isKinematic = wasKinematic;
                rb.AddTorque(new Vector3(0.25f, 0.25f, 0.25f));
                rb.AddForce(GM.CurrentPlayerBody.Head.forward * 25);
            }

            logger.LogInfo("Tippy Toy flip animation complete - should be activated!");
        }


        public void SpawnHydration()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
            
            // Play before-action sound
            
            SpawnObject("SuppressorBottle", "Hydration");
            
            // Play after-action sound
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

            // Use WeaponManager's list-based spawning (uses GunList/MagazineList config)
            var weaponManager = plugin.GetWeaponManager();
            if (weaponManager != null)
            {
                weaponManager.SpawnSkittySubGun();
            }
            else
            {
                logger.LogWarning("WeaponManager not available for SpawnSkittySubGun");
            }

            // Play after-action sound
        }

        public void SwapHeldGun()
        {
            var weaponManager = plugin.GetWeaponManager();
            if (weaponManager != null)
            {
                weaponManager.SwapHeldGun();
            }
            else
            {
                logger.LogWarning("WeaponManager not available for SwapHeldGun");
            }
        }

        public void SpawnSkittyBigGun()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);

            // Play before-action sound

            // Use WeaponManager's list-based spawning (uses GunList/MagazineList config)
            var weaponManager = plugin.GetWeaponManager();
            if (weaponManager != null)
            {
                weaponManager.SpawnSkittyBigGun();
            }
            else
            {
                logger.LogWarning("WeaponManager not available for SpawnSkittyBigGun");
            }

            // Play after-action sound
        }

        /// <summary>
        /// Spawn Air Strike Smoke Grenade from JerryAr
        /// Count and pin pull chance are configurable via [AirStrike] config section.
        /// Falls back to a vanilla smoke grenade if the JerryAr mod is not installed,
        /// so something always spawns.
        /// Mod: https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/
        /// </summary>
        public void SpawnAirStrikeGrenade()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                int count;
                float pinPullChance;
                plugin.GetAirStrikeConfig(out count, out pinPullChance);

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);

                // Play before-action sound

                // Resolve the grenade to spawn - guaranteed fallback chain
                FVRObject obj = ResolveAirStrikeGrenadeObject();
                if (obj == null)
                {
                    logger.LogError("SpawnAirStrikeGrenade: No spawnable grenade found in ItemManager at all.");
                    return;
                }

                int armedCount = 0;
                Vector3 headForward = GM.CurrentPlayerBody.Head.forward;
                // Spawn in front of the player so the grenade doesn't collide with their head
                Vector3 throwOrigin = GM.CurrentPlayerBody.Head.position + (headForward * 0.75f) + new Vector3(0f, 0.25f, 0f);
                for (int i = 0; i < count; i++)
                {
                    // Spread multiple grenades slightly so they don't stack inside each other
                    Vector3 offset = i == 0 ? Vector3.zero : UnityEngine.Random.insideUnitSphere * 0.2f;
                    GameObject go = Instantiate(obj.GetGameObject(), throwOrigin + offset, GM.CurrentPlayerBody.Head.rotation);

                    bool armed = UnityEngine.Random.value < pinPullChance;
                    if (armed)
                    {
                        ArmGrenade(go);
                        armedCount++;
                    }

                    // Throw it forward in a proper arc
                    var rb = go.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.velocity = (headForward * 12f) + (Vector3.up * 4f);
                        rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f);
                    }
                }

                logger.LogInfo($"Spawned {count} Air Strike grenade(s) ({armedCount} armed, pin pull chance {pinPullChance:P0}, ID: {obj.ItemID})");

                // Play after-action sound
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnAirStrikeGrenade failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds the air strike grenade, falling back to vanilla smoke/pinned grenades
        /// so the redeem always spawns something.
        /// </summary>
        private FVRObject ResolveAirStrikeGrenadeObject()
        {
            // Preferred: JerryAr's Air Strike Smoke Grenade
            const string airStrikeID = "JerryAr_AirStrikeSmokeGrenade";
            if (IM.OD.ContainsKey(airStrikeID))
            {
                return IM.OD[airStrikeID];
            }

            logger.LogWarning("Air Strike Smoke Grenade not available. Install: https://thunderstore.io/c/h3vr/p/JerryAr/AirStrikeSmokeGrenade/");

            // Fallback 1: any item with 'airstrike' in its ID
            foreach (var kvp in IM.OD)
            {
                if (kvp.Value != null && kvp.Key.ToLower().Contains("airstrike"))
                {
                    logger.LogInfo($"Using airstrike fallback item: {kvp.Key}");
                    return kvp.Value;
                }
            }

            // Fallback 2: vanilla smoke grenade
            const string smokeID = "M18SmokeGrenade";
            if (IM.OD.ContainsKey(smokeID))
            {
                logger.LogInfo($"Using vanilla smoke grenade fallback: {smokeID}");
                return IM.OD[smokeID];
            }

            // Fallback 3: any pinned grenade in the game
            foreach (var kvp in IM.OD)
            {
                if (kvp.Value == null) continue;
                string key = kvp.Key.ToLower();
                if (key.Contains("smokegrenade") || key.Contains("grenade"))
                {
                    logger.LogInfo($"Using generic grenade fallback: {kvp.Key}");
                    return kvp.Value;
                }
            }

            return null;
        }

        /// <summary>
        /// Pulls the pin / releases the lever on a spawned grenade object.
        /// </summary>
        private void ArmGrenade(GameObject go)
        {
            // Pull the pin and release lever to arm the grenade
            PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
            if (grenade != null)
            {
                grenade.ReleaseLever();
                logger.LogInfo("Air Strike grenade pin pulled and lever released!");
                return;
            }

            // Try to find any grenade-like component and activate it
            var allComponents = go.GetComponentsInChildren<Component>(true);
            foreach (var comp in allComponents)
            {
                if (comp == null) continue;
                var compType = comp.GetType();

                // Try common grenade activation methods
                var releaseMethod = compType.GetMethod("ReleaseLever",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (releaseMethod != null)
                {
                    releaseMethod.Invoke(comp, null);
                    logger.LogInfo($"Air Strike grenade activated via {compType.Name}.ReleaseLever()");
                    return;
                }

                var armMethod = compType.GetMethod("Arm",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.Instance);
                if (armMethod != null && armMethod.GetParameters().Length == 0)
                {
                    armMethod.Invoke(comp, null);
                    logger.LogInfo($"Air Strike grenade activated via {compType.Name}.Arm()");
                    return;
                }
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

                // Titan Machine Item ID
                string titanID = "JerryAr_TitanMachine";

                if (!IM.OD.ContainsKey(titanID))
                {
                    // Fallback: search for any titan item in the ItemManager
                    string fallbackID = null;
                    foreach (var kvp in IM.OD)
                    {
                        if (kvp.Value != null && kvp.Key.ToLower().Contains("titan"))
                        {
                            fallbackID = kvp.Key;
                            break;
                        }
                    }

                    if (fallbackID == null)
                    {
                        logger.LogWarning("Titan Machine not available. Install: https://thunderstore.io/c/h3vr/p/JerryAr/TitanMachine/");
                        logger.LogInfo($"Expected Item ID: {titanID}");
                        return;
                    }

                    logger.LogInfo($"Using titan fallback item: {fallbackID}");
                    titanID = fallbackID;
                }

                FVRObject obj = IM.OD[titanID];

                // Snap the spawn point down to the ground so the titan doesn't spawn floating
                RaycastHit hit;
                if (Physics.Raycast(spawnPos + Vector3.up, Vector3.down, out hit, 20f))
                {
                    spawnPos = hit.point + (Vector3.up * 0.1f);
                }

                // Face the player
                Vector3 toPlayer = GM.CurrentPlayerBody.Head.position - spawnPos;
                toPlayer.y = 0f;
                Quaternion spawnRot = toPlayer.sqrMagnitude > 0.001f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;
                GameObject go = Instantiate(obj.GetGameObject(), spawnPos, spawnRot);

                // Try to configure as hostile AI if it has sosig-like components (check children too)
                var sosig = go.GetComponentInChildren<Sosig>();
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
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnTitanMachine failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Spawn a devastating nuke effect - massive explosion barrage
        /// Creates multiple large explosions around the player's forward position
        /// </summary>
        public void SpawnNuke()
        {
            try
            {
                if (!ValidateSpawnConditions()) return;

                Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
                Vector3 targetPos = playerPos + (GM.CurrentPlayerBody.Head.forward * 15f);

                // Play dramatic warning sound

                logger.LogInfo("NUKE INCOMING! Taking cover is advised...");

                // Start the nuke sequence
                StartCoroutine(NukeSequence(targetPos));
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnNuke failed: {ex.Message}");
            }
        }

        private IEnumerator NukeSequence(Vector3 targetPosition)
        {
            // Brief delay for dramatic effect
            yield return new WaitForSeconds(1.5f);

            // Play nuke detonation sound

            // Check for danger close cartridge
            string nukeCartridgeID = "Cartridge50mmFlareDangerClose";
            if (!IM.OD.ContainsKey(nukeCartridgeID))
            {
                logger.LogWarning("Nuke cartridge not available - using fallback explosion");
                yield break;
            }

            FVRObject obj = IM.OD[nukeCartridgeID];

            // Primary blast - center explosion
            SpawnNukeExplosion(obj, targetPosition, 0f);

            yield return new WaitForSeconds(0.1f);

            // Secondary ring of explosions
            int ringCount = 8;
            float ringRadius = 5f;
            for (int i = 0; i < ringCount; i++)
            {
                float angle = (360f / ringCount) * i;
                Vector3 offset = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * ringRadius,
                    UnityEngine.Random.Range(-1f, 2f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * ringRadius
                );
                SpawnNukeExplosion(obj, targetPosition + offset, 0.05f * i);
            }

            yield return new WaitForSeconds(0.3f);

            // Outer ring of explosions
            int outerRingCount = 12;
            float outerRadius = 10f;
            for (int i = 0; i < outerRingCount; i++)
            {
                float angle = (360f / outerRingCount) * i + 15f; // Offset from inner ring
                Vector3 offset = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * outerRadius,
                    UnityEngine.Random.Range(-2f, 3f),
                    Mathf.Cos(angle * Mathf.Deg2Rad) * outerRadius
                );
                SpawnNukeExplosion(obj, targetPosition + offset, 0.03f * i);
            }

            yield return new WaitForSeconds(0.5f);

            // Final skyward explosions
            for (int i = 0; i < 5; i++)
            {
                Vector3 skyOffset = new Vector3(
                    UnityEngine.Random.Range(-3f, 3f),
                    5f + (i * 2f),
                    UnityEngine.Random.Range(-3f, 3f)
                );
                SpawnNukeExplosion(obj, targetPosition + skyOffset, 0.1f * i);
            }

            logger.LogInfo("Nuke detonation complete - area devastated!");
            
            // Play aftermath sound
        }

        private void SpawnNukeExplosion(FVRObject explosiveObj, Vector3 position, float delay)
        {
            try
            {
                GameObject go = Instantiate(explosiveObj.GetGameObject(), position, UnityEngine.Random.rotation);

                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Small random velocity for variation
                    rb.velocity = UnityEngine.Random.insideUnitSphere * 5f;
                }

                FVRFireArmRound cartridge = go.GetComponent<FVRFireArmRound>();
                if (cartridge != null)
                {
                    TryExplodeCartridge(cartridge, delay);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"SpawnNukeExplosion failed: {ex.Message}");
            }
        }

        public void DangerCloseBarrage()
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("Cartridge50mmFlareDangerClose")) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);
                
                // Play before-action danger close warning

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
            }
            catch (Exception ex)
            {
                logger.LogError($"DangerCloseBarrage failed: {ex.Message}");
            }
        }

        private IEnumerator PlayDelayedExplosionSound(Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);
        }

        public void DestroyQuickbelt()
        {
            try
            {
                Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
                
                // Play before-action destruction sound

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
            SpawnGrenade("PinnedGrenadeXM84", "Flash", 500f, true);
        }

        public void SpawnFlash2()
        {
            try
            {
                if (!ValidateSpawnConditions() || !IM.OD.ContainsKey("PinnedGrenadeXM84")) return;

                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);

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
                if (!ValidateSpawnConditions()) return;

                if (!IM.OD.ContainsKey("PinnedGrenadeM67"))
                {
                    logger.LogError("SpawnNadeRain: 'PinnedGrenadeM67' not found in ItemManager.");
                    return;
                }

                StartCoroutine(NadeRainRoutine());
            }
            catch (Exception ex)
            {
                logger.LogError($"SpawnNadeRain failed: {ex.Message}");
            }
        }

        private IEnumerator NadeRainRoutine()
        {
            Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, 0.25f, 0f);

            FVRObject obj = IM.OD["PinnedGrenadeM67"];
            const int grenadeCount = 10;
            const float launchSpeed = 15.0f;
            const float maxAngle = 8.0f;

            for (int i = 0; i < grenadeCount; i++)
            {
                LaunchNadeRainGrenade(obj, launchSpeed, maxAngle);
                yield return new WaitForSeconds(0.35f);
            }

            logger.LogInfo($"NadeRain complete ({grenadeCount} grenades launched)");
        }

        private void LaunchNadeRainGrenade(FVRObject obj, float launchSpeed, float maxAngle)
        {
            try
            {
                // Launch straight up with a random tilt so grenades rain down around the player
                Vector2 randRot = UnityEngine.Random.insideUnitCircle;
                Vector3 grenadePosition = GM.CurrentPlayerBody.Head.position + (Vector3.up * 0.5f);
                GameObject go = Instantiate(obj.GetGameObject(), grenadePosition, Quaternion.LookRotation(Vector3.up));

                go.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

                var rb = go.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = go.transform.forward * launchSpeed;
                    rb.AddTorque(UnityEngine.Random.insideUnitSphere * 2f);
                }

                // ~25% of grenades come down armed
                if (UnityEngine.Random.Range(0, 4) == 0)
                {
                    PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
                    if (grenade != null)
                    {
                        grenade.ReleaseLever();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"NadeRain grenade launch failed: {ex.Message}");
            }
        }

        public void DestroyHeld()
        {
            try
            {
                Vector3 handPos = GM.CurrentPlayerBody.RightHand.position;
                
                // Play before-action sound

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
