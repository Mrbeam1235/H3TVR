using BepInEx.Configuration;
using UnityEngine;

namespace H3TVR
{
    public class SosigCustomizationUI : MonoBehaviour
    {
        private Rect windowRect = new Rect(20, 20, 300, 280);
        private bool showUI = false;

        public static ConfigEntry<int> AllyArmor { get; private set; }
        public static ConfigEntry<int> EnemyArmor { get; private set; }
        public static ConfigEntry<bool> EnableGuns { get; private set; }
        public static ConfigEntry<bool> SlomoOnKill { get; private set; }

        public void Initialize(ConfigFile config)
        {
            AllyArmor = config.Bind("Sosig Customization", "AllyArmor", 0, "Armor for ally sosigs");
            EnemyArmor = config.Bind("Sosig Customization", "EnemyArmor", 0, "Armor for enemy sosigs");
            EnableGuns = config.Bind("Sosig Customization", "EnableGuns", true, "Enable or disable sosig guns");
            SlomoOnKill = config.Bind("Sosig Customization", "SlomoOnKill", false, "Enable slow motion on kill");
        }

        public static void SetAllyArmor(int armorLevel)
        {
            if (AllyArmor != null && armorLevel >= 0 && armorLevel <= 5)
            {
                AllyArmor.Value = armorLevel;
            }
        }

        public static void SetEnemyArmor(int armorLevel)
        {
            if (EnemyArmor != null && armorLevel >= 0 && armorLevel <= 5)
            {
                EnemyArmor.Value = armorLevel;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                showUI = !showUI;
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;

            windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Sosig Customization");
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.Label("Ally Armor Level (0-5)");
            AllyArmor.Value = (int)GUILayout.HorizontalSlider(AllyArmor.Value, 0, 5);
            GUILayout.Label($"Current: {GetArmorLabel(AllyArmor.Value)}");

            GUILayout.Space(10);

            GUILayout.Label("Enemy Armor Level (0-5)");
            EnemyArmor.Value = (int)GUILayout.HorizontalSlider(EnemyArmor.Value, 0, 5);
            GUILayout.Label($"Current: {GetArmorLabel(EnemyArmor.Value)}");

            GUILayout.Space(10);

            EnableGuns.Value = GUILayout.Toggle(EnableGuns.Value, "Enable Guns");

            SlomoOnKill.Value = GUILayout.Toggle(SlomoOnKill.Value, "Slomo on Kill");

            GUI.DragWindow();
        }

        private string GetArmorLabel(int armorLevel)
        {
            switch (armorLevel)
            {
                case 0: return "None";
                case 1: return "Light";
                case 2: return "Medium";
                case 3: return "Heavy";
                case 4: return "Juggernaut";
                case 5: return "God Mode";
                default: return "Unknown";
            }
        }
    }
}
