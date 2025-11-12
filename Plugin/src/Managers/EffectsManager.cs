using UnityEngine;
using FistVR;
using System.Collections;
using System;
using BepInEx.Logging;
using Valve.VR;

namespace H3TVR
{
    /// <summary>
    /// Manages all special effects including slomo, zero gravity, and VR interactions
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private SlomoMovementController slomoController;
        private ManualLogSource logger;
        private static EffectsManager instance;

        // Slomo ramp state tracking
        private float rampStartTime;
        private float rampStartValue;
        private float rampTargetValue;
        private bool isRampingDown;
        private bool isRampingUp;

        public void Initialize(H3TVRImproved pluginInstance, SlomoMovementController controller, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            slomoController = controller;
            logger = logSource;
            instance = this;
            
            // Initialize ramp state
            rampStartTime = 0f;
            rampStartValue = 1f;
            rampTargetValue = 1f;
            isRampingDown = false;
            isRampingUp = false;
        }

        /// <summary>
        /// Check if EffectsManager is initialized
        /// </summary>
        public static bool IsInitialized()
        {
            return instance != null;
        }

        #region Slomo Effects
        public void SlomoScaleDown()
        {
            float maxSlomoValue, waitTime, scaleSpeed, returnSpeed;
            plugin.GetSlomoConfig(out maxSlomoValue, out waitTime, out scaleSpeed, out returnSpeed);
            
            bool useRamp;
            string curve;
            float rampDuration, returnDuration;
            plugin.GetSlomoRampConfig(out useRamp, out curve, out rampDuration, out returnDuration);
        
            if (useRamp)
            {
                // Use smooth ramp speed
                if (!isRampingDown)
                {
                    // Start ramping
                    rampStartTime = Time.unscaledTime;
                    rampStartValue = Time.timeScale;
                    rampTargetValue = maxSlomoValue;
                    isRampingDown = true;
                    isRampingUp = false;
                }
    
                float elapsed = Time.unscaledTime - rampStartTime;
                float t = Mathf.Clamp01(elapsed / rampDuration);
            
                // Apply easing curve
                float easedT = ApplyEasingCurve(t, curve);
                
                // Interpolate between start and target
                Time.timeScale = Mathf.Lerp(rampStartValue, rampTargetValue, easedT);
                Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
    
                slomoController?.UpdateMovementScale(Time.timeScale);
                
                if (t >= 1f)
                {
                    // Ramp complete
                    isRampingDown = false;
                    plugin.SetSlomoStatus("Wait");
                }
            }
            else
            {
                // Original linear scaling
                if (Time.timeScale > maxSlomoValue)
                {
                    Time.timeScale -= scaleSpeed * Time.unscaledDeltaTime;
                    Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                    Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
                    
                    slomoController?.UpdateMovementScale(Time.timeScale);
                }

                if (Time.timeScale <= maxSlomoValue)
                {
                    plugin.SetSlomoStatus("Wait");
                }
            }
        }

        public void SlomoReturn()
        {
            float maxSlomoValue, waitTime, scaleSpeed, returnSpeed;
            plugin.GetSlomoConfig(out maxSlomoValue, out waitTime, out scaleSpeed, out returnSpeed);
   
            bool useRamp;
            string curve;
            float rampDuration, returnDuration;
            plugin.GetSlomoRampConfig(out useRamp, out curve, out rampDuration, out returnDuration);
         
            if (useRamp)
            {
                // Use smooth ramp speed for return
                if (!isRampingUp)
                {
                    // Start ramping up
                    rampStartTime = Time.unscaledTime;
                    rampStartValue = Time.timeScale;
                    rampTargetValue = 1f;
                    isRampingUp = true;
                    isRampingDown = false;
                }
                
                float elapsed = Time.unscaledTime - rampStartTime;
                float t = Mathf.Clamp01(elapsed / returnDuration);
                   
                // Apply easing curve
                float easedT = ApplyEasingCurve(t, curve);
        
                // Interpolate to normal speed
                Time.timeScale = Mathf.Lerp(rampStartValue, rampTargetValue, easedT);
                Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
        
                slomoController?.UpdateMovementScale(Time.timeScale);
        
                if (t >= 1f)
                {
                    // Ramp complete
                    isRampingUp = false;
                }
            }
            else
            {
                // Original linear return
                if (Time.timeScale != 1)
                {
                    Time.timeScale += returnSpeed * Time.unscaledDeltaTime;
                    Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                    Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
                
                    slomoController?.UpdateMovementScale(Time.timeScale);
                }
             }
        }
        
        public IEnumerator SlomoWait(System.Action onComplete)
        {
            float maxSlomoValue, waitTime, scaleSpeed, returnSpeed;
            plugin.GetSlomoConfig(out maxSlomoValue, out waitTime, out scaleSpeed, out returnSpeed);
            yield return new WaitForSecondsRealtime(waitTime);
            onComplete?.Invoke();
        }

