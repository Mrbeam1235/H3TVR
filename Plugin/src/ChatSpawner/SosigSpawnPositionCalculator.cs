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
        public Vector3 CalculateAllySpawnPoint()
   {
if (GM.CurrentPlayerBody?.Head?.transform == null)
         return Vector3.zero;
            
    var playerPos = GM.CurrentPlayerBody.Head.transform.position;
   float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
   float distance = UnityEngine.Random.Range(2f, 4f);
         
    return new Vector3(
    playerPos.x + Mathf.Cos(angle) * distance,
  playerPos.y,
        playerPos.z + Mathf.Sin(angle) * distance
  );
        }
        
        public Vector3 CalculateEnemySpawnPoint()
        {
         if (GM.CurrentPlayerBody?.Head?.transform == null)
      return Vector3.zero;
          
     var playerPos = GM.CurrentPlayerBody.Head.transform.position;
  float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
      float distance = UnityEngine.Random.Range(8f, 15f);
        
return new Vector3(
           playerPos.x + Mathf.Cos(angle) * distance,
    playerPos.y,
     playerPos.z + Mathf.Sin(angle) * distance
       );
  }
   
     public Vector3 CalculateBossSpawnPoint()
     {
            if (GM.CurrentPlayerBody?.Head?.transform == null)
  return Vector3.zero;
    
       var playerPos = GM.CurrentPlayerBody.Head.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
      float distance = UnityEngine.Random.Range(20f, 30f); // Bosses spawn further
     
         return new Vector3(
      playerPos.x + Mathf.Cos(angle) * distance,
         playerPos.y,
      playerPos.z + Mathf.Sin(angle) * distance
      );
        }
    }
}
