using UnityEngine;
using UnityEngine.UI;

namespace H3TVR
{
    /// <summary>
    /// Manages nameplate display above sosigs
/// </summary>
    public class SosigNameplateManager
    {
        public void AttachNameplate(FistVR.Sosig sosig, string name, GameObject nameplatePrefab, bool isEnemy)
        {
    try
   {
     if (sosig.Links.Count == 0 || nameplatePrefab == null) return;
    
                GameObject nameplate = GameObject.Instantiate(nameplatePrefab, sosig.Links[0].transform, false);
      nameplate.transform.localPosition = new Vector3(0f, 0.3f, 0f);
        nameplate.transform.localRotation = Quaternion.identity;
  
          var textComponents = nameplate.GetComponentsInChildren<Text>();
        foreach (Text text in textComponents)
                {
    text.text = name;
             }
   }
    catch (System.Exception)
            {
          // Silent fail - nameplate not critical
     }
        }
    }
}
