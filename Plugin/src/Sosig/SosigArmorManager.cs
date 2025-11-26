using System.Collections.Generic;
using FistVR;
using UnityEngine;

namespace H3TVR
{
    public static class SosigArmorManager
    {
        private static readonly Dictionary<int, SosigConfigTemplate.SosigLink> LightArmor = new Dictionary<int, SosigConfigTemplate.SosigLink>
        {
            {0, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Kevlar_1" } },
            {1, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Kevlar_1" } },
            {2, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Kevlar_1" } },
            {3, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Kevlar_1" } },
            {4, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Kevlar_1" } }
        };

        private static readonly Dictionary<int, SosigConfigTemplate.SosigLink> MediumArmor = new Dictionary<int, SosigConfigTemplate.SosigLink>
        {
            {0, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Tac_2" } },
            {1, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Tac_2" } },
            {2, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Tac_2" } },
            {3, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Tac_2" } },
            {4, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Tac_2" } }
        };

        private static readonly Dictionary<int, SosigConfigTemplate.SosigLink> HeavyArmor = new Dictionary<int, SosigConfigTemplate.SosigLink>
        {
            {0, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Oni_1" } },
            {1, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Oni_1" } },
            {2, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Oni_1" } },
            {3, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Oni_1" } },
            {4, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Oni_1" } }
        };

        private static readonly Dictionary<int, SosigConfigTemplate.SosigLink> JuggernautArmor = new Dictionary<int, SosigConfigTemplate.SosigLink>
        {
            {0, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Bulwark_1" } },
            {1, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Bulwark_1" } },
            {2, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Bulwark_1" } },
            {3, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Bulwark_1" } },
            {4, new SosigConfigTemplate.SosigLink { LinkID = "SosigLink_Armor_Bulwark_1" } }
        };

        public static void ApplyArmorToSosig(Sosig sosig, int armorLevel)
        {
            if (sosig == null) return;

            Dictionary<int, SosigConfigTemplate.SosigLink> armorToApply = null;

            switch (armorLevel)
            {
                case 1:
                    armorToApply = LightArmor;
                    break;
                case 2:
                    armorToApply = MediumArmor;
                    break;
                case 3:
                    armorToApply = HeavyArmor;
                    break;
                case 4:
                    armorToApply = JuggernautArmor;
                    break;
                case 5: // God Mode
                    foreach (var part in sosig.Body.SosigBodyParts)
                    {
                        part.Health = 99999f;
                    }
                    return;
                default:
                    return; // No armor
            }

            if (armorToApply != null)
            {
                for (int i = 0; i < sosig.Links.Count; i++)
                {
                    if (armorToApply.ContainsKey(i))
                    {
                        sosig.Links[i] = armorToApply[i];
                    }
                }
                sosig.RebuildSosig();
            }
        }
    }
}
