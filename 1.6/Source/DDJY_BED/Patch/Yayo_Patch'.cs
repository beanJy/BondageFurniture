using HarmonyLib;
using RimWorld;
using Verse;
using System.Linq;
using System;

namespace DDJY_BED.Patch
{
    //HarmonyLib初始化
    [StaticConstructorOnStartup]
    public class Yayo_Patch
    {

        static Yayo_Patch()
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(mod =>
                mod.PackageId.Equals("com.yayo.yayoani.continued", StringComparison.OrdinalIgnoreCase)))
            {
                instance.PatchCategory("Yayo");
            }
        }
        public static Harmony instance = new Harmony("Yayo_Patch");
    }

    [HarmonyPatchCategory("Yayo")]
    public static class AnimationCore_AniLaying
    {
        [HarmonyTargetMethod]
        public static System.Reflection.MethodBase TargetMethod()
        {
            var type = AccessTools.TypeByName("YayoAnimation.AnimationCore");
            if (type == null) return null;
            return AccessTools.Method(type, "AniLaying");
        }

        public static bool Prefix(Pawn pawn, object pdd, string defName)
        {
            if (defName == "LayDown" && (pawn.jobs?.curDriver?.asleep ?? false))
            {
                var bed = pawn.CurrentBed();
                if (bed is Building_BondageBed)
                {
                    return false;
                }
            }

            return true;
        }
    }

}