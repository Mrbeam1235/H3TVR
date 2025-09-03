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
        
        // Configurable slomo parameters
        private ConfigEntry<float> MaxSlomo;
        private ConfigEntry<float> SlomoWaitTime;
        private ConfigEntry<float> SlomoScaleSpeed;
        private ConfigEntry<float> SlomoReturnSpeed;
        private ConfigEntry<bool> SlomoVRControllerEnabled;
        private ConfigEntry<string> SlomoVRButton;
        
        // Configurable gun randomization
        private ConfigEntry<bool> UseItemManagerForGunRandomization;
        
        // Configurable shuriken settings
        private ConfigEntry<float> ShurikenScale;
        
        // Movement scaling during slomo
        private ConfigEntry<bool> SlomoAffectsMovement;
        private ConfigEntry<float> SlomoMovementScale;
        
        // Slomo movement controller
        private SlomoMovementController slomoMovementController;
        
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
        private const float MalfunctionBoostDuration = 120f; // 2 minutes
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
            
            // Slomo configuration
            MaxSlomo = Config.Bind("Slomo", "MaxSlowmoScale", 0.1f, "Maximum slomo scale (0.01 = 1% speed, 0.1 = 10% speed)");
            SlomoWaitTime = Config.Bind("Slomo", "WaitTime", 2f, "Time to wait at max slomo before returning to normal speed");
            SlomoScaleSpeed = Config.Bind("Slomo", "ScaleDownSpeed", 1f, "Speed at which time slows down (higher = faster transition)");
            SlomoReturnSpeed = Config.Bind("Slomo", "ReturnSpeed", 0.33f, "Speed at which time returns to normal (higher = faster return)");
            SlomoVRControllerEnabled = Config.Bind("Slomo", "VRControllerEnabled", true, "Enable VR controller button to trigger slomo");
            SlomoVRButton = Config.Bind("Slomo", "VRButton", "LeftX", "VR button to trigger slomo (LeftX, RightX, LeftY, RightY, LeftGrip, RightGrip, LeftTrigger, RightTrigger, LeftTouchpad, RightTouchpad)");
            
            // Movement scaling during slomo
            SlomoAffectsMovement = Config.Bind("Slomo", "AffectsMovement", true, "Whether slomo affects player movement speed");
            SlomoMovementScale = Config.Bind("Slomo", "MovementScale", 0.3f, "Movement speed multiplier during slomo (0.3 = 30% of normal speed)");
            
            // Gun randomization configuration
            UseItemManagerForGunRandomization = Config.Bind("GunRandomization", "UseItemManager", true, 
                "Use ItemManager for gun randomization (includes all H3VR and modded guns). If false, uses GunList/MagazineList config files.");
            
            // Shuriken configuration
            ShurikenScale = Config.Bind("Shuriken", "Scale", 10f, "Scale multiplier for spawned shurikens (1.0 = normal size, 10.0 = 10x larger)");
            
            // Initialize slomo movement controller
            slomoMovementController = new SlomoMovementController();
            
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
            KeyBoostMalfunction = Config.Bind("General", "KeyBindForMeatyceiverMalfunctionBoost", KeyCode.F9, "Redeem: Boost Meatyceiver malfunction chance for 120s");
        }

        public void Awake()
        {
            Harmony.CreateAndPatchAll(this.GetType());
            
            // Initialize movement controller with config values
            slomoMovementController.Initialize(SlomoMovementScale.Value, SlomoAffectsMovement.Value, Logger);
            
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

            // Trigger slomo - check VR controller if enabled, or keyboard key
            bool slomoTriggered = Input.GetKeyDown(Key7.Value);
            if (SlomoVRControllerEnabled.Value && GM.CurrentMovementManager != null 
                && GM.CurrentMovementManager.Hands != null 
                && GM.CurrentMovementManager.Hands.Length > 0)
            {
                bool vrButtonPressed = CheckVRButtonPress(SlomoVRButton.Value);
                if (vrButtonPressed)
                {
                    slomoTriggered = true;
                    Logger.LogInfo($"Detected {SlomoVRButton.Value} Button Press!");
                }
            }
            
            if (slomoTriggered)
            {
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
                // Ensure movement is restored when slomo is completely off
                slomoMovementController?.UpdateMovementScale(Time.timeScale);
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
            // Random number for spawn chance - 1 out of 10 times (10% chance)
            int spawnChance = UnityEngine.Random.Range(1, 11);
            if (spawnChance != 1)
            {
                return; // Don't spawn grenade this time
            }

            //Set cartridge speed
            float howFast = 15.0f;

            //Set max angle
            float maxAngle = 4.0f;

            Transform PointingTransfrom = transform;

            //Get Random direction for bullet
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

            //old spray
            GameObject go0 = Instantiate(obj.GetGameObject(), shuriPosition0, Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward));

            //Set Object Direction
            go0.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

            //add scale for funnies using configurable value
            go0.transform.localScale = new Vector3(ShurikenScale.Value, ShurikenScale.Value, ShurikenScale.Value);
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
            if (Time.timeScale > MaxSlomo.Value)
            {
                Time.timeScale -= SlomoScaleSpeed.Value * Time.unscaledDeltaTime;
                Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
                
                // Update movement scaling based on current time scale
                slomoMovementController?.UpdateMovementScale(Time.timeScale);
            }

            if (Time.timeScale <= MaxSlomo.Value)
            {
                SlomoStatus = ("Wait");
            }
        }

        public void SlomoReturn()
        {
            if (Time.timeScale != 1)
            {
                Time.timeScale += SlomoReturnSpeed.Value * Time.unscaledDeltaTime;
                Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
                Time.timeScale = Mathf.Clamp(Time.timeScale, 0f, 1f);
                
                // Update movement scaling based on current time scale
                slomoMovementController?.UpdateMovementScale(Time.timeScale);
            }
        }

        IEnumerator SlomoWait()
        {
            yield return new WaitForSecondsRealtime(SlomoWaitTime.Value);
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

            // Get the object you want to spawnz
            FVRObject obj = IM.OD["12GaugeShellFreedomfetti"];

            //Set Object Position
            Vector3 shellPosition0 = GM.CurrentPlayerBody.RightHand.position + (GM.CurrentPlayerBody.RightHand.forward + GM.CurrentPlayerBody.RightHand.up * 0.5f) * 0.02f;

            GameObject go0 = Instantiate(obj.GetGameObject(), shellPosition0, Quaternion.LookRotation(GM.CurrentPlayerBody.RightHand.forward));

            //Set Object Direction
            go0.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

            //Detonate Shell?
            FVRFireArmRound cartridge = go0.GetComponent<FVRFireArmRound>();
            cartridge.Splode(0.01f, false, true);
        }

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

        //we want to spawn 4 flashbangs infront of the player with little notice
        public void SpawnFlash2()
        {
            // Get the object you want to spawn
            FVRObject obj = IM.OD["PinnedGrenadeXM84"];

            // Spawn 4 flashbangs with different positions and rotations
            for (int i = 0; i < 4; i++)
            {
                // Calculate spread positions around the player's head
                float angle = i * 90f; // 0°, 90°, 180°, 270° for even distribution
                Vector3 offsetDirection = new Vector3(
                    Mathf.Sin(angle * Mathf.Deg2Rad) * 0.3f, // X offset
                    UnityEngine.Random.Range(-0.1f, 0.2f),   // Y offset (slight vertical variation)
                    Mathf.Cos(angle * Mathf.Deg2Rad) * 0.3f  // Z offset
                );

                // Calculate spawn position relative to head
                Vector3 spawnPosition = GM.CurrentPlayerBody.Head.position + 
                                      GM.CurrentPlayerBody.Head.TransformDirection(offsetDirection) + 
                                      new Vector3(0f, 0.25f, 0f);

                // Instantiate (spawn) the object
                Logger.LogInfo($"Spawned Flash2 Object {i + 1}");
                GameObject go = Instantiate(obj.GetGameObject(), spawnPosition, GM.CurrentPlayerBody.Head.rotation);

                // Add slight rotation variation to each flashbang
                go.transform.Rotate(UnityEngine.Random.Range(-15f, 15f), UnityEngine.Random.Range(-15f, 15f), 0f);

                //prime the flash object
                Logger.LogInfo($"Getting Component {i + 1}");
                PinnedGrenade grenade = go.GetComponentInChildren<PinnedGrenade>();
                Logger.LogInfo($"Releasing Lever {i + 1}");
                grenade.ReleaseLever();

                //add force with slight variation for each flashbang
                Vector3 forceDirection = GM.CurrentPlayerBody.Head.forward + 
                                       new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 
                                                  UnityEngine.Random.Range(-0.1f, 0.3f), 
                                                  UnityEngine.Random.Range(-0.2f, 0.2f));
                
                Logger.LogInfo($"Adding Force {i + 1}");
                go.GetComponent<Rigidbody>().AddForce(forceDirection * UnityEngine.Random.Range(400f, 600f));
            }
        }

        public void ZeroGravityBumpDown()
        {
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.None;
            GM.CurrentSceneSettings.RefreshGravity();
            ZeroGStatus = "On";
        }

        public void ZeroGravityBumpUp()
        {
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Playful;
            GM.CurrentSceneSettings.RefreshGravity();
            ZeroGStatus = "Off";
        }

        public void RealisticFall()
        {
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.Realistic;
            GM.CurrentSceneSettings.RefreshGravity();
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

        public void SpawnSkittyBigGun()
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
                gunListString = GunList.Value;
            }

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

            string magListString;
            if (File.Exists(MagazineList.Value))
            {
                using (StreamReader magListReader = new StreamReader(MagazineList.Value))
                {
                    magListString = magListReader.ReadToEnd();
                }
            }
            else
            {
                magListString = MagazineList.Value;
            }

            string[] magazineList = magListString
                .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim())
                .Where(m => m.Length > 0)
                .ToArray();

            if (magazineList.Length == 0)
            {
                Logger.LogError("Magazine list is empty after parsing.");
                return;
            }

            string TopGun = gunList[0];
            string TopGunTruncated = new string(TopGun.Take(5).ToArray());
            Logger.LogInfo("TopGun: " + TopGun);
            Logger.LogInfo("TopGunTruncated: " + TopGunTruncated);

            string MatchingMagazine = magazineList.FirstOrDefault(o => o.Contains(TopGunTruncated));
            Logger.LogInfo("MatchingMagazine: " + MatchingMagazine);

            if (!IM.OD.ContainsKey(TopGun))
            {
                Logger.LogError("Gun key '" + TopGun + "' not found in IM.OD dictionary.");
                return;
            }
            if (string.IsNullOrEmpty(MatchingMagazine) || !IM.OD.ContainsKey(MatchingMagazine))
            {
                Logger.LogError("Magazine key '" + MatchingMagazine + "' not found in IM.OD dictionary.");
                return;
            }

            FVRObject obj = IM.OD[TopGun];
            FVRObject obj2 = IM.OD[MatchingMagazine];

            GameObject go = Instantiate(obj.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);
            GameObject go2 = Instantiate(obj2.GetGameObject(), new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position, GM.CurrentPlayerBody.Head.rotation);

            go.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));
            go2.GetComponent<Rigidbody>().AddTorque(new Vector3(.25f, .25f, .25f));

            go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);
            go2.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 100);

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
            
            // Clean up movement controller
            slomoMovementController?.Reset();
        }

        // Add this method to manually test/adjust movement scaling
        public void TestMovementScaling()
        {
            if (slomoMovementController != null)
            {
                // Update settings from current config values in case they changed
                slomoMovementController.UpdateSettings(SlomoMovementScale.Value, SlomoAffectsMovement.Value);
                Logger.LogInfo($"Movement scaling updated - Scale: {SlomoMovementScale.Value}, Enabled: {SlomoAffectsMovement.Value}");
            }
        }

        private bool CheckVRButtonPress(string buttonName)
        {
            try
            {
                var hands = GM.CurrentMovementManager?.Hands;
                if (hands == null || hands.Length == 0) return false;

                switch (buttonName.ToLower())
                {
                    case "leftx":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.AXButtonDown;
                    case "rightx":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.AXButtonDown;
                    case "lefty":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.BYButtonDown;
                    case "righty":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.BYButtonDown;
                    case "leftgrip":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.GripDown;
                    case "rightgrip":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.GripDown;
                    case "lefttrigger":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.TriggerDown;
                    case "righttrigger":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.TriggerDown;
                    case "lefttouchpad":
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.TouchpadDown;
                    case "righttouchpad":
                        return hands.Length > 1 && hands[1] != null && hands[1].Input.TouchpadDown;
                    default:
                        Logger.LogWarning($"Unknown VR button configuration: {buttonName}. Using default LeftX.");
                        return hands.Length > 0 && hands[0] != null && hands[0].Input.AXButtonDown;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"CheckVRButtonPress failed for button {buttonName}: {ex.Message}");
                return false;
            }
        }

        private void ToggleHeldGunFireMode()
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null) 
                {
                    Logger.LogWarning("ToggleHeldGunFireMode: No firearm found in hands");
                    return;
                }

                string gunType = firearm.GetType().Name;
                Logger.LogInfo($"ToggleHeldGunFireMode: Attempting to toggle fire mode on {gunType}");

                // Strategy 1: Try comprehensive list of method names
                string[] methodNames = { 
                    "CycleFireMode", "CycleFireSelector", "ToggleFireMode", "NextFireMode",
                    "CycleSelectorMode", "AdvanceFireSelector", "SwitchFireMode",
                    "UpdateFireMode", "ChangeFireMode", "CycleFiringMode",
                    "AdvanceFireSelectorState", "CycleFireSelectorState", "ToggleSafetyFireSelector"
                };

                foreach (var methodName in methodNames)
                {
                    MethodInfo mi = firearm.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mi != null && mi.GetParameters().Length == 0)
                    {
                        mi.Invoke(firearm, null);
                        Logger.LogInfo($"ToggleHeldGunFireMode: Successfully toggled via method '{methodName}'");
                        return;
                    }
                }

                // Strategy 2: Try comprehensive list of field names for fire selectors
                string[] selectorFieldNames = { 
                    "m_fireSelector", "FireSelector", "m_selector", "fireSelector", 
                    "m_FireSelector", "m_fireSelectorMode", "FireSelectorMode",
                    "m_firingMode", "FiringMode", "m_mode", "Mode", "SelectorState",
                    "m_selectorState", "SafetyState", "m_safetyState"
                };

                foreach (var fieldName in selectorFieldNames)
                {
                    FieldInfo selectorField = firearm.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (selectorField != null && selectorField.FieldType.IsEnum)
                    {
                        if (TryToggleEnumField(firearm, selectorField, fieldName))
                        {
                            Logger.LogInfo($"ToggleHeldGunFireMode: Successfully toggled via field '{fieldName}'");
                            return;
                        }
                    }
                }

                // Strategy 3: Look for properties with enum types
                PropertyInfo[] properties = firearm.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var prop in properties)
                {
                    if (prop.PropertyType.IsEnum && prop.CanWrite && 
                        (prop.Name.ToLower().Contains("fire") || prop.Name.ToLower().Contains("selector") || 
                         prop.Name.ToLower().Contains("mode") || prop.Name.ToLower().Contains("safety")))
                    {
                        if (TryToggleEnumProperty(firearm, prop))
                        {
                            Logger.LogInfo($"ToggleHeldGunFireMode: Successfully toggled via property '{prop.Name}'");
                            return;
                        }
                    }
                }

                // Strategy 4: Try to find any enum field/property that might control firing
                FieldInfo[] allFields = firearm.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var field in allFields)
                {
                    if (field.FieldType.IsEnum && Enum.GetValues(field.FieldType).Length > 1)
                    {
                        string fieldNameLower = field.Name.ToLower();
                        if (fieldNameLower.Contains("fire") || fieldNameLower.Contains("mode") || 
                            fieldNameLower.Contains("selector") || fieldNameLower.Contains("safety"))
                        {
                            if (TryToggleEnumField(firearm, field, field.Name))
                            {
                                Logger.LogInfo($"ToggleHeldGunFireMode: Successfully toggled via discovered field '{field.Name}'");
                                return;
                            }
                        }
                    }
                }

                Logger.LogWarning($"ToggleHeldGunFireMode: Could not find fire mode control for {gunType}");
            }
            catch (Exception ex)
            {
                Logger.LogError("ToggleHeldGunFireMode failed: " + ex);
            }
        }

        private FVRFireArm GetHeldFirearm()
        {
            FVRViveHand[] hands = GM.CurrentMovementManager?.Hands;
            if (hands == null || hands.Length == 0) return null;

            // Check right hand first, then left
            for (int handIndex = hands.Length - 1; handIndex >= 0; handIndex--)
            {
                var hand = hands[handIndex];
                if (hand?.CurrentInteractable == null) continue;

                var firearm = hand.CurrentInteractable as FVRFireArm;
                if (firearm != null) return firearm;

                // Check if it's a subclass of FVRFireArm
                if (hand.CurrentInteractable.GetType().IsSubclassOf(typeof(FVRFireArm)))
                    return (FVRFireArm)hand.CurrentInteractable;
            }
            return null;
        }

        private bool TryToggleEnumField(FVRFireArm firearm, FieldInfo field, string fieldName)
        {
            try
            {
                object currentVal = field.GetValue(firearm);
                if (currentVal == null) return false;

                Array enumValues = Enum.GetValues(currentVal.GetType());
                if (enumValues.Length <= 1) return false;

                int currentIndex = Array.IndexOf(enumValues, currentVal);
                int nextIndex = (currentIndex + 1) % enumValues.Length;
                object nextVal = enumValues.GetValue(nextIndex);
                
                field.SetValue(firearm, nextVal);

                // Try to call update methods
                TryCallFireSelectorUpdateMethods(firearm, nextVal, fieldName);
                
                Logger.LogInfo($"ToggleHeldGunFireMode: Changed {fieldName} from {currentVal} to {nextVal}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"TryToggleEnumField failed for {fieldName}: {ex}");
                return false;
            }
        }

        private bool TryToggleEnumProperty(FVRFireArm firearm, PropertyInfo property)
        {
            try
            {
                object currentVal = property.GetValue(firearm, null);
                if (currentVal == null) return false;

                Array enumValues = Enum.GetValues(currentVal.GetType());
                if (enumValues.Length <= 1) return false;

                int currentIndex = Array.IndexOf(enumValues, currentVal);
                int nextIndex = (currentIndex + 1) % enumValues.Length;
                object nextVal = enumValues.GetValue(nextIndex);
                
                property.SetValue(firearm, nextVal, null);

                // Try to call update methods
                TryCallFireSelectorUpdateMethods(firearm, nextVal, property.Name);
                
                Logger.LogInfo($"ToggleHeldGunFireMode: Changed {property.Name} from {currentVal} to {nextVal}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"TryToggleEnumProperty failed for {property.Name}: {ex}");
                return false;
            }
        }

        private void TryCallFireSelectorUpdateMethods(FVRFireArm firearm, object newValue, string changedFieldName)
        {
            // Try to call update/setter methods that might need to be invoked after changing the selector
            string[] updateMethods = { 
                "SetFireSelector", "UpdateFireSelector", "OnFireSelectorChanged", 
                "UpdateFireMode", "SetFireMode", "OnFireModeChanged",
                "UpdateSafetyState", "SetSafetyState", "OnSafetyChanged",
                "RefreshFireSelector", "ApplyFireMode", "ConfigureFireMode",
                "UpdateGunState", "RefreshGunState", "UpdateWeaponState"
            };

            foreach (var methodName in updateMethods)
            {
                try
                {
                    MethodInfo method = firearm.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (method != null)
                    {
                        var parameters = method.GetParameters();
                        if (parameters.Length == 0)
                        {
                            method.Invoke(firearm, null);
                        }
                        else if (parameters.Length == 1 && parameters[0].ParameterType == newValue.GetType())
                        {
                            method.Invoke(firearm, new object[] { newValue });
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Silently continue - not all methods will exist or work
                    continue;
                }
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

                Vector3 pos = inter.transform.position;
                Quaternion rot = inter.transform.rotation;
                string currentKey = firearm.ObjectWrapper != null ? firearm.ObjectWrapper.ItemID : null;

                // Check config setting to determine gun source
                if (UseItemManagerForGunRandomization.Value)
                {
                    RandomizeFromItemManager(firearm, pos, rot, currentKey);
                }
                else
                {
                    RandomizeFromConfigLists(firearm, pos, rot, currentKey);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("RandomizeHeldGun failed: " + ex);
            }
        }

        private void RandomizeFromItemManager(FVRFireArm firearm, Vector3 pos, Quaternion rot, string currentKey)
        {
            // Get all firearms from ItemManager's ObjectDictionary (includes H3VR and modded guns)
            var allFirearms = IM.OD.Values
                .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Firearm)
                .ToArray();

            if (allFirearms.Length == 0) 
            { 
                Logger.LogError("RandomizeHeldGun: No firearms found in ItemManager."); 
                return; 
            }

            // Filter out the current gun if possible
            FVRObject[] selectableFirearms = currentKey != null 
                ? allFirearms.Where(obj => obj.ItemID != currentKey).ToArray() 
                : allFirearms;
            
            if (selectableFirearms.Length == 0) selectableFirearms = allFirearms;

            // Pick a random firearm
            FVRObject selectedFirearm = selectableFirearms[UnityEngine.Random.Range(0, selectableFirearms.Length)];

            Logger.LogInfo($"RandomizeHeldGun: Selected {selectedFirearm.DisplayName} (ID: {selectedFirearm.ItemID})");

            Destroy(firearm.gameObject);

            // Spawn the new gun
            GameObject newGunGO = Instantiate(selectedFirearm.GetGameObject(), pos, rot);
            var gunRB = newGunGO.GetComponent<Rigidbody>();
            if (gunRB != null) { gunRB.velocity = Vector3.zero; gunRB.angularVelocity = Vector3.zero; }

            // Try to spawn a matching magazine
            TrySpawnMatchingMagazine(selectedFirearm, pos, rot);

            Logger.LogInfo($"RandomizeHeldGun: Successfully replaced gun with {selectedFirearm.DisplayName}");
        }

        private void RandomizeFromConfigLists(FVRFireArm firearm, Vector3 pos, Quaternion rot, string currentKey)
        {
            // Use the original logic with config file gun lists
            string gunListString = File.Exists(GunList.Value)
                ? File.ReadAllText(GunList.Value)
                : GunList.Value;
            string[] gunList = gunListString
                .Split(new[] { '\r', '\n', ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(g => g.Trim())
                .Where(g => g.Length > 0)
                .ToArray();
            if (gunList.Length == 0) { Logger.LogError("RandomizeHeldGun: Gun list empty."); return; }

            string[] selectable = currentKey != null ? gunList.Where(k => k != currentKey).ToArray() : gunList;
            if (selectable.Length == 0) selectable = gunList;

            string newGunKey = selectable[UnityEngine.Random.Range(0, selectable.Length)];
            if (!IM.OD.ContainsKey(newGunKey)) { Logger.LogError("RandomizeHeldGun: Key '" + newGunKey + "' not found in IM.OD."); return; }

            Destroy(firearm.gameObject);

            FVRObject gunObj = IM.OD[newGunKey];
            GameObject newGunGO = Instantiate(gunObj.GetGameObject(), pos, rot);
            var gunRB = newGunGO.GetComponent<Rigidbody>();
            if (gunRB != null) { gunRB.velocity = Vector3.zero; gunRB.angularVelocity = Vector3.zero; }

            // Try to spawn magazine using config lists
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
                            Logger.LogInfo("RandomizeHeldGun: Spawned matching magazine: " + magKey);
                        }
                        else Logger.LogWarning("RandomizeHeldGun: Matching mag key not in IM.OD: " + magKey);
                    }
                    else Logger.LogWarning("RandomizeHeldGun: No matching magazines found for truncated key: " + truncated);
                }
                else Logger.LogWarning("RandomizeHeldGun: Magazine list empty.");
            }
            catch (Exception magEx) { Logger.LogError("RandomizeHeldGun: Magazine spawn failed: " + magEx); }

            Logger.LogInfo("RandomizeHeldGun: Replaced held gun with: " + newGunKey);
        }

        // Helper method to spawn a matching magazine for the given firearm
        private void TrySpawnMatchingMagazine(FVRObject selectedFirearm, Vector3 pos, Quaternion rot)
        {
            try
            {
                // Get all magazines from ItemManager
                var allMagazines = IM.OD.Values
                    .Where(obj => obj != null && obj.Category == FVRObject.ObjectCategory.Magazine)
                    .ToArray();

                if (allMagazines.Length == 0)
                {
                    Logger.LogWarning("RandomizeHeldGun: No magazines found in ItemManager.");
                    return;
                }

                FVRObject matchingMag = null;
                string gunNameLower = selectedFirearm.DisplayName.ToLower();
                string gunIdLower = selectedFirearm.ItemID.ToLower();

                // Strategy 1: Direct ID prefix matching (most reliable)
                string gunIdPrefix = GetGunIdPrefix(selectedFirearm.ItemID);
                if (!string.IsNullOrEmpty(gunIdPrefix))
                {
                    var prefixMatches = allMagazines.Where(mag => 
                        mag.ItemID.StartsWith(gunIdPrefix, StringComparison.OrdinalIgnoreCase)).ToArray();
                    
                    if (prefixMatches.Length > 0)
                    {
                        matchingMag = prefixMatches[UnityEngine.Random.Range(0, prefixMatches.Length)];
                        Logger.LogInfo($"RandomizeHeldGun: Found magazine match using ID prefix '{gunIdPrefix}': {matchingMag.DisplayName}");
                    }
                }

                // Strategy 2: Brand/Model matching
                if (matchingMag == null)
                {
                    string[] gunBrandModel = ExtractBrandAndModel(selectedFirearm.DisplayName, selectedFirearm.ItemID);
                    foreach (string identifier in gunBrandModel)
                    {
                        if (string.IsNullOrEmpty(identifier) || identifier.Length < 3) continue;
                        
                        var brandMatches = allMagazines.Where(mag => 
                            mag.DisplayName.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) >= 0 || 
                            mag.ItemID.IndexOf(identifier, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                        
                        if (brandMatches.Length > 0)
                        {
                            matchingMag = brandMatches[UnityEngine.Random.Range(0, brandMatches.Length)];
                            Logger.LogInfo($"RandomizeHeldGun: Found magazine match using brand/model '{identifier}': {matchingMag.DisplayName}");
                            break;
                        }
                    }
                }

                // Strategy 3: Caliber-based matching
                if (matchingMag == null)
                {
                    string gunCaliber = ExtractCaliber(selectedFirearm.DisplayName, selectedFirearm.ItemID);
                    if (!string.IsNullOrEmpty(gunCaliber))
                    {
                        var caliberMatches = allMagazines.Where(mag => 
                            mag.DisplayName.IndexOf(gunCaliber, StringComparison.OrdinalIgnoreCase) >= 0 || 
                            mag.ItemID.IndexOf(gunCaliber, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                        
                        if (caliberMatches.Length > 0)
                        {
                            matchingMag = caliberMatches[UnityEngine.Random.Range(0, caliberMatches.Length)];
                            Logger.LogInfo($"RandomizeHeldGun: Found magazine match using caliber '{gunCaliber}': {matchingMag.DisplayName}");
                        }
                    }
                }

                // Strategy 4: Weapon type matching (rifle, pistol, etc.)
                if (matchingMag == null)
                {
                    string weaponType = DetermineWeaponType(selectedFirearm.DisplayName, selectedFirearm.ItemID);
                    if (!string.IsNullOrEmpty(weaponType))
                    {
                        var typeMatches = allMagazines.Where(mag => 
                            mag.DisplayName.IndexOf(weaponType, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                        
                        if (typeMatches.Length > 0)
                        {
                            matchingMag = typeMatches[UnityEngine.Random.Range(0, typeMatches.Length)];
                            Logger.LogInfo($"RandomizeHeldGun: Found magazine match using weapon type '{weaponType}': {matchingMag.DisplayName}");
                        }
                    }
                }

                // Strategy 5: Fallback to truncated ID matching (legacy behavior)
                if (matchingMag == null && selectedFirearm.ItemID.Length >= 5)
                {
                    string truncated = selectedFirearm.ItemID.Substring(0, 5);
                    var truncatedMatches = allMagazines.Where(mag => 
                        mag.ItemID.Contains(truncated) || 
                        mag.DisplayName.ToLower().Contains(truncated.ToLower())).ToArray();
                    
                    if (truncatedMatches.Length > 0)
                    {
                        matchingMag = truncatedMatches[UnityEngine.Random.Range(0, truncatedMatches.Length)];
                        Logger.LogInfo($"RandomizeHeldGun: Found magazine match using truncated ID '{truncated}': {matchingMag.DisplayName}");
                    }
                }
                
                // Strategy 6: Smart random selection based on weapon characteristics
                if (matchingMag == null)
                {
                    var smartMatches = GetSmartMagazineMatches(selectedFirearm, allMagazines);
                    if (smartMatches.Length > 0)
                    {
                        matchingMag = smartMatches[UnityEngine.Random.Range(0, smartMatches.Length)];
                        Logger.LogInfo($"RandomizeHeldGun: Using smart random magazine: {matchingMag.DisplayName}");
                    }
                }

                // Strategy 7: Absolute fallback to any magazine
                if (matchingMag == null)
                {
                    matchingMag = allMagazines[UnityEngine.Random.Range(0, allMagazines.Length)];
                    Logger.LogInfo($"RandomizeHeldGun: Using completely random magazine: {matchingMag.DisplayName}");
                }

                // Spawn the magazine
                if (matchingMag != null)
                {
                    Vector3 magPos = pos + Vector3.up * 0.05f + (GM.CurrentPlayerBody != null ? GM.CurrentPlayerBody.Head.forward * 0.1f : Vector3.forward * 0.1f);
                    GameObject magGO = Instantiate(matchingMag.GetGameObject(), magPos, rot);
                    var magRB = magGO.GetComponent<Rigidbody>();
                    if (magRB != null) { magRB.velocity = Vector3.zero; magRB.angularVelocity = Vector3.zero; }
                    Logger.LogInfo("RandomizeHeldGun: Spawned magazine: " + matchingMag.DisplayName);
                }
            }
            catch (Exception magEx)
            {
                Logger.LogError("RandomizeHeldGun: Magazine spawn failed: " + magEx);
            }
        }

        // Helper method to get gun ID prefix (everything before the first underscore or number)
        private string GetGunIdPrefix(string gunId)
        {
            if (string.IsNullOrEmpty(gunId)) return string.Empty;
            
            // Look for common separators
            int separatorIndex = -1;
            char[] separators = { '_', '-', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            
            foreach (char sep in separators)
            {
                int index = gunId.IndexOf(sep);
                if (index > 0 && (separatorIndex == -1 || index < separatorIndex))
                {
                    separatorIndex = index;
                }
            }
            
            return separatorIndex > 0 ? gunId.Substring(0, separatorIndex) : gunId;
        }

        // Helper method to extract brand and model information
        private string[] ExtractBrandAndModel(string displayName, string itemId)
        {
            var identifiers = new List<string>();
            
            // Common gun manufacturers and their abbreviations
            var brandMappings = new Dictionary<string, string[]>
            {
                { "ak", new[] { "ak", "kalashnikov", "izhmash" } },
                { "ar", new[] { "ar", "armalite", "colt", "m16", "m4" } },
                { "glock", new[] { "glock", "glock17", "glock19" } },
                { "sig", new[] { "sig", "sauer", "p226", "p229", "p320" } },
                { "beretta", new[] { "beretta", "92", "m9" } },
                { "hk", new[] { "hk", "heckler", "koch", "mp5", "g36", "416" } },
                { "fn", new[] { "fn", "fabrique", "scar", "fal", "p90" } },
                { "remington", new[] { "remington", "870", "700" } },
                { "mossberg", new[] { "mossberg", "500", "590" } },
                { "winchester", new[] { "winchester", "1897", "model" } },
                { "springfield", new[] { "springfield", "m1a", "1911" } },
                { "smith", new[] { "smith", "wesson", "sw" } },
                { "ruger", new[] { "ruger", "10/22", "mini" } }
            };

            string nameLower = displayName.ToLower();
            string idLower = itemId.ToLower();
            
            // Extract from brand mappings
            foreach (var mapping in brandMappings)
            {
                foreach (var variant in mapping.Value)
                {
                    if (nameLower.Contains(variant) || idLower.Contains(variant))
                    {
                        identifiers.AddRange(mapping.Value);
                        break;
                    }
                }
            }
            
            // Extract direct words from display name
            string[] nameWords = displayName.Split(new char[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string word in nameWords)
            {
                if (word.Length >= 3 && !IsCommonWord(word.ToLower()))
                {
                    identifiers.Add(word);
                }
            }
            
            return identifiers.Distinct().ToArray();
        }

        // Helper method to extract caliber information
        private string ExtractCaliber(string displayName, string itemId)
        {
            string combined = (displayName + " " + itemId).ToLower();
            
            // Common calibers
            string[] calibers = {
                "9mm", "9x19", ".45", "45acp", ".40", "40sw", ".38", "38special",
                "357mag", ".357", "10mm", ".22", "22lr", ".380", "380acp",
                "5.56", "556", "223", ".223", "7.62", "762", "308", ".308",
                "30-06", "3006", "270", ".270", "300", ".300", "338", ".338",
                "50bmg", ".50", "12gauge", "20gauge", "410", ".410"
            };
            
            foreach (string caliber in calibers)
            {
                if (combined.Contains(caliber))
                {
                    return caliber;
                }
            }
            
            return string.Empty;
        }

        // Helper method to determine weapon type
        private string DetermineWeaponType(string displayName, string itemId)
        {
            string combined = (displayName + " " + itemId).ToLower();
            
            if (combined.Contains("rifle") || combined.Contains("ar") || combined.Contains("ak") || 
                combined.Contains("m16") || combined.Contains("m4") || combined.Contains("scar"))
                return "rifle";
            
            if (combined.Contains("pistol") || combined.Contains("handgun") || combined.Contains("glock") ||
                combined.Contains("1911") || combined.Contains("beretta") || combined.Contains("sig"))
                return "pistol";
                
            if (combined.Contains("shotgun") || combined.Contains("12gauge") || combined.Contains("20gauge") ||
                combined.Contains("870") || combined.Contains("500") || combined.Contains("590"))
                return "shotgun";
                
            if (combined.Contains("smg") || combined.Contains("submachine") || combined.Contains("mp5") ||
                combined.Contains("uzi") || combined.Contains("p90"))
                return "smg";
                
            if (combined.Contains("sniper") || combined.Contains("bolt") || combined.Contains("precision") ||
                combined.Contains("700") || combined.Contains("m24"))
                return "sniper";
            
            return string.Empty;
        }

        // Helper method to filter magazines based on weapon characteristics
        private FVRObject[] GetSmartMagazineMatches(FVRObject firearm, FVRObject[] allMagazines)
        {
            string weaponType = DetermineWeaponType(firearm.DisplayName, firearm.ItemID);
            string caliber = ExtractCaliber(firearm.DisplayName, firearm.ItemID);
            
            var smartMatches = new List<FVRObject>();
            
            foreach (var mag in allMagazines)
            {
                string magName = mag.DisplayName.ToLower();
                string magId = mag.ItemID.ToLower();
                string magCombined = magName + " " + magId;
                
                int score = 0;
                
                // Weapon type match
                if (!string.IsNullOrEmpty(weaponType))
                {
                    if (weaponType == "pistol" && (magCombined.Contains("pistol") || magCombined.Contains("handgun")))
                        score += 3;
                    else if (weaponType == "rifle" && magCombined.Contains("rifle"))
                        score += 3;
                    else if (weaponType == "shotgun" && magCombined.Contains("shotgun"))
                        score += 3;
                    else if (weaponType == "smg" && (magCombined.Contains("smg") || magCombined.Contains("submachine")))
                        score += 3;
                }
                
                // Caliber match
                if (!string.IsNullOrEmpty(caliber) && magCombined.Contains(caliber))
                    score += 5;
                
                // Capacity considerations (prefer reasonable magazine sizes)
                if (magName.Contains("30") || magName.Contains("20") || magName.Contains("15") || magName.Contains("10"))
                    score += 1;
                
                // Avoid obviously wrong matches
                if (weaponType == "pistol" && (magCombined.Contains("drum") || magCombined.Contains("100")))
                    score -= 2;
                if (weaponType == "sniper" && magCombined.Contains("drum"))
                    score -= 2;
                
                if (score >= 2) // Only include magazines with decent compatibility score
                    smartMatches.Add(mag);
            }
            
            return smartMatches.ToArray();
        }

        // Helper method to check if a word is too common to be useful for matching
        private bool IsCommonWord(string word)
        {
            string[] commonWords = { 
                "the", "and", "or", "but", "gun", "weapon", "firearm", "military", "tactical",
                "modern", "classic", "standard", "custom", "special", "edition", "variant",
                "version", "model", "mark", "type", "style", "series"
            };
            
            return commonWords.Contains(word);
        }

        private void EmptyHeldGunChamber()
        {
            try
            {
                FVRFireArm firearm = GetHeldFirearm();
                if (firearm == null)
                {
                    Logger.LogWarning("EmptyHeldGunChamber: No firearm found in hands");
                    return;
                }

                string gunType = firearm.GetType().Name;
                Logger.LogInfo($"EmptyHeldGunChamber: Attempting to empty chamber on {gunType}");

                // Strategy 1: Try comprehensive list of eject methods
                string[] methodNames = { 
                    "EjectChamberedRound", "EjectRound", "EjectChambered", "Eject", 
                    "ExtractRound", "DumpChamber", "ClearChamber", "EmptyChamber",
                    "EjectCurrentRound", "DropChamberedRound", "UnloadChamber",
                    "EjectCartridge", "ExtractCartridge", "ClearChamberedRound"
                };

                foreach (var methodName in methodNames)
                {
                    MethodInfo mi = firearm.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mi != null && mi.GetParameters().Length == 0) 
                    { 
                        mi.Invoke(firearm, null); 
                        Logger.LogInfo($"EmptyHeldGunChamber: Successfully ejected via method '{methodName}'");
                        return; 
                    }
                }

                // Strategy 2: Try to find and manipulate chamber objects directly
                if (TryDirectChamberManipulation(firearm, gunType)) return;

                // Strategy 3: Try multi-barrel/cylinder weapons
                if (TryMultiChamberWeapons(firearm, gunType)) return;

                Logger.LogWarning($"EmptyHeldGunChamber: Could not find chamber eject method for {gunType}");
            }
            catch (Exception ex)
            {
                Logger.LogError("EmptyHeldGunChamber failed: " + ex);
            }
        }

        private bool TryDirectChamberManipulation(FVRFireArm firearm, string gunType)
        {
            try
            {
                // Strategy A: Try chamber-specific eject methods first
                string[] chamberMethods = { "EjectRound", "Eject", "ExtractRound", "Clear", "Empty", "DumpRound" };
                foreach (var method in chamberMethods)
                {
                    MethodInfo mi = firearm.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mi != null && mi.GetParameters().Length == 0)
                    {
                        mi.Invoke(firearm, null);
                        Logger.LogInfo($"EmptyHeldGunChamber: Ejected via method '{method}' on {gunType}");
                        return true;
                    }
                }

                // Strategy B: Find and manually eject the round
                FVRFireArmRound round = FindRoundInChamber(firearm);
                if (round != null && round.gameObject != null)
                {
                    // Physically eject the round
                    Transform rT = round.transform; 
                    rT.parent = null;
                    
                    var rrb = round.GetComponent<Rigidbody>();
                    if (rrb != null)
                    {
                        rrb.isKinematic = false;
                        rrb.velocity = firearm.transform.forward * 2f + firearm.transform.up * 1f;
                        rrb.angularVelocity = UnityEngine.Random.insideUnitSphere * 10f;
                    }

                    // Clear the round reference from the chamber
                    ClearRoundFromChamber(firearm, round);
                    
                    Logger.LogInfo($"EmptyHeldGunChamber: Manually ejected round from {gunType}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"TryDirectChamberManipulation failed for {gunType}: {ex}");
                return false;
            }
        }

        private bool TryMultiChamberWeapons(FVRFireArm firearm, string gunType)
        {
            try
            {
                // Handle revolvers, break-action shotguns, etc.
                string[] multiChamberFields = { 
                    "Chambers", "m_chambers", "Cylinder", "m_cylinder", 
                    "Barrels", "m_barrels", "ChamberArray", "m_chamberArray"
                };

                foreach (var fieldName in multiChamberFields)
                {
                    FieldInfo field = firearm.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (field != null)
                    {
                        object chambersObj = field.GetValue(firearm);
                        if (chambersObj != null)
                        {
                            if (TryEjectFromMultiChamber(chambersObj, firearm, fieldName)) return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"TryMultiChamberWeapons failed: {ex.Message}");
                return false;
            }
        }

        private bool TryEjectFromMultiChamber(object chambersObj, FVRFireArm firearm, string fieldName)
        {
            try
            {
                // Handle arrays or lists of chambers
                if (chambersObj is System.Collections.IList chamberList)
                {
                    bool ejectedAny = false;
                    for (int i = 0; i < chamberList.Count; i++)
                    {
                        if (chamberList[i] != null && TryEjectFromChamber(chamberList[i], firearm, $"{fieldName}[{i}]"))
                        {
                            ejectedAny = true;
                        }
                    }
                    return ejectedAny;
                }
                else if (chambersObj.GetType().IsArray)
                {
                    Array chamberArray = (Array)chambersObj;
                    bool ejectedAny = false;
                    for (int i = 0; i < chamberArray.Length; i++)
                    {
                        object chamber = chamberArray.GetValue(i);
                        if (chamber != null && TryEjectFromChamber(chamber, firearm, $"{fieldName}[{i}]"))
                        {
                            ejectedAny = true;
                        }
                    }
                    return ejectedAny;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"TryEjectFromMultiChamber failed: {ex.Message}");
                return false;
            }
        }

        private bool TryEjectFromChamber(object chamberObj, FVRFireArm firearm, string chamberName)
        {
            try
            {
                Type chamberType = chamberObj.GetType();
                
                // Strategy A: Try chamber-specific eject methods first
                string[] chamberMethods = { "EjectRound", "Eject", "ExtractRound", "Clear", "Empty", "DumpRound" };
                foreach (var method in chamberMethods)
                {
                    MethodInfo mi = chamberType.GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (mi != null && mi.GetParameters().Length == 0)
                    {
                        mi.Invoke(chamberObj, null);
                        Logger.LogInfo($"EmptyHeldGunChamber: Ejected via chamber method '{method}' on {chamberName}");
                        return true;
                    }
                }

                // Strategy B: Find and manually eject the round
                FVRFireArmRound round = FindRoundInChamber(chamberObj);
                if (round != null && round.gameObject != null)
                {
                    // Physically eject the round
                    Transform rT = round.transform; 
                    rT.parent = null;
                    
                    var rrb = round.GetComponent<Rigidbody>();
                    if (rrb != null)
                    {
                        rrb.isKinematic = false;
                        rrb.velocity = firearm.transform.forward * 2f + firearm.transform.up * 1f;
                        rrb.angularVelocity = UnityEngine.Random.insideUnitSphere * 10f;
                    }

                    // Clear the round reference from the chamber
                    ClearRoundFromChamber(chamberObj, round);
                    
                    Logger.LogInfo($"EmptyHeldGunChamber: Manually ejected round from {chamberName}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"TryEjectFromChamber failed for {chamberName}: {ex.Message}");
                return false;
            }
        }

        private FVRFireArmRound FindRoundInChamber(object chamberObj)
        {
            try
            {
                Type chamberType = chamberObj.GetType();
                string[] roundFieldNames = { 
                    "Round", "m_round", "ChamberedRound", "m_chamberedRound", 
                    "LoadedRound", "m_loadedRound", "CurrentRound", "m_currentRound",
                    "Cartridge", "m_cartridge", "LoadedCartridge", "m_loadedCartridge"
                };

                // Check fields
                foreach (var fieldName in roundFieldNames)
                {
                    FieldInfo rf = chamberType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (rf != null && rf.FieldType.IsAssignableFrom(typeof(FVRFireArmRound))) 
                    {
                        return rf.GetValue(chamberObj) as FVRFireArmRound;
                    }
                }

                // Check properties
                PropertyInfo[] properties = chamberType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var prop in properties)
                {
                    if (typeof(FVRFireArmRound).IsAssignableFrom(prop.PropertyType) && prop.CanRead)
                    {
                        try
                        {
                            FVRFireArmRound round = prop.GetValue(chamberObj, null) as FVRFireArmRound;
                            if (round != null) return round;
                        }
                        catch { continue; }
                    }
                }

                // Last resort: find any FVRFireArmRound field
                FieldInfo anyRoundField = chamberType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .FirstOrDefault(f => typeof(FVRFireArmRound).IsAssignableFrom(f.FieldType));
                if (anyRoundField != null)
                {
                    return anyRoundField.GetValue(chamberObj) as FVRFireArmRound;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"FindRoundInChamber failed: {ex.Message}");
                return null;
            }
        }

        private void ClearRoundFromChamber(object chamberObj, FVRFireArmRound round)
        {
            try
            {
                Type chamberType = chamberObj.GetType();
                string[] roundFieldNames = { 
                    "Round", "m_round", "ChamberedRound", "m_chamberedRound", 
                    "LoadedRound", "m_loadedRound", "CurrentRound", "m_currentRound",
                    "Cartridge", "m_cartridge", "LoadedCartridge", "m_loadedCartridge"
                };

                // Clear fields
                foreach (var fieldName in roundFieldNames)
                {
                    FieldInfo rf = chamberType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (rf != null && rf.FieldType.IsAssignableFrom(typeof(FVRFireArmRound))) 
                    {
                        rf.SetValue(chamberObj, null);
                    }
                }

                // Clear properties
                PropertyInfo[] properties = chamberType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                foreach (var prop in properties)
                {
                    if (prop.CanWrite && prop.PropertyType.IsAssignableFrom(typeof(FVRFireArmRound)))
                    {
                        try
                        {
                            prop.SetValue(chamberObj, null, null);
                        }
                        catch { continue; }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"ClearRoundFromChamber failed: {ex.Message}");
            }
        }

        private void ActivateMalfunctionBoost()
        {
            _malfunctionBoostActive = true;
            _malfunctionBoostEndTime = Time.time + MalfunctionBoostDuration;
            Logger.LogInfo("Meatyceiver malfunction boost activated for " + MalfunctionBoostDuration + " seconds.");
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