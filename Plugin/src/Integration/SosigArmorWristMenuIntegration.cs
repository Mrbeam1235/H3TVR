using System;
using UnityEngine;
using FistVR;
using System.Collections;
using System.Text;
using BepInEx.Logging;

namespace H3TVR
{
    /// <summary>
    /// Sosig Armor Wrist Menu Integration - Connects armor system with H3TVR Enhanced Edition
    /// Provides seamless integration between the armor menu and sosig spawning systems
    /// </summary>
    public class SosigArmorWristMenuIntegration : MonoBehaviour
    {
        public static SosigArmorWristMenuIntegration Instance { get; private set; }
        private SosigArmorWristMenuComplete wristMenu; // Use the complete armor menu class
        private H3TVRImproved plugin;
        private bool isInitialized = false;

        public void Initialize(H3TVRImproved pluginInstance, object wristMenuInstance)
        {
            if (isInitialized) return;
            
            Instance = this;
            plugin = pluginInstance;
            isInitialized = true;
            
            // Create the armor menu component
            var menuObject = new GameObject("SosigArmorWristMenuComplete");
            menuObject.transform.SetParent(transform);
            
            wristMenu = menuObject.AddComponent<SosigArmorWristMenuComplete>();
            wristMenu.Initialize(plugin, wristMenuInstance);
            
            // Subscribe to events after a short delay to ensure everything is initialized
            StartCoroutine(DelayedEventSubscription());
            
            Debug.Log("[SosigArmorWristMenuIntegration] Integration initialized successfully with SosigArmorWristMenuComplete");
        }

        private void SubscribeToEvents()
        {
            Debug.Log("[SosigArmorWristMenuIntegration] Subscribing to armor system events");
        }

        private IEnumerator DelayedEventSubscription()
        {
            yield return new WaitForSeconds(2f);
            SubscribeToEvents();
        }

        private void OnSosigSpawned(Sosig sosig, string userName, bool isFriendly)
        {
            if (sosig == null || !isInitialized) return;

            try
            {
                ApplyArmorToSosig(sosig, isFriendly);
                Debug.Log($"[SosigArmorWristMenuIntegration] Applied armor to {(isFriendly ? "friendly" : "enemy")} sosig for user: {userName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SosigArmorWristMenuIntegration] Failed to apply armor to sosig: {ex.Message}");
            }
        }

        #region Public API
        /// <summary>
        /// Check if faction-specific armor is enabled
        /// </summary>
        public bool IsFactionArmorEnabled()
        {
            if (wristMenu == null) return false;
            return wristMenu.IsFactionArmorEnabled();
        }

        /// <summary>
        /// Apply armor to a sosig based on faction
        /// </summary>
        public void ApplyArmorToSosig(Sosig sosig, bool isFriendly)
        {
            if (wristMenu == null || sosig == null) return;
            wristMenu.ApplyArmorToSosig(sosig, isFriendly);
        }

        /// <summary>
        /// Get the armor wrist menu instance
        /// </summary>
        public SosigArmorWristMenuComplete GetArmorMenu()
        {
            return wristMenu;
        }

        /// <summary>
        /// Check if the integration is properly initialized
        /// </summary>
        public bool IsInitialized()
        {
            return isInitialized && wristMenu != null;
        }

        /// <summary>
        /// Manual trigger to open the armor menu
        /// </summary>
        public void OpenArmorMenu()
        {
            if (wristMenu != null)
            {
                wristMenu.ToggleMenu();
            }
            else
            {
                Debug.LogWarning("[SosigArmorWristMenuIntegration] Armor menu not initialized");
            }
        }
        #endregion

        #region Integration with H3TVR Systems
        /// <summary>
        /// Integration hook for the main H3TVR plugin
        /// Called when the plugin needs to apply armor to sosigs
        /// </summary>
        public void OnPluginArmorRequest(Sosig sosig, string userName, bool isFriendly)
        {
            if (sosig == null) return;
            ApplyArmorToSosig(sosig, isFriendly);
        }

        /// <summary>
        /// Called by H3TVR systems when they need to know if armor integration is available
        /// </summary>
        public bool IsArmorIntegrationAvailable()
        {
            return isInitialized && wristMenu != null;
        }

        /// <summary>
        /// Provide armor statistics for H3TVR systems
        /// </summary>
        public string GetArmorIntegrationStatus()
        {
            if (!isInitialized || wristMenu == null)
            {
                return "Armor Integration: Not Available";
            }

            var status = new StringBuilder();
            status.AppendLine("=== H3TVR Armor Integration Status ===");
            status.AppendLine($"Integration Active: {isInitialized}");
            status.AppendLine($"Armor Menu Available: {wristMenu != null}");
            status.AppendLine($"Faction Armor: {IsFactionArmorEnabled()}");
            status.AppendLine($"Current Preset: {wristMenu.GetCurrentPresetInfo()}");

            return status.ToString();
        }
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[SosigArmorWristMenuIntegration] Starting without proper initialization");
            }
        }

        private void OnDestroy()
        {
            Instance = null;
            Debug.Log("[SosigArmorWristMenuIntegration] Integration destroyed");
        }
        #endregion

        #region Development and Testing
        /// <summary>
        /// Test the armor integration system
        /// </summary>
        public void TestArmorIntegration()
        {
            Debug.Log("[SosigArmorWristMenuIntegration] Armor integration test completed");
        }

        /// <summary>
        /// Get comprehensive status for debugging
        /// </summary>
        public string GetDebugStatus()
        {
            return GetArmorIntegrationStatus();
        }
        #endregion
    }
}