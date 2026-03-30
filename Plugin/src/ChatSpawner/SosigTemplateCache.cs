using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using FistVR;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Template cache for Update 120 Experimental 14 sosig spawn system
    /// Uses IM.Instance.odicSosigObjsByID for modern sosig template access
    /// </summary>
    public class SosigTemplateCache
    {
        private Dictionary<SosigEnemyID, SosigEnemyTemplate> templateCache = new Dictionary<SosigEnemyID, SosigEnemyTemplate>();
        private ManualLogSource logger;

        public void Initialize(ManualLogSource logSource)
        {
            logger = logSource;
        }

        public void BuildCache(List<SosigEnemyID> allyPoolIDs, List<SosigEnemyID> enemyPoolIDs)
        {
            try
            {
                if (IM.Instance == null)
                {
                    logger?.LogWarning("Cannot build template cache - IM.Instance is null");
                    return;
                }

                if (IM.Instance.odicSosigObjsByID == null)
                {
                    logger?.LogWarning("Cannot build template cache - odicSosigObjsByID is null");
                    return;
                }

                int cacheCount = 0;
                logger?.LogInfo("Building template cache from IM.Instance...");

                foreach (var id in allyPoolIDs.Concat(enemyPoolIDs).Distinct())
                {
                    if (IM.Instance.odicSosigObjsByID.ContainsKey(id))
                    {
                        var template = IM.Instance.odicSosigObjsByID[id];
                        if (template != null)
                        {
                            templateCache[id] = template;
                            cacheCount++;
                            logger?.LogDebug($"  Cached: {id}");
                        }
                        else
                        {
                            logger?.LogWarning($"  Template null for {id}");
                        }
                    }
                    else
                    {
                        logger?.LogWarning($"  ID not found in IM: {id}");
                    }
                }

                logger?.LogInfo($"Template cache built: {cacheCount}/{allyPoolIDs.Count + enemyPoolIDs.Count} templates loaded");
                logger?.LogInfo($"Template cache status: {templateCache.Count} total templates");
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Failed to build template cache: {ex.Message}");
            }
        }

        public SosigEnemyTemplate GetTemplate(SosigEnemyID enemyID)
        {
            // Method 1: Try cached template first
            if (templateCache.ContainsKey(enemyID))
            {
                return templateCache[enemyID];
            }

            // Method 2: Try IM.Instance direct access (Update 120 API)
            if (IM.Instance != null && IM.Instance.odicSosigObjsByID != null)
            {
                if (IM.Instance.odicSosigObjsByID.ContainsKey(enemyID))
                {
                    var template = IM.Instance.odicSosigObjsByID[enemyID];
                    templateCache[enemyID] = template;
                    logger?.LogInfo($"Cached template for {enemyID} from IM.Instance");
                    return template;
                }
            }

            // Method 3: Try Resources.FindObjectsOfTypeAll as fallback
            logger?.LogWarning($"Template not in cache for {enemyID}, searching Resources...");
            var allTemplates = Resources.FindObjectsOfTypeAll<SosigEnemyTemplate>();
            foreach (var t in allTemplates)
            {
                if (t != null && t.SosigEnemyID == enemyID)
                {
                    templateCache[enemyID] = t;
                    logger?.LogInfo($"Found and cached template for {enemyID}");
                    return t;
                }
            }

            logger?.LogError($"Could not find template for SosigEnemyID: {enemyID}");
            return null;
        }

        public bool HasTemplate(SosigEnemyID enemyID)
        {
            return templateCache.ContainsKey(enemyID);
        }

        public int GetCacheSize()
        {
            return templateCache.Count;
        }
    }
}
