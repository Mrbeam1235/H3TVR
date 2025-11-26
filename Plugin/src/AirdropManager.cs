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
            
            FVRObject crateTemplate = IM.Instance.GetFVRObject(CrateId);
            if (crateTemplate == null)
            {
                logger?.LogError($"Cannot start airdrop: Crate template '{CrateId}' not found.");
                yield break;
            }

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
                // Spawn a random high-tier gun
                var gun = IM.Instance.GetRandomItem(ItemManager.ItemCategory.Firearm, ItemManager.SubCategory.Firearm_Rifle);
                IM.Instance.CreateObject(gun.ItemID, position, Quaternion.identity);

                // Spawn some health
                IM.Instance.CreateObject("Health_Sausage", position + Vector3.up * 0.1f, Quaternion.identity);
            }
            else
            {
                logger?.LogInfo("Airdrop is... a TROLL!");
                // Spawn a live grenade
                IM.Instance.CreateObject("Grenade_Frag_M67_Live", position, Quaternion.identity);
            }
        }
    }
}