        public IEnumerator ActivatePillowSlomo(float duration)
        {
            float maxSlomoValue, waitTime, scaleSpeed, returnSpeed;
            plugin.GetSlomoConfig(out maxSlomoValue, out waitTime, out scaleSpeed, out returnSpeed);
     float originalTimeScale = Time.timeScale;
  
     Time.timeScale = maxSlomoValue;
    Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
   slomoController?.UpdateMovementScale(Time.timeScale);

        logger.LogInfo($"Pillow slow motion activated for {duration} seconds (scale: {maxSlomoValue})");

   yield return new WaitForSecondsRealtime(duration);

            Time.timeScale = originalTimeScale;
      Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
            slomoController?.UpdateMovementScale(Time.timeScale);
         
          logger.LogInfo("Pillow slow motion effect ended");
 }

        /// <summary>
        /// Apply easing curve to time value for smooth transitions
        /// </summary>
        private float ApplyEasingCurve(float t, string curveType)
        {
            switch (curveType.ToLower())
            {
                case "linear":
                    return t;
            
                case "easein":
                    // Slow start, fast end
                    return t * t;
                        
                case "easeout":
                    // Fast start, slow end
                    return t * (2f - t);
                
                case "easeinout":
                    // Slow start and end
                    return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
                
                case "smooth":
                    // Smooth cubic easing
                    return t * t * (3f - 2f * t);
                
                case "cinematic":
                    // Very smooth quintic easing (most cinematic)
                    return t * t * t * (t * (t * 6f - 15f) + 10f);
                
                default:
                    logger.LogWarning($"Unknown easing curve: {curveType}. Using EaseInOut.");
                    return t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t;
            }
        }
        #endregion

        #region Zero Gravity Effects
        public void ZeroGravityBumpDown()
        {
            try
            {
                GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.None;
                GM.CurrentSceneSettings.RefreshGravity();
                // Zero gravity status is managed by the main plugin
            }
            catch (Exception ex)
            {
                logger.LogError($"ZeroGravityBumpDown failed: {ex.Message}");
            }
        }

        public void ZeroGravityBumpUp()
        {
            try
            {
                GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Playful;
                GM.CurrentSceneSettings.RefreshGravity();
            }
            catch (Exception ex)
            {
                logger.LogError($"ZeroGravityBumpUp failed: {ex.Message}");
            }
        }

        public void RealisticFall()
        {
            try
            {
                GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Realistic;
                GM.CurrentSceneSettings.RefreshGravity();
            }
            catch (Exception ex)
            {
                logger.LogError($"RealisticFall failed: {ex.Message}");
            }
        }

        public IEnumerator ZeroGWait(System.Action onComplete)
        {
            yield return new WaitForSeconds(6f); // ZeroGWaitTime constant
            onComplete?.Invoke();
        }

        public IEnumerator RealisticFallWait(System.Action onComplete)
        {
            yield return new WaitForSecondsRealtime(1f); // RealisticFallTime constant
            onComplete?.Invoke();
        }

        public IEnumerator ActivatePillowZeroGravity(float duration)
        {
            var originalGravityMode = GM.Options.SimulationOptions.ObjectGravityMode;
            
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.None;
            GM.CurrentSceneSettings.RefreshGravity();
            logger.LogInfo($"Pillow zero gravity activated for {duration} seconds");

            yield return new WaitForSecondsRealtime(duration);

            GM.Options.SimulationOptions.ObjectGravityMode = originalGravityMode;
            GM.CurrentSceneSettings.RefreshGravity();
            logger.LogInfo("Pillow zero gravity effect ended");
        }
        #endregion

        #region VR Input Handling
        public bool CheckVRButtonPress(string buttonName)
        {
            try
            {
                var hands = GM.CurrentMovementManager?.Hands;
                if (hands == null || hands.Length == 0) return false;

                switch (buttonName.ToLower())
                {
                    case "leftx":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.AXButtonDown;
                    case "rightx":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.AXButtonDown;
                    case "lefty":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.BYButtonDown;
                    case "righty":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.BYButtonDown;
                    case "leftgrip":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.GripDown;
                    case "rightgrip":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.GripDown;
                    case "lefttrigger":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.TriggerDown;
                    case "righttrigger":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.TriggerDown;
                    case "lefttouchpad":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.TouchpadDown;
                    case "righttouchpad":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.TouchpadDown;
                    default:
                        logger.LogWarning($"Unknown VR button configuration: {buttonName}. Using default LeftX.");
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.AXButtonDown;
                }
            }
            catch (Exception ex)
            {
                logger.LogError($"CheckVRButtonPress failed for button {buttonName}: {ex.Message}");
                return false;
            }
        }
        #endregion

        #region Other Effects
        public void EnableMeatHands()
        {
            try
            {
                var hands = GM.CurrentMovementManager?.Hands;
                if (hands == null || hands.Length < 2)
                {
                    logger.LogWarning("Cannot enable meat hands: Hand references not available");
                    return;
                }

                hands[0].SpawnSausageFingers();
                hands[1].SpawnSausageFingers();
                logger.LogInfo("Meat hands enabled");
            }
            catch (Exception ex)
            {
                logger.LogError($"EnableMeatHands failed: {ex.Message}");
            }
        }
        #endregion

