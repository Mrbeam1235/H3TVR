using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Advanced AI system for sosigs - works alongside H3TwitchTools proven patterns
    /// Adds tactical behaviors, cover system, and squad coordination
    /// INCLUDES FRIENDLY FIRE PREVENTION
    /// </summary>
    public class AdvancedSosigAI : MonoBehaviour
    {
        #region Configuration
        public static bool EnableAdvancedAI { get; set; } = true;
        public static bool EnableCoverSystem { get; set; } = true;
        public static bool EnableSquadCoordination { get; set; } = false; // Optional
        public static bool EnableTacticalMovement { get; set; } = true;
        public static float CoverSearchRadius { get; set; } = 15f;
        public static float SuppressionRadius { get; set; } = 10f;
        public static bool PreventFriendlyFire { get; set; } = true; // Friendly fire prevention
        #endregion

        #region AI State
        public enum AIState
        {
            Following,      // H3TwitchTools: Follow player
            Assault,        // H3TwitchTools: Attack enemy
            TakingCover,    // Advanced: Use cover
            Suppressing,    // Advanced: Suppressive fire
            Flanking,       // Advanced: Tactical movement
            Retreating,     // Advanced: Fall back
            HoldingPosition // Advanced: Defend area
        }

        private AIState currentState = AIState.Following;
        private Transform coverPoint;
        private float lastStateChangeTime;
        private Vector3 lastKnownEnemyPosition;
        private bool hasLineOfSight;
        private int sosigIFF = -1; // Track sosig's IFF code
        private bool isAlly = false; // Is this sosig an ally?
        #endregion

        #region Components
        private Sosig sosig;
        private ManualLogSource logger;
        private bool isInitialized;
        #endregion

        #region Initialization
        public void Initialize(Sosig sosigInstance, ManualLogSource logSource)
        {
            sosig = sosigInstance;
            logger = logSource;
            isInitialized = true;

            if (sosig != null)
            {
                // Determine if this is an ally or enemy based on IFF
                sosigIFF = sosig.E.IFFCode;
                isAlly = (sosigIFF == 0); // IFF 0 = player/ally faction
                
                logger?.LogDebug($"[AdvancedAI] Initialized for sosig with IFF {sosigIFF} (Ally: {isAlly})");
                
                // Ensure sosig cannot target player if it's an ally
                if (isAlly && PreventFriendlyFire)
                {
                    ConfigureFriendlyFirePrevention();
                }
            }

            if (EnableAdvancedAI)
            {
                StartCoroutine(AIUpdateLoop());
            }
        }

        /// <summary>
        /// Configure sosig to prevent targeting the player
        /// </summary>
        private void ConfigureFriendlyFirePrevention()
        {
            try
            {
                if (sosig == null || !isAlly) return;

                // Set sosig to same IFF as player (0)
                sosig.E.IFFCode = 0;
                sosig.SetIFF(0);
                
                logger?.LogDebug("[AdvancedAI] Friendly fire prevention configured for ally sosig");
            }
            catch (Exception ex)
            {
                logger?.LogError($"[AdvancedAI] Error configuring friendly fire prevention: {ex.Message}");
            }
        }

        private IEnumerator AIUpdateLoop()
        {
            // Run at 2-second intervals to avoid performance overhead
            var wait = new WaitForSeconds(2f);

            while (isInitialized && sosig != null && sosig.BodyState != Sosig.SosigBodyState.Dead)
            {
                yield return wait;

                try
                {
                    if (EnableAdvancedAI)
                    {
                        // Ensure friendly fire prevention is active for allies
                        if (isAlly && PreventFriendlyFire)
                        {
                            EnforceFriendlyFirePrevention();
                        }

                        UpdateAIBehavior();
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError($"Advanced AI update error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Continuously enforce friendly fire prevention
        /// </summary>
        private void EnforceFriendlyFirePrevention()
        {
            try
            {
                // Ensure IFF stays at 0 for allies
                if (sosig.E.IFFCode != 0)
                {
                    sosig.E.IFFCode = 0;
                    sosig.SetIFF(0);
                    logger?.LogDebug("[AdvancedAI] Reset ally IFF to 0");
                }
            }
            catch (Exception ex)
            {
                logger?.LogDebug($"[AdvancedAI] Error enforcing friendly fire prevention: {ex.Message}");
            }
        }
        #endregion

        #region Core AI Logic
        private void UpdateAIBehavior()
        {
            if (sosig == null || sosig.m_isStunned) return;

            // Update perception (with friendly fire check)
            UpdateTargetTracking();

            // Determine best state based on situation
            EvaluateState();

            // Execute current state behavior
            ExecuteCurrentState();
        }

        private void UpdateTargetTracking()
        {
            if (sosig.Priority.HasFreshTarget())
            {
                // Get the target position
                Vector3 targetPos = sosig.m_assaultPoint;
                
                // Don't track player as enemy if we're an ally
                if (!IsTargetingPlayer())
                {
                    lastKnownEnemyPosition = targetPos;
                    hasLineOfSight = HasLineOfSight(targetPos);
                }
                else
                {
                    hasLineOfSight = false;
                    logger?.LogDebug("[AdvancedAI] Prevented ally from targeting player");
                }
            }
            else
            {
                hasLineOfSight = false;
            }
        }

        /// <summary>
        /// Check if current target is the player (simple proximity check for safety)
        /// </summary>
        private bool IsTargetingPlayer()
        {
            if (!isAlly || GM.CurrentPlayerBody == null) return false;

            // Check if assault point is near player position
            float distanceToPlayer = Vector3.Distance(sosig.m_assaultPoint, GM.CurrentPlayerBody.transform.position);
            return distanceToPlayer < 2f; // If targeting within 2m of player, assume targeting player
        }

        private void EvaluateState()
        {
            // Don't change state too frequently
            if (Time.time - lastStateChangeTime < 3f) return;

            // ALLY BEHAVIOR: Never attack player
            if (isAlly)
            {
                EvaluateAllyState();
            }
            else
            {
                EvaluateEnemyState();
            }
        }

        /// <summary>
        /// Evaluate AI state for ally sosigs (never target player)
        /// </summary>
        private void EvaluateAllyState()
        {
            // Check for valid non-player targets
            if (!sosig.Priority.HasFreshTarget() || IsTargetingPlayer())
            {
                // No valid targets - default to following behavior
                SetState(AIState.Following);
                return;
            }

            // Has valid enemy target
            float distanceToThreat = Vector3.Distance(sosig.transform.position, lastKnownEnemyPosition);
            float healthPercent = GetHealthPercent();

            // Low health - seek cover
            if (healthPercent < 0.3f && EnableCoverSystem)
            {
                if (FindNearestCover())
                {
                    SetState(AIState.TakingCover);
                    return;
                }
                SetState(AIState.Retreating);
                return;
            }

            // Medium health - use cover if available
            if (EnableCoverSystem && distanceToThreat > 8f && healthPercent < 0.6f)
            {
                if (FindNearestCover())
                {
                    SetState(AIState.TakingCover);
                    return;
                }
            }

            // Assault enemy targets
            SetState(AIState.Assault);
        }

        /// <summary>
        /// Evaluate AI state for enemy sosigs
        /// </summary>
        private void EvaluateEnemyState()
        {
            if (!sosig.Priority.HasFreshTarget())
            {
                // No threats - default behavior
                SetState(AIState.Assault);
                return;
            }

            // Evaluate threat level
            float distanceToThreat = Vector3.Distance(sosig.transform.position, lastKnownEnemyPosition);
            float healthPercent = GetHealthPercent();

            // Low health - consider retreat or cover
            if (healthPercent < 0.3f && EnableCoverSystem)
            {
                if (FindNearestCover())
                {
                    SetState(AIState.TakingCover);
                    return;
                }
                SetState(AIState.Retreating);
                return;
            }

            // Close range - assault
            if (distanceToThreat < 8f && healthPercent > 0.5f)
            {
                SetState(AIState.Assault);
                return;
            }

            // Medium range with cover available
            if (EnableCoverSystem && distanceToThreat > 8f && distanceToThreat < 25f)
            {
                if (!hasLineOfSight || healthPercent < 0.6f)
                {
                    if (FindNearestCover())
                    {
                        SetState(AIState.TakingCover);
                        return;
                    }
                }
            }

            // Default to assault when in combat
            SetState(AIState.Assault);
        }

        private void SetState(AIState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                lastStateChangeTime = Time.time;
                logger?.LogDebug($"Sosig AI state changed to: {newState}");
            }
        }
        #endregion

        #region State Execution
        private void ExecuteCurrentState()
        {
            // SAFETY CHECK: Allies should never assault player
            if (isAlly && currentState == AIState.Assault && IsTargetingPlayer())
            {
                SetState(AIState.Following);
                return;
            }

            switch (currentState)
            {
                case AIState.Following:
                    // Handled by H3TwitchTools base logic
                    break;

                case AIState.Assault:
                    ExecuteAssault();
                    break;

                case AIState.TakingCover:
                    ExecuteTakeCover();
                    break;

                case AIState.Suppressing:
                    ExecuteSuppression();
                    break;

                case AIState.Flanking:
                    ExecuteFlank();
                    break;

                case AIState.Retreating:
                    ExecuteRetreat();
                    break;

                case AIState.HoldingPosition:
                    ExecuteHold();
                    break;
            }
        }

        private void ExecuteAssault()
        {
            // FRIENDLY FIRE CHECK
            if (isAlly && IsTargetingPlayer())
            {
                SetState(AIState.Following);
                return;
            }

            if (!hasLineOfSight && EnableTacticalMovement)
            {
                // Move to last known position
                sosig.CommandAssaultPoint(lastKnownEnemyPosition);
            }
            else
            {
                // Direct assault
                sosig.SetCurrentOrder(Sosig.SosigOrder.Assault);
            }
        }

        private void ExecuteTakeCover()
        {
            if (coverPoint != null)
            {
                // Move to cover
                float distanceToCover = Vector3.Distance(sosig.transform.position, coverPoint.position);

                if (distanceToCover > 1.5f)
                {
                    sosig.CommandAssaultPoint(coverPoint.position);
                }
                else
                {
                    // At cover - peek and shoot (but not at player if ally)
                    if (hasLineOfSight && !(isAlly && IsTargetingPlayer()))
                    {
                        sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                    }
                    else
                    {
                        sosig.SetCurrentOrder(Sosig.SosigOrder.GuardPoint);
                    }
                }
            }
            else
            {
                // No cover found - fallback to assault
                SetState(AIState.Assault);
            }
        }

        private void ExecuteSuppression()
        {
            // FRIENDLY FIRE CHECK
            if (isAlly && IsTargetingPlayer())
            {
                SetState(AIState.Following);
                return;
            }

            // Suppressive fire toward enemy position
            if (hasLineOfSight)
            {
                sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
            }
            else
            {
                sosig.CommandAssaultPoint(lastKnownEnemyPosition);
            }
        }

        private void ExecuteFlank()
        {
            // Calculate flank position
            Vector3 flankPosition = CalculateFlankPosition();

            if (flankPosition != Vector3.zero)
            {
                sosig.CommandAssaultPoint(flankPosition);
            }
            else
            {
                // Can't flank - assault instead
                SetState(AIState.Assault);
            }
        }

        private void ExecuteRetreat()
        {
            // Find safe position away from threat
            Vector3 retreatPosition = CalculateRetreatPosition();

            if (retreatPosition != Vector3.zero)
            {
                sosig.CommandAssaultPoint(retreatPosition);
            }

            // If health recovers, return to combat
            if (GetHealthPercent() > 0.5f)
            {
                SetState(AIState.Assault);
            }
        }

        private void ExecuteHold()
        {
            sosig.SetCurrentOrder(Sosig.SosigOrder.GuardPoint);

            // Return to combat if enemy is close (but not if ally and enemy is player)
            if (!(isAlly && IsTargetingPlayer()) && 
                Vector3.Distance(sosig.transform.position, lastKnownEnemyPosition) < 10f)
            {
                SetState(AIState.Assault);
            }
        }
        #endregion

        #region Helper Methods
        private bool HasLineOfSight(Vector3 targetPosition)
        {
            if (sosig == null || sosig.Links.Count == 0) return false;

            Vector3 eyePosition = sosig.Links[0].transform.position; // Head
            Vector3 direction = targetPosition - eyePosition;

            return !Physics.Raycast(eyePosition, direction.normalized, direction.magnitude, LayerMask.GetMask("Environment"));
        }

        private bool FindNearestCover()
        {
            if (!EnableCoverSystem) return false;

            Collider[] nearbyObjects = Physics.OverlapSphere(sosig.transform.position, CoverSearchRadius);
            float closestDistance = float.MaxValue;
            Transform bestCover = null;

            foreach (var obj in nearbyObjects)
            {
                // Look for objects that can provide cover
                if (obj.gameObject.layer == LayerMask.NameToLayer("Environment"))
                {
                    float distance = Vector3.Distance(sosig.transform.position, obj.transform.position);

                    // Check if it's actually cover (between sosig and enemy)
                    if (IsCoverEffective(obj.transform.position) && distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestCover = obj.transform;
                    }
                }
            }

            if (bestCover != null)
            {
                coverPoint = bestCover;
                return true;
            }

            return false;
        }

        private bool IsCoverEffective(Vector3 coverPosition)
        {
            // Cover should be between sosig and enemy
            Vector3 toEnemy = lastKnownEnemyPosition - sosig.transform.position;
            Vector3 toCover = coverPosition - sosig.transform.position;

            float dotProduct = Vector3.Dot(toEnemy.normalized, toCover.normalized);

            // Cover is in the right direction if dot product > 0.5
            return dotProduct > 0.5f;
        }

        private Vector3 CalculateFlankPosition()
        {
            if (lastKnownEnemyPosition == Vector3.zero) return Vector3.zero;

            // Calculate perpendicular position to flank
            Vector3 toEnemy = lastKnownEnemyPosition - sosig.transform.position;
            Vector3 right = Vector3.Cross(toEnemy, Vector3.up).normalized;

            // Try both sides
            Vector3 flankLeft = sosig.transform.position + right * 10f;
            Vector3 flankRight = sosig.transform.position - right * 10f;

            // Choose side with clear path
            if (!Physics.Linecast(sosig.transform.position, flankLeft, LayerMask.GetMask("Environment")))
            {
                return flankLeft;
            }
            else if (!Physics.Linecast(sosig.transform.position, flankRight, LayerMask.GetMask("Environment")))
            {
                return flankRight;
            }

            return Vector3.zero;
        }

        private Vector3 CalculateRetreatPosition()
        {
            if (lastKnownEnemyPosition == Vector3.zero) return Vector3.zero;

            // Move away from threat
            Vector3 awayFromEnemy = sosig.transform.position - lastKnownEnemyPosition;
            Vector3 retreatPoint = sosig.transform.position + awayFromEnemy.normalized * 15f;

            // Validate path is clear
            if (!Physics.Linecast(sosig.transform.position, retreatPoint, LayerMask.GetMask("Environment")))
            {
                return retreatPoint;
            }

            return Vector3.zero;
        }

        private float GetHealthPercent()
        {
            if (sosig == null || sosig.Links.Count == 0) return 0f;

            float totalHealth = 0f;
            int linkCount = 0;

            foreach (var link in sosig.Links)
            {
                // Use current integrity - assume max is 1.0 (100%)
                // This is a simplified approach since we don't have access to max integrity
                totalHealth += link.m_integrity;
                linkCount++;
            }

            // Average integrity across all links as health percentage
            return linkCount > 0 ? totalHealth / linkCount : 0f;
        }
        #endregion

        #region Public API
        /// <summary>
        /// Force sosig into specific state (for testing or scripting)
        /// </summary>
        public void ForceState(AIState state)
        {
            if (state == currentState) return;
            
            try
            {
                SetState(state);
                logger?.LogDebug($"[AdvancedAI] Forced state change to: {state}");
            }
            catch (Exception ex)
            {
                logger?.LogError($"[AdvancedAI] ForceState error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get current AI state
        /// </summary>
        public AIState GetCurrentState()
        {
            return currentState;
        }
        
        #endregion
        
        #region Cleanup
        private void OnDestroy()
        {
            isInitialized = false;
            sosig = null;
            coverPoint = null;
        }
        #endregion
    }

    /// <summary>
    /// Configuration for Advanced AI system
    /// </summary>
    public static class AdvancedAIConfig
    {
        public static void ApplyConfig(BepInEx.Configuration.ConfigFile config)
        {
            var enableAI = config.Bind("Advanced AI", "EnableAdvancedAI", true, 
                "Enable advanced AI behaviors (cover, tactics, etc)");
            var enableCover = config.Bind("Advanced AI", "EnableCoverSystem", true, 
                "Enable sosigs taking cover");
            var enableSquad = config.Bind("Advanced AI", "EnableSquadCoordination", false, 
                "Enable squad coordination behaviors");
            var enableTactical = config.Bind("Advanced AI", "EnableTacticalMovement", true, 
                "Enable tactical movement (flanking, suppression)");
            var coverRadius = config.Bind("Advanced AI", "CoverSearchRadius", 15f, 
                "Radius to search for cover points");
            var suppressionRadius = config.Bind("Advanced AI", "SuppressionRadius", 10f, 
                "Radius for suppressive fire");
            var preventFF = config.Bind("Advanced AI", "PreventFriendlyFire", true,
                "Prevent ally sosigs from targeting the player");

            AdvancedSosigAI.EnableAdvancedAI = enableAI.Value;
            AdvancedSosigAI.EnableCoverSystem = enableCover.Value;
            AdvancedSosigAI.EnableSquadCoordination = enableSquad.Value;
            AdvancedSosigAI.EnableTacticalMovement = enableTactical.Value;
            AdvancedSosigAI.CoverSearchRadius = coverRadius.Value;
            AdvancedSosigAI.SuppressionRadius = suppressionRadius.Value;
            AdvancedSosigAI.PreventFriendlyFire = preventFF.Value;
        }
    }
}
