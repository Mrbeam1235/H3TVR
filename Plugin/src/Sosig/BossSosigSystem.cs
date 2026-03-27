using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Boss Sosig System - Warlord boss with special abilities
    /// Giant 5x scaled boss that spawns minions
    /// </summary>
    public class BossSosigSystem : MonoBehaviour
    {
        #region Configuration
        public static bool EnableBossSosigs { get; set; } = true;
        public static float BossHealthMultiplier { get; set; } = 5.0f;
        public static float BossDamageMultiplier { get; set; } = 2.5f;
        public static float BossSpeedMultiplier { get; set; } = 0.6f;
        public static int MaxBossesPerSession { get; set; } = 3;
        public static float BossSpawnCooldown { get; set; } = 120f; // 2 minutes
        public static bool EnableBossAbilities { get; set; } = true;
        public static bool EnableBossMinions { get; set; } = true;
        #endregion

        #region Boss Tracking
        private static List<BossSosig> activeBosses = new List<BossSosig>();
        private static float lastBossSpawnTime;
        private static int totalBossesSpawned;
        #endregion

        #region Boss Types
        public enum BossType
        {
            Warlord         // Giant 5x scaled boss, spawns minions, Twitch nameplate
        }
        #endregion

        #region Public API
        /// <summary>
        /// Spawn a boss sosig at specified position
        /// </summary>
        public static BossSosig SpawnBoss(Vector3 position, Quaternion rotation, BossType type, ManualLogSource logger = null)
        {
            if (!EnableBossSosigs)
            {
                logger?.LogWarning("[BossSystem] Boss sosigs are disabled");
                return null;
            }

            if (activeBosses.Count >= MaxBossesPerSession)
            {
                logger?.LogWarning($"[BossSystem] Maximum bosses reached ({MaxBossesPerSession})");
                return null;
            }

            if (Time.time - lastBossSpawnTime < BossSpawnCooldown)
            {
                float remainingCooldown = BossSpawnCooldown - (Time.time - lastBossSpawnTime);
                logger?.LogWarning($"[BossSystem] Boss spawn on cooldown ({remainingCooldown:F1}s remaining)");
                return null;
            }

            try
            {
                // Create boss GameObject
                GameObject bossObj = new GameObject($"Boss_{type}_{totalBossesSpawned}");
                bossObj.transform.position = position;
                bossObj.transform.rotation = rotation;

                // Add boss component
                BossSosig boss = bossObj.AddComponent<BossSosig>();
                boss.Initialize(type, logger);

                // Track boss
                activeBosses.Add(boss);
                lastBossSpawnTime = Time.time;
                totalBossesSpawned++;

                logger?.LogInfo($"[BossSystem] Spawned {type} boss at {position} (Total: {activeBosses.Count}/{MaxBossesPerSession})");

                return boss;
            }
            catch (Exception ex)
            {
                logger?.LogError($"[BossSystem] Failed to spawn boss: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Get random boss type
        /// </summary>
        public static BossType GetRandomBossType()
        {
            Array values = Enum.GetValues(typeof(BossType));
            return (BossType)values.GetValue(UnityEngine.Random.Range(0, values.Length));
        }

        /// <summary>
        /// Remove boss from tracking
        /// </summary>
        public static void RemoveBoss(BossSosig boss)
        {
            activeBosses.Remove(boss);
        }

        /// <summary>
        /// Get active boss count
        /// </summary>
        public static int GetActiveBossCount()
        {
            return activeBosses.Count;
        }

        /// <summary>
        /// Clear all bosses
        /// </summary>
        public static void ClearAllBosses()
        {
            for (int i = activeBosses.Count - 1; i >= 0; i--)
            {
                if (activeBosses[i] != null)
                {
                    Destroy(activeBosses[i].gameObject);
                }
            }
            activeBosses.Clear();
        }
        #endregion
    }

    /// <summary>
    /// Individual boss sosig component
    /// </summary>
    public class BossSosig : MonoBehaviour
    {
        #region Boss Data
        public BossSosigSystem.BossType bossType;
        public Sosig sosig;
        public AdvancedSosigAI advancedAI;
        private ManualLogSource logger;

        // Boss visual
        public string bossName = "WARLORD";
        public float bossScale = 1.0f;
        private GameObject nameplateObject;

        // Boss stats
        private float maxHealth;
        private float currentHealth;
        private float damageMultiplier;
        private float speedMultiplier;
        private bool isEnraged;
        private float enrageThreshold = 0.3f; // 30% health

        // Ability tracking
        private float lastAbilityTime;
        private float abilityCooldown = 10f;
        private List<Sosig> spawnedMinions = new List<Sosig>();
        #endregion

        #region Initialization
        public void Initialize(BossSosigSystem.BossType type, ManualLogSource logSource, string customName = null)
        {
            bossType = type;
            logger = logSource;
            
            // Set custom name if provided
            if (!string.IsNullOrEmpty(customName))
            {
                bossName = customName;
            }
            else
            {
                bossName = GetDefaultBossName(type);
            }

            StartCoroutine(DelayedBossSetup());
        }

        private string GetDefaultBossName(BossSosigSystem.BossType type)
        {
            return "?? WARLORD ??";
        }

        private IEnumerator DelayedBossSetup()
        {
            // Wait for sosig to be properly spawned
            yield return new WaitForSeconds(0.5f);

            // Try to find sosig component
            sosig = GetComponentInChildren<Sosig>();
            if (sosig == null)
            {
                logger?.LogError("[BossSosig] No sosig component found - boss setup failed");
                Destroy(gameObject);
                yield break;
            }

            // Apply scale for Warlord
            if (bossType == BossSosigSystem.BossType.Warlord)
            {
                bossScale = 5.0f;
                ApplyBossScale(bossScale);
            }

            // Apply boss enhancements
            ApplyBossStats();
            ApplyBossArmor();
            ApplyBossWeapons();

            // Create nameplate above boss
            CreateBossNameplate();

            // Attach Advanced AI if available
            advancedAI = sosig.gameObject.GetComponent<AdvancedSosigAI>();
            if (advancedAI == null && AdvancedSosigAI.EnableAdvancedAI)
            {
                advancedAI = sosig.gameObject.AddComponent<AdvancedSosigAI>();
                advancedAI.Initialize(sosig, logger);
                logger?.LogDebug($"[BossSosig] Attached Advanced AI to {bossType} boss");
            }

            // Configure AI for boss behavior
            ConfigureBossAI();

            // Start boss update loop
            StartCoroutine(BossUpdateLoop());

            logger?.LogInfo($"[BossSosig] {bossType} boss '{bossName}' fully initialized (Scale: {bossScale}x)");
        }

        /// <summary>
        /// Apply scale to the boss sosig
        /// </summary>
        private void ApplyBossScale(float scale)
        {
            try
            {
                if (sosig == null) return;

                // Scale the sosig
                sosig.transform.localScale = Vector3.one * scale;

                logger?.LogInfo($"[BossSosig] Applied scale {scale}x to {bossType} boss");
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"[BossSosig] Failed to apply scale: {ex.Message}");
            }
        }

        /// <summary>
        /// Create a floating nameplate above the boss
        /// </summary>
        private void CreateBossNameplate()
        {
            try
            {
                if (sosig == null) return;

                // Create nameplate GameObject
                nameplateObject = new GameObject("BossNameplate");
                
                // Position above the boss head
                Transform headLink = sosig.Links.Count > 0 ? sosig.Links[0].transform : sosig.transform;
                nameplateObject.transform.SetParent(headLink);
                nameplateObject.transform.localPosition = new Vector3(0, 0.8f * bossScale, 0);

                // Add TextMesh for the name
                TextMesh textMesh = nameplateObject.AddComponent<TextMesh>();
                textMesh.text = bossName;
                textMesh.fontSize = 24;
                textMesh.characterSize = 0.1f * bossScale;
                textMesh.anchor = TextAnchor.MiddleCenter;
                textMesh.alignment = TextAlignment.Center;
                textMesh.color = GetBossNameColor();
                textMesh.fontStyle = FontStyle.Bold;

                // Make it face the camera (billboard effect)
                var billboard = nameplateObject.AddComponent<BossNameplateBillboard>();
                billboard.Initialize();

                logger?.LogDebug($"[BossSosig] Created nameplate '{bossName}' for {bossType} boss");
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"[BossSosig] Failed to create nameplate: {ex.Message}");
            }
        }

        /// <summary>
        /// Get color for boss nameplate based on type
        /// </summary>
        private Color GetBossNameColor()
        {
            // Warlord only - Red color
            return new Color(1f, 0.2f, 0.2f);
        }
        #endregion

        #region Boss Configuration
        private void ApplyBossStats()
        {
            if (sosig == null) return;

            // Warlord stats - Giant boss with massive health
            float healthMult = BossSosigSystem.BossHealthMultiplier * 5.0f;
            damageMultiplier = BossSosigSystem.BossDamageMultiplier * 2.5f;
            speedMultiplier = BossSosigSystem.BossSpeedMultiplier * 0.6f;
            abilityCooldown = 12f; // Spawns minions every 12 seconds

            // Apply health multiplier to all links
            foreach (var link in sosig.Links)
            {
                link.m_integrity *= healthMult;
            }

            // Calculate total health
            maxHealth = GetTotalHealth();
            currentHealth = maxHealth;

            logger?.LogDebug($"[BossSosig] WARLORD stats - Health: {maxHealth:F0}, Damage: x{damageMultiplier:F1}, Speed: x{speedMultiplier:F1}");
        }

        private void ApplyBossArmor()
        {
            // Apply God-tier armor to Warlord
            if (sosig != null)
            {
                SosigArmorManager.ApplyArmorToSosig(sosig, 5); // God armor
            }
            logger?.LogDebug("[BossSosig] Applied WARLORD God armor");
        }

        private void ApplyBossWeapons()
        {
            // Warlord gets enhanced weapons
            logger?.LogDebug("[BossSosig] Applied WARLORD weapon loadout");
        }

        private void ConfigureBossAI()
        {
            if (advancedAI == null) return;

            // Warlord: Aggressive assault behavior
            advancedAI.ForceState(AdvancedSosigAI.AIState.Assault);
            logger?.LogDebug("[BossSosig] Configured WARLORD AI - Assault mode");
        }
        #endregion

        #region Boss Update Loop
        private IEnumerator BossUpdateLoop()
        {
            var wait = new WaitForSeconds(1f);

            while (sosig != null && sosig.BodyState != Sosig.SosigBodyState.Dead)
            {
                yield return wait;

                try
                {
                    UpdateBossHealth();
                    UpdateBossAbilities();
                    UpdateMinions();
                }
                catch (Exception ex)
                {
                    logger?.LogError($"[BossSosig] Update error: {ex.Message}");
                }
            }

            // Boss died
            OnBossDeath();
        }

        private void UpdateBossHealth()
        {
            currentHealth = GetTotalHealth();
            float healthPercent = currentHealth / maxHealth;

            // Check for enrage
            if (!isEnraged && healthPercent < enrageThreshold)
            {
                TriggerEnrage();
            }
        }

        private void UpdateBossAbilities()
        {
            if (!BossSosigSystem.EnableBossAbilities) return;
            if (Time.time - lastAbilityTime < abilityCooldown) return;

            // Warlord spawns minions
            SpawnWarlordMinions();
            lastAbilityTime = Time.time;
        }

        private void UpdateMinions()
        {
            if (!BossSosigSystem.EnableBossMinions) return;

            // Clean up dead minions
            spawnedMinions.RemoveAll(m => m == null || m.BodyState == Sosig.SosigBodyState.Dead);
        }
        #endregion

        #region Boss Abilities
        private void TriggerEnrage()
        {
            isEnraged = true;
            damageMultiplier *= 1.5f;
            speedMultiplier *= 1.3f;
            abilityCooldown *= 0.7f;

            logger?.LogInfo($"[BossSosig] WARLORD ENRAGED! (Health < {enrageThreshold * 100}%)");
        }

        /// <summary>
        /// Warlord spawns minions around itself
        /// </summary>
        private void SpawnWarlordMinions()
        {
            if (spawnedMinions.Count >= 5) return; // Max 5 minions for Warlord

            try
            {
                // Spawn 2-3 minions near the Warlord
                int minionCount = UnityEngine.Random.Range(2, 4);
                
                for (int i = 0; i < minionCount; i++)
                {
                    // Spawn in a circle around the Warlord
                    float angle = (360f / minionCount) * i + UnityEngine.Random.Range(-15f, 15f);
                    float distance = UnityEngine.Random.Range(4f, 7f);
                    Vector3 spawnPos = transform.position + new Vector3(
                        Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                        0f,
                        Mathf.Cos(angle * Mathf.Deg2Rad) * distance
                    );

                    logger?.LogDebug($"[BossSosig] Warlord spawning minion {i + 1}/{minionCount} at {spawnPos}");
                }

                logger?.LogInfo($"[BossSosig] WARLORD summoned {minionCount} minions!");
            }
            catch (Exception ex)
            {
                logger?.LogError($"[BossSosig] Warlord minion spawn failed: {ex.Message}");
            }
        }
        #endregion



        #region Helper Methods
        private float GetTotalHealth()
        {
            if (sosig == null || sosig.Links.Count == 0) return 0f;

            float totalHealth = 0f;
            foreach (var link in sosig.Links)
            {
                totalHealth += link.m_integrity;
            }
            return totalHealth;
        }

        private void OnBossDeath()
        {
            logger?.LogInfo($"[BossSosig] {bossType} boss defeated! Health: {currentHealth:F0}/{maxHealth:F0}");

            // Clean up minions
            foreach (var minion in spawnedMinions)
            {
                if (minion != null)
                {
                    minion.TickDownToClear(5);
                }
            }

            // Remove from tracking
            BossSosigSystem.RemoveBoss(this);

            // Destroy component
            Destroy(this);
        }
        #endregion

        #region Cleanup
        private void OnDestroy()
        {
            BossSosigSystem.RemoveBoss(this);
        }
        #endregion
    }

    /// <summary>
    /// Boss configuration for BepInEx
    /// </summary>
    public static class BossConfig
    {
        public static void ApplyConfig(BepInEx.Configuration.ConfigFile config)
        {
            var enableBosses = config.Bind("Boss Sosigs", "EnableBossSosigs", true,
                "Enable boss sosig system");
            var healthMultiplier = config.Bind("Boss Sosigs", "BossHealthMultiplier", 3.0f,
                "Health multiplier for boss sosigs (default: 3x)");
            var damageMultiplier = config.Bind("Boss Sosigs", "BossDamageMultiplier", 1.5f,
                "Damage multiplier for boss sosigs (default: 1.5x)");
            var speedMultiplier = config.Bind("Boss Sosigs", "BossSpeedMultiplier", 1.2f,
                "Speed multiplier for boss sosigs (default: 1.2x)");
            var maxBosses = config.Bind("Boss Sosigs", "MaxBossesPerSession", 3,
                "Maximum number of bosses that can exist at once");
            var spawnCooldown = config.Bind("Boss Sosigs", "BossSpawnCooldown", 120f,
                "Cooldown between boss spawns (seconds)");
            var enableAbilities = config.Bind("Boss Sosigs", "EnableBossAbilities", true,
                "Enable boss special abilities");
            var enableMinions = config.Bind("Boss Sosigs", "EnableBossMinions", true,
                "Enable boss minion spawning (Summoner type)");

            BossSosigSystem.EnableBossSosigs = enableBosses.Value;
            BossSosigSystem.BossHealthMultiplier = healthMultiplier.Value;
            BossSosigSystem.BossDamageMultiplier = damageMultiplier.Value;
            BossSosigSystem.BossSpeedMultiplier = speedMultiplier.Value;
            BossSosigSystem.MaxBossesPerSession = maxBosses.Value;
            BossSosigSystem.BossSpawnCooldown = spawnCooldown.Value;
            BossSosigSystem.EnableBossAbilities = enableAbilities.Value;
            BossSosigSystem.EnableBossMinions = enableMinions.Value;
        }
    }

    /// <summary>
    /// Makes the nameplate always face the camera/player
    /// </summary>
    public class BossNameplateBillboard : MonoBehaviour
    {
        private Transform playerHead;

        public void Initialize()
        {
            // Will find player head each frame if needed
        }

        private void LateUpdate()
        {
            try
            {
                // Find player head if not cached
                if (playerHead == null && GM.CurrentPlayerBody != null)
                {
                    playerHead = GM.CurrentPlayerBody.Head;
                }

                if (playerHead != null)
                {
                    // Face the player
                    Vector3 lookDir = playerHead.position - transform.position;
                    lookDir.y = 0; // Keep upright
                    if (lookDir != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(-lookDir);
                    }
                }
            }
            catch
            {
                // Silently fail - don't spam logs
            }
        }
    }
}
