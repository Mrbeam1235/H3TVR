using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx.Logging;

namespace H3TVR
{
    public class AirdropManager : MonoBehaviour
    {
        private H3TVRImproved plugin;
        private ManualLogSource logger;

        private const string CrateId = "Crate_Wood_1";
        private const float SpawnHeight = 40f;
        private const float ParachuteSlowdown = 0.2f;

        public void Initialize(H3TVRImproved pluginInstance, ManualLogSource logSource)
        {
            plugin = pluginInstance;
            logger = logSource;
            logger?.LogInfo("Airdrop Manager initialized.");
        }

        public void CallAirdrop(string username)
        {
            logger?.LogInfo($"Airdrop called in by {username}!");
            StartCoroutine(AirdropSequence());
        }

        private IEnumerator AirdropSequence()
        {
            if (GM.CurrentPlayerBody == null)
            {
                logger?.LogError("Cannot start airdrop: Player body not found.");
                yield break;
            }

            Vector3 spawnPos = GM.CurrentPlayerBody.transform.position + Vector3.up * SpawnHeight;
            
            if (!IM.OD.ContainsKey(CrateId))
            {
                logger?.LogError($"Cannot start airdrop: Crate template '{CrateId}' not found.");
                yield break;
            }

            FVRObject crateTemplate = IM.OD[CrateId];
            GameObject crateGO = Instantiate(crateTemplate.GetGameObject(), spawnPos, Quaternion.identity);
            Rigidbody rb = crateGO.GetComponent<Rigidbody>();
            if (rb == null)
            {
                logger?.LogError("Airdrop crate has no Rigidbody!");
                Destroy(crateGO);
                yield break;
            }

            // Attach a "parachute" by slowing its fall
            rb.drag = ParachuteSlowdown;

            // Decide loot type: 70% chance of helpful, 30% chance of troll
            bool isHelpful = UnityEngine.Random.value < 0.7f;
            
            // Wait for the crate to get close to the ground
            yield return new WaitUntil(() => crateGO == null || crateGO.transform.position.y < GM.CurrentPlayerBody.transform.position.y + 2f);

            if (crateGO != null)
            {
                PopulateCrate(crateGO.transform.position, isHelpful);
                
                // Break the crate on impact
                crateGO.SendMessage("Damage", 1000f, SendMessageOptions.DontRequireReceiver);
                logger?.LogInfo("Airdrop has landed!");
            }
        }

        private void PopulateCrate(Vector3 position, bool isHelpful)
        {
            if (isHelpful)
            {
                logger?.LogInfo("Airdrop is... HELPFUL!");
                // Spawn a random item from available ones
                SpawnRandomHelpfulItem(position);

                // Spawn some health
                SpawnItem("Health_Sausage", position + Vector3.up * 0.1f);
            }
            else
            {
                logger?.LogInfo("Airdrop is... a TROLL!");
                // Spawn a live grenade
                SpawnItem("PinnedGrenadeM67", position);
            }
        }

        private void SpawnRandomHelpfulItem(Vector3 position)
        {
            // List of helpful item IDs
            string[] helpfulItems = new string[]
            {
                "MeatBatBaseball",
                "Health_Sausage",
                "SuppressorBottle"
            };

            string itemId = helpfulItems[UnityEngine.Random.Range(0, helpfulItems.Length)];
            SpawnItem(itemId, position);
        }

        private void SpawnItem(string itemId, Vector3 position)
        {
            try
            {
                if (IM.OD.ContainsKey(itemId))
                {
                    FVRObject obj = IM.OD[itemId];
                    Instantiate(obj.GetGameObject(), position, Quaternion.identity);
                }
                else
                {
                    logger?.LogWarning($"Item '{itemId}' not found in ObjectDictionary");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError($"Failed to spawn item '{itemId}': {ex.Message}");
            }
        }
    }
}
