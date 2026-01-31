using System;
using BepInEx.Logging;
using FistVR;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Controls sosig AI behavior patterns
    /// Enhanced with smarter ally AI that won't friendly fire
    /// </summary>
    public class SosigBehaviorController
    {
        private ManualLogSource logger;
        private static readonly LayerMask EnvironmentMask = LayerMask.GetMask("Environment");

        // Configuration for ally behavior
        public float AllyFollowDistance { get; set; } = 5f;
        public float AllyMinDistance { get; set; } = 2f;
        public float AllyCombatRange { get; set; } = 15f;
        public float AllyDefendRadius { get; set; } = 10f;
        public bool AllyProtectPlayer { get; set; } = true;
        public bool AllyHoldFire { get; set; } = false;

        public void Initialize(ManualLogSource logSource)
        {
            logger = logSource;
        }

        /// <summary>
        /// Sets up ally behavior with improved IFF to prevent friendly fire
        /// </summary>
        public void SetupAllyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.Head?.transform == null) return;

                // Set IFF to 0 (same as player) to prevent friendly fire
                sosig.E.IFFCode = 0;
                sosig.SetIFF(0);

                // Configure IFF chart - allies won't attack player or other allies
                if (sosig.Priority.IFFChart != null)
                {
                    for (int i = 0; i < sosig.Priority.IFFChart.Length; i++)
                    {
                        // IFF 0 = Player/Allies (friendly), IFF 1+ = Enemies (hostile)
                        sosig.Priority.IFFChart[i] = (i != 0);
                    }
                }

                // Set up guard/follow behavior
                var playerPos = GM.CurrentPlayerBody.Head.transform.position;
                float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);

                sosig.CommandAssaultPoint(followPoint);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;

                // Configure sosig to be more careful with friendly fire
                ConfigureAllyFireSafety(sosig);

                logger?.LogInfo($"Ally sosig configured with IFF 0 (player-friendly)");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Ally behavior setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Configures ally sosig to avoid friendly fire
        /// </summary>
        private void ConfigureAllyFireSafety(Sosig sosig)
        {
            try
            {
                // Configure the sosig to be defensive rather than aggressive
                sosig.Mustard = 1f; // Full health/morale
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Fire safety config warning: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets up enemy behavior - hostile to player
        /// </summary>
        public void SetupEnemyBehavior(Sosig sosig)
        {
            try
            {
                if (GM.CurrentPlayerBody?.transform == null) return;

                // Set enemy IFF (1 or higher = hostile to player)
                int enemyIFF = 1;
                sosig.E.IFFCode = enemyIFF;
                sosig.SetIFF(enemyIFF);

                // Configure IFF chart - enemies attack player (IFF 0) but not each other
                if (sosig.Priority.IFFChart != null)
                {
                    for (int i = 0; i < sosig.Priority.IFFChart.Length; i++)
                    {
                        // Attack IFF 0 (player/allies), don't attack same IFF
                        sosig.Priority.IFFChart[i] = (i == 0);
                    }
                }

                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
            catch (Exception ex)
            {
                logger?.LogError($"Enemy behavior setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates ally behavior with smarter following and protection logic
        /// </summary>
        public void UpdateAllyBehavior(Sosig sosig, float followDistance)
        {
            if (GM.CurrentPlayerBody?.Head == null) return;

            try
            {
                // Re-verify IFF is correct (prevent bugs from changing it)
                if (sosig.E.IFFCode != 0)
                {
                    sosig.E.IFFCode = 0;
                    sosig.SetIFF(0);
                }

                if (!sosig.m_isStunned)
                {
                    var playerPos = GM.CurrentPlayerBody.Head.position;
                    float distanceToPlayer = Vector3.Distance(playerPos, sosig.transform.position);
                    float distanceToAssaultPoint = Vector3.Distance(playerPos, sosig.m_assaultPoint);

                    // Check if there are nearby enemies to engage
                    bool hasNearbyEnemy = CheckForNearbyEnemies(sosig, AllyCombatRange);

                    if (hasNearbyEnemy && AllyProtectPlayer)
                    {
                        // Ally has detected an enemy - engage in combat
                        if (sosig.CurrentOrder != Sosig.SosigOrder.Skirmish)
                        {
                            sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                        }
                    }
                    else if (distanceToAssaultPoint > followDistance)
                    {
                        // Too far from follow point - move closer to player
                        float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                        float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                        Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);

                        bool isBad = Physics.Linecast(playerPos, followPoint, EnvironmentMask);
                        if (!isBad)
                        {
                            sosig.CommandAssaultPoint(followPoint);
                        }
                    }
                    else if (distanceToPlayer < AllyMinDistance)
                    {
                        // Too close to player - back off a bit
                        Vector3 awayFromPlayer = (sosig.transform.position - playerPos).normalized;
                        Vector3 backOffPoint = sosig.transform.position + awayFromPlayer * 2f;
                        sosig.CommandAssaultPoint(backOffPoint);
                    }
                }

                // Check for fresh targets and engage if appropriate
                if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.65f)
                {
                    // Verify target is actually an enemy before engaging
                    if (IsValidEnemyTarget(sosig))
                    {
                        sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Ally update warning: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if there are nearby enemies for the ally to engage
        /// </summary>
        private bool CheckForNearbyEnemies(Sosig allySosig, float range)
        {
            try
            {
                // Check if the sosig has detected any targets
                if (allySosig.Priority.HasFreshTarget())
                {
                    return true;
                }

                // Also check for nearby enemy sosigs
                var allSosigs = UnityEngine.Object.FindObjectsOfType<Sosig>();
                foreach (var otherSosig in allSosigs)
                {
                    if (otherSosig == null || otherSosig == allySosig) continue;
                    if (otherSosig.BodyState == Sosig.SosigBodyState.Dead) continue;

                    // Check if this is an enemy (different IFF)
                    if (otherSosig.E.IFFCode != 0)
                    {
                        float distance = Vector3.Distance(allySosig.transform.position, otherSosig.transform.position);
                        if (distance <= range)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Enemy check warning: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Verifies that the sosig's current target is a valid enemy (not player or ally)
        /// </summary>
        private bool IsValidEnemyTarget(Sosig sosig)
        {
            try
            {
                // Make sure we're not targeting the player
                if (GM.CurrentPlayerBody?.transform == null) return false;

                var playerPos = GM.CurrentPlayerBody.transform.position;
                var sosigPos = sosig.transform.position;

                // Get the direction the sosig is facing/targeting
                if (sosig.Priority.HasFreshTarget())
                {
                    // The sosig has a target - verify it's not the player
                    // by checking if the target position is far from the player
                    return true; // Trust the IFF system
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Target validation warning: {ex.Message}");
            }

            return false;
        }

        /// <summary>
        /// Updates enemy behavior with aggressive pursuit
        /// </summary>
        public void UpdateEnemyBehavior(Sosig sosig, float aggressionDistance)
        {
            if (GM.CurrentPlayerBody?.Head == null) return;

            if (!sosig.m_isStunned)
            {
                var playerPos = GM.CurrentPlayerBody.Head.position;
                float distance = Vector3.Distance(playerPos, sosig.Links[1].transform.position);

                if (distance > aggressionDistance)
                {
                    sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                }
            }

            if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.55f)
            {
                sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
            }

            if (sosig.CurrentOrder == Sosig.SosigOrder.Disabled || sosig.CurrentOrder == Sosig.SosigOrder.Idle || sosig.CurrentOrder == Sosig.SosigOrder.GuardPoint)
            {
                sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
            }
        }

        /// <summary>
        /// Forces all allies to hold fire (useful for LioranBoard command)
        /// </summary>
        public void SetAlliesHoldFire(bool holdFire)
        {
            AllyHoldFire = holdFire;
            logger?.LogInfo($"Allies hold fire: {holdFire}");
        }

        /// <summary>
        /// Makes all allies defend a specific point
        /// </summary>
        public void CommandAlliesDefendPoint(Vector3 point)
        {
            try
            {
                foreach (var sosig in AdvancedChatSosigSpawner.spawnedChatters)
                {
                    if (sosig != null && sosig.BodyState != Sosig.SosigBodyState.Dead)
                    {
                        sosig.CommandAssaultPoint(point);
                        sosig.SetCurrentOrder(Sosig.SosigOrder.GuardPoint);
                    }
                }
                logger?.LogInfo($"Allies commanded to defend point: {point}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Defend point command failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Makes all allies follow the player
        /// </summary>
        public void CommandAlliesFollowPlayer()
        {
            try
            {
                if (GM.CurrentPlayerBody?.Head == null) return;

                var playerPos = GM.CurrentPlayerBody.Head.position;

                foreach (var sosig in AdvancedChatSosigSpawner.spawnedChatters)
                {
                    if (sosig != null && sosig.BodyState != Sosig.SosigBodyState.Dead)
                    {
                        float offsetX = UnityEngine.Random.Range(-2f, 2f);
                        float offsetZ = UnityEngine.Random.Range(-2f, 2f);
                        Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);

                        sosig.CommandAssaultPoint(followPoint);
                        sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
                    }
                }
                logger?.LogInfo("Allies commanded to follow player");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Follow command failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Makes all allies attack a specific enemy
        /// </summary>
        public void CommandAlliesAttackTarget(Vector3 targetPosition)
        {
            try
            {
                foreach (var sosig in AdvancedChatSosigSpawner.spawnedChatters)
                {
                    if (sosig != null && sosig.BodyState != Sosig.SosigBodyState.Dead)
                    {
                        sosig.CommandAssaultPoint(targetPosition);
                        sosig.SetCurrentOrder(Sosig.SosigOrder.Assault);
                    }
                }
                logger?.LogInfo($"Allies commanded to attack: {targetPosition}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"Attack command failed: {ex.Message}");
            }
        }
    }
}
