using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using BepInEx;
using System.IO;
using System.Text;

namespace H3TVR
{
    /// <summary>
    /// Monitors performance and system health for the sosig spawner system
    /// </summary>
    public class SosigPerformanceMonitor : MonoBehaviour
    {
        [Header("Performance Monitoring")]
        public bool enablePerformanceMonitoring = true;
        public bool enableFrameRateTracking = true;
        public bool enableMemoryTracking = true;
        public bool enableSpawnTimeTracking = true;
        public float updateInterval = 1.0f;
        
        [Header("Performance Thresholds")]
        public float lowFPSThreshold = 30.0f;
        public float highMemoryThreshold = 512.0f; // MB
        public float maxSpawnTime = 0.1f; // seconds
        public int maxActiveSosigs = 50;
        
        // Performance tracking
        private List<float> frameRates = new List<float>();
        private List<float> memoryUsages = new List<float>();
        private List<float> spawnTimes = new List<float>();
        private Dictionary<string, float> componentPerformance = new Dictionary<string, float>();
        
        // Timing
        private float lastUpdateTime;
        private Stopwatch performanceStopwatch;
        
        // System references
        private SosigSpawnerManager spawnerManager;
        private SosigStatsManager statsManager;
        
        // Performance alerts
        private Queue<string> performanceAlerts = new Queue<string>();
        private float lastAlertTime;
        private float alertCooldown = 5.0f;
        
        void Start()
        {
            InitializePerformanceMonitor();
        }
        
        void Update()
        {
            if (!enablePerformanceMonitoring)
                return;
                
            if (Time.time - lastUpdateTime >= updateInterval)
            {
                UpdatePerformanceMetrics();
                CheckPerformanceThresholds();
                lastUpdateTime = Time.time;
            }
        }
        
        private void InitializePerformanceMonitor()
        {
            performanceStopwatch = new Stopwatch();
            spawnerManager = FindObjectOfType<SosigSpawnerManager>();
            statsManager = FindObjectOfType<SosigStatsManager>();
            
            // Initialize tracking lists with capacity
            frameRates.Capacity = 60; // Store last 60 samples
            memoryUsages.Capacity = 60;
            spawnTimes.Capacity = 100;
            
            UnityEngine.Debug.Log("[SosigPerformanceMonitor] Performance monitoring initialized");
        }
        
        private void UpdatePerformanceMetrics()
        {
            // Track frame rate
            if (enableFrameRateTracking)
            {
                float currentFPS = 1.0f / Time.deltaTime;
                AddSample(frameRates, currentFPS);
            }
            
            // Track memory usage
            if (enableMemoryTracking)
            {
                float memoryMB = (float)GC.GetTotalMemory(false) / (1024 * 1024);
                AddSample(memoryUsages, memoryMB);
            }
            
            // Track component performance
            UpdateComponentPerformance();
        }
        
        private void UpdateComponentPerformance()
        {
            performanceStopwatch.Restart();
            
            // Measure spawner manager performance
            if (spawnerManager != null)
            {
                var sosigCount = GameObject.FindObjectsOfType<FistVR.Sosig>().Length;
                componentPerformance["ActiveSosigs"] = sosigCount;
            }
            
            performanceStopwatch.Stop();
            componentPerformance["MonitoringOverhead"] = (float)performanceStopwatch.Elapsed.TotalMilliseconds;
        }
        
        private void CheckPerformanceThresholds()
        {
            // Check FPS threshold
            if (enableFrameRateTracking && frameRates.Count > 0)
            {
                float avgFPS = GetAverageFromList(frameRates);
                if (avgFPS < lowFPSThreshold)
                {
                    AddPerformanceAlert($"Low FPS detected: {avgFPS:F1} FPS (threshold: {lowFPSThreshold})");
                }
            }
            
            // Check memory threshold
            if (enableMemoryTracking && memoryUsages.Count > 0)
            {
                float currentMemory = memoryUsages[memoryUsages.Count - 1];
                if (currentMemory > highMemoryThreshold)
                {
                    AddPerformanceAlert($"High memory usage: {currentMemory:F1}MB (threshold: {highMemoryThreshold}MB)");
                }
            }
            
            // Check sosig count
            if (componentPerformance.ContainsKey("ActiveSosigs"))
            {
                int sosigCount = (int)componentPerformance["ActiveSosigs"];
                if (sosigCount > maxActiveSosigs)
                {
                    AddPerformanceAlert($"Too many active sosigs: {sosigCount} (max: {maxActiveSosigs})");
                }
            }
        }
        
        public void RecordSpawnTime(float spawnTime)
        {
            if (!enableSpawnTimeTracking)
                return;
                
            AddSample(spawnTimes, spawnTime);
            
            if (spawnTime > maxSpawnTime)
            {
                AddPerformanceAlert($"Slow spawn detected: {spawnTime:F3}s (max: {maxSpawnTime:F3}s)");
            }
        }
        
        private void AddSample(List<float> list, float value)
        {
            list.Add(value);
            if (list.Count > list.Capacity)
            {
                list.RemoveAt(0);
            }
        }
        
        private float GetAverageFromList(List<float> list)
        {
            if (list.Count == 0) return 0f;
            
            float sum = 0f;
            foreach (float value in list)
            {
                sum += value;
            }
            return sum / list.Count;
        }
        
        private void AddPerformanceAlert(string alert)
        {
            if (Time.time - lastAlertTime < alertCooldown)
                return;
                
            performanceAlerts.Enqueue($"[{DateTime.Now:HH:mm:ss}] {alert}");
            UnityEngine.Debug.LogWarning($"[SosigPerformanceMonitor] {alert}");
            
            // Keep only last 10 alerts
            while (performanceAlerts.Count > 10)
            {
                performanceAlerts.Dequeue();
            }
            
            lastAlertTime = Time.time;
        }
        
        public string GetPerformanceReport()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine("=== SOSIG SPAWNER PERFORMANCE REPORT ===");
            report.AppendLine($"Monitoring Active: {enablePerformanceMonitoring}");
            report.AppendLine($"Update Interval: {updateInterval}s");
            report.AppendLine();
            
            // Frame rate statistics
            if (enableFrameRateTracking && frameRates.Count > 0)
            {
                report.AppendLine("FRAME RATE:");
                report.AppendLine($"  Current: {(frameRates.Count > 0 ? frameRates[frameRates.Count - 1] : 0):F1} FPS");
                report.AppendLine($"  Average: {GetAverageFromList(frameRates):F1} FPS");
                report.AppendLine($"  Threshold: {lowFPSThreshold} FPS");
                report.AppendLine();
            }
            
            // Memory statistics
            if (enableMemoryTracking && memoryUsages.Count > 0)
            {
                report.AppendLine("MEMORY USAGE:");
                report.AppendLine($"  Current: {(memoryUsages.Count > 0 ? memoryUsages[memoryUsages.Count - 1] : 0):F1} MB");
                report.AppendLine($"  Average: {GetAverageFromList(memoryUsages):F1} MB");
                report.AppendLine($"  Threshold: {highMemoryThreshold} MB");
                report.AppendLine();
            }
            
            // Spawn time statistics
            if (enableSpawnTimeTracking && spawnTimes.Count > 0)
            {
                report.AppendLine("SPAWN PERFORMANCE:");
                report.AppendLine($"  Average Spawn Time: {GetAverageFromList(spawnTimes):F3}s");
                report.AppendLine($"  Max Spawn Time: {maxSpawnTime:F3}s");
                report.AppendLine($"  Total Spawns Tracked: {spawnTimes.Count}");
                report.AppendLine();
            }
            
            // Component performance
            if (componentPerformance.Count > 0)
            {
                report.AppendLine("COMPONENT PERFORMANCE:");
                foreach (var kvp in componentPerformance)
                {
                    report.AppendLine($"  {kvp.Key}: {kvp.Value:F2}");
                }
                report.AppendLine();
            }
            
            // Recent alerts
            if (performanceAlerts.Count > 0)
            {
                report.AppendLine("RECENT PERFORMANCE ALERTS:");
                foreach (string alert in performanceAlerts)
                {
                    report.AppendLine($"  {alert}");
                }
                report.AppendLine();
            }
            
            // Recommendations
            report.AppendLine("PERFORMANCE RECOMMENDATIONS:");
            if (frameRates.Count > 0 && GetAverageFromList(frameRates) < lowFPSThreshold)
            {
                report.AppendLine("  • Consider reducing max sosig count");
                report.AppendLine("  • Disable visual effects on spawned sosigs");
                report.AppendLine("  • Increase spawn delay between waves");
            }
            
            if (memoryUsages.Count > 0 && GetAverageFromList(memoryUsages) > highMemoryThreshold * 0.8f)
            {
                report.AppendLine("  • Enable automatic sosig cleanup");
                report.AppendLine("  • Reduce sosig lifetime");
                report.AppendLine("  • Consider garbage collection optimization");
            }
            
            if (spawnTimes.Count > 0 && GetAverageFromList(spawnTimes) > maxSpawnTime * 0.8f)
            {
                report.AppendLine("  • Optimize sosig configuration loading");
                report.AppendLine("  • Pre-cache weapon loadouts");
                report.AppendLine("  • Reduce attachment complexity");
            }
            
            return report.ToString();
        }
        
        public void SavePerformanceReport()
        {
            try
            {
                string reportPath = Path.Combine(Paths.ConfigPath, "H3TVR_PerformanceReport.txt");
                File.WriteAllText(reportPath, GetPerformanceReport());
                UnityEngine.Debug.Log($"[SosigPerformanceMonitor] Performance report saved to: {reportPath}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"[SosigPerformanceMonitor] Failed to save performance report: {ex.Message}");
            }
        }
        
        public void OptimizePerformance()
        {
            UnityEngine.Debug.Log("[SosigPerformanceMonitor] Running performance optimization...");
            
            // Force garbage collection
            GC.Collect();
            
            // Clean up old sosigs if too many are active
            if (componentPerformance.ContainsKey("ActiveSosigs") && 
                componentPerformance["ActiveSosigs"] > maxActiveSosigs)
            {
                CleanupOldSosigs();
            }
            
            // Clear old performance samples
            if (frameRates.Count > 30) frameRates.RemoveRange(0, frameRates.Count - 30);
            if (memoryUsages.Count > 30) memoryUsages.RemoveRange(0, memoryUsages.Count - 30);
            if (spawnTimes.Count > 50) spawnTimes.RemoveRange(0, spawnTimes.Count - 50);
            
            AddPerformanceAlert("Performance optimization completed");
        }
        
        private void CleanupOldSosigs()
        {
            var sosigs = GameObject.FindObjectsOfType<FistVR.Sosig>();
            int cleanupCount = sosigs.Length - maxActiveSosigs;
            
            if (cleanupCount > 0)
            {
                // Remove oldest sosigs (this is a simplified approach)
                for (int i = 0; i < cleanupCount && i < sosigs.Length; i++)
                {
                    if (sosigs[i] != null)
                    {
                        Destroy(sosigs[i].gameObject);
                    }
                }
                
                UnityEngine.Debug.Log($"[SosigPerformanceMonitor] Cleaned up {cleanupCount} sosigs for performance");
            }
        }
        
        // GUI for performance display
        void OnGUI()
        {
            if (!enablePerformanceMonitoring)
                return;
                
            // Simple performance overlay in top-right corner
            GUILayout.BeginArea(new Rect(Screen.width - 200, 10, 190, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("Performance");
            
            if (enableFrameRateTracking && frameRates.Count > 0)
            {
                float currentFPS = frameRates[frameRates.Count - 1];
                GUILayout.Label($"FPS: {currentFPS:F1}");
            }
            
            if (enableMemoryTracking && memoryUsages.Count > 0)
            {
                float currentMemory = memoryUsages[memoryUsages.Count - 1];
                GUILayout.Label($"Memory: {currentMemory:F1}MB");
            }
            
            if (componentPerformance.ContainsKey("ActiveSosigs"))
            {
                GUILayout.Label($"Sosigs: {componentPerformance["ActiveSosigs"]:F0}");
            }
            
            if (GUILayout.Button("Optimize"))
            {
                OptimizePerformance();
            }
            
            if (GUILayout.Button("Save Report"))
            {
                SavePerformanceReport();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}