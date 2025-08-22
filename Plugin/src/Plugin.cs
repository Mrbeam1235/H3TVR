using BepInEx;
using BepInEx.Configuration;
using FistVR;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public string filePath = string.Empty;
        private ConfigEntry<string> GunList;
        public ConfigEntry<string> MagazineList;
        private ConfigEntry<KeyCode> Key0;
        private ConfigEntry<KeyCode> Key1;
        private ConfigEntry<KeyCode> Key2;
        private ConfigEntry<KeyCode> Key3;
        private ConfigEntry<KeyCode> Key4;
        private ConfigEntry<KeyCode> Key5;
        private ConfigEntry<KeyCode> Key6;
        private ConfigEntry<KeyCode> Key7;
        private ConfigEntry<KeyCode> Key8;
        private ConfigEntry<KeyCode> Key9;
        private ConfigEntry<KeyCode> Key10;
        private ConfigEntry<KeyCode> Key11;
        private ConfigEntry<KeyCode> Key12;
        private ConfigEntry<KeyCode> Key13;
        private ConfigEntry<KeyCode> Key14;
        private ConfigEntry<KeyCode> Key15;

        public H3TVR()
        {
            _hooks = new Hooks();
            _hooks.Hook();
            Logger.LogInfo("Loading H3TVR");
        }

        public void Awake()
        {

            Harmony.CreateAndPatchAll(this.GetType());
            Logger.LogInfo("Successfully loaded H3TVR!");

            // Initialize non-nullable fields with default values
            GunList = Config.Bind("General", "GunList", string.Empty, "List of guns");
            MagazineList = Config.Bind("General", "MagazineList", string.Empty, "List of magazines");

            // Initialize Key bindings with default values
            Key0 = Config.Bind("General", "KeyBindForWonderToy", KeyCode.Keypad0, "The key used to spawn WonderToy");
            Key1 = Config.Bind("General", "KeyBindForPillow", KeyCode.Keypad1, "The key used to spawn Pillow");
            Key2 = Config.Bind("General", "KeyBindForFlash", KeyCode.Keypad2, "The key used to spawn Flash");
            Key3 = Config.Bind("General", "KeyBindForShuri", KeyCode.Keypad3, "The key used to spawn Shuri");
            Key4 = Config.Bind("General", "KeyBindForNadeRain", KeyCode.Keypad4, "The key used to spawn Nade Rain");
            Key5 = Config.Bind("General", "KeyBindForHydration", KeyCode.Keypad5, "The key used to spawn Hydration");
            Key6 = Config.Bind("General", "KeyBindForJeditToy", KeyCode.Keypad6, "The key used to spawn Jedit Toy");
            Key7 = Config.Bind("General", "KeyBindForSlomo", KeyCode.Keypad7, "The key used to trigger Slomo");
            Key8 = Config.Bind("General", "KeyBindForDestroyHeld", KeyCode.Keypad8, "The key used to destroy held object");
            Key9 = Config.Bind("General", "KeyBindForSkittySubGun", KeyCode.Keypad9, "The key used to spawn Skitty Sub Gun");
            Key10 = Config.Bind("General", "KeyBindForZeroGravity", KeyCode.KeypadMinus, "The key used to toggle Zero Gravity");
            Key11 = Config.Bind("General", "KeyBindForMeatHands", KeyCode.KeypadPlus, "The key used to enable Meat Hands");
            Key12 = Config.Bind("General", "KeyBindForDangerClose", KeyCode.F1, "The key used for Danger Close Barrage");
            Key13 = Config.Bind("General", "KeyBindForFlash2", KeyCode.F2, "The key used to spawn Flash2");
            Key14 = Config.Bind("General", "KeyBindForDestroyQuickbelt", KeyCode.F3, "The key used to destroy Quickbelt");
            Key15 = Config.Bind("General", "KeyBindForSkittyBigGun", KeyCode.F4, "The key used to spawn Skitty Big Gun");
        }

        public void Update()
        {
            //wonderful toy spawn
            if (Input.GetKeyDown(Key0.Value))

            {
                SpawnWonderfulToy();
            }

            //body pillow spawn
            if (Input.GetKeyDown(Key1.Value))

            {
                SpawnPillow();
            }

            //flash spawn
            if (Input.GetKeyDown(Key2.Value))
            {
                SpawnFlash();
            }

            //shuri spawn
            if (Input.GetKey(Key3.Value))
            {
                SpawnShuri();
            }

            //nade spawn
            if (Input.GetKeyDown(Key4.Value))
            {
                SpawnNadeRain();
            }

            //hydration spawn
            if (Input.GetKeyDown(Key5.Value))
            {
                SpawnHydration();
            }

            //jedit tt spawn
            if (Input.GetKeyDown(Key6.Value))
            {
                SpawnJeditToy();
            }

            if (GM.CurrentMovementManager.Hands[1].Input.AXButtonDown || Input.GetKeyDown(Key7.Value))
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

            if (Input.GetKeyDown(Key8.Value))
            {
                DestroyHeld();
            }

            if (Input.GetKeyDown(Key9.Value))
            {
                SpawnSkittySubGun();
            }

            if (Input.GetKeyDown(Key10.Value))
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

            if (Input.GetKeyDown(Key11.Value))
            {
                EnableMeatHands();
            }

            if (Input.GetKey(Key12.Value))
            {
                DangerCloseBarrage();
            }


            if (Input.GetKeyDown(Key13.Value))
            {
                SpawnFlash2();
            }

            if (Input.GetKeyDown(Key14.Value))
            {
                DestroyQuickbelt();

            }
           
            if (Input.GetKeyDown(Key15.Value))
            {
                SpawnSkittyBigGun();
            }
        }


        public void SpawnWonderfulToy()
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

        public void SpawnJeditToy()
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

        public void SpawnPillow()
        {

            // Get the object you want to spawn
            FVRObject obj = IM.OD["BodyPillow"];


            // Instantiate (spawn) the object above the player head
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);


            //add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 4000);
        }



        //we want to spawn a flashbang infront of the player with little notice
        public void SpawnFlash()
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

        public void SpawnNadeRain()
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

        public void SpawnShuri()

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

        public void DangerCloseBarrage()

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

        public void SlomoScaleDown()
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

        public void SlomoReturn()
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

        public void SpawnHydration()
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

        public void DestroyHeld()

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

        public void SpawnSkittySubGun()
        {
            // Convert the ConfigEntry<string> to a List<string>
            List<string> gunList = GunList.Value.Split(',').ToList(); 
            List<string> magazineList = MagazineList.Value.Split(',').ToList(); 

            gunList.Shuffle(); // Shuffle the gun list
            magazineList.Shuffle(); // Shuffle the magazine list

            string TopGun = gunList.ElementAt(0);
            string TopGunTruncated = new string(TopGun.Take(5).ToArray());
            Logger.LogInfo(TopGunTruncated);
            Logger.LogInfo(TopGun);
            string MatchingMagazine = magazineList.Find(o => o.Contains(TopGunTruncated));
            Logger.LogInfo(MatchingMagazine);

            // Get the object you want to spawn
            FVRObject obj = IM.OD[TopGun];
            FVRObject obj2 = IM.OD[MatchingMagazine];

            // Instantiate (spawn) the object above the player's head
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);
            GameObject go2 = Instantiate(obj2.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            // Add some spin
            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));
            go2.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));

            // Add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);
            go2.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);
        }

        //we want to spawn a flashbang infront of the player with little notice
        public void SpawnFlash2()
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
        public void ZeroGravityBumpDown()
        {
            //GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.None;
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.None;
            GM.CurrentSceneSettings.RefreshGravity();
            StartCoroutine(ZeroGWait());
            //Logger.LogInfo("Gravity Is Now " + GM.Options.SimulationOptions.PlayerGravityMode);

        }

        public void ZeroGravityBumpUp()
        {
            //GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.Playful;
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Playful;
            GM.CurrentSceneSettings.RefreshGravity();
            ZeroGStatus = "Off";
            //Logger.LogInfo("Gravity Is Now " + GM.Options.SimulationOptions.PlayerGravityMode);
        }

        public void RealisticFall()
        {
            //GM.Options.SimulationOptions.PlayerGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.CurrentSceneSettings.RefreshGravity();
            //Logger.LogInfo("Gravity Is Now " + GM.Options.SimulationOptions.PlayerGravityMode);
        }

        public void EnableMeatHands()
        {
            GM.CurrentMovementManager.Hands[0].SpawnSausageFingers();
            GM.CurrentMovementManager.Hands[1].SpawnSausageFingers();
        }



        public void DestroyQuickbelt()
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

        // Fix for CS1955: Non-invocable member 'H3TVR.GunList' cannot be used like a method.
        // The issue is that 'GunList' is a ConfigEntry<string>, not a method. 
        // To fix this, we need to access its 'Value' property instead of trying to invoke it as a method.

        public void SpawnSkittyBigGun()
        {
            // Assuming GunList and MagazineList are ConfigEntry<string>, we need to access their 'Value' property.
            List<string> gunList = GunList.Value.Split(',').ToList(); // Convert the ConfigEntry<string> to a List<string>
            List<string> magazineList = MagazineList.Value.Split(',').ToList(); // Convert the ConfigEntry<string> to a List<string>

            gunList.Shuffle(); // Shuffle the gun list
            magazineList.Shuffle(); // Shuffle the magazine list

            string TopGun = gunList.ElementAt(0);
            string TopGunTruncated = new string(TopGun.Take(5).ToArray());
            Logger.LogInfo(TopGunTruncated);
            Logger.LogInfo(TopGun);
            string MatchingMagazine = magazineList.Find(o => o.Contains(TopGunTruncated));
            Logger.LogInfo(MatchingMagazine);

            // Get the object you want to spawn
            FVRObject obj = IM.OD[TopGun];
            FVRObject obj2 = IM.OD[MatchingMagazine];

            // Instantiate (spawn) the object above the player's head
            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);
            GameObject go2 = Instantiate(obj2.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            // Add some spin
            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));
            go2.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));

            // Add force
            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);
            go2.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);

            // Scale up for "Big Gun"
            go.transform.localScale = new Vector3(5, 5, 5);
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







