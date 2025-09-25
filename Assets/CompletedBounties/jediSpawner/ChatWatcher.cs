using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FistVR;
using BepInEx;
using BepInEx.Configuration;
using System.IO;
using Sodalite;
using Sodalite.Api;
using Sodalite.ModPanel;

using Sodalite.UiWidgets;
using Sodalite.Utilities;
using System.Reflection;

namespace jediSpawner
{


    [BepInPlugin("h3vr.arpy.chatspawner", "ChatSpawner", "1.0.1")]
    public class ChatWatcher : BaseUnityPlugin
    {
        public static ChatWatcher instance;
        public static List<Sosig> spawnedChatters;
        public static List<Sosig> spawnedEnemyChatters;
        //private static Vector3 followPoint = new Vector3(0, 0, 0);
        private static LayerMask Mask;
        public string filePath = string.Empty;
        private ConfigEntry<string> filePathToTextFolder;
        private ConfigEntry<string> filePathToTextFolderforEnemySosig;
        private ConfigEntry<KeyCode> keyToSpawnEnemySosig;
        private ConfigEntry<KeyCode> keyToSpawn;
        private GameObject PrefabToSpawn;
        public string SpawnerName;
        private static readonly string BasePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public TNH_Manager TNHManager;
        public int enemyIFF = 3;
        //public string keyName;

        public void Awake()
        {
            filePathToTextFolder = Config.Bind("General",
                "FilePath",
                "null",
                "The File Path to where the name of the chatter can be found");
            filePathToTextFolderforEnemySosig = Config.Bind("General",
                "Filepath for enemy sosig",
                "null",
                "The File Path to where the name of the enemy chatter can be found");
            keyToSpawnEnemySosig = Config.Bind("General",
               "KeyBindForEnemySpawn",
               KeyCode.Keypad7,
               "The key used to spawn the enemy sosigs");
            keyToSpawn = Config.Bind("General",
                "KeyBind",
                KeyCode.P,
                "The key used to spawn the sosigs");
            var bundle = AssetBundle.LoadFromFile(Path.Combine(BasePath, "JediSpawner"));
            if (bundle)
            {
                PrefabToSpawn = bundle.LoadAsset<GameObject>("Jedit'sSpawner");
            }
            instance = this;
            spawnedChatters = new List<Sosig>();
            spawnedEnemyChatters = new List<Sosig>();
            Mask = LayerMask.GetMask("Environment");

        }

        public void Start()
        {

        }
        // Update is called once per frame
        public void Update()
        {
            if (Input.GetKeyDown(keyToSpawn.Value))
            {

                //SpawnerName
                string str = File.ReadAllText(filePathToTextFolder.Value);
                int index = str.IndexOf('"');
                string res = string.Empty;
                for (int i = index; i < str.LastIndexOf('"') - 1; i++)
                    res += str[i + 1];
                SpawnerName = res;
                Debug.Log(str);
                Vector3 spawnPoint = new Vector3(GM.CurrentPlayerBody.Head.transform.position.x, GM.CurrentPlayerBody.transform.position.y, GM.CurrentPlayerBody.Head.transform.position.z + 1);
                //GameObject spawner =
                ChatSpawner CS = Instantiate(PrefabToSpawn, spawnPoint, Quaternion.identity).GetComponent<ChatSpawner>();
                CS.SpawningSequence();
                //Instantiate(PrefabToSpawn, spawnPoint, Quaternion.identity);
                //GameObject spawner = Instantiate(PrefabToSpawn, GM.CurrentPlayerBody.Head.transform.position, Quaternion.identity);
                //Rigidbody spawnerBody = (Rigidbody)spawner.GetComponent(typeof(Rigidbody));
                //spawnerBody.AddForce(GM.CurrentPlayerBody.Head.forward, ForceMode.Impulse);
            }
            //This spawns the enemy sosig with different values and whatknot
            if (Input.GetKeyDown(keyToSpawnEnemySosig.Value))
            {
                if (TNHManager == null && GM.TNH_Manager != null)
                {
                    TNHManager = GM.TNH_Manager;

                }
                else if(TNHManager == null && GM.TNH_Manager == null)
                     Debug.Log("Did not find TNHManager");
                if (TNHManager != null)
                {
                    //SpawnerName
                    string str = File.ReadAllText(filePathToTextFolderforEnemySosig.Value);
                    int index = str.IndexOf('"');
                    string res = string.Empty;
                    for (int i = index; i < str.LastIndexOf('"') - 1; i++)
                        res += str[i + 1];
                    SpawnerName = res;
                    Debug.Log(str);
                    Vector3 spawnPoint = new Vector3(0,0,0);
                    int iff = 1;
                    if (TNHManager.Phase == TNH_Phase.Hold)
                    {
                        //this sets the spawnpoint of the sosig to one of the attack vectors during a hold
                        spawnPoint = TNHManager.m_curHoldPoint.AttackVectors[Random.Range(0, TNHManager.m_curHoldPoint.AttackVectors.Count - 1)].SpawnPoints_Sosigs_Attack[1].position;
                        iff = TNHManager.m_curHoldPoint.m_curPhase.IFFUsed;
                    }else if (TNHManager.Phase == TNH_Phase.Take)
                    {
                        spawnPoint = TNHManager.m_curHoldPoint.SpawnPoints_Turrets[0].transform.position;
                        if (TNHManager.m_curLevel.PatrolChallenge.Patrols.Count > 0)
                            iff = TNHManager.m_curLevel.PatrolChallenge.Patrols[0].IFFUsed;
                        else iff = TNHManager.m_curHoldPoint.m_curPhase.IFFUsed;
                        //iff = TNHManager.m_curLevel.PatrolChallenge.Patrols[0].IFFUsed;
                    }
                    //spawnPoint = new Vector3(GM.CurrentPlayerBody.Head.transform.position.x, GM.CurrentPlayerBody.transform.position.y, GM.CurrentPlayerBody.Head.transform.position.z + 1);

                    //GameObject obj = Instantiate(PrefabToSpawn, spawnPoint, Quaternion.identity);
                    ChatSpawner CS = Instantiate(PrefabToSpawn, spawnPoint, Quaternion.identity).GetComponent<ChatSpawner>();
                    CS.SpawningSequenceEnemy(iff);

                }
            }
            if (spawnedChatters.Count > 0)
            {




                for (var i = spawnedChatters.Count - 1; i > -1; i--)
                {
                    if (spawnedChatters[i] == null)
                    {
                        spawnedChatters.RemoveAt(i);
                        //Debug.Log("removed a sosig");
                    }
                }
                foreach (Sosig selectedSosig in spawnedChatters)
                {
                    if (!selectedSosig.m_isStunned)
                    {

                        if (Vector3.Distance(GM.CurrentPlayerBody.Head.position, selectedSosig.m_assaultPoint) > 6)
                        {
                            bool isBad = true;

                            float one = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
                            float two = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
                            Vector3 followPoint = new Vector3(GM.CurrentPlayerBody.Head.position.x + one, GM.CurrentPlayerBody.Head.position.y, GM.CurrentPlayerBody.Head.position.z + two);
                            isBad = Physics.Linecast(GM.CurrentPlayerBody.Head.position, followPoint, Mask);
                            if (!isBad)
                            {
                                selectedSosig.CommandAssaultPoint(followPoint);
                            }

                            //selectedSosig.CommandAssaultPoint(followPoint);

                        }
                        /*if (Vector3.Distance(followPoint, selectedSosig.m_assaultPoint) > 6)
                        {
                            selectedSosig.CommandAssaultPoint(followPoint);
                        }
                             float oldSpeed = selectedSosig.Speed_Run;
                             while (Vector3.Distance(followPoint, selectedSosig.m_assaultPoint) > 25)
                             {
                                 selectedSosig.Speed_Run = 10;
                                 if (selectedSosig.Speed_Run != oldSpeed)
                                 {
                                     selectedSosig.Speed_Run = oldSpeed;
                                 }
                             }*/

                    }
                }
                //Debug.Log("updated the assault point");


                foreach (Sosig sosig in (spawnedChatters))
                {
                    if (sosig.BodyState == Sosig.SosigBodyState.Dead)
                    {
                        sosig.TickDownToClear(3);
                    }
                    if (sosig.Priority.HasFreshTarget() && sosig.CurrentOrder == Sosig.SosigOrder.Investigate && sosig.m_entityRecognition >= 0.65f)
                    {
                        sosig.SetCurrentOrder(Sosig.SosigOrder.Skirmish);
                    }
                }

            }
            //Here we are gonna update the enemy chatters and make sure they're not being extra silly today
            if (spawnedEnemyChatters.Count > 0)
            {




                for (var i = spawnedEnemyChatters.Count - 1; i > -1; i--)
                {
                    if (spawnedEnemyChatters[i] == null)
                    {
                        spawnedEnemyChatters.RemoveAt(i);
                        //Debug.Log("removed a sosig");
                    }
                }
                foreach (Sosig selectedSosig in spawnedEnemyChatters)
                {
                    if (!selectedSosig.m_isStunned)
                    {

                        if (Vector3.Distance(GM.CurrentPlayerBody.Head.position, selectedSosig.Links[1].transform.position) > 20)
                        {
                            // bool isBad = true;
                            selectedSosig.CommandAssaultPoint(GM.CurrentPlayerBody.transform.position);
                            //float one = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
                            //float two = ((Random.Range(0, 2) * 2 - 1) * Random.Range(0.75f, 2.5f));
                            //Vector3 followPoint = new Vector3(GM.CurrentPlayerBody.Head.position.x + one, GM.CurrentPlayerBody.Head.position.y, GM.CurrentPlayerBody.Head.position.z + two);
                            //isBad = Physics.Linecast(GM.CurrentPlayerBody.Head.position, followPoint, Mask);
                            //if (!isBad)
                            //{
                                //selectedSosig.CommandAssaultPoint(followPoint);
                            //}

                            //selectedSosig.CommandAssaultPoint(followPoint);

                        }
                        /*if (Vector3.Distance(followPoint, selectedSosig.m_assaultPoint) > 6)
                        {
                            selectedSosig.CommandAssaultPoint(followPoint);
                        }
                             float oldSpeed = selectedSosig.Speed_Run;
                             while (Vector3.Distance(followPoint, selectedSosig.m_assaultPoint) > 25)
                             {
                                 selectedSosig.Speed_Run = 10;
                                 if (selectedSosig.Speed_Run != oldSpeed)
                                 {
                                     selectedSosig.Speed_Run = oldSpeed;
                                 }
                             }*/

                    }
                }
                //Debug.Log("updated the assault point");


                foreach (Sosig sosig in (spawnedEnemyChatters))
                {
                    if (sosig.BodyState == Sosig.SosigBodyState.Dead)
                    {
                        sosig.TickDownToClear(3);
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

        public ChatWatcher()
        {
            instance = this;
        }
        // public void addSosigToList(Sosig Jeff)
        // {
        //    spawnedChatters.Add(Jeff);
        //}
    }
}