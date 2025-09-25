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

        public void Initialize(H3TVRImproved pluginInstance, SlomoMovementController controller, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            slomoController = controller;
            logger = logSource;
        }

        #region Slomo Effects
        public void SlomoScaleDown()
        {
            float maxSlomoValue, waitTime, scaleSpeed, returnSpeed;
            plugin.GetSlomoConfig(out maxSlomoValue, out waitTime, out scaleSpeed, out returnSpeed);
            
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

        public void SlomoReturn()
        {
            float maxSlomoValue, waitTime, scaleSpeed, returnSpeed;
            plugin.GetSlomoConfig(out maxSlomoValue, out waitTime, out scaleSpeed, out returnSpeed);
            
            if (Time.timeScale != 1)
            {
                Time.timeScale += returnSpeed * Time.unscaledDeltaTime;
                Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
                
                slomoController?.UpdateMovementScale(Time.timeScale);
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
    }
}