using BepInEx;
using BepInEx.Configuration;
using FistVR;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Valve.VR;


namespace H3TVR
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    [BepInProcess("h3vr.exe")]
    public class H3TVR : BaseUnityPlugin
    {
        private const float SlowdownFactor = .001f;
        private const float SlowdownLength = 6f;
        public string SlomoStatus = "Off";
        private const float MaxSlomo = .1f;
        private const float SlomoWaitTime = 2f;
        private const float ZeroGWaitTime = 6f;
        private const float RealisticFallTime = 1f;
        private string ZeroGStatus = "Off";
        private readonly Hooks _hooks;
        private List<string> GunList = File.ReadAllLines(@"c:\H3TVR\GunList.txt").ToList();
        private List<string> MagazineList = File.ReadAllLines(@"c:\H3TVR\MagazineList.txt").ToList();
        public string filepath = string.Empty;
        private ConfigEntry<string> filePathToTextFolderGunList;
        private ConfigEntry<string> filePathToTextFolderMagzineList;


#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        public H3TVR()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        {
            ConfigEntry<string> configEntry = Config.Bind("General",
                           "FilePath",
                           "null",
                           "The File path to the skitty subs gun list");
            ConfigEntry<string> filePathtoTextFolderGunList = configEntry;
ConfigEntry<string> filePathToTextFolderMagzineList = Config.Bind("General",
                    "FilePath",
                    "null",
                    "The File Path to where the Skitty sub MagzineList is");
            ConfigEntry<KeyCode> keytoSpawnwonderToy = Config.Bind("General",
                           "KeyBindForWonderToy",
                        KeyCode.Keypad0,
                        "The key used to spawn WonderToy");
            ConfigEntry<KeyCode> keytoSpawnBodypillows = Config.Bind("General",
                        "KeyBindForBodyPillows",
                        KeyCode.Keypad1,
                        "The Key to spawn Body Pillows ");
            ConfigEntry<KeyCode> keyToSpawnFlash = Config.Bind("General",
                             "KeyBindForFlash",
                             KeyCode.Keypad2,
                             "The key to spawn flash nades");
            ConfigEntry<KeyCode> KeyToSpawnShuri = Config.Bind("General",
                "KeyBindForShuri",
                KeyCode.Keypad3,
                "The key to spawn shurkins");
            ConfigEntry<KeyCode> KeytoSpawnNadeRain = Config.Bind("General",
                    "KeyBindForNadeRain",
                    KeyCode.Keypad4,
                    "The Key to Spawn Nade Rain");
            ConfigEntry<KeyCode> KeytoSpawnHydration = Config.Bind("General",
                "KeyBindForHydration",
                KeyCode.Keypad5,
                "The key to spawn Hydration");
            ConfigEntry<KeyCode> KeytoSpawnJeditToy = Config.Bind("General",
                "KeyBindForJeditToy",
                KeyCode.Keypad6,
                "The key to spawn JeditToy");
            ConfigEntry<KeyCode> KeyToSpawnSloMo = Config.Bind("General",
                "KeyBindForSloMo",
                KeyCode.Keypad7,
                "The key to active slomo (keyboard press not controller button)");
            ConfigEntry<KeyCode> KeytoSpawnDestoryHeld = Config.Bind("General",
                "KeyBindForDestoryHeld",
                KeyCode.Keypad8,
                "The key to Destory what is held");
            ConfigEntry<KeyCode> KeyToSpawnSkittysubGuns = Config.Bind("General",
                "KeyBindForSkittySubGuns",
                KeyCode.Keypad9,
                "The key to spawn SkittySubGuns");
            ConfigEntry<KeyCode> KeyToBumpGravityDown = Config.Bind("General",
                "KeyBindForBumpGravityDown",
                KeyCode.KeypadDivide,
                "The key to Bump gravityDown");
            ConfigEntry<KeyCode> KeyToEnableMeatHands = Config.Bind("General",
                "KeyBindToEnableMeatHands",
                KeyCode.KeypadEnter,
                "The key to EnableMeatHands");
            ConfigEntry<KeyCode> KeytoSpawnDangerClose = Config.Bind("General",
                "KeyBindForDangerClose",
                KeyCode.KeypadMinus,
                "The key to use DangerClose");
            ConfigEntry<KeyCode> keyToSpawnFlash2 = Config.Bind("General",
                "KeyBindForFlash2",
                KeyCode.KeypadMultiply,
                "The key to spawn flash2");
            ConfigEntry<KeyCode> KeytoDestoryQuickbelt = Config.Bind("General",
                "KeyBindForDestoryQuickbelt",
                KeyCode.KeypadPeriod,
                "The key to DestoryQuickbelt");
            ConfigEntry<KeyCode> KeytoBigSkittySubGuns = Config.Bind("General",
                "KeyBindForBigSkittySubGuns",
                KeyCode.KeypadPlus,
                "The key to SpawnBigSkittySubGuns");




            _hooks = new Hooks();
            _hooks.Hook();
            Logger.LogInfo("Loading H3TVR");
        }

        private void Awake()
        {
            Harmony.CreateAndPatchAll(this.GetType());
            Logger.LogInfo("Successfully loaded H3TVR!");
        }

        private void Update()
        {
            //wonderful toy spawn
            if (Input.GetKeyDown(KeyCode.H))

            {
                SpawnWonderfulToy();
            }

            //body pillow spawn
            if (Input.GetKeyDown(KeyCode.J))

            {
                SpawnPillow();
            }

            //flash spawn
            if (Input.GetKeyDown(KeyCode.K))
            {
                SpawnFlash();
            }

            //shuri spawn
            if (Input.GetKey(KeyCode.B))
            {
                SpawnShuri();
            }

            //nade spawn
            if (Input.GetKeyDown(KeyCode.V))
            {
                SpawnNadeRain();
            }

            //hydration spawn
            if (Input.GetKeyDown(KeyCode.I))
            {
                SpawnHydration();
            }

            //jedit tt spawn
            if (Input.GetKeyDown(KeyCode.U))
            {
                SpawnJeditToy();
            }

            if (GM.CurrentMovementManager.Hands[1].Input.AXButtonDown || Input.GetKeyDown(KeyCode.Space))
            {
                Logger.LogInfo("Detected Right A Press!");
                SlomoStatus = "Slowing";
            }

            if (SlomoStatus == "Slowing")
            {
                Logger.LogInfo("Slowing!");
                SlomoScaleDown();
            }

            if (SlomoStatus == "Wait")
            {
                Logger.LogInfo("Waiting!");
                SlomoStatus = "Paused";
                StartCoroutine(SlomoWait());
            }

            if (SlomoStatus == "Return")
            {
                Logger.LogInfo("Returning!");
                SlomoReturn();
            }

            if (Time.timeScale == 1)
            {
                SlomoStatus = ("Off");
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                DestroyHeld();
            }

            if (Input.GetKeyDown(KeyCode.N))
            {
                SpawnSkittySubGun();
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                ZeroGravityBumpDown();
            }

            if (ZeroGStatus == "On")
            {
                StartCoroutine(ZeroGWait());
            }

            if (ZeroGStatus == "Falling")
            {
                StartCoroutine(RealisticFallWait());
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                EnableMeatHands();
            }

            if (Input.GetKey(KeyCode.F))
            {
                DangerCloseBarrage();
            }


            if (Input.GetKeyDown(KeyCode.E))
            {
                SpawnFlash2();
            }

            if (Input.GetKeyDown(KeyCode.C))
            {
                DestroyQuickbelt();

            }
           
            if (Input.GetKeyDown(KeyCode.O))
            {
                SpawnSkittyBigGun();
            }
        }


        private void SpawnWonderfulToy()
        {
            // Get the object you want to spawn
            FVRObject obj = IM.OD["TippyToyAnton"];


            // Instantiate (spawn) the object above the player's right hand
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            //add some speeeeen
            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));


            //add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 25);
        }

        private void SpawnJeditToy()
        {
            // Get the object you want to spawn
            FVRObject obj = IM.OD["JediTippyToy"];


            // Instantiate (spawn) the object above the player's right hand
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            //add some speeeeen
            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));


            //add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 25);
        }

        private void SpawnPillow()
        {

            // Get the object you want to spawn
            FVRObject obj = IM.OD["BodyPillow"];


            // Instantiate (spawn) the object above the player head
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);


            //add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 4000);
        }



        //we want to spawn a flashbang infront of the player with little notice
        private void SpawnFlash()
        {
            // Get the object you want to spawn
            FVRObject obj = IM.OD["PinnedGrenadeXM84"];


            // Instantiate (spawn) the object above the player head
            Logger.LogInfo("Spawned Object");
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);


            //prime the flash object
            Logger.LogInfo("Getting Component");
            PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
            Logger.LogInfo("Releasing Lever");
            grenade.ReleaseLever();



            //add force
            Logger.LogInfo("Adding Force");
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 500);
        }

        private void SpawnNadeRain()
        {
            //    //Set cartridge speed
            float howFast = 15.0f;

            //    //Set max angle
            float maxAngle = 4.0f;

            Transform PointingTransfrom = transform;

            //    //Get Random direction for bullet
            Vector2 randRot = Random.insideUnitCircle;

            // Random number for pull chance
            int pullChance = Random.Range(1, 20);
            Logger.LogInfo(pullChance);

            // Get the object you want to spawn
            FVRObject obj = IM.OD["PinnedGrenadeM67"];

            //Set Object Position
            Vector3 grenadePosition0 = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.up * 0.02f);

            // Instantiate (spawn) the object above the player head
            Logger.LogInfo("Spawned Object");
            GameObject go = Instantiate(obj.GetGameObject(), grenadePosition0, Quaternion.LookRotation(GM.CurrentPlayerBody.Head.up));

            //Set Object Direction
            go.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

            //add force
            Logger.LogInfo("Adding Force");
            go.GetComponent<Rigidbody>().velocity = go.transform.forward * howFast;


            if (pullChance == 10)
            {
                //prime the grenade object
                Logger.LogInfo("Getting Component");
                PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
                Logger.LogInfo("Releasing Lever");
                grenade.ReleaseLever();
            }

        }

        private void SpawnShuri()

        {
            //Set cartridge speed
            float howFast = 30.0f;

            //Set max angle
            float maxAngle = 4.0f;

            Transform PointingTransfrom = transform;



            //Get Random direction for bullet
            Vector2 randRot = Random.insideUnitCircle;

            // Get the object I want to spawnz
            FVRObject obj = IM.OD["Shuriken"];

            //Set Object Position
            Vector3 shuriPosition0 = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 0.02f);


            //Create Bullet
            //GameObject go0 = Instantiate(obj.GetGameObject(), bulletPosition0, Quaternion.LookRotation(-GM.CurrentPlayerBody.LeftHand.upxx));
            //GameObject go1 = Instantiate(obj.GetGameObject(), bulletPosition0, Quaternion.LookRotation(-GM.CurrentPlayerBody.LeftHand.up));

            //old spray
            GameObject go0 = Instantiate(obj.GetGameObject(), shuriPosition0, Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward));


            //Set Object Direction
            go0.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);


            //Add Force


            //go0.GetComponent<Rigidbody>().velocity = GM.CurrentPlayerBody.LeftHand.forward * howFast;
            //go1.GetComponent<Rigidbody>().velocity = GM.CurrentPlayerBody.LeftHand.forward * howFast;

            //old spray
            //add scale for funnies
            go0.transform.localScale = new Vector3(10, 10, 10);
            go0.GetComponent<Rigidbody>().velocity = go0.transform.forward * howFast;

            Destroy(go0, 60f);

        }

        private void DangerCloseBarrage()

        {
            //Set cartridge speed
            float howFast = 30.0f;

            //Set max angle
            float maxAngle = 2.0f;

            Transform PointingTransfrom = transform;



            //Get Random direction for bullet
            Vector2 randRot = Random.insideUnitCircle;

            // Get the object I want to spawnz
            FVRObject obj = IM.OD["Cartridge50mmFlareDangerClose"];

            //Set Object Position
            Vector3 dangerClosePosition0 = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 0.02f);

            //old spray
            GameObject go0 = Instantiate(obj.GetGameObject(), dangerClosePosition0, Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward));


            //Set Object Direction
            go0.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

            //old spray
            go0.GetComponent<Rigidbody>().velocity = go0.transform.forward * howFast;
            FVRFireArmRound cartridge = go0.GetComponent<FVRFireArmRound>();
            cartridge.Splode(0.5f, false, true);


        }

        private void SlomoScaleDown()
        {
            if (Time.timeScale > MaxSlomo)
            {
                Time.timeScale -= (1f) * Time.unscaledDeltaTime;
                Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
            }

            if (Time.timeScale <= MaxSlomo)
            {
                SlomoStatus = ("Wait");
            }
        }

        private void SlomoReturn()
        {
            if (Time.timeScale != 1)
            {
                Time.timeScale += (1f / 3f) * Time.unscaledDeltaTime;
                Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
            }
        }

        IEnumerator SlomoWait()
        {
            yield return new WaitForSecondsRealtime(SlomoWaitTime);
            SlomoStatus = "Return";
        }

        IEnumerator ZeroGWait()
        {
            yield return new WaitForSeconds(ZeroGWaitTime);
            ZeroGStatus = "Falling";
            RealisticFall();
        }

        IEnumerator RealisticFallWait()
        {
            yield return new WaitForSecondsRealtime(RealisticFallTime);
            ZeroGravityBumpUp();
        }

        //private void SpawnNade()
        //{

        //}

        private void SpawnHydration()
        {
            // Get the object you want to spawn
            FVRObject obj = IM.OD["SuppressorBottle"];


            // Instantiate (spawn) the object above the player's right hand
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            //add some speeeeen
            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));


            //add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 25);
        }

        private void DestroyHeld()

        {
            if (GM.CurrentMovementManager.Hands[1].CurrentInteractable != null && GM.CurrentMovementManager.Hands[1].CurrentInteractable is FVRPhysicalObject)
            {
                Destroy(GM.CurrentMovementManager.Hands[1].CurrentInteractable.gameObject);
            }

            //Set max angle
            float maxAngle = 4.0f;

            Transform PointingTransfrom = transform;



            //Get Random direction for bullet
            Vector2 randRot = Random.insideUnitCircle;

            // Get the object I want to spawnz
            FVRObject obj = IM.OD["12GaugeShellFreedomfetti"];

            //Set Object Position
            Vector3 shellPosition0 = GM.CurrentPlayerBody.RightHand.position + (GM.CurrentPlayerBody.RightHand.forward * 0.02f);

            GameObject go0 = Instantiate(obj.GetGameObject(), shellPosition0, Quaternion.LookRotation(GM.CurrentPlayerBody.RightHand.forward));


            //Set Object Direction
            go0.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

            //Detonate Shell?
            FVRFireArmRound cartridge = go0.GetComponent<FVRFireArmRound>();
            cartridge.Splode(0.01f, false, true);

        }

        private void SpawnSkittySubGun()
        {
            GunList.Shuffle();
            MagazineList.Shuffle();
            string TopGun = GunList.ElementAt(0);
            string TopGunTruncated = new string(TopGun.Take(5).ToArray());
            Logger.LogInfo(TopGunTruncated);
            Logger.LogInfo(TopGun);
            string MatchingMagazine = MagazineList.Find(o => o.Contains(TopGunTruncated));
            Logger.LogInfo(MatchingMagazine);

            // Get the object you want to spawn
            FVRObject obj = IM.OD[TopGun];
            FVRObject obj2 = IM.OD[MatchingMagazine];

            // Instantiate (spawn) the object above the player's head
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);
            GameObject go2 = Instantiate(obj2.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            //add some speeeeen
            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));
            go2.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));

            //add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);
            go2.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);

        }

        //we want to spawn a flashbang infront of the player with little notice
        private void SpawnFlash2()
        {
            // Get the object you want to spawn
            FVRObject obj = IM.OD["PinnedGrenadeXM84"];


            // Instantiate (spawn) the object above the player head
            Logger.LogInfo("Spawned Object");
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);


            //prime the flash object
            Logger.LogInfo("Getting Component");
            PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
            Logger.LogInfo("Releasing Lever");
            grenade.ReleaseLever();



            //add force
            Logger.LogInfo("Adding Force");
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 500);
        }
        private void ZeroGravityBumpDown()
        {
            //GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.None;
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.None;
            GM.CurrentSceneSettings.RefreshGravity();
            StartCoroutine(ZeroGWait());
            //Logger.LogInfo("Gravity Is Now " + GM.Options.SimulationOptions.PlayerGravityMode);

        }

        private void ZeroGravityBumpUp()
        {
            //GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.Playful;
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Playful;
            GM.CurrentSceneSettings.RefreshGravity();
            ZeroGStatus = "Off";
            //Logger.LogInfo("Gravity Is Now " + GM.Options.SimulationOptions.PlayerGravityMode);
        }

        private void RealisticFall()
        {
            //GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.CurrentSceneSettings.RefreshGravity();
            //Logger.LogInfo("Gravity Is Now " + GM.Options.SimulationOptions.PlayerGravityMode);
        }

        private void EnableMeatHands()
        {
            GM.CurrentMovementManager.Hands[0].SpawnSausageFingers();
            GM.CurrentMovementManager.Hands[1].SpawnSausageFingers();
        }



        private void DestroyQuickbelt()
        {
            List<FVRQuickBeltSlot> slots = new List<FVRQuickBeltSlot>();

            var field = AccessTools.Field(typeof(FVRQuickBeltSlot), "QBSlots_Internal"); //QBSlots_Internal doesn't exist (?????)

#pragma warning disable CS8602 // Dereference of a possibly null reference.
            foreach (var slot in field.GetValue(GM.CurrentPlayerBody) as List<FVRQuickBeltSlot>)//get all quickbelt slots
{
                if (slot.CurObject != null)
                {
                    slots.Add(slot); //if current quickbelt slot has an object, add it to the list
                }
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            if (slots.Count > 0)
            {
                var randomItemIndex = Random.Range(0, slots.Count - 1); //get random index

  slots[randomItemIndex].CurObject.SetQuickBeltSlot(null); //remove item from quickbelt
            }

        }

        private void SpawnSkittyBigGun()
        {
            GunList.Shuffle();
            MagazineList.Shuffle();
            string TopGun = GunList.ElementAt(0);
            string TopGunTruncated = new string(TopGun.Take(5).ToArray());
            Logger.LogInfo(TopGunTruncated);
            Logger.LogInfo(TopGun);
            string MatchingMagazine = MagazineList.Find(o => o.Contains(TopGunTruncated));
            Logger.LogInfo(MatchingMagazine);

            // Get the object you want to spawn
            FVRObject obj = IM.OD[TopGun];
            FVRObject obj2 = IM.OD[MatchingMagazine];

            // Instantiate (spawn) the object above the player's head
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);
            GameObject go2 = Instantiate(obj2.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            //add some speeeeen
            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));
            go2.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));

            //add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);
            go2.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);
            go.transform.localScale = new Vector3(5, 5, 5 );
            go2.transform.localScale = new Vector3(5, 5, 5);
        }


        [HarmonyPatch(typeof(AudioSource), "pitch", MethodType.Setter)]
            [HarmonyPrefix]
            public static void FixPitch(ref float value)
            {
                if (Time.timeScale != 1f)
                {
                    value *= Time.timeScale;
                }
                else
                {
                    value *= 1f;
                }
            }


        private void OnDestroy()
        {
            _hooks.Unhook();
        }
    }
}




