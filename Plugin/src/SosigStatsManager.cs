using System;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using System.IO;

namespace H3TVR
{
    /// <summary>
    /// Statistics tracking system for sosig spawning and combat
    /// </summary>
    public class SosigStatsManager : MonoBehaviour
    {
        [System.Serializable]
        public class SosigStats
        {
            public int totalSpawned = 0;
            public int alliesSpawned = 0;
            public int enemiesSpawned = 0;
            public int bossesSpawned = 0;
            public int sosigKills = 0;
            public int playerDeaths = 0;
            public float totalPlayTime = 0f;
            public Dictionary<string, int> loadoutUsage = new Dictionary<string, int>();
            public Dictionary<string, int> weaponKills = new Dictionary<string, int>();
            public Dictionary<string, float> sosigSurvivalTimes = new Dictionary<string, float>();
            public int longestSurvivalStreak = 0;
            public int currentStreak = 0;
        }

        // Public stats accessible from performance monitor
        private Dictionary<FistVR.Sosig, float> sosigSpawnTimes = new Dictionary<FistVR.Sosig, float>();
        private string statsFilePath = "BepInEx/config/H3TVR_Stats.json";

        public SosigStats Stats { get; private set; } = new SosigStats();

        private void Start()
        {
            LoadStats();
        }

        public void RecordSosigSpawn(FistVR.Sosig sosig, string loadoutName, bool isAlly)
        {
            Stats.totalSpawned++;
            if (isAlly) Stats.alliesSpawned++;
            else Stats.enemiesSpawned++;

            sosigSpawnTimes[sosig] = Time.time;

            if (!Stats.loadoutUsage.ContainsKey(loadoutName))
                Stats.loadoutUsage[loadoutName] = 0;
            Stats.loadoutUsage[loadoutName]++;
        }

        public void RecordSosigDeath(FistVR.Sosig sosig, string causeOfDeath)
        {
            if (sosigSpawnTimes.ContainsKey(sosig))
            {
                float survivalTime = Time.time - sosigSpawnTimes[sosig];
                Stats.sosigSurvivalTimes[sosig.name] = survivalTime;
                sosigSpawnTimes.Remove(sosig);
            }

            Stats.sosigKills++;
            Stats.currentStreak++;
            
            if (Stats.currentStreak > Stats.longestSurvivalStreak)
                Stats.longestSurvivalStreak = Stats.currentStreak;
        }

        public void RecordPlayerDeath()
        {
            Stats.playerDeaths++;
            Stats.currentStreak = 0;
        }

        public void RecordWeaponKill(string weaponName)
        {
            if (!Stats.weaponKills.ContainsKey(weaponName))
                Stats.weaponKills[weaponName] = 0;
            Stats.weaponKills[weaponName]++;
        }

        public void UpdatePlayTime()
        {
            Stats.totalPlayTime += Time.unscaledDeltaTime;
        }

        public SosigStats GetStats()
        {
            return Stats;
        }

        public string GetStatsReport()
        {
            var report = $@"=== H3TVR SOSIG STATISTICS ===
Total Spawned: {Stats.totalSpawned}
Allies: {Stats.alliesSpawned}
Enemies: {Stats.enemiesSpawned}
Bosses: {Stats.bossesSpawned}
Kills: {Stats.sosigKills}
Deaths: {Stats.playerDeaths}
Current Streak: {Stats.currentStreak}
Best Streak: {Stats.longestSurvivalStreak}
Play Time: {Stats.totalPlayTime:F1}s";

            return report;
        }

        private void LoadStats()
        {
            if (File.Exists(statsFilePath))
            {
                try
                {
                    string json = File.ReadAllText(statsFilePath);
                    Stats = JsonUtility.FromJson<SosigStats>(json) ?? new SosigStats();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to load stats: {ex.Message}");
                    Stats = new SosigStats();
                }
            }
        }

        public void SaveStats()
        {
            try
            {
                string json = JsonUtility.ToJson(Stats, true);
                Directory.CreateDirectory(Path.GetDirectoryName(statsFilePath));
                File.WriteAllText(statsFilePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save stats: {ex.Message}");
            }
        }

        void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveStats();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SaveStats();
        }

        void OnDestroy()
        {
            SaveStats();
        }
    }
}