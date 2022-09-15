using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using HarmonyLib;

namespace Aquaman
{
    [BepInPlugin("miZyind.Aquaman", "Aquaman", "2020.09.15")]
    public class AquamanPatches : BaseUnityPlugin
    {
        private static List<string> allowedItems = new List<string> { "$item_hammer" };

        private void Awake()
        {
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
        }

        [HarmonyPatch(typeof(Character), "IsSwiming")]
        public static class Character_IsSwiming_Patch
        {
            public static bool Prefix(Humanoid __instance, float ___m_swimTimer)
            {
                if (___m_swimTimer < 0.5f)
                {
                    if (__instance.IsPlayer())
                    {
                        var stackTrace = new StackTrace();
                        var stackNames = "";
                        var tracingFrame = 2;

                        while (tracingFrame < stackTrace.FrameCount && tracingFrame < 5)
                        {
                            stackNames += stackTrace.GetFrame(tracingFrame).GetMethod().Name;
                            tracingFrame++;
                        }

                        if (stackNames.Contains("UpdateEquipment") || stackNames.Contains("EquipItem"))
                        {
                            var rightItem = __instance.GetRightItem();
                            var leftItem = __instance.GetLeftItem();

                            if (
                                rightItem != null && !allowedItems.Contains(rightItem.m_shared.m_name) ||
                                leftItem != null && !allowedItems.Contains(leftItem.m_shared.m_name)
                            )
                            {
                                __instance.HideHandItems();
                            }
                            else
                            {
                                return false;
                            }

                        }
                    }

                    return true;
                }

                return false;
            }
        }
    }
}