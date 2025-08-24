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
using System; // Add this namespace for StringSplitOptions


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
        public readonly string filePath = string.Empty;
        // Update the type of GunList and MagazineList to ConfigEntry<string> instead of ConfigEntry<File>
        public ConfigEntry<string> GunList;
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
        private ConfigEntry<KeyCode> KeyToggleFireMode; // new key for toggling held gun fire mode
        private ConfigEntry<KeyCode> KeyRandomizeHeldGun; // key to randomize held gun
        private ConfigEntry<KeyCode> KeyEmptyChamber; // key to empty chamber of held gun
        private ConfigEntry<KeyCode> KeyBoostMalfunction; // new redeem key
        private bool _malfunctionBoostActive;
        private float _malfunctionBoostEndTime;
        private ConfigEntry<float> MalfunctionBoostDurationSeconds; // configurable duration in seconds
        private ConfigEntry<float> MalfunctionBoostDurationMinutes; // optional duration in minutes (overrides seconds if > 0)
        private const float ForcedMalfunctionChance = 0.75f; // 75% each trigger pull during boost

        public ConfigFile FilePath { get; set; }

        public H3TVR()
        {
            _hooks = new Hooks();
            _hooks.Hook();
            Logger.LogInfo("Loading H3TVR");

            // Initialize ConfigFile properly
            FilePath = new ConfigFile("configPath.cfg", true);

            // Update the initialization of GunList and MagazineList to match the correct type
            GunList = Config.Bind("General", "GunList", "DefaultGunList", "List of guns");
            MagazineList = Config.Bind("General", "MagazineList", "DefaultMagazineList", "List of magazines");
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
            KeyToggleFireMode = Config.Bind("General", "KeyBindForToggleHeldGunFireMode", KeyCode.F6, "Key used to toggle fire mode of currently held gun");
            KeyRandomizeHeldGun = Config.Bind("General", "KeyBindForRandomizeHeldGun", KeyCode.F7, "Key used to replace currently held gun with a random one from GunList");
            KeyEmptyChamber = Config.Bind("General", "KeyBindForEmptyHeldGunChamber", KeyCode.F8, "Key used to eject / empty the chambered round of the currently held gun");
            KeyBoostMalfunction = Config.Bind("General", "KeyBindForMeatyceiverMalfunctionBoost", KeyCode.F9, "Redeem: Boost Meatyceiver malfunction chance (uses configured seconds/minutes)");
            MalfunctionBoostDurationSeconds = Config.Bind("General", "MeatyceiverMalfunctionBoostSeconds", 600f, "Fallback duration in seconds (ignored if minutes > 0). Clamped 5 - 3600.");
            MalfunctionBoostDurationMinutes = Config.Bind("General", "MeatyceiverMalfunctionBoostMinutes", 10f, "Primary duration in minutes (set to 0 to use seconds). Clamped 0.0833 - 60.");
        }

        public void Awake()
        {

            Harmony.CreateAndPatchAll(this.GetType());
            Logger.LogInfo("Successfully loaded H3TVR!");
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

            // Trigger slomo ONLY on left-hand X button (AX) press
            if ((GM.CurrentMovementManager != null
                && GM.CurrentMovementManager.Hands != null
                && GM.CurrentMovementManager.Hands.Length > 0
                && GM.CurrentMovementManager.Hands[0].Input.AXButtonDown)
                || Input.GetKeyDown(Key7.Value))
            {
                Logger.LogInfo("Detected Left X Button Press!");
                SlomoStatus = "Slowing";
            }

            if (SlomoStatus == "Slowing")
            {
                Logger.LogInfo("Slowing!");
                // Fix for CS1525: Invalid expression term '||'
                // The issue is likely caused by a misplaced closing parenthesis in the following condition.
                // Correcting the condition by moving the closing parenthesis to the correct position.

                if (GM.CurrentMovementManager != null
                    && GM.CurrentMovementManager.Hands != null
                    && GM.CurrentMovementManager.Hands.Length > 0
                    && (GM.CurrentMovementManager.Hands[0].Input.AXButtonDown || Input.GetKeyDown(Key7.Value)))
                {
                    Logger.LogInfo("Detected Left X Button Press!");
                    SlomoStatus = "Slowing";
                }
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
            // Toggle fire mode of currently held gun
            if (Input.GetKeyDown(KeyToggleFireMode.Value))
            {
                ToggleHeldGunFireMode();
            }
            // Randomize currently held gun
            if (Input.GetKeyDown(KeyRandomizeHeldGun.Value))
            {
                RandomizeHeldGun();
            }
            if (Input.GetKeyDown(KeyEmptyChamber.Value))
            {
                EmptyHeldGunChamber();
            }
            if (Input.GetKeyDown(KeyBoostMalfunction.Value))
            {
                ActivateMalfunctionBoost();
            }
            if (_malfunctionBoostActive)
            {
                if (Time.time >= _malfunctionBoostEndTime)
                {
                    _malfunctionBoostActive = false;
                    Logger.LogInfo("Meatyceiver malfunction boost ended.");
                }
                else
                {
                    ApplyMalfunctionLogic();
                }
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
            const string key = "JediTippyToy"; // original desired key
            string? foundKey = null; // Use nullable string type

            // 1. Exact match (and not null)
            if (IM.OD.ContainsKey(key) && IM.OD[key] != null)
            {
                foundKey = key;
            }
            else
            {
                // 2. Case-insensitive exact
                foundKey = IM.OD.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase) && IM.OD[k] != null);
                // 3. Starts with prefix 'Jedi'
                if (foundKey == null)
                    foundKey = IM.OD.Keys.FirstOrDefault(k => k.StartsWith("Jedi", StringComparison.OrdinalIgnoreCase) && IM.OD[k] != null);
                // 4. Contains fragment 'Tippy'
                if (foundKey == null)
                    foundKey = IM.OD.Keys.FirstOrDefault(k => k.IndexOf("Tippy", StringComparison.OrdinalIgnoreCase) >= 0 && IM.OD[k] != null);
            }

            if (foundKey == null)
            {
                // Show a larger sample to aid debugging
                var sample = string.Join(", ", IM.OD.Keys.Take(15).ToArray());
                Logger.LogError($"SpawnJeditToy: Key '{key}' not found. Sample available keys: {sample}");
                return;
            }

            FVRObject obj = IM.OD[foundKey];
            GameObject go = Instantiate(obj.GetGameObject(),
                GM.CurrentPlayerBody.Head.position + new Vector3(0f, .25f, 0f),
                GM.CurrentPlayerBody.Head.rotation);

            var rb = go.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddTorque(new Vector3(.25f, .25f, .25f));
                rb.AddForce(GM.CurrentPlayerBody.Head.forward * 25);
            }
            else
            {
                Logger.LogWarning("SpawnJeditToy: Spawned object has no Rigidbody (key resolved to '" + foundKey + "').");
            }
            Logger.LogInfo("SpawnJeditToy: Spawned object with resolved key '" + foundKey + "'.");
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
            Vector2 randRot = UnityEngine.Random.insideUnitCircle;

            // Random number for pull chance
            int pullChance = UnityEngine.Random.Range(1, 20);
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
            Vector2 randRot = UnityEngine.Random.insideUnitCircle;

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
            Vector2 randRot = UnityEngine.Random.insideUnitCircle;

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
            Vector2 randRot = UnityEngine.Random.insideUnitCircle;

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

        // Fix for CS7003: Unexpected use of an unbound generic name
        // The issue is that the generic type `List<>` is missing its type argument. 
        // Based on the context, it should be `List<string>` since `GunList` and `MagazineList` are strings being split into lists.

        public void SpawnSkittySubGun()
        {
            string gunListString;
            if (File.Exists(GunList.Value))
            {
                using (StreamReader gunListReader = new StreamReader(GunList.Value))
                {
                    gunListString = gunListReader.ReadToEnd();
                }
            }
            else
            {
                // Treat the config value itself as the list
                gunListString = GunList.Value;
            }

            // Support space, tab, semicolon etc. separated lists as well
            string[] gunList = gunListString
                .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .Where(g => g.Length > 0)
                .ToArray();

            if (gunList.Length == 0)
            {
                Logger.LogError("Gun list is empty after parsing.");
                return;
            }

            // Pick a random gun from the parsed list
            int randomGunIndex = UnityEngine.Random.Range(0, gunList.Length);
            string selectedGun = gunList[randomGunIndex];
            string selectedGunTruncated = new string(selectedGun.Take(5).ToArray());
            Logger.LogInfo($"Random Gun Index: {randomGunIndex} / {gunList.Length - 1}");
            Logger.LogInfo("SelectedGun: " + selectedGun);
            Logger.LogInfo("SelectedGunTruncated: " + selectedGunTruncated);

            string magazineListString;
            if (File.Exists(MagazineList.Value))
            {
                using (StreamReader magazineListReader = new StreamReader(MagazineList.Value))
                {
                    magazineListString = magazineListReader.ReadToEnd();
                }
            }
            else
            {
                magazineListString = MagazineList.Value;
            }

            string[] magazineList = magazineListString
                .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => m.Length > 0)
                .ToArray();

            // Find all magazines containing the truncated gun key portion, pick one at random
            var matchingMagazines = magazineList.Where(o => o.Contains(selectedGunTruncated)).ToArray();
            string selectedMagazine = string.Empty;
            if (matchingMagazines.Length > 0)
            {
                int randomMagIndex = UnityEngine.Random.Range(0, matchingMagazines.Length);
                selectedMagazine = matchingMagazines[randomMagIndex];
                Logger.LogInfo($"Random Magazine Index: {randomMagIndex} / {matchingMagazines.Length - 1}");
            }

            Logger.LogInfo("SelectedMagazine: " + selectedMagazine);

            if (!IM.OD.ContainsKey(selectedGun))
            {
                Logger.LogError("Gun key '" + selectedGun + "' not found in IM.OD dictionary.");
                return;
            }
            if (string.IsNullOrEmpty(selectedMagazine) || !IM.OD.ContainsKey(selectedMagazine))
            {
                Logger.LogError("Matching magazine not found for gun '" + selectedGun + "'.");
                return;
            }

            FVRObject obj = IM.OD[selectedGun];
            FVRObject obj2 = IM.OD[selectedMagazine];

            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);
            GameObject go2 = Instantiate(obj2.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));
            go2.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));

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
            try
            {
                FVRQuickBeltSlot[] allSlots = UnityEngine.Object.FindObjectsOfType<FVRQuickBeltSlot>();
                if (allSlots == null || allSlots.Length == 0)
                {
                    Logger.LogInfo("No quickbelt slots found in scene.");
                    return;
                }

                int droppedCount = 0;
                foreach (var slot in allSlots)
                {
                    var obj = slot?.CurObject;
                    if (obj == null) continue;

                    // Detach from slot
                    obj.SetQuickBeltSlot(null);

                    // Enable / adjust physics so it actually drops
                    var rb = obj.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = false; // ensure physics
                        rb.velocity = GM.CurrentPlayerBody.Head.forward * 1.5f + UnityEngine.Random.insideUnitSphere * 0.25f;
                        rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 2f;
                    }
                    droppedCount++;
                }

                Logger.LogInfo($"Dropped {droppedCount} quickbelt object(s).");
            }
            catch (System.Exception ex)
            {
                Logger.LogError("DestroyQuickbelt drop failed: " + ex);
            }
        }

        // Fix for CS1955: Non-invocable member 'H3TVR.GunList' cannot be used like a method.
        // The issue is that 'GunList' is a ConfigEntry<string>, not a method. 
        // To fix this, we need to access its 'Value' property instead of trying to invoke it as a method.

        public void SpawnSkittyBigGun()
        {
            try
            {
                // Read gun list (can be path or inline list)
                string gunListString = File.Exists(GunList.Value) ? File.ReadAllText(GunList.Value) : GunList.Value;
                string[] gunList = gunListString
                    .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .Where(g => g.Length > 0)
                    .ToArray();
                if (gunList.Length == 0)
                {
                    Logger.LogError("SpawnSkittyBigGun: Gun list empty.");
                    return;
                }

                // Filter to guns that actually exist in IM.OD, then pick one at random
                var validGuns = gunList.Where(k => IM.OD.ContainsKey(k)).ToArray();
                if (validGuns.Length == 0)
                {
                    Logger.LogError("SpawnSkittyBigGun: None of the provided gun keys exist in IM.OD.");
                    return;
                }
                string topGun = validGuns[UnityEngine.Random.Range(0, validGuns.Length)];

                string truncated = new string(topGun.Take(5).ToArray());
                Logger.LogInfo("SpawnSkittyBigGun PickedGun: " + topGun + " (Trunc: " + truncated + ")");

                // Read magazine list (path or inline)
                string magazineListString = File.Exists(MagazineList.Value) ? File.ReadAllText(MagazineList.Value) : MagazineList.Value;
                string[] magazineList = magazineListString
                    .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(m => m.Trim())
                    .Where(m => m.Length > 0)
                    .ToArray();

                string matchingMagazine = string.Empty;
                if (magazineList.Length > 0)
                {
                    // Priority tiers for matching
                    // 1. Contains full 5-char truncated key
                    var tier1 = magazineList.Where(m => m.Contains(truncated)).ToArray();
                    // 2. Starts with first 5
                    var tier2 = magazineList.Where(m => m.StartsWith(truncated)).ToArray();
                    // 3. Contains first 4 chars
                    string short4 = truncated.Length >= 4 ? truncated.Substring(0, 4) : truncated;
                    var tier3 = magazineList.Where(m => m.Contains(short4)).ToArray();
                    // 4. Any mag that exists in dictionary whose first 3 chars match
                    string short3 = truncated.Length >= 3 ? truncated.Substring(0, 3) : truncated;
                    var tier4 = magazineList.Where(m => m.StartsWith(short3) && IM.OD.ContainsKey(m)).ToArray();

                    string PickRandom(string[] arr) => arr.Length == 0 ? null : arr[UnityEngine.Random.Range(0, arr.Length)];

                    matchingMagazine = PickRandom(tier1)
                        ?? PickRandom(tier2)
                        ?? PickRandom(tier3)
                        ?? PickRandom(tier4)
                        ?? magazineList.FirstOrDefault(m => IM.OD.ContainsKey(m));
                }

                if (!IM.OD.ContainsKey(topGun))
                {
                    Logger.LogError("SpawnSkittyBigGun: Gun key '" + topGun + "' not in IM.OD.");
                    return;
                }

                if (string.IsNullOrEmpty(matchingMagazine) || !IM.OD.ContainsKey(matchingMagazine))
                {
                    Logger.LogWarning("SpawnSkittyBigGun: No matching magazine found for gun '" + topGun + "'. Spawning gun only.");
                    matchingMagazine = null; // ensure null for spawn logic
                }
                else
                {
                    Logger.LogInfo("SpawnSkittyBigGun MatchingMagazine: " + matchingMagazine);
                }

                // Spawn gun
                FVRObject gunObj = IM.OD[topGun];
                Vector3 spawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, .25f, 0f);
                Quaternion spawnRot = GM.CurrentPlayerBody.Head.rotation;
                GameObject gunGO = Instantiate(gunObj.GetGameObject(), spawnPos, spawnRot);
                var gunRb = gunGO.GetComponent<Rigidbody>();
                if (gunRb != null)
                {
                    gunRb.AddTorque(new Vector3(.25f, .25f, .25f));
                    gunRb.AddForce(GM.CurrentPlayerBody.Head.forward * 100f, ForceMode.VelocityChange);
                }

                // Scale ONLY the gun
                gunGO.transform.localScale = new Vector3(5f, 5f, 5f);

                // Spawn magazine if available (no scaling to avoid physics / alignment issues)
                if (matchingMagazine != null)
                {
                    FVRObject magObj = IM.OD[matchingMagazine];
                    GameObject magGO = Instantiate(magObj.GetGameObject(), spawnPos, spawnRot);
                    var magRb = magGO.GetComponent<Rigidbody>();
                    if (magRb != null)
                    {
                        magRb.AddTorque(new Vector3(.25f, .25f, .25f));
                        magRb.AddForce(GM.CurrentPlayerBody.Head.forward * 100f, ForceMode.VelocityChange);
                    }
                    // Scale magazine as requested
                    magGO.transform.localScale = new Vector3(5f, 5f, 5f);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("SpawnSkittyBigGun failed: " + ex);
            }
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

        private void ToggleHeldGunFireMode()
        {
            try // Add missing try block to fix the unbalanced braces
            {
                // Prefer right hand, then left
                FVRViveHand[] hands = GM.CurrentMovementManager != null ? GM.CurrentMovementManager.Hands : null;
                if (hands == null || hands.Length == 0) return;

                FVRInteractiveObject inter = null;
                if (hands.Length > 1 && hands[1] != null && hands[1].CurrentInteractable != null)
                    inter = hands[1].CurrentInteractable;
                if (inter == null && hands[0] != null && hands[0].CurrentInteractable != null)
                    inter = hands[0].CurrentInteractable;
                if (inter == null) return;

                // Ensure it's a firearm
                var firearm = inter as FVRFireArm;
                if (firearm == null && inter.GetType().IsSubclassOf(typeof(FVRFireArm)))
                {
                    firearm = (FVRFireArm)inter;
                }
                if (firearm == null) return;

                // Try common method names first
                MethodInfo mi = firearm.GetType().GetMethod("CycleFireMode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? firearm.GetType().GetMethod("CycleFireSelector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mi != null)
                {
                    mi.Invoke(firearm, null);
                    Logger.LogInfo("Toggled fire mode via method reflection.");
                    return;
                }

                // Fallback: manipulate selector enum field and call potential setter
                FieldInfo selectorField = firearm.GetType().GetField("m_fireSelector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? firearm.GetType().GetField("FireSelector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? firearm.GetType().GetField("m_selector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (selectorField != null)
                {
                    object currentVal = selectorField.GetValue(firearm);
                    if (currentVal != null && currentVal.GetType().IsEnum)
                    {
                        Array vals = Enum.GetValues(currentVal.GetType());
                        int idx = Array.IndexOf(vals, currentVal);
                        int next = (idx + 1) % vals.Length;
                        object nextVal = vals.GetValue(next);
                        selectorField.SetValue(firearm, nextVal);

                        MethodInfo setMethod = firearm.GetType().GetMethod("SetFireSelector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                            ?? firearm.GetType().GetMethod("UpdateFireSelector", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (setMethod != null)
                        {
                            var ps = setMethod.GetParameters();
                            if (ps.Length == 1 && ps[0].ParameterType == currentVal.GetType())
                                setMethod.Invoke(firearm, new object[] { nextVal });
                            else if (ps.Length == 0)
                                setMethod.Invoke(firearm, null);
                        }
                        Logger.LogInfo("Toggled fire mode via enum field reflection to: " + nextVal);
                        return;
                    }
                }

                Logger.LogWarning("Could not toggle fire mode: no suitable method or field found.");
            }
            catch (Exception ex)
            {
                Logger.LogError("ToggleHeldGunFireMode failed: " + ex);
            }
        }

        private void RandomizeHeldGun()
        {
            try
            {
                FVRViveHand[] hands = GM.CurrentMovementManager != null ? GM.CurrentMovementManager.Hands : null;
                if (hands == null || hands.Length == 0) return;

                FVRInteractiveObject inter = null;
                if (hands.Length > 1 && hands[1] != null && hands[1].CurrentInteractable != null)
                    inter = hands[1].CurrentInteractable;
                if (inter == null && hands[0] != null && hands[0].CurrentInteractable != null)
                    inter = hands[0].CurrentInteractable;
                if (inter == null) return;

                var firearm = inter as FVRFireArm;
                if (firearm == null && inter.GetType().IsSubclassOf(typeof(FVRFireArm)))
                    firearm = (FVRFireArm)inter;
                if (firearm == null) return;

                string gunListString = File.Exists(GunList.Value)
                    ? File.ReadAllText(GunList.Value)
                    : GunList.Value;
                string[] gunList = gunListString
                    .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .Where(g => g.Length > 0)
                    .ToArray();
                if (gunList.Length == 0) { Logger.LogError("RandomizeHeldGun: Gun list empty."); return; }

                string currentKey = firearm.ObjectWrapper != null ? firearm.ObjectWrapper.ItemID : null;
                string[] selectable = currentKey != null ? gunList.Where(k => k != currentKey).ToArray() : gunList;
                if (selectable.Length == 0) selectable = gunList;

                string newGunKey = selectable[UnityEngine.Random.Range(0, selectable.Length)];
                if (!IM.OD.ContainsKey(newGunKey)) { Logger.LogError("RandomizeHeldGun: Key '" + newGunKey + "' not found in IM.OD."); return; }

                Vector3 pos = inter.transform.position;
                Quaternion rot = inter.transform.rotation;
                Destroy(inter.gameObject);

                FVRObject gunObj = IM.OD[newGunKey];
                GameObject newGunGO = Instantiate(gunObj.GetGameObject(), pos, rot);
                var gunRB = newGunGO.GetComponent<Rigidbody>();
                if (gunRB != null) { gunRB.velocity = Vector3.zero; gunRB.angularVelocity = Vector3.zero; }

                try
                {
                    string magListString = File.Exists(MagazineList.Value)
                        ? File.ReadAllText(MagazineList.Value)
                        : MagazineList.Value;
                    string[] magazineList = magListString
                        .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(m => m.Trim())
                        .Where(m => m.Length > 0)
                        .ToArray();
                    if (magazineList.Length > 0)
                    {
                        string truncated = new string(newGunKey.Take(5).ToArray());
                        var matchingMags = magazineList.Where(m => m.Contains(truncated)).ToArray();
                        if (matchingMags.Length > 0)
                        {
                            string magKey = matchingMags[UnityEngine.Random.Range(0, matchingMags.Length)];
                            if (IM.OD.ContainsKey(magKey))
                            {
                                FVRObject magObj = IM.OD[magKey];
                                Vector3 magPos = pos + Vector3.up * 0.05f + (GM.CurrentPlayerBody != null ? GM.CurrentPlayerBody.Head.forward * 0.1f : Vector3.forward * 0.1f);
                                GameObject magGO = Instantiate(magObj.GetGameObject(), magPos, rot);
                                var magRB = magGO.GetComponent<Rigidbody>();
                                if (magRB != null) { magRB.velocity = Vector3.zero; magRB.angularVelocity = Vector3.zero; }
                                Logger.LogInfo("Spawned matching magazine: " + magKey);
                            }
                            else Logger.LogWarning("RandomizeHeldGun: Matching mag key not in IM.OD: " + magKey);
                        }
                        else Logger.LogWarning("RandomizeHeldGun: No matching magazines found for truncated key: " + truncated);
                    }
                    else Logger.LogWarning("RandomizeHeldGun: Magazine list empty.");
                }
                catch (Exception magEx) { Logger.LogError("RandomizeHeldGun: Magazine spawn failed: " + magEx); }

                Logger.LogInfo("Replaced held gun with random gun: " + newGunKey);
            }
            catch (Exception ex)
            {
                Logger.LogError("RandomizeHeldGun failed: " + ex);
            }
        }

        private void EmptyHeldGunChamber()
        {
            try
            {
                FVRViveHand[] hands = GM.CurrentMovementManager != null ? GM.CurrentMovementManager.Hands : null;
                if (hands == null || hands.Length == 0) return;
                FVRInteractiveObject inter = null;
                if (hands.Length > 1 && hands[1] != null && hands[1].CurrentInteractable != null) inter = hands[1].CurrentInteractable;
                if (inter == null && hands[0] != null && hands[0].CurrentInteractable != null) inter = hands[0].CurrentInteractable;
                if (inter == null) return;

                var firearm = inter as FVRFireArm;
                if (firearm == null && inter.GetType().IsSubclassOf(typeof(FVRFireArm))) firearm = (FVRFireArm)inter;
                if (firearm == null) return;

                string[] methodNames = { "EjectChamberedRound", "EjectRound", "EjectChambered", "Eject", "ExtractRound", "DumpChamber" };
                foreach (var mn in methodNames)
                {
                    MethodInfo mi = firearm.GetType().GetMethod(mn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mi != null && mi.GetParameters().Length == 0) { mi.Invoke(firearm, null); Logger.LogInfo("EmptyHeldGunChamber: Invoked method " + mn); return; }
                }

                object chamberObj = firearm.GetType().GetField("Chamber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(firearm)
                    ?? firearm.GetType().GetField("m_chamber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(firearm)
                    ?? firearm.GetType().GetField("PrimaryChamber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(firearm);
                if (chamberObj == null)
                {
                    PropertyInfo chamberProp = firearm.GetType().GetProperty("Chamber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? firearm.GetType().GetProperty("PrimaryChamber", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (chamberProp != null) chamberObj = chamberProp.GetValue(firearm, null);
                }
                if (chamberObj == null) { Logger.LogWarning("EmptyHeldGunChamber: No chamber object found via reflection."); return; }

                FVRFireArmRound round = null;
                Type chamberType = chamberObj.GetType();
                string[] roundNames = { "Round", "m_round", "ChamberedRound", "m_chamberedRound", "LoadedRound" };
                foreach (var rn in roundNames)
                {
                    FieldInfo rf = chamberType.GetField(rn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (rf != null) { round = rf.GetValue(chamberObj) as FVRFireArmRound; if (round != null) break; }
                    PropertyInfo rp = chamberType.GetProperty(rn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (rp != null) { round = rp.GetValue(chamberObj, null) as FVRFireArmRound; if (round != null) break; }
                }
                if (round == null)
                {
                    FieldInfo anyRoundField = chamberType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .FirstOrDefault(f => typeof(FVRFireArmRound).IsAssignableFrom(f.FieldType));
                    if (anyRoundField != null) round = anyRoundField.GetValue(chamberObj) as FVRFireArmRound;
                }
                if (round == null || round.gameObject == null) { Logger.LogWarning("EmptyHeldGunChamber: No round found in chamber."); return; }

                Transform rT = round.transform; rT.parent = null;
                var rrb = round.GetComponent<Rigidbody>();
                if (rrb != null)
                {
                    rrb.isKinematic = false;
                    rrb.velocity = firearm.transform.forward * 1.5f + firearm.transform.up * 0.25f;
                    rrb.angularVelocity = UnityEngine.Random.insideUnitSphere * 5f;
                }

                foreach (var rn in roundNames)
                {
                    FieldInfo rf = chamberType.GetField(rn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (rf != null && rf.FieldType.IsAssignableFrom(typeof(FVRFireArmRound))) rf.SetValue(chamberObj, null);
                    PropertyInfo rp = chamberType.GetProperty(rn, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (rp != null && rp.CanWrite && rp.PropertyType.IsAssignableFrom(typeof(FVRFireArmRound))) rp.SetValue(chamberObj, null, null);
                }
                Logger.LogInfo("EmptyHeldGunChamber: Ejected chambered round.");
            }
            catch (Exception ex)
            {
                Logger.LogError("EmptyHeldGunChamber failed: " + ex);
            }
        }

        private void ActivateMalfunctionBoost()
        {
            _malfunctionBoostActive = true;
            // Determine raw duration (minutes takes precedence if > 0)
            float minutes = MalfunctionBoostDurationMinutes.Value;
            // Clamp minutes if supplied (>0)
            if (minutes > 0f)
            {
                minutes = Mathf.Clamp(minutes, 0.0833f, 60f); // 0.0833m ~5s, up to 60m
            }
            float secondsConfigured = MalfunctionBoostDurationSeconds.Value;
            secondsConfigured = Mathf.Clamp(secondsConfigured, 5f, 3600f); // 5s to 1h
            float appliedSeconds = minutes > 0f ? minutes * 60f : secondsConfigured;
            _malfunctionBoostEndTime = Time.time + appliedSeconds;
            Logger.LogInfo($"Meatyceiver malfunction boost activated for {appliedSeconds:F1} seconds (configured Minutes={MalfunctionBoostDurationMinutes.Value}, Seconds={MalfunctionBoostDurationSeconds.Value}).");
        }

        private void ApplyMalfunctionLogic()
        {
            try
            {
                var mm = GM.CurrentMovementManager;
                if (mm == null || mm.Hands == null) return;
                foreach (var hand in mm.Hands)
                {
                    if (hand == null || hand.CurrentInteractable == null) continue;
                    var firearm = hand.CurrentInteractable as FVRFireArm;
                    if (firearm == null && hand.CurrentInteractable.GetType().IsSubclassOf(typeof(FVRFireArm))) firearm = (FVRFireArm)hand.CurrentInteractable;
                    if (firearm == null) continue;

                    string id = null; try { if (firearm.ObjectWrapper != null) id = firearm.ObjectWrapper.ItemID; } catch { }
                    string name = firearm.gameObject != null ? firearm.gameObject.name : string.Empty;
                    bool isMeaty = (!string.IsNullOrEmpty(id) && id.IndexOf("meaty", StringComparison.OrdinalIgnoreCase) >= 0) ||
                                   (!string.IsNullOrEmpty(name) && name.IndexOf("meaty", StringComparison.OrdinalIgnoreCase) >= 0);
                    if (!isMeaty) continue;

                    if (hand.Input.TriggerDown && UnityEngine.Random.value < ForcedMalfunctionChance)
                        ForceMalfunction(firearm);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ApplyMalfunctionLogic failed: " + ex);
            }
        }

        private void ForceMalfunction(FVRFireArm firearm)
        {
            try
            {
                string[] methods = { "ForceMalfunction", "DoMalfunction", "AttemptMalfunction", "Jam", "CauseMalfunction" };
                foreach (var m in methods)
                {
                    var mi = firearm.GetType().GetMethod(m, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mi != null && mi.GetParameters().Length == 0) { mi.Invoke(firearm, null); Logger.LogInfo("Forced malfunction via method: " + m); return; }
                }
                string[] fields = { "MalfunctionChance", "m_malfunctionChance", "JamChance", "m_jamChance" };
                foreach (var f in fields)
                {
                    var fi = firearm.GetType().GetField(f, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (fi != null && (fi.FieldType == typeof(float) || fi.FieldType == typeof(double)))
                    {
                        if (fi.FieldType == typeof(float)) fi.SetValue(firearm, 1f); else fi.SetValue(firearm, (double)1.0);
                        Logger.LogInfo("Set high malfunction/jam chance via field: " + f);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ForceMalfunction reflection failed: " + ex);
            }
        }
    }
}
