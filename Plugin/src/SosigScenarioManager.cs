using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;

namespace H3TVR
{
    /// <summary>
    /// Scenario system for creating complex spawn sequences and events
    /// </summary>
    public class SosigScenarioManager : MonoBehaviour
    {
        [System.Serializable]
        public class SpawnWave
        {
            public string name;
            public float delay;
            public List<string> sosigTypes = new List<string>();
            public int count;
            public Vector3 spawnOffset;
            public bool waitForPreviousWave = true;
        }

        [System.Serializable]
        public class Scenario
        {
            public string scenarioName;
            public string description;
            public List<SpawnWave> waves = new List<SpawnWave>();
            public float totalDuration;
            public bool infiniteMode = false;
            public string victoryCondition; // "eliminate_all", "survive_time", "defend_point"
            public Vector3 objectiveLocation;
        }

        // Configuration
        public static ConfigEntry<KeyCode> StartScenarioKey;
        public static ConfigEntry<KeyCode> StopScenarioKey;
        public static ConfigEntry<bool> EnableScenarios;

        private Dictionary<string, Scenario> scenarios = new Dictionary<string, Scenario>();
        private Coroutine currentScenario;
        private bool scenarioActive = false;
        private int currentWave = 0;
        private List<Sosig> scenarioSosigs = new List<Sosig>();

        public void InitializeScenarios()
        {
            LoadScenarioConfigurations();
            CreateDefaultScenarios();
        }

        private void LoadScenarioConfigurations()
        {
            string scenarioPath = "BepInEx/config/H3TVR_Scenarios.ini";
            if (!System.IO.File.Exists(scenarioPath))
            {
                CreateDefaultScenarioINI(scenarioPath);
            }
            // Parse scenario configurations
        }

        private void CreateDefaultScenarios()
        {
            // Survival Scenario
            var survival = new Scenario
            {
                scenarioName = "Survival Mode",
                description = "Endless waves of enemies with increasing difficulty",
                infiniteMode = true,
                victoryCondition = "survive_time"
            };

            var wave1 = new SpawnWave
            {
                name = "Initial Contact",
                delay = 5f,
                sosigTypes = new List<string> { "Standard Grunt", "Standard Grunt" },
                count = 2,
                spawnOffset = Vector3.forward * 10f
            };

            var wave2 = new SpawnWave
            {
                name = "Reinforcements",
                delay = 30f,
                sosigTypes = new List<string> { "Standard Grunt", "Heavy Assault" },
                count = 3,
                spawnOffset = Vector3.forward * 15f
            };

            survival.waves.Add(wave1);
            survival.waves.Add(wave2);
            scenarios["Survival Mode"] = survival;

            // Boss Rush Scenario
            var bossRush = new Scenario
            {
                scenarioName = "Boss Rush",
                description = "Face multiple bosses in sequence",
                victoryCondition = "eliminate_all"
            };

            var bossWave1 = new SpawnWave
            {
                name = "Warlord Supreme",
                delay = 10f,
                sosigTypes = new List<string> { "Warlord Supreme" },
                count = 1,
                spawnOffset = Vector3.forward * 20f
            };

            var bossWave2 = new SpawnWave
            {
                name = "Shadow Assassin",
                delay = 45f,
                sosigTypes = new List<string> { "Shadow Assassin" },
                count = 1,
                spawnOffset = Vector3.forward * 20f,
                waitForPreviousWave = true
            };

            bossRush.waves.Add(bossWave1);
            bossRush.waves.Add(bossWave2);
            scenarios["Boss Rush"] = bossRush;
        }

        private void CreateDefaultScenarioINI(string path)
        {
            string defaultContent = @"# H3TVR Scenario Configuration
# Define complex spawn sequences and game modes

[Zombie Horde]
description=Endless zombie-like enemies in waves
infinite_mode=true
victory_condition=survive_time
wave1_delay=5.0
wave1_types=Berserker,Berserker,Standard Grunt
wave1_count=3
wave2_delay=30.0
wave2_types=Berserker,Heavy Assault,Standard Grunt,Standard Grunt
wave2_count=4

[Military Assault]
description=Coordinated military attack with multiple phases
infinite_mode=false
victory_condition=eliminate_all
wave1_delay=10.0
wave1_types=Standard Grunt,Standard Grunt,Elite Sniper
wave1_count=3
wave2_delay=60.0
wave2_types=Heavy Assault,Commando,Standard Grunt
wave2_count=3
wave3_delay=120.0
wave3_types=Warlord Supreme
wave3_count=1";

            System.IO.File.WriteAllText(path, defaultContent);
        }

        public void StartScenario(string scenarioName)
        {
            if (scenarios.ContainsKey(scenarioName) && !scenarioActive)
            {
                currentScenario = StartCoroutine(RunScenario(scenarios[scenarioName]));
                scenarioActive = true;
                Debug.Log($"Started scenario: {scenarioName}");
            }
        }

        public void StopScenario()
        {
            if (currentScenario != null)
            {
                StopCoroutine(currentScenario);
                scenarioActive = false;
                ClearScenarioSosigs();
                Debug.Log("Scenario stopped");
            }
        }

        private IEnumerator RunScenario(Scenario scenario)
        {
            currentWave = 0;
            
            foreach (var wave in scenario.waves)
            {
                yield return new WaitForSeconds(wave.delay);
                
                if (wave.waitForPreviousWave)
                {
                    // Wait for previous wave to be eliminated
                    while (scenarioSosigs.Count > 0)
                    {
                        scenarioSosigs.RemoveAll(s => s == null);
                        yield return new WaitForSeconds(1f);
                    }
                }

                SpawnWave(wave);
                currentWave++;
            }

            if (scenario.infiniteMode)
            {
                // Restart with increased difficulty
                StartCoroutine(RunScenario(scenario));
            }
            else
            {
                scenarioActive = false;
                Debug.Log($"Scenario '{scenario.scenarioName}' completed!");
            }
        }

        private void SpawnWave(SpawnWave wave)
        {
            Debug.Log($"Spawning wave: {wave.name}");
            
            for (int i = 0; i < wave.count; i++)
            {
                if (wave.sosigTypes.Count > 0)
                {
                    string sosigType = wave.sosigTypes[Random.Range(0, wave.sosigTypes.Count)];
                    // Spawn sosig using the main spawner system
                    // This would integrate with SosigSpawnerManager
                }
            }
        }

        private void ClearScenarioSosigs()
        {
            foreach (var sosig in scenarioSosigs)
            {
                if (sosig != null)
                {
                    Destroy(sosig.gameObject);
                }
            }
            scenarioSosigs.Clear();
        }

        public List<string> GetAvailableScenarios()
        {
            return new List<string>(scenarios.Keys);
        }

        public bool IsScenarioActive()
        {
            return scenarioActive;
        }

        public string GetCurrentScenarioStatus()
        {
            if (!scenarioActive) return "No active scenario";
            return $"Wave {currentWave + 1}, {scenarioSosigs.Count} enemies remaining";
        }
    }
}