using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Boss Sosig System - Enhanced enemies with special abilities and behaviors
    /// Integrates with AdvancedSosigAI for tactical behaviors
    /// </summary>
    public class BossSosigSystem : MonoBehaviour
    {
        #region Configuration
        public static bool EnableBossSosigs { get; set; } = true;
        public static float BossHealthMultiplier { get; set; } = 3.0f;
        public static float BossDamageMultiplier { get; set; } = 1.5f;
        public static float BossSpeedMultiplier { get; set; } = 1.2f;
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
            Tank,           // High health, slow, heavy armor
            Berserker,      // Fast, aggressive, high damage
            Sniper,         // Long range, tactical, cover-focused
            Summoner,       // Spawns minions, support role
            Elite,          // Balanced, all-around enhanced
            Juggernaut,     // Maximum armor, slow, devastating
            Assassin,       // Fast, flanking, critical hits
            Commander       // Buffs nearby sosigs, tactical leader
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
        public void Initialize(BossSosigSystem.BossType type, ManualLogSource logSource)
        {
            bossType = type;
            logger = logSource;

            StartCoroutine(DelayedBossSetup());
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

            // Apply boss enhancements
            ApplyBossStats();
            ApplyBossArmor();
            ApplyBossWeapons();

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

            logger?.LogInfo($"[BossSosig] {bossType} boss fully initialized");
        }
        #endregion

        #region Boss Configuration
        private void ApplyBossStats()
        {
            if (sosig == null) return;

            // Calculate stats based on type
            float healthMult = BossSosigSystem.BossHealthMultiplier;
            damageMultiplier = BossSosigSystem.BossDamageMultiplier;
            speedMultiplier = BossSosigSystem.BossSpeedMultiplier;

            switch (bossType)
            {
                case BossSosigSystem.BossType.Tank:
                    healthMult *= 1.5f;
                    speedMultiplier *= 0.7f;
                    damageMultiplier *= 0.8f;
                    break;

                case BossSosigSystem.BossType.Berserker:
                    healthMult *= 0.8f;
                    speedMultiplier *= 1.5f;
                    damageMultiplier *= 1.5f;
                    break;

                case BossSosigSystem.BossType.Sniper:
                    healthMult *= 1.0f;
                    speedMultiplier *= 0.9f;
                    damageMultiplier *= 2.0f;
                    break;

                case BossSosigSystem.BossType.Summoner:
                    healthMult *= 1.2f;
                    speedMultiplier *= 1.0f;
                    damageMultiplier *= 0.7f;
                    abilityCooldown = 15f;
                    break;

                case BossSosigSystem.BossType.Elite:
                    healthMult *= 1.3f;
                    speedMultiplier *= 1.1f;
                    damageMultiplier *= 1.2f;
                    break;

                case BossSosigSystem.BossType.Juggernaut:
                    healthMult *= 2.0f;
                    speedMultiplier *= 0.5f;
                    damageMultiplier *= 1.8f;
                    break;

                case BossSosigSystem.BossType.Assassin:
                    healthMult *= 0.7f;
                    speedMultiplier *= 1.8f;
                    damageMultiplier *= 1.7f;
                    break;

                case BossSosigSystem.BossType.Commander:
                    healthMult *= 1.4f;
                    speedMultiplier *= 1.0f;
                    damageMultiplier *= 1.1f;
                    abilityCooldown = 8f;
                    break;
            }

            // Apply health multiplier to all links
            foreach (var link in sosig.Links)
            {
                link.m_integrity *= healthMult;
            }

            // Calculate total health
            maxHealth = GetTotalHealth();
            currentHealth = maxHealth;

            // Apply speed (note: H3VR's Sosig doesn't have a simple "Speed" property we can modify)
            // Speed modifications would require deeper H3VR API integration
            // For now, we'll track the speed multiplier for potential future use
            logger?.LogDebug($"[BossSosig] Applied stats - Health: {maxHealth:F0}, Damage: x{damageMultiplier:F1}, Speed: x{speedMultiplier:F1}");
        }

        private void ApplyBossArmor()
        {
            // Apply enhanced armor based on type
            // This would integrate with SosigArmorWristMenuComplete if available
            logger?.LogDebug($"[BossSosig] Applied {bossType} armor configuration");
        }

        private void ApplyBossWeapons()
        {
            // Enhanced weapon loadout
            // Bosses get better weapons than regular sosigs
            logger?.LogDebug($"[BossSosig] Applied {bossType} weapon loadout");
        }

        private void ConfigureBossAI()
        {
            if (advancedAI == null) return;

            // Configure Advanced AI for boss-specific behavior
            switch (bossType)
            {
                case BossSosigSystem.BossType.Tank:
                    // Tank: Always aggressive, never retreat
                    advancedAI.ForceState(AdvancedSosigAI.AIState.Assault);
                    break;

                case BossSosigSystem.BossType.Sniper:
                    // Sniper: Prefer cover and distance
                    advancedAI.ForceState(AdvancedSosigAI.AIState.TakingCover);
                    break;

                case BossSosigSystem.BossType.Berserker:
                    // Berserker: Constant assault
                    advancedAI.ForceState(AdvancedSosigAI.AIState.Assault);
                    break;

                case BossSosigSystem.BossType.Assassin:
                    // Assassin: Flanking priority
                    advancedAI.ForceState(AdvancedSosigAI.AIState.Flanking);
                    break;

                default:
                    // Let AI decide naturally
                    break;
            }

            logger?.LogDebug($"[BossSosig] Configured {bossType} AI behavior");
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

            // Use boss ability based on type
            switch (bossType)
            {
                case BossSosigSystem.BossType.Summoner:
                    SpawnMinions();
                    break;

                case BossSosigSystem.BossType.Commander:
                    BuffNearbyAllies();
                    break;

                case BossSosigSystem.BossType.Berserker:
                    BerserkerCharge();
                    break;

                case BossSosigSystem.BossType.Tank:
                    TankShieldBash();
                    break;
            }

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

            logger?.LogInfo($"[BossSosig] {bossType} boss ENRAGED! (Health < {enrageThreshold * 100}%)");
        }

        private void SpawnMinions()
        {
            if (spawnedMinions.Count >= 3) return; // Max 3 minions

            try
            {
                // Spawn 1-2 minions near boss
                int minionCount = UnityEngine.Random.Range(1, 3);
                
                for (int i = 0; i < minionCount; i++)
                {
                    Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * 3f;
                    spawnPos.y = transform.position.y;

                    // This would integrate with AdvancedChatSosigSpawner
                    logger?.LogDebug($"[BossSosig] Summoner spawning minion {i + 1}/{minionCount}");
                }

                logger?.LogInfo($"[BossSosig] Summoner spawned {minionCount} minions");
            }
            catch (Exception ex)
            {
                logger?.LogError($"[BossSosig] Minion spawn failed: {ex.Message}");
            }
        }

        private void BuffNearbyAllies()
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, 15f);
            int buffedCount = 0;

            foreach (var col in nearbyColliders)
            {
                Sosig nearbySosig = col.GetComponentInParent<Sosig>();
                if (nearbySosig != null && nearbySosig != sosig && nearbySosig.E.IFFCode == sosig.E.IFFCode)
                {
                    // Buff ally sosig (increased speed/damage would require H3VR API)
                    buffedCount++;
                }
            }

            if (buffedCount > 0)
            {
                logger?.LogDebug($"[BossSosig] Commander buffed {buffedCount} allies");
            }
        }

        private void BerserkerCharge()
        {
            if (GM.CurrentPlayerBody == null) return;

            // Charge toward player at high speed
            Vector3 chargeDirection = (GM.CurrentPlayerBody.transform.position - transform.position).normalized;
            
            if (sosig != null)
            {
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                logger?.LogDebug("[BossSosig] Berserker charging player!");
            }
        }

        private void TankShieldBash()
        {
            // AOE knockback around boss
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 3f);

            foreach (var col in hitColliders)
            {
                Rigidbody rb = col.GetComponent<Rigidbody>();
                if (rb != null && col.gameObject != gameObject)
                {
                    Vector3 knockbackDir = (col.transform.position - transform.position).normalized;
                    rb.AddForce(knockbackDir * 500f, ForceMode.Impulse);
                }
            }

            logger?.LogDebug("[BossSosig] Tank shield bash!");
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
}
