using System;
using BepInEx.Logging;
using FistVR;
using UnityEngine;

namespace H3TVR
{
    /// <summary>
    /// Controls sosig AI behavior patterns
    /// </summary>
    public class SosigBehaviorController
    {
        private ManualLogSource logger;
        private static readonly LayerMask EnvironmentMask = LayerMask.GetMask("Environment");
  
        public void Initialize(ManualLogSource logSource)
        {
       logger = logSource;
        }
    
        public void SetupAllyBehavior(Sosig sosig)
        {
 try
            {
if (GM.CurrentPlayerBody?.Head?.transform == null) return;
           
     var playerPos = GM.CurrentPlayerBody.Head.transform.position;
 float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
        float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
  Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
      
          sosig.CommandAssaultPoint(followPoint);
       sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            }
    catch (Exception ex)
            {
       logger?.LogError($"Ally behavior setup failed: {ex.Message}");
            }
        }
        
   public void SetupEnemyBehavior(Sosig sosig)
        {
     try
            {
 if (GM.CurrentPlayerBody?.transform == null) return;
          
       sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                sosig.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
 }
        catch (Exception ex)
    {
       logger?.LogError($"Enemy behavior setup failed: {ex.Message}");
 }
 }
        
     public void UpdateAllyBehavior(Sosig sosig, float followDistance)
        {
   if (GM.CurrentPlayerBody?.Head == null) return;
     
  if (!sosig.m_isStunned)
            {
     var playerPos = GM.CurrentPlayerBody.Head.position;
float distance = Vector3.Distance(playerPos, sosig.m_assaultPoint);
       
        if (distance > followDistance)
          {
 float offsetX = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
                 float offsetZ = ((UnityEngine.Random.Range(0, 2) * 2 - 1) * UnityEngine.Random.Range(0.75f, 2.5f));
        Vector3 followPoint = new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
          
   bool isBad = Physics.Linecast(playerPos, followPoint, EnvironmentMask);
     if (!isBad)
     {
      sosig.CommandAssaultPoint(followPoint);
          }
         }
  }
          
  if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.65f)
 {
                sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
            }
        }
     
        public void UpdateEnemyBehavior(Sosig sosig, float aggressionDistance)
     {
     if (GM.CurrentPlayerBody?.Head == null) return;
      
        if (!sosig.m_isStunned)
      {
    var playerPos = GM.CurrentPlayerBody.Head.position;
        float distance = Vector3.Distance(playerPos, sosig.Links[1].transform.position);
      
       if (distance > aggressionDistance)
   {
          sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
   }
     }
     
       if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.55f)
         {
     sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
    }
        
          if (sosig.CurrentOrder == Sosig.SosigOrder.Disabled || sosig.CurrentOrder == Sosig.SosigOrder.Idle || sosig.CurrentOrder == Sosig.SosigOrder.GuardPoint)
            {
     sosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
 }
        }
    }
}
