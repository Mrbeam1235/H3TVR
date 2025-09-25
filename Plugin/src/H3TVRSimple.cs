using BepInEx;
using BepInEx.Configuration;
using FistVR;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace H3TVR
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInProcess("h3vr.exe")]
    public class H3TVRSimple : BaseUnityPlugin
    {
        #region Configuration Fields
        private ConfigEntry<float> maxSlomo;
        private ConfigEntry<KeyCode> slomoKey;
        private ConfigEntry<KeyCode> spawnPillowKey;
        private ConfigEntry<KeyCode> spawnWonderToyKey;
        #endregion

        public void Awake()
        {
            // Initialize configuration
            maxSlomo = Config.Bind("Slomo", "MaxSlowmoScale", 0.1f, "Maximum slomo scale");
            slomoKey = Config.Bind("Keys", "SlomoKey", KeyCode.Keypad7, "Key to trigger slomo");
            spawnPillowKey = Config.Bind("Keys", "PillowKey", KeyCode.Keypad1, "Key to spawn pillow");
            spawnWonderToyKey = Config.Bind("Keys", "WonderToyKey", KeyCode.Keypad0, "Key to spawn wonder toy");
            
            Logger.LogInfo("H3TVR Simple Version Loaded!");
        }

        public void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(slomoKey.Value))
            {
                TriggerSlomo();
            }
            
            if (Input.GetKeyDown(spawnPillowKey.Value))
            {
                SpawnObject("BodyPillow", "Pillow");
            }
            
            if (Input.GetKeyDown(spawnWonderToyKey.Value))
            {
                SpawnObject("TippyToyAnton", "WonderToy");
            }
        }

        private void TriggerSlomo()
        {
            try
            {
                Time.timeScale = maxSlomo.Value;
                Logger.LogInfo("Slomo activated!");
                
                // Return to normal after 3 seconds
                Invoke(nameof(ResetTimeScale), 3f);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Slomo failed: {ex.Message}");
            }
        }

        private void ResetTimeScale()
        {
            Time.timeScale = 1f;
            Logger.LogInfo("Time scale reset to normal");
        }

        private void SpawnObject(string itemID, string objectName)
        {
            try
            {
                if (GM.CurrentPlayerBody?.Head == null)
                {
                    Logger.LogWarning("Cannot spawn: Player head reference is null");
                    return;
                }

                if (!IM.OD.ContainsKey(itemID))
                {
                    Logger.LogError($"Item '{itemID}' not found in ObjectDictionary");
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

                Logger.LogInfo($"Successfully spawned {objectName}");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to spawn {objectName}: {ex.Message}");
            }
        }
    }
}