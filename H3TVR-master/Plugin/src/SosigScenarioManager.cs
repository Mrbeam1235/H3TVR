using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Configuration;
using FistVR;
using System;

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
            public string name = string.Empty;
            public float delay;
            public List<string> sosigTypes = new List<string>();
            public int count;
            public Vector3 spawnOffset;
            public bool waitForPreviousWave = true;
        }

        [System.Serializable]
        public class Scenario
        {
            public string scenarioName = string.Empty;
            public string description = string.Empty;
            public List<SpawnWave> waves = new List<SpawnWave>();
            public float totalDuration;
            public bool infiniteMode = false;
            public string victoryCondition = "eliminate_all"; // "eliminate_all", "survive_time", "defend_point"
            public Vector3 objectiveLocation;
        }

        // Configuration
        public static ConfigEntry<KeyCode>? StartScenarioKey;
        public static ConfigEntry<KeyCode>? StopScenarioKey;
        public static ConfigEntry<bool>? EnableScenarios;

        private Dictionary<string, Scenario> scenarios = new Dictionary<string, Scenario>();
        private Coroutine? currentScenario;
        private bool scenarioActive = false;
        private int currentWave = 0;
        private List<Sosig> scenarioSosigs = new List<Sosig>();

        // Reference to the main sosig spawner
        private SosigSpawnerManager? spawnerManager;

        void Start()
        {
            InitializeConfiguration();
            InitializeScenarios();
            
            // Get reference to spawner manager
            spawnerManager = FindObjectOfType<SosigSpawnerManager>();
            if (spawnerManager == null)
            {
                Debug.LogWarning("[SosigScenarioManager] SosigSpawnerManager not found - scenarios may not work properly");
            }
            
            Debug.Log("[SosigScenarioManager] Scenario system initialized");
        }

        void Update()
        {
            if (Input.GetKeyDown(StartScenarioKey?.Value ?? KeyCode.None))
            {
                if (!scenarioActive && scenarios.Count > 0)
                {
                    var scenarioNames = new List<string>(scenarios.Keys);
                    string randomScenario = scenarioNames[UnityEngine.Random.Range(0, scenarioNames.Count)];
                    StartScenario(randomScenario);
                }
            }

            if (Input.GetKeyDown(StopScenarioKey?.Value ?? KeyCode.None))
            {
                StopScenario();
            }
        }

        private void InitializeConfiguration()
        {
            // Find the main plugin to get configuration
            var plugin = FindObjectOfType<H3TVR>();
            if (plugin != null)
            {
                var config = plugin.Config;
                
                StartScenarioKey = config.Bind("Scenario Manager", "StartScenarioKey", KeyCode.F5, "Key to start a random scenario");
                StopScenarioKey = config.Bind("Scenario Manager", "StopScenarioKey", KeyCode.F6, "Key to stop the current scenario");
                EnableScenarios = config.Bind("Scenario Manager", "EnableScenarios", true, "Enable scenario system");
            }
            else
            {
                Debug.LogWarning("[SosigScenarioManager] Could not find main plugin for configuration");
            }
        }

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
            // Parse scenario configurations - implementation could be added later
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
                sosigTypes = new List<string> { "Warlord" },
                count = 1,
                spawnOffset = Vector3.forward * 20f
            };

            var bossWave2 = new SpawnWave
            {
                name = "Shadow Assassin",
                delay = 45f,
                sosigTypes = new List<string> { "Assassin Lord" },
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
wave3_types=Warlord
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

                ExecuteSpawnWave(wave);
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

        private void ExecuteSpawnWave(SpawnWave wave)
        {
            Debug.Log($"Spawning wave: {wave.name}");
            
            if (GM.CurrentPlayerBody == null)
            {
                Debug.LogWarning("Player body not found - cannot spawn wave");
                return;
            }
            
            for (int i = 0; i < wave.count; i++)
            {
                if (wave.sosigTypes.Count > 0)
                {
                    string sosigType = wave.sosigTypes[UnityEngine.Random.Range(0, wave.sosigTypes.Count)];
                    SpawnSosigForScenario(sosigType, wave.spawnOffset, i);
                }
            }
        }

        private void SpawnSosigForScenario(string sosigType, Vector3 baseOffset, int index)
        {
            try
            {
                if (spawnerManager == null)
                {
                    Debug.LogWarning("Spawner manager not available - using fallback spawn");
                    SpawnSosigFallback(sosigType, baseOffset, index);
                    return;
                }

                // Calculate spawn position
                Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
                Vector3 spawnPos = playerPos + baseOffset + (Vector3.right * index * 2f);
                
                // Try to use the loadout-based spawning system
                var availableLoadouts = GetAvailableSosigTypes();
                string loadoutName = FindMatchingLoadout(sosigType, availableLoadouts);
                
                if (!string.IsNullOrEmpty(loadoutName))
                {
                    // Use the advanced spawner system if available
                    spawnerManager.SpawnSosigFromAdvancedLoadout(loadoutName, spawnPos);
                }
                else
                {
                    // Fallback to basic spawning
                    SpawnSosigFallback(sosigType, baseOffset, index);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error spawning sosig for scenario: {ex.Message}");
                SpawnSosigFallback(sosigType, baseOffset, index);
            }
        }

        private void SpawnSosigFallback(string sosigType, Vector3 baseOffset, int index)
        {
            // Basic fallback spawning using a simple prefab approach
            Debug.Log($"Using fallback spawn for {sosigType}");
            
            Vector3 playerPos = GM.CurrentPlayerBody.Head.position;
            Vector3 spawnPos = playerPos + baseOffset + (Vector3.right * index * 2f);
            
            // Look for existing sosigs to clone
            Sosig existingSosig = FindObjectOfType<Sosig>();
            if (existingSosig != null)
            {
                GameObject sosigClone = Instantiate(existingSosig.gameObject, spawnPos, Quaternion.identity);
                Sosig newSosig = sosigClone.GetComponent<Sosig>();
                
                if (newSosig != null)
                {
                    // Configure the sosig as an enemy
                    newSosig.E.IFFCode = 1; // Enemy IFF
                    newSosig.CommandAssaultPoint(GM.CurrentPlayerBody.Head.position);
                    newSosig.FallbackOrder = Sosig.SosigOrder.Assault;
                    
                    scenarioSosigs.Add(newSosig);
                    Debug.Log($"Spawned fallback sosig: {sosigType}");
                }
            }
            else
            {
                Debug.LogWarning($"Cannot spawn {sosigType} - no sosig templates found");
            }
        }

        private List<string> GetAvailableSosigTypes()
        {
            var types = new List<string>();
            
            if (spawnerManager != null)
            {
                try
                {
                    types = spawnerManager.GetAvailableH3VRLoadouts();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Could not get H3VR loadouts: {ex.Message}");
                }
            }
            
            // Add fallback types
            if (types.Count == 0)
            {
                types.AddRange(new string[] { 
                    "Standard Grunt", "Heavy Assault", "Elite Sniper", "Commando", 
                    "Warlord", "Assassin Lord", "Berserker", "Support Medic" 
                });
            }
            
            return types;
        }

        private string FindMatchingLoadout(string sosigType, List<string> availableTypes)
        {
            // Direct match first
            foreach (var type in availableTypes)
            {
                if (type.Equals(sosigType, StringComparison.OrdinalIgnoreCase))
                    return type;
            }
            
            // Partial match
            foreach (var type in availableTypes)
            {
                if (type.IndexOf(sosigType, StringComparison.OrdinalIgnoreCase) >= 0)
                    return type;
            }
            
            // Keyword matching
            var keywords = new Dictionary<string, string[]>
            {
                { "grunt", new[] { "soldier", "infantry", "standard" } },
                { "heavy", new[] { "assault", "armored", "tank" } },
                { "sniper", new[] { "marksman", "rifle", "elite" } },
                { "boss", new[] { "warlord", "commander", "chief" } }
            };
            
            string lowerSosigType = sosigType.ToLower();
            foreach (var kvp in keywords)
            {
                if (lowerSosigType.Contains(kvp.Key))
                {
                    foreach (var keyword in kvp.Value)
                    {
                        foreach (var type in availableTypes)
                        {
                            if (type.ToLower().Contains(keyword))
                                return type;
                        }
                    }
                }
            }
            
            return string.Empty; // No match found
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
            
            // Clean up null sosigs before counting
            scenarioSosigs.RemoveAll(s => s == null);
            
            return $"Wave {currentWave + 1}, {scenarioSosigs.Count} enemies remaining";
        }

        /// <summary>
        /// Register a sosig that was spawned by this scenario system
        /// </summary>
        public void RegisterScenarioSosig(Sosig sosig)
        {
            if (sosig != null && !scenarioSosigs.Contains(sosig))
            {
                scenarioSosigs.Add(sosig);
            }
        }

        /// <summary>
        /// Remove a sosig from scenario tracking (e.g., when it dies)
        /// </summary>
        public void UnregisterScenarioSosig(Sosig sosig)
        {
            scenarioSosigs.Remove(sosig);
        }

        /// <summary>
        /// Get all sosigs currently active in scenarios
        /// </summary>
        public List<Sosig> GetActiveScenarioSosigs()
        {
            scenarioSosigs.RemoveAll(s => s == null);
            return new List<Sosig>(scenarioSosigs);
        }

        /// <summary>
        /// Force complete the current scenario
        /// </summary>
        public void ForceCompleteScenario()
        {
            if (scenarioActive)
            {
                StopScenario();
                Debug.Log("Scenario force completed");
            }
        }

        /// <summary>
        /// Add a custom scenario at runtime
        /// </summary>
        public void AddScenario(Scenario scenario)
        {
            if (scenario != null && !string.IsNullOrEmpty(scenario.scenarioName))
            {
                scenarios[scenario.scenarioName] = scenario;
                Debug.Log($"Added custom scenario: {scenario.scenarioName}");
            }
        }

        /// <summary>
        /// Remove a scenario by name
        /// </summary>
        public void RemoveScenario(string scenarioName)
        {
            if (scenarios.ContainsKey(scenarioName))
            {
                scenarios.Remove(scenarioName);
                Debug.Log($"Removed scenario: {scenarioName}");
            }
        }
    }
}