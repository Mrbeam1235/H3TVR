using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FistVR;
using UnityEngine.UI;

namespace jediSpawner
{
    public class ChatSpawner : MonoBehaviour
    {
        public SosigEnemyTemplate t;
        public List<SosigEnemyTemplate> EnemyT;
        //public nameList nameList;
        public GameObject namething;
        public GameObject enemyNamePlate;
        public bool isEnemy = false;
        public float enemyIFF = 1;
        
        public void Start()
        {

            //SpawningSequence();

        }

        // Update is called once per frame
        public void Update()
        {

        }

        public void SpawningSequence()
        {
            //Quaternion rot = new Quaternion(0, 0, 0, 0);
            Sosig Jeff = SpawnAChatter(t.SosigPrefabs[Random.Range(0, t.SosigPrefabs.Count)].GetGameObject(), t.WeaponOptions[Random.Range(0, t.WeaponOptions.Count)].GetGameObject(), t.WeaponOptions_Secondary[Random.Range(0, t.WeaponOptions_Secondary.Count)].GetGameObject(), t.WeaponOptions_Tertiary[Random.Range(0, t.WeaponOptions_Tertiary.Count)].GetGameObject(), gameObject.transform.position, gameObject.transform.rotation, t.ConfigTemplates[Random.Range(0, t.ConfigTemplates.Count)], t.OutfitConfig[Random.Range(0, t.OutfitConfig.Count)], 0);
            float one = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
            float two = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
            Vector3 followPoint = new Vector3(GM.CurrentPlayerBody.Head.position.x + one, GM.CurrentPlayerBody.Head.position.y, GM.CurrentPlayerBody.Head.position.z + two);
            Jeff.CommandAssaultPoint(followPoint);

            Jeff.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            //SpawnEnemy(t, transform.position, gameObject.transform.rotation, 0);
            EmotionalConnection(namething, Jeff, false);
            ChatWatcher.spawnedChatters.Add(Jeff);


        }
        public void SpawningSequenceEnemy(int IFF)
        {
            //Quaternion rot = new Quaternion(0, 0, 0, 0);
            SosigEnemyTemplate ET = EnemyT[Random.Range(0, EnemyT.Count-1)];
            Sosig Jeff = SpawnAChatter(ET.SosigPrefabs[Random.Range(0, ET.SosigPrefabs.Count)].GetGameObject(), ET.WeaponOptions[Random.Range(0, ET.WeaponOptions.Count)].GetGameObject(), ET.WeaponOptions_Secondary[Random.Range(0, ET.WeaponOptions_Secondary.Count)].GetGameObject(), ET.WeaponOptions_Tertiary[Random.Range(0, ET.WeaponOptions_Tertiary.Count)].GetGameObject(), gameObject.transform.position, gameObject.transform.rotation, ET.ConfigTemplates[Random.Range(0, ET.ConfigTemplates.Count)], ET.OutfitConfig[Random.Range(0, ET.OutfitConfig.Count)], IFF);
            float one = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
            float two = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
            //Vector3 followPoint = new Vector3(GM.CurrentPlayerBody.Head.position.x + one, GM.CurrentPlayerBody.Head.position.y, GM.CurrentPlayerBody.Head.position.z + two);
            Jeff.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);

            Jeff.FallbackOrder = Sosig.SosigOrder.SearchForEquipment;
            //SpawnEnemy(t, transform.position, gameObject.transform.rotation, 0);
            EmotionalConnectionButEvil(enemyNamePlate, Jeff, true);
            ChatWatcher.spawnedEnemyChatters.Add(Jeff);


        }

        /* private Sosig SpawnEnemy(
           SosigEnemyTemplate t,
           Vector3 pos,
           Quaternion rot,
           int IFF
           )
         {

             GameObject weaponPrefab = //null;
           // if (t.WeaponOptions.Count > 0)
                 weaponPrefab = t.WeaponOptions[Random.Range(0, t.WeaponOptions.Count)].GetGameObject();
             GameObject weaponPrefab2 = //null;
             //if (t.WeaponOptions_Secondary.Count > 0 && Random.Range(0.0f, 1f) <= t.SecondaryChance)
                 weaponPrefab2 = t.WeaponOptions_Secondary[Random.Range(0, t.WeaponOptions_Secondary.Count)].GetGameObject();
             GameObject weaponPrefab3 = //null;
             //if (t.WeaponOptions_Tertiary.Count > 0 && Random.Range(0.0f, 1f) <= t.TertiaryChance)
                 weaponPrefab3 = t.WeaponOptions_Tertiary[Random.Range(0, t.WeaponOptions_Tertiary.Count)].GetGameObject();
             SosigConfigTemplate configTemplate = t.ConfigTemplates[Random.Range(0, t.ConfigTemplates.Count)];

             return SpawnAChatter(t.SosigPrefabs[Random.Range(0, t.SosigPrefabs.Count)].GetGameObject(), t.WeaponOptions[Random.Range(0, t.WeaponOptions.Count)].GetGameObject(), t.WeaponOptions_Secondary[Random.Range(0, t.WeaponOptions_Secondary.Count)].GetGameObject(), t.WeaponOptions_Tertiary[Random.Range(0, t.WeaponOptions_Tertiary.Count)].GetGameObject(), pos, rot, configTemplate, t.OutfitConfig[Random.Range(0, t.OutfitConfig.Count)], IFF);
         }*/
        private Sosig SpawnAChatter(
          GameObject prefab,
          GameObject weaponPrefab,
          GameObject weaponPrefab2,
          GameObject weaponPrefab3,
          Vector3 pos,
          Quaternion rot,
          SosigConfigTemplate t,
          SosigOutfitConfig o,
          int IFF)
        {
            Sosig componentInChildren = Instantiate(prefab, pos, rot).GetComponentInChildren<Sosig>();
           
            componentInChildren.Configure(t);
            componentInChildren.E.IFFCode = IFF;
            componentInChildren.Priority.IFFChart[IFF] = true;
            if (weaponPrefab != null)
            {
                SosigWeapon component1 = Instantiate(weaponPrefab, pos + Vector3.up * 0.1f, rot).GetComponent<SosigWeapon>();
                component1.SetAutoDestroy(true);
                component1.O.SpawnLockable = false;
                if (component1.Type == SosigWeapon.SosigWeaponType.Gun)
                    componentInChildren.Inventory.FillAmmoWithType(component1.AmmoType);
                componentInChildren.Inventory.Init();
                componentInChildren.Inventory.FillAllAmmo();
                if (component1 != null)
                {
                    componentInChildren.InitHands();
                    componentInChildren.ForceEquip(component1);
                    component1.SetAmmoClamping(true);
                    //if (GM.TNH_Manager.SosiggunShakeReloading == TNH_SosiggunShakeReloading.Off)
                    component1.IsShakeReloadable = false;

                }
                if (weaponPrefab2 != null)
                {
                    SosigWeapon component2 = Instantiate(weaponPrefab2, pos + Vector3.up * 0.1f, rot).GetComponent<SosigWeapon>();
                    component2.SetAutoDestroy(true);
                    component2.O.SpawnLockable = false;
                    component2.SetAmmoClamping(true);
                    //if (GM.TNH_Manager.SosiggunShakeReloading == TNH_SosiggunShakeReloading.Off)
                    component2.IsShakeReloadable = false;
                    if (component2.Type == SosigWeapon.SosigWeaponType.Gun)
                        componentInChildren.Inventory.FillAmmoWithType(component2.AmmoType);
                    if (component2 != null)
                        componentInChildren.ForceEquip(component2);
                }
                if (weaponPrefab3 != null)
                {
                    SosigWeapon component3 = Instantiate(weaponPrefab3, pos + Vector3.up * 0.1f, rot).GetComponent<SosigWeapon>();
                    component3.SetAutoDestroy(true);
                    component3.O.SpawnLockable = false;
                    component3.SetAmmoClamping(true);
                    //if (GM.TNH_Manager.SosiggunShakeReloading == TNH_SosiggunShakeReloading.Off)
                    component3.IsShakeReloadable = false;
                    if (component3.Type == SosigWeapon.SosigWeaponType.Gun)
                        componentInChildren.Inventory.FillAmmoWithType(component3.AmmoType);
                    if (component3 != null)
                        componentInChildren.ForceEquip(component3);

                }
            }
            if (Random.Range(0.0f, 1f) < o.Chance_Headwear)
                this.SpawnAccesoryToLink(o.Headwear, componentInChildren.Links[0]);
            if (Random.Range(0.0f, 1f) < o.Chance_Facewear)
                this.SpawnAccesoryToLink(o.Facewear, componentInChildren.Links[0]);
            if (Random.Range(0.0f, 1f) < o.Chance_Eyewear)
                this.SpawnAccesoryToLink(o.Eyewear, componentInChildren.Links[0]);
            if (Random.Range(0.0f, 1f) < o.Chance_Torsowear)
                this.SpawnAccesoryToLink(o.Torsowear, componentInChildren.Links[1]);
            if (Random.Range(0.0f, 1f) < o.Chance_Pantswear)
                this.SpawnAccesoryToLink(o.Pantswear, componentInChildren.Links[2]);
            if (Random.Range(0.0f, 1f) < o.Chance_Pantswear_Lower)
                this.SpawnAccesoryToLink(o.Pantswear_Lower, componentInChildren.Links[3]);
            if (Random.Range(0.0f, 1f) < o.Chance_Backpacks)
                this.SpawnAccesoryToLink(o.Backpacks, componentInChildren.Links[1]);
            if (Random.Range(0.0f, 1f) < o.Chance_TorosDecoration)
                this.SpawnAccesoryToLink(o.TorosDecoration, componentInChildren.Links[1]);
            if (t.UsesLinkSpawns)
            {
                for (int index = 0; index < componentInChildren.Links.Count; ++index)
                {
                    float num = Random.Range(0.0f, 1f);
                    if (t.LinkSpawns.Count > index && t.LinkSpawns[index] != null && t.LinkSpawns[index].Category != FVRObject.ObjectCategory.Loot && num < t.LinkSpawnChance[index])
                        componentInChildren.Links[index].RegisterSpawnOnDestroy(t.LinkSpawns[index]);
                }
            }


            return componentInChildren;
        }

        private void SpawnAccesoryToLink(List<FVRObject> gs, SosigLink l)
        {
            GameObject gameObject = Instantiate(gs[Random.Range(0, gs.Count)].GetGameObject(), l.transform.position, l.transform.rotation);
            gameObject.transform.SetParent(l.transform);
            gameObject.GetComponent<SosigWearable>().RegisterWearable(l);
        }

        public void EmotionalConnection(GameObject nameBox, Sosig james, bool enemy)
        {
            /* int nameChoice = Random.Range(0, nameList.names.Count - 1);
             string name = nameList.names[nameChoice];*/
            string name = ChatWatcher.instance.SpawnerName;
            Component[] displays;
            GameObject nameMe = Instantiate(nameBox, james.Links[1].transform, false);
            nameMe.transform.localPosition.Set(0, 0, 0);
            nameMe.transform.localRotation.Set(0, 0, 0, 0);
            displays = nameMe.GetComponentsInChildren(typeof(Text));
            foreach (Text text in displays)
            {
                text.text = name;
             
            }
            

            

        }
        public void EmotionalConnectionButEvil(GameObject nameBox, Sosig james, bool enemy)
        {
            /* int nameChoice = Random.Range(0, nameList.names.Count - 1);
             string name = nameList.names[nameChoice];*/
            string name = ChatWatcher.instance.SpawnerName;
            Component[] displays;
            GameObject nameMe = Instantiate(nameBox, james.Links[1].transform, false);
            nameMe.transform.localPosition.Set(0, 0, 0);
            nameMe.transform.localRotation.Set(0, 0, 0,0);
            displays = nameMe.GetComponentsInChildren(typeof(Text));
            foreach (Text text in displays)
            {
                text.text = name;
           
            }




        }

    }
}