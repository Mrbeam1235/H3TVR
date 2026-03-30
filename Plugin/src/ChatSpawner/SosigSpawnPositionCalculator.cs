using System;
using UnityEngine;
using FistVR;

namespace H3TVR
{
    /// <summary>
    /// Calculates spawn positions for sosigs
    /// </summary>
    public class SosigSpawnPositionCalculator
    {
        /// <summary>
        /// Calculate ally spawn point near the player
        /// Returns Vector3.zero if player isn't available (caller should check!)
        /// </summary>
        public Vector3 CalculateAllySpawnPoint()
        {
            if (GM.CurrentPlayerBody == null || GM.CurrentPlayerBody.Head == null || GM.CurrentPlayerBody.Head.transform == null)
            {
                Debug.LogWarning("[H3TVR] CalculateAllySpawnPoint: Player not ready");
                return Vector3.zero;
            }

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(2f, 4f);

            Vector3 spawnPos = new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );

            Debug.Log($"[H3TVR] Ally spawn position calculated: {spawnPos}");
            return spawnPos;
        }

        /// <summary>
        /// Calculate enemy spawn point further from the player
        /// Returns Vector3.zero if player isn't available (caller should check!)
        /// </summary>
        public Vector3 CalculateEnemySpawnPoint()
        {
            if (GM.CurrentPlayerBody == null || GM.CurrentPlayerBody.Head == null || GM.CurrentPlayerBody.Head.transform == null)
            {
                Debug.LogWarning("[H3TVR] CalculateEnemySpawnPoint: Player not ready");
                return Vector3.zero;
            }

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(8f, 15f);

            Vector3 spawnPos = new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );

            Debug.Log($"[H3TVR] Enemy spawn position calculated: {spawnPos}");
            return spawnPos;
        }

        /// <summary>
        /// Calculate boss spawn point far from the player
        /// Returns Vector3.zero if player isn't available (caller should check!)
        /// </summary>
        public Vector3 CalculateBossSpawnPoint()
        {
            if (GM.CurrentPlayerBody == null || GM.CurrentPlayerBody.Head == null || GM.CurrentPlayerBody.Head.transform == null)
            {
                Debug.LogWarning("[H3TVR] CalculateBossSpawnPoint: Player not ready");
                return Vector3.zero;
            }

            var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float distance = UnityEngine.Random.Range(20f, 30f);

            Vector3 spawnPos = new Vector3(
                playerPos.x + Mathf.Cos(angle) * distance,
                playerPos.y,
                playerPos.z + Mathf.Sin(angle) * distance
            );

            Debug.Log($"[H3TVR] Boss spawn position calculated: {spawnPos}");
            return spawnPos;
        }

        /// <summary>
        /// Check if player is ready for spawning
        /// </summary>
        public bool IsPlayerReady()
        {
            return GM.CurrentPlayerBody != null && 
                   GM.CurrentPlayerBody.Head != null && 
                   GM.CurrentPlayerBody.Head.transform != null;
        }
    }
}