        /// <summary>
        /// Play Stovepipe malfunction particle effects
        /// </summary>
        public static void PlayStovepipeParticles(Vector3 position, StovepipeIntegrationManager.MalfunctionType malfunctionType)
        {
            try
            {
                if (instance == null)
                {
                    instance?.logger?.LogWarning("EffectsManager not initialized for Stovepipe particles");
                    return;
                }

                // Create appropriate particle effect based on malfunction type
                GameObject particleEffect = CreateStovepipeParticleEffect(position, malfunctionType);
                
                if (particleEffect != null)
                {
                    instance.logger?.LogDebug($"Playing Stovepipe particle effect for {malfunctionType} at {position}");
                    
                    // Auto-destroy particle effect after a short time
                    MonoBehaviour.Destroy(particleEffect, 3.0f);
                }
            }
            catch (Exception ex)
            {
                instance?.logger?.LogError($"PlayStovepipeParticles failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Create particle effect for specific malfunction type
        /// </summary>
        private static GameObject CreateStovepipeParticleEffect(Vector3 position, StovepipeIntegrationManager.MalfunctionType malfunctionType)
        {
            try
            {
                GameObject effectObject = new GameObject($"StovepipeEffect_{malfunctionType}");
                effectObject.transform.position = position;

                // Add particle system
                var particles = effectObject.AddComponent<ParticleSystem>();
                var main = particles.main;
                var emission = particles.emission;
                var shape = particles.shape;
                var velocityOverLifetime = particles.velocityOverLifetime;

                // Configure based on malfunction type
                switch (malfunctionType)
                {
                    case StovepipeIntegrationManager.MalfunctionType.Stovepipe:
                        // Brass casing stuck in ejection port
                        main.startColor = new Color(0.8f, 0.6f, 0.2f, 0.8f); // Brass color
                        main.startSize = 0.02f;
                        main.startLifetime = 2.0f;
                        emission.rateOverTime = 20;
                        break;

                    case StovepipeIntegrationManager.MalfunctionType.DoubleFeed:
                        // Two rounds jamming
                        main.startColor = new Color(0.7f, 0.7f, 0.2f, 0.9f); // Brass/steel mix
                        main.startSize = 0.015f;
                        main.startLifetime = 1.5f;
                        emission.rateOverTime = 30;
                        break;

                    case StovepipeIntegrationManager.MalfunctionType.FailureToEject:
                        // Spent casing stuck
                        main.startColor = new Color(0.6f, 0.4f, 0.2f, 0.7f); // Dirty brass
                        main.startSize = 0.018f;
                        main.startLifetime = 2.5f;
                        emission.rateOverTime = 15;
                        break;

                    case StovepipeIntegrationManager.MalfunctionType.FailureToFeed:
                        // Round not chambering properly
                        main.startColor = new Color(0.8f, 0.8f, 0.3f, 0.6f); // Fresh brass
                        main.startSize = 0.012f;
                        main.startLifetime = 1.0f;
                        emission.rateOverTime = 25;
                        break;

                    case StovepipeIntegrationManager.MalfunctionType.DirtyGun:
                        // Fouling/dirt particles
                        main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.8f); // Dark/dirty
                        main.startSize = 0.008f;
                        main.startLifetime = 3.0f;
                        emission.rateOverTime = 40;
                        break;

                    default:
                        // Generic malfunction effect
                        main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Neutral gray
                        main.startSize = 0.01f;
                        main.startLifetime = 1.5f;
                        emission.rateOverTime = 20;
                        break;
                }

                // Common settings
                main.startSpeed = 0.5f;
                main.maxParticles = 50;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 15f;
                shape.radius = 0.05f;

                // Add some movement
                velocityOverLifetime.enabled = true;
                velocityOverLifetime.space = ParticleSystemSimulationSpace.Local;
                // Remove the .radial property access that doesn't exist in older Unity versions
                // velocityOverLifetime.radial = new ParticleSystem.MinMaxCurve(0.1f);

                return effectObject;
            }
            catch (Exception ex)
            {
                instance?.logger?.LogError($"CreateStovepipeParticleEffect failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create smoke effect for weapon malfunction
        /// </summary>
        public static void CreateMalfunctionSmokeEffect(Vector3 position, float intensity = 1.0f)
        {
            try
            {
                if (instance == null) return;

                GameObject smokeEffect = new GameObject("MalfunctionSmoke");
                smokeEffect.transform.position = position;

                var particles = smokeEffect.AddComponent<ParticleSystem>();
                var main = particles.main;
                var emission = particles.emission;
                var shape = particles.shape;

                // Smoke configuration
                main.startColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
                main.startSize = 0.1f * intensity;
                main.startLifetime = 5.0f;
                main.startSpeed = 0.2f;
                main.maxParticles = (int)(30 * intensity);

                emission.rateOverTime = 10 * intensity;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.03f;

                // Auto-destroy
                MonoBehaviour.Destroy(smokeEffect, 6.0f);

                instance.logger?.LogDebug($"Created malfunction smoke effect at {position} with intensity {intensity}");
            }
            catch (Exception ex)
            {
                instance?.logger?.LogError($"CreateMalfunctionSmokeEffect failed: {ex.Message}");
            }
        }
    }
}