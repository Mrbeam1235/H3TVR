using System.Collections.Generic;
using UnityEngine;
using System;

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
            public DateTime sessionStart;
        }

        private SosigStats currentStats = new SosigStats();
        private Dictionary<Sosig, float> sosigSpawnTimes = new Dictionary<Sosig, float>();
        private string statsFilePath = "BepInEx/config/H3TVR_Stats.json";

        void Start()
        {
            LoadStats();
            currentStats.sessionStart = DateTime.Now;
        }

        void Update()
        {
            currentStats.totalPlayTime += Time.deltaTime;
            
            // Clean up destroyed sosigs from tracking
            var keysToRemove = new List<Sosig>();
            foreach (var kvp in sosigSpawnTimes)
            {
                if (kvp.Key == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                sosigSpawnTimes.Remove(key);
            }
        }

        public void RecordSosigSpawn(Sosig sosig, string loadoutName, bool isAlly, bool isBoss)
        {
            currentStats.totalSpawned++;
            
            if (isAlly)
                currentStats.alliesSpawned++;
            else
                currentStats.enemiesSpawned++;
                
            if (isBoss)
                currentStats.bossesSpawned++;

            // Track loadout usage
            if (!currentStats.loadoutUsage.ContainsKey(loadoutName))
                currentStats.loadoutUsage[loadoutName] = 0;
            currentStats.loadoutUsage[loadoutName]++;

            // Track spawn time for survival calculation
            sosigSpawnTimes[sosig] = Time.time;
        }

        public void RecordSosigDeath(Sosig sosig, string causeOfDeath = "unknown")
        {
            if (sosigSpawnTimes.ContainsKey(sosig))
            {
                float survivalTime = Time.time - sosigSpawnTimes[sosig];
                string sosigType = sosig.name.Replace("(Clone)", "").Trim();
                
                if (!currentStats.sosigSurvivalTimes.ContainsKey(sosigType))
                    currentStats.sosigSurvivalTimes[sosigType] = 0f;
                    
                currentStats.sosigSurvivalTimes[sosigType] = 
                    (currentStats.sosigSurvivalTimes[sosigType] + survivalTime) / 2f; // Running average

                sosigSpawnTimes.Remove(sosig);
            }

            currentStats.sosigKills++;
            currentStats.currentStreak++;
            
            if (currentStats.currentStreak > currentStats.longestSurvivalStreak)
                currentStats.longestSurvivalStreak = currentStats.currentStreak;

            // Track weapon kills if available
            if (!string.IsNullOrEmpty(causeOfDeath))
            {
                if (!currentStats.weaponKills.ContainsKey(causeOfDeath))
                    currentStats.weaponKills[causeOfDeath] = 0;
                currentStats.weaponKills[causeOfDeath]++;
            }
        }

        public void RecordPlayerDeath()
        {
            currentStats.playerDeaths++;
            currentStats.currentStreak = 0; // Reset kill streak
        }

        public void SaveStats()
        {
            try
            {
                string json = JsonUtility.ToJson(currentStats, true);
                System.IO.File.WriteAllText(statsFilePath, json);
                Debug.Log("Stats saved successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save stats: {e.Message}");
            }
        }

        public void LoadStats()
        {
            try
            {
                if (System.IO.File.Exists(statsFilePath))
                {
                    string json = System.IO.File.ReadAllText(statsFilePath);
                    currentStats = JsonUtility.FromJson<SosigStats>(json);
                    Debug.Log("Stats loaded successfully");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load stats: {e.Message}");
                currentStats = new SosigStats(); // Reset to default
            }
        }

        public void ResetStats()
        {
            currentStats = new SosigStats();
            currentStats.sessionStart = DateTime.Now;
            sosigSpawnTimes.Clear();
            SaveStats();
            Debug.Log("Statistics reset");
        }

        public SosigStats GetCurrentStats()
        {
            return currentStats;
        }

        public string GetStatsReport()
        {
            var report = $@"=== SOSIG SPAWNER STATISTICS ===
Session Started: {currentStats.sessionStart:yyyy-MM-dd HH:mm:ss}
Play Time: {TimeSpan.FromSeconds(currentStats.totalPlayTime):hh\:mm\:ss}

SPAWNING STATS:
Total Spawned: {currentStats.totalSpawned}
- Allies: {currentStats.alliesSpawned}
- Enemies: {currentStats.enemiesSpawned}  
- Bosses: {currentStats.bossesSpawned}

COMBAT STATS:
Sosig Kills: {currentStats.sosigKills}
Player Deaths: {currentStats.playerDeaths}
Current Streak: {currentStats.currentStreak}
Best Streak: {currentStats.longestSurvivalStreak}
K/D Ratio: {(currentStats.playerDeaths > 0 ? (float)currentStats.sosigKills / currentStats.playerDeaths : currentStats.sosigKills):F2}

TOP LOADOUTS:";

            // Add top 5 most used loadouts
            var sortedLoadouts = new List<KeyValuePair<string, int>>(currentStats.loadoutUsage);
            sortedLoadouts.Sort((x, y) => y.Value.CompareTo(x.Value));
            
            for (int i = 0; i < Math.Min(5, sortedLoadouts.Count); i++)
            {
                report += $"\n{i + 1}. {sortedLoadouts[i].Key}: {sortedLoadouts[i].Value} spawns";
            }

            return report;
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