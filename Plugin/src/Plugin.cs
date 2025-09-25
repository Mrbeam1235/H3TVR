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
        private ConfigEntry<int> ShurikenMinCount;
        private ConfigEntry<int> ShurikenMaxCount;
        
        // Configurable pillow settings
        private ConfigEntry<int> PillowMinCount;
        private ConfigEntry<int> PillowMaxCount;
        private ConfigEntry<bool> PillowGrenadeEnabled;
        private ConfigEntry<float> PillowGrenadeChance;
        private ConfigEntry<float> PillowGrenadeArmedChance;
        private ConfigEntry<bool> PillowZeroGravityEnabled;
        private ConfigEntry<float> PillowZeroGravityChance;
        private ConfigEntry<float> PillowZeroGravityDuration;
        private ConfigEntry<bool> PillowSlomoEnabled;
        private ConfigEntry<float> PillowSlomoChance;
        private ConfigEntry<float> PillowSlomoDuration;
        
        // Configurable danger close settings
        private ConfigEntry<int> DangerCloseMinCount;
        private ConfigEntry<int> DangerCloseMaxCount;
        
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
            ShurikenMinCount = Config.Bind("Shuriken", "MinCount", 15, "Minimum number of shurikens to spawn");
            ShurikenMaxCount = Config.Bind("Shuriken", "MaxCount", 30, "Maximum number of shurikens to spawn");
            
            // Pillow configuration
            PillowMinCount = Config.Bind("Pillow", "MinCount", 1, "Minimum number of pillows to spawn");
            PillowMaxCount = Config.Bind("Pillow", "MaxCount", 3, "Maximum number of pillows to spawn");
            PillowGrenadeEnabled = Config.Bind("Pillow", "GrenadeEnabled", true, "Enable random grenade spawning with pillows");
            PillowGrenadeChance = Config.Bind("Pillow", "GrenadeChance", 0.1f, "Chance (0.0-1.0) for a grenade to spawn with pillows (0.1 = 10% chance)");
            PillowGrenadeArmedChance = Config.Bind("Pillow", "GrenadeArmedChance", 0.1f, "Chance (0.0-1.0) for spawned grenades to be armed/pin pulled (0.1 = 10% chance)");
            PillowZeroGravityEnabled = Config.Bind("Pillow", "ZeroGravityEnabled", true, "Enable random zero gravity activation with pillows");
            PillowZeroGravityChance = Config.Bind("Pillow", "ZeroGravityChance", 0.15f, "Chance (0.0-1.0) for zero gravity to activate with pillows (0.15 = 15% chance)");
            PillowZeroGravityDuration = Config.Bind("Pillow", "ZeroGravityDuration", 5f, "Duration in seconds for pillow-triggered zero gravity effect");
            PillowSlomoEnabled = Config.Bind("Pillow", "SlomoEnabled", true, "Enable random slow motion activation with pillows");
            PillowSlomoChance = Config.Bind("Pillow", "SlomoChance", 0.2f, "Chance (0.0-1.0) for slow motion to activate with pillows (0.2 = 20% chance)");
            PillowSlomoDuration = Config.Bind("Pillow", "SlomoDuration", 3f, "Duration in seconds for pillow-triggered slow motion effect");
            
            // Danger Close configuration
            DangerCloseMinCount = Config.Bind("DangerClose", "MinCount", 1, "Minimum number of danger close rounds to spawn per barrage");
            DangerCloseMaxCount = Config.Bind("DangerClose", "MaxCount", 5, "Maximum number of danger close rounds to spawn per barrage");
            
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
            
            // Initialize sosig spawner integration
            InitializeSosigSpawner();
            
            Logger.LogInfo("Successfully loaded H3TVR!");
        }

        private void InitializeSosigSpawner()
        {
            GameObject sosigSpawnerObject = new GameObject("SosigSpawnerIntegration");
            sosigSpawnerObject.transform.SetParent(transform);
            sosigSpawnerObject.AddComponent<SosigSpawnerIntegration>();
            
            // Initialize the standalone Twitch Chat Sosig Manager
            GameObject twitchChatSosigObject = new GameObject("TwitchChatSosigManager");
            twitchChatSosigObject.transform.SetParent(transform);
            twitchChatSosigObject.AddComponent<TwitchChatSosigManager>();
            
            Logger.LogInfo("Sosig Spawner Integration and Twitch Chat Sosig Manager initialized!");
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
            // Determine how many pillows to spawn
            int pillowCount = UnityEngine.Random.Range(PillowMinCount.Value, PillowMaxCount.Value + 1);
            Logger.LogInfo($"Spawning {pillowCount} pillow(s)");

            // Spawn the pillows
            for (int i = 0; i < pillowCount; i++)
            {
                // Get the object you want to spawn
                FVRObject obj = IM.OD["BodyPillow"];

                // Spawn pillows directly above player head with no random variation
                Vector3 spawnPosition = new Vector3(0f, .25f, 0f) + GM.CurrentPlayerBody.Head.position;
                GameObject go = Instantiate(obj.GetGameObject(), spawnPosition, GM.CurrentPlayerBody.Head.rotation);

                // Add force directly forward with no variation
                go.GetComponent<Rigidbody>().AddForce(GM.CurrentPlayerBody.Head.forward * 4000f);
            }

            // Check for grenade spawn chance
            if (PillowGrenadeEnabled.Value && UnityEngine.Random.value < PillowGrenadeChance.Value)
            {
                Logger.LogInfo("Pillow grenade spawn triggered!");
                SpawnPillowGrenade();
            }

            // Check for zero gravity activation chance
            if (PillowZeroGravityEnabled.Value && UnityEngine.Random.value < PillowZeroGravityChance.Value)
            {
                Logger.LogInfo($"Pillow zero gravity triggered! Duration: {PillowZeroGravityDuration.Value}s");
                StartCoroutine(ActivatePillowZeroGravity());
            }

            // Check for slow motion activation chance
            if (PillowSlomoEnabled.Value && UnityEngine.Random.value < PillowSlomoChance.Value)
            {
                Logger.LogInfo($"Pillow slow motion triggered! Duration: {PillowSlomoDuration.Value}s");
                StartCoroutine(ActivatePillowSlomo());
            }
        }

        private void SpawnPillowGrenade()
        {
            try
            {
                // Get the grenade object
                FVRObject grenadeObj = IM.OD["PinnedGrenadeM67"];

                // Spawn grenade directly above player head (same as pillows)
                Vector3 grenadeSpawnPos = GM.CurrentPlayerBody.Head.position + new Vector3(0f, .25f, 0f);

                // Instantiate the grenade
                GameObject grenadeGO = Instantiate(grenadeObj.GetGameObject(), grenadeSpawnPos, GM.CurrentPlayerBody.Head.rotation);

                // Check if grenade should be armed based on configured chance
                bool shouldArmGrenade = UnityEngine.Random.value < PillowGrenadeArmedChance.Value;
                
                if (shouldArmGrenade)
                {
                    // Get the PinnedGrenade component and release the lever to arm it
                    PinnedGrenade grenade = grenadeGO.GetComponentInChildren<PinnedGrenade>();
                    if (grenade != null)
                    {
                        grenade.ReleaseLever();
                        Logger.LogInfo($"Pillow grenade armed and released! ({PillowGrenadeArmedChance.Value * 100}% chance triggered)");
                    }
                }
                else
                {
                    Logger.LogInfo("Pillow grenade spawned but not armed (safe)");
                }

                // Add physics force (same as pillows)
                Rigidbody grenadeRB = grenadeGO.GetComponent<Rigidbody>();
                if (grenadeRB != null)
                {
                    grenadeRB.AddForce(GM.CurrentPlayerBody.Head.forward * 4000f);
                    grenadeRB.AddTorque(UnityEngine.Random.insideUnitSphere * 5f);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("SpawnPillowGrenade failed: " + ex);
            }
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
            // Determine how many shurikens to spawn
            int shurikenCount = UnityEngine.Random.Range(ShurikenMinCount.Value, ShurikenMaxCount.Value + 1);
            Logger.LogInfo($"Spawning {shurikenCount} shuriken(s)");

            // Set consistent properties for all shurikens (no individual variation)
            float howFast = 30.0f;
            
            // Get the object once
            FVRObject obj = IM.OD["Shuriken"];
            
            // Set consistent position and rotation for all shurikens
            Vector3 shuriPosition = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 0.02f);
            Quaternion shuriRotation = Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward);

            // Spawn the shurikens with identical properties
            for (int i = 0; i < shurikenCount; i++)
            {
                // Spawn all shurikens at exact same position and rotation
                GameObject go = Instantiate(obj.GetGameObject(), shuriPosition, shuriRotation);

                // Apply consistent scale to all shurikens
                go.transform.localScale = new Vector3(ShurikenScale.Value, ShurikenScale.Value, ShurikenScale.Value);
                
                // Apply consistent velocity to all shurikens (straight forward, no variation)
                go.GetComponent<Rigidbody>().velocity = GM.CurrentPlayerBody.Head.forward * howFast;

                // Auto-destroy after 60 seconds
                Destroy(go, 60f);
            }
        }

        public void DangerCloseBarrage()
        {
            // Determine how many danger close rounds to spawn
            int dangerCloseCount = UnityEngine.Random.Range(DangerCloseMinCount.Value, DangerCloseMaxCount.Value + 1);
            Logger.LogInfo($"Spawning {dangerCloseCount} danger close round(s)");

            // Spawn the danger close rounds
            for (int i = 0; i < dangerCloseCount; i++)
            {
                //Set cartridge speed
                float howFast = 30.0f;

                //Set max angle for spread
                float maxAngle = 2.0f;

                Transform PointingTransfrom = transform;

                //Get Random direction for each round
                Vector2 randRot = UnityEngine.Random.insideUnitCircle;

                // Get the object I want to spawn
                FVRObject obj = IM.OD["Cartridge50mmFlareDangerClose"];

                //Set Object Position
                Vector3 dangerClosePosition0 = GM.CurrentPlayerBody.Head.position + (GM.CurrentPlayerBody.Head.forward * 0.02f);

                //Spawn the round
                GameObject go0 = Instantiate(obj.GetGameObject(), dangerClosePosition0, Quaternion.LookRotation(GM.CurrentPlayerBody.Head.forward));

                //Set Object Direction with random spread
                go0.transform.Rotate(new Vector3(randRot.x * maxAngle, randRot.y * maxAngle, 0.0f), Space.Self);

                //Apply velocity and explode
                go0.GetComponent<Rigidbody>().velocity = go0.transform.forward * howFast;
                FVRFireArmRound cartridge = go0.GetComponent<FVRFireArmRound>();
                cartridge.Splode(0.5f, false, true);
            }
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

        // Pillow-triggered zero gravity effect
        IEnumerator ActivatePillowZeroGravity()
        {
            // Store original gravity mode
            var originalGravityMode = GM.Options.SimulationOptions.ObjectGravityMode;
            
            // Activate zero gravity
            GM.Options.SimulationOptions.ObjectGravityMode = SimulationOptions.GravityMode.None;
            GM.CurrentSceneSettings.RefreshGravity();
            Logger.LogInfo($"Pillow zero gravity activated for {PillowZeroGravityDuration.Value} seconds");

            // Wait for configured duration
            yield return new WaitForSecondsRealtime(PillowZeroGravityDuration.Value);

            // Restore original gravity mode
            GM.Options.SimulationOptions.ObjectGravityMode = originalGravityMode;
            GM.CurrentSceneSettings.RefreshGravity();
            Logger.LogInfo("Pillow zero gravity effect ended");
        }

        // Pillow-triggered slow motion effect
        IEnumerator ActivatePillowSlomo()
        {
            // Store original time scale
            float originalTimeScale = Time.timeScale;
            
            // Activate slow motion
            Time.timeScale = MaxSlomo.Value;
            Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
            
            // Update movement scaling if enabled
            slomoMovementController?.UpdateMovementScale(Time.timeScale);
            
            Logger.LogInfo($"Pillow slow motion activated for {PillowSlomoDuration.Value} seconds (scale: {MaxSlomo.Value})");

            // Wait for configured duration
            yield return new WaitForSecondsRealtime(PillowSlomoDuration.Value);

            // Restore original time scale
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = Time.timeScale / SteamVR.instance.hmd_DisplayFrequency;
            
            // Update movement scaling back to normal
            slomoMovementController?.UpdateMovementScale(Time.timeScale);
            
            Logger.LogInfo("Pillow slow motion effect ended");
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
                float angle = i * 90f; // 0�, 90�, 180�, 270� for even distribution
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
            FVRViveHand[] hands = GM.CurrentMovementManager.Hands;
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

            FVRObject selectedFirearm = null;

            // Strategy 1: Use MagazinePatcher-style advanced gun compatibility matching
            if (firearm.ObjectWrapper != null)
            {
                selectedFirearm = FindBestGunMatchMagazinePatcher(firearm.ObjectWrapper, allFirearms, currentKey);
                if (selectedFirearm != null)
                {
                    Logger.LogInfo($"RandomizeHeldGun: Found compatible gun using MagazinePatcher advanced matching: {selectedFirearm.DisplayName}");
                }
            }

            // Strategy 2: Fallback to filtered random selection
            if (selectedFirearm == null)
            {
                // Filter out the current gun if possible
                FVRObject[] selectableFirearms = currentKey != null 
                    ? allFirearms.Where(obj => obj.ItemID != currentKey).ToArray() 
                    : allFirearms;
                
                if (selectableFirearms.Length == 0) selectableFirearms = allFirearms;

                // Pick a random firearm
                selectedFirearm = selectableFirearms[UnityEngine.Random.Range(0, selectableFirearms.Length)];
                Logger.LogInfo($"RandomizeHeldGun: Using random selection: {selectedFirearm.DisplayName}");
            }

            Logger.LogInfo($"RandomizeHeldGun: Selected {selectedFirearm.DisplayName} (ID: {selectedFirearm.ItemID})");

            Destroy(firearm.gameObject);

            // Spawn the new gun
            GameObject newGunGO = Instantiate(selectedFirearm.GetGameObject(), pos, rot);
            var gunRB = newGunGO.GetComponent<Rigidbody>();
            if (gunRB != null) { gunRB.velocity = Vector3.zero; gunRB.angularVelocity = Vector3.zero; }

            // Try to spawn a matching magazine using MagazinePatcher-style advanced compatibility
            TrySpawnMatchingMagazineMagazinePatcher(selectedFirearm, pos, rot);

            Logger.LogInfo($"RandomizeHeldGun: Successfully replaced gun with {selectedFirearm.DisplayName}");
        }

        // ===== MAGAZINEPATCHER-STYLE ADVANCED COMPATIBILITY METHODS =====
        
        // Enhanced magazine spawning method using MagazinePatcher-style advanced compatibility
        private void TrySpawnMatchingMagazineMagazinePatcher(FVRObject selectedFirearm, Vector3 pos, Quaternion rot)
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

                // Strategy 1: Use H3VR's built-in CompatibleMagazines list (highest priority)
                if (selectedFirearm.CompatibleMagazines != null && selectedFirearm.CompatibleMagazines.Count > 0)
                {
                    foreach (var compatibleMag in selectedFirearm.CompatibleMagazines)
                    {
                        var match = allMagazines.FirstOrDefault(m => m.ItemID == compatibleMag.ItemID);
                        if (match != null)
                        {
                            Logger.LogInfo($"MagazinePatcher: Found exact compatible magazine: {match.DisplayName}");
                            matchingMag = match;
                            break;
                        }
                    }
                }

                // Strategy 2: Advanced MagazinePatcher compatibility scoring
                if (matchingMag == null)
                {
                    var magazineScores = new List<MagazinePatcherScore>();
                    
                    foreach (var mag in allMagazines)
                    {
                        int score = CalculateMagazinePatcherCompatibilityScore(selectedFirearm, mag);
                        if (score > 0)
                        {
                            magazineScores.Add(new MagazinePatcherScore { magazine = mag, score = score });
                        }
                    }

                    // Sort by score and return the best match
                    if (magazineScores.Count > 0)
                    {
                        magazineScores.Sort((x, y) => y.score.CompareTo(x.score));
                        var bestMatch = magazineScores[0];
                        
                        // Randomly select from top tier (within 20% of best score)
                        var topTierMatches = magazineScores.Where(s => s.score >= bestMatch.score * 0.8f).ToArray();
                        var selectedMatch = topTierMatches[UnityEngine.Random.Range(0, topTierMatches.Length)];
                        
                        Logger.LogInfo($"MagazinePatcher: Selected magazine with score {selectedMatch.score}: {selectedMatch.magazine.DisplayName}");
                        matchingMag = selectedMatch.magazine;
                    }
                }

                // Strategy 3: Fallback to random magazine
                if (matchingMag == null)
                {
                    matchingMag = allMagazines[UnityEngine.Random.Range(0, allMagazines.Length)];
                    Logger.LogInfo($"RandomizeHeldGun: Using random magazine: {matchingMag.DisplayName}");
                }

                // Spawn the magazine
                if (matchingMag != null)
                {
                    Vector3 magPos = pos + Vector3.up * 0.1f + GM.CurrentPlayerBody.Head.right * UnityEngine.Random.Range(-0.1f, 0.1f);
                    GameObject magGO = Instantiate(matchingMag.GetGameObject(), magPos, rot);
                    var magRB = magGO.GetComponent<Rigidbody>();
                    if (magRB != null) 
                    { 
                        magRB.velocity = Vector3.zero; 
                        magRB.angularVelocity = Vector3.zero; 
                        magRB.AddTorque(UnityEngine.Random.insideUnitSphere * 1f);
                        magRB.AddForce(GM.CurrentPlayerBody.Head.forward * UnityEngine.Random.Range(50f, 75f));
                    }
                    Logger.LogInfo($"RandomizeHeldGun: Successfully spawned magazine: {matchingMag.DisplayName}");
                }
            }
            catch (Exception magEx)
            {
                Logger.LogError("RandomizeHeldGun: Magazine spawn failed: " + magEx);
            }
        }

        // MagazinePatcher-inspired advanced magazine compatibility matching
        private FVRObject FindBestMagazineMatchMagazinePatcher(FVRObject firearm, FVRObject[] allMagazines)
        {
            try
            {
                // Strategy 1: Use H3VR's built-in CompatibleMagazines list (highest priority)
                if (firearm.CompatibleMagazines != null && firearm.CompatibleMagazines.Count > 0)
                {
                    foreach (var compatibleMag in firearm.CompatibleMagazines)
                    {
                        var match = allMagazines.FirstOrDefault(m => m.ItemID == compatibleMag.ItemID);
                        if (match != null)
                        {
                            Logger.LogInfo($"MagazinePatcher: Found exact compatible magazine: {match.DisplayName}");
                            return match;
                        }
                    }
                }

                // Strategy 2: Advanced MagazinePatcher compatibility scoring
                var magazineScores = new List<MagazinePatcherScore>();
                
                foreach (var mag in allMagazines)
                {
                    int score = CalculateMagazinePatcherCompatibilityScore(firearm, mag);
                    if (score > 0)
                    {
                        magazineScores.Add(new MagazinePatcherScore { magazine = mag, score = score });
                    }
                }

                // Sort by score and return the best match
                if (magazineScores.Count > 0)
                {
                    magazineScores.Sort((x, y) => y.score.CompareTo(x.score));
                    var bestMatch = magazineScores[0];
                    
                    // Randomly select from top tier (within 20% of best score)
                    var topTierMatches = magazineScores.Where(s => s.score >= bestMatch.score * 0.8f).ToArray();
                    var selectedMatch = topTierMatches[UnityEngine.Random.Range(0, topTierMatches.Length)];
                    
                    Logger.LogInfo($"MagazinePatcher: Selected magazine with score {selectedMatch.score}: {selectedMatch.magazine.DisplayName}");
                    return selectedMatch.magazine;
                }

                return null;
            }
            catch (Exception ex)
            {
                Logger.LogError($"FindBestMagazineMatchMagazinePatcher failed: {ex}");
                return null;
            }
        }

        // MagazinePatcher-style compatibility scoring
        private int CalculateMagazinePatcherCompatibilityScore(FVRObject firearm, FVRObject magazine)
        {
            int score = 0;

            try
            {
                // Critical compatibility checks (100+ points each)
                
                // 1. MagazineType exact match (H3VR's primary compatibility system)
                if (firearm.MagazineType != 0 && magazine.MagazineType == firearm.MagazineType)
                {
                    score += 150; // Highest priority
                }

                // 2. RoundType compatibility (ammunition type matching)
                if (firearm.UsesRoundTypeFlag && magazine.UsesRoundTypeFlag && 
                    firearm.RoundType != 0 && firearm.RoundType == magazine.RoundType)
                {
                    score += 120;
                }

                // 3. ItemID family matching
                int itemIdScore = CalculateItemIdCompatibility(firearm.ItemID, magazine.ItemID);
                score += itemIdScore;

                // High priority compatibility (50-90 points)
                
                // 4. FirearmAction compatibility
                if (firearm.TagFirearmAction != FVRObject.OTagFirearmAction.None && 
                    firearm.TagFirearmAction == magazine.TagFirearmAction)
                {
                    score += 90;
                }

                // 5. Era compatibility
                if (firearm.TagEra != FVRObject.OTagEra.None && firearm.TagEra == magazine.TagEra)
                {
                    score += 80;
                }

                // 6. Country of origin
                if (firearm.TagFirearmCountryOfOrigin != FVRObject.OTagFirearmCountryOfOrigin.None && 
                    firearm.TagFirearmCountryOfOrigin == magazine.TagFirearmCountryOfOrigin)
                {
                    score += 70;
                }

                // 7. Set compatibility (Real vs Fictional)
                if (firearm.TagSet == magazine.TagSet)
                {
                    score += 60;
                }

                // Medium priority compatibility (20-50 points)
                
                // 8. Round power correlation
                if (firearm.TagFirearmRoundPower != FVRObject.OTagFirearmRoundPower.None && 
                    magazine.TagFirearmRoundPower == firearm.TagFirearmRoundPower)
                {
                    score += 50;
                }

                // 9. Firearm size and magazine capacity correlation
                if (CorrelateFirearmSizeWithMagazineMagazinePatcher(firearm.TagFirearmSize, magazine.MagazineCapacity))
                {
                    score += 40;
                }

                // 10. Brand/manufacturer matching
                int brandScore = CalculateBrandCompatibilityMagazinePatcher(firearm.DisplayName, magazine.DisplayName);
                score += brandScore;

                // 11. Advanced caliber matching
                int caliberScore = CalculateCaliberCompatibilityMagazinePatcher(firearm, magazine);
                score += caliberScore;

                return score;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error calculating MagazinePatcher compatibility score for {magazine.DisplayName}: {ex.Message}");
                return 0;
            }
        }

        // Enhanced gun compatibility for gun-to-gun randomization
        private FVRObject FindBestGunMatchMagazinePatcher(FVRObject currentGun, FVRObject[] allFirearms, string currentKey)
        {
            try
            {
                var gunScores = new List<GunPatcherScore>();
                
                foreach (var gun in allFirearms)
                {
                    if (gun.ItemID == currentKey) continue; // Skip current gun
                    
                    int score = CalculateGunCompatibilityScoreMagazinePatcher(currentGun, gun);
                    if (score > 0)
                    {
                        gunScores.Add(new GunPatcherScore { gun = gun, score = score });
                    }
                }

                if (gunScores.Count == 0) return null;

                // Sort by score and randomly select from top tier
                gunScores.Sort((x, y) => y.score.CompareTo(x.score));
                var bestScore = gunScores[0].score;
                var topTierGuns = gunScores.Where(g => g.score >= bestScore * 0.7f).ToArray();
                
                var selectedGun = topTierGuns[UnityEngine.Random.Range(0, topTierGuns.Length)];
                Logger.LogInfo($"MagazinePatcher Gun Match: Selected {selectedGun.gun.DisplayName} with score {selectedGun.score}");
                
                return selectedGun.gun;
            }
            catch (Exception ex)
            {
                Logger.LogError($"FindBestGunMatchMagazinePatcher failed: {ex}");
                return null;
            }
        }

        // Gun-to-gun compatibility scoring
        private int CalculateGunCompatibilityScoreMagazinePatcher(FVRObject currentGun, FVRObject candidateGun)
        {
            int score = 0;

            try
            {
                // High priority compatibility factors
                


                // 1. Era compatibility (historical context)
                if (currentGun.TagEra == candidateGun.TagEra && currentGun.TagEra != FVRObject.OTagEra.None)
                {
                    score += 60;
                }

                // 2. Country of origin compatibility
                if (currentGun.TagFirearmCountryOfOrigin == candidateGun.TagFirearmCountryOfOrigin && 
                    currentGun.TagFirearmCountryOfOrigin != FVRObject.OTagFirearmCountryOfOrigin.None)
                {
                    score += 50;
                }

                // 3. Set compatibility (Real vs Fictional)
                if (currentGun.TagSet == candidateGun.TagSet)
                {
                    score += 45;
                }

                // 4. Firearm action compatibility
                if (currentGun.TagFirearmAction == candidateGun.TagFirearmAction && 
                    currentGun.TagFirearmAction != FVRObject.OTagFirearmAction.None)
                {
                    score += 40;
                }

                // 5. Size compatibility
                if (currentGun.TagFirearmSize == candidateGun.TagFirearmSize && 
                    currentGun.TagFirearmSize != FVRObject.OTagFirearmSize.None)
                {
                    score += 35;
                }

                // 6. Round power compatibility
                if (currentGun.TagFirearmRoundPower == candidateGun.TagFirearmRoundPower && 
                    currentGun.TagFirearmRoundPower != FVRObject.OTagFirearmRoundPower.None)
                {
                    score += 30;
                }

                return score;
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"Error calculating gun compatibility score: {ex.Message}");
                return 0;
            }
        }

        // Missing core methods that were in the original file
        
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

                // Try comprehensive list of eject methods
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

                Logger.LogWarning($"EmptyHeldGunChamber: Could not find chamber eject method for {gunType}");
            }
            catch (Exception ex)
            {
                Logger.LogError("EmptyHeldGunChamber failed: " + ex);
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

        // Helper structs for MagazinePatcher compatibility scoring
        private struct MagazinePatcherScore
        {
            public FVRObject magazine;
            public int score;
        }

        private struct GunPatcherScore
        {
            public FVRObject gun;
            public int score;
        }

        // Missing helper methods for MagazinePatcher compatibility system
        
        // Advanced ItemID compatibility matching inspired by MagazinePatcher
        private int CalculateItemIdCompatibility(string gunId, string magId)
        {
            if (string.IsNullOrEmpty(gunId) || string.IsNullOrEmpty(magId)) return 0;

            // Exact prefix matches (highest scores)
            for (int prefixLength = Math.Min(gunId.Length, 8); prefixLength >= 3; prefixLength--)
            {
                string gunPrefix = gunId.Substring(0, Math.Min(prefixLength, gunId.Length));
                if (magId.StartsWith(gunPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    return 100 - (8 - prefixLength) * 10; // Longer matches get higher scores
                }
            }

            // Contains matching
            string[] gunParts = gunId.Split(new char[] { '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
            string[] magParts = magId.Split(new char[] { '_', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);

            int containsScore = 0;
            foreach (string gunPart in gunParts)
            {
                if (gunPart.Length >= 3)
                {
                    foreach (string magPart in magParts)
                    {
                        if (gunPart.Equals(magPart, StringComparison.OrdinalIgnoreCase))
                        {
                            containsScore += 30; // Exact part match
                        }
                        else if (magPart.Contains(gunPart) || gunPart.Contains(magPart))
                        {
                            containsScore += 15; // Partial part match
                        }
                    }
                }
            }

            return Math.Min(containsScore, 80); // Cap at 80 to preserve hierarchy
        }

        // Firearm size to magazine capacity correlation
        private bool CorrelateFirearmSizeWithMagazineMagazinePatcher(FVRObject.OTagFirearmSize firearmSize, int magazineCapacity)
        {
            // More sophisticated correlation based on real weapon characteristics
            switch (firearmSize)
            {
                case FVRObject.OTagFirearmSize.Pocket:
                    return magazineCapacity >= 3 && magazineCapacity <= 12; // Pocket pistols
                case FVRObject.OTagFirearmSize.Pistol:
                    return magazineCapacity >= 5 && magazineCapacity <= 25; // Standard pistols
                case FVRObject.OTagFirearmSize.Compact:
                    return magazineCapacity >= 8 && magazineCapacity <= 35; // Compact rifles, SMGs
                case FVRObject.OTagFirearmSize.Carbine:
                    return magazineCapacity >= 10 && magazineCapacity <= 50; // Carbines
                case FVRObject.OTagFirearmSize.FullSize:
                    return magazineCapacity >= 15 && magazineCapacity <= 75; // Full-size rifles
                case FVRObject.OTagFirearmSize.Bulky:
                    return magazineCapacity >= 20 && magazineCapacity <= 100; // Battle rifles, DMRs
                case FVRObject.OTagFirearmSize.Oversize:
                    return magazineCapacity >= 30 && magazineCapacity <= 200; // LMGs, HMGs
                default:
                    return true; // Unknown size, allow any capacity
            }
        }

        // Enhanced brand compatibility matching
        private int CalculateBrandCompatibilityMagazinePatcher(string gunName, string magName)
        {
            string[] gunWords = gunName.ToLower().Split(new char[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
            string[] magWords = magName.ToLower().Split(new char[] { ' ', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);

            // Comprehensive brand matching with weight scoring
            var brandMappings = new Dictionary<string, string[]>
            {
                { "ak", new[] { "ak", "kalashnikov", "izhmash", "saiga", "vepr" } },
                { "ar", new[] { "ar", "armalite", "colt", "m16", "m4", "daniel", "bcm", "anderson" } },
                { "glock", new[] { "glock", "g17", "g19", "g20", "g21", "g22", "g23" } },
                { "sig", new[] { "sig", "sauer", "p226", "p229", "p320", "p365", "mcx", "mpx" } },
                { "beretta", new[] { "beretta", "92", "m9", "px4", "apx" } },
                { "hk", new[] { "hk", "heckler", "koch", "mp5", "g36", "416", "417", "usp", "vp9" } },
                { "fn", new[] { "fn", "fabrique", "scar", "fal", "p90", "five", "seven", "fnx" } },
                { "cz", new[] { "cz", "czech", "75", "82", "p10", "scorpion", "bren" } },
                { "smith", new[] { "smith", "wessen", "sw", "mp", "shield" } },
                { "remington", new[] { "remington", "870", "700", "1100", "11-87" } },
                { "mossberg", new[] { "mossberg", "500", "590", "835" } },
                { "winchester", new[] { "winchester", "1897", "1912", "sxp" } },
                { "ruger", new[] { "ruger", "10/22", "mini", "american", "precision" } },
                { "springfield", new[] { "springfield", "m1a", "1911", "xd", "hellcat" } }
            };

            int brandScore = 0;
            foreach (var mapping in brandMappings)
            {
                bool gunHasBrand = mapping.Value.Any(brand => gunWords.Any(w => w.Contains(brand)));
                bool magHasBrand = mapping.Value.Any(brand => magWords.Any(w => w.Contains(brand)));
                
                if (gunHasBrand && magHasBrand)
                {
                    brandScore += 35; // Strong brand family match
                    break; // Only count highest match
                }
            }

            // Direct word matches for unknown brands
            if (brandScore == 0)
            {
                foreach (string gunWord in gunWords)
                {
                    if (gunWord.Length >= 3)
                    {
                        foreach (string magWord in magWords)
                        {
                            if (gunWord.Equals(magWord, StringComparison.OrdinalIgnoreCase))
                            {
                                brandScore += 20; // Direct word match
                            }
                            else if (gunWord.Length >= 4 && magWord.Length >= 4 && 
                                    (gunWord.Contains(magWord) || magWord.Contains(gunWord)))
                            {
                                brandScore += 10; // Partial word match
                            }
                        }
                    }
                }
            }

            return Math.Min(brandScore, 35); // Cap to maintain hierarchy
        }

        // Advanced caliber compatibility with round power correlation
        private int CalculateCaliberCompatibilityMagazinePatcher(FVRObject firearm, FVRObject magazine)
        {
            int score = 0;

            // Round power matching (primary)
            if (firearm.TagFirearmRoundPower != FVRObject.OTagFirearmRoundPower.None && 
                magazine.TagFirearmRoundPower == firearm.TagFirearmRoundPower)
            {
                score += 30;
            }

            // Text-based caliber matching (secondary)
            string gunText = (firearm.DisplayName + " " + firearm.ItemID).ToLower();
            string magText = (magazine.DisplayName + " " + magazine.ItemID).ToLower();

            // Comprehensive caliber patterns with families
            var caliberFamilies = new Dictionary<string, string[]>
            {
                { "9mm", new[] { "9mm", "9x19", "9para", "luger" } },
                { "45acp", new[] { ".45", "45acp", ".45acp", "45auto" } },
                { "40sw", new[] { ".40", "40sw", ".40sw", "40s&w" } },
                { "357", new[] { ".357", "357mag", "357magnum", ".357mag" } },
                { "38", new[] { ".38", "38special", ".38special", "38spl" } },
                { "380", new[] { ".380", "380acp", ".380acp", "380auto" } },
                { "10mm", new[] { "10mm", "10mmauto" } },
                { "22lr", new[] { ".22", "22lr", ".22lr", "22long" } },
                { "556", new[] { "5.56", "556", "223", ".223", "5.56x45", "223rem" } },
                { "762", new[] { "7.62", "762", "308", ".308", "7.62x51", "308win" } },
                { "762x39", new[] { "7.62x39", "762x39" } },
                { "762x54", new[] { "7.62x54", "762x54", "54r" } },
                { "30-06", new[] { "30-06", "3006", ".30-06", "30.06" } },
                { "270", new[] { ".270", "270win", "270winchester" } },
                { "300", new[] { ".300", "300win", "300blackout", "300blk" } },
                { "50bmg", new[] { ".50", "50bmg", ".50bmg", "50cal" } },
                { "12gauge", new[] { "12gauge", "12ga", "12g" } },
                { "20gauge", new[] { "20gauge", "20ga", "20g" } }
            };

            foreach (var family in caliberFamilies)
            {
                bool gunHasFamily = family.Value.Any(cal => gunText.Contains(cal));
                bool magHasFamily = family.Value.Any(cal => magText.Contains(cal));
                
                if (gunHasFamily && magHasFamily)
                {
                    score += 25; // Strong caliber family match
                    break; // Only count one family match
                }
            }

            return score;
        }
    }
}