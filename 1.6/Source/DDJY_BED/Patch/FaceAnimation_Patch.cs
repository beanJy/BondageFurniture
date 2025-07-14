using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using RimWorld;

namespace DDJY_BED.Patch
{
    [StaticConstructorOnStartup]
    public class FacialAnimation_Patch
    {

        static FacialAnimation_Patch()
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(mod =>
                mod.PackageId.Equals("nals.facialanimation", StringComparison.OrdinalIgnoreCase)))
            {
                instance.PatchCategory("FacialAnimation");
            }
        }
        public static Harmony instance = new Harmony("FacialAnimation_Patch");
    }
    [HarmonyPatchCategory("FacialAnimation")]
    public static class FacialAnimationControllerComp_Patch
    {
        private static readonly Type FAHelperType = AccessTools.TypeByName("FacialAnimation.FAHelper");
        private static readonly MethodInfo FilterAnimationListWithCurrentStatus = AccessTools.Method(FAHelperType, "FilterAnimationListWithCurrentStatus");
        private static readonly FieldInfo CurrentJobAnimationList = AccessTools.Field(AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp"), "currentJobAnimationList");
        private static readonly FieldInfo PawnField = AccessTools.Field(AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp"), "pawn");

        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var compType = AccessTools.TypeByName("FacialAnimation.FacialAnimationControllerComp");
            return AccessTools.Method(compType, "UpdateAnimation");
        }

        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var codes = instructions.ToList();

            for (int i = 2; i < codes.Count; i++)
            {
                if (codes[i - 2].Calls(FilterAnimationListWithCurrentStatus))
                {
                    codes.InsertRange(i, new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, PawnField),
                        new CodeInstruction(OpCodes.Ldarg_0),
                        new CodeInstruction(OpCodes.Ldfld, CurrentJobAnimationList),
                        new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(FacialAnimationControllerComp_Patch), nameof(FilterAnimations))),
                        new CodeInstruction(OpCodes.Stfld, CurrentJobAnimationList)
                    });
                    break;
                }
            }
            return codes;
        }

        public static IEnumerable<object> FilterAnimations(Pawn pawn, IEnumerable<object> animations)
        {
            if (pawn == null || animations == null)
                return animations;

            var bed = pawn.CurrentBed();
            if (bed != null && bed is Building_BondageBed)
            {
                if (pawn.CurJobDef != JobDefOf.LayDown || !(pawn.jobs?.curDriver?.asleep ?? false))
                {
                    return animations.Where(anim =>
                    {
                        var animDef = Traverse.Create(anim).Field("animationDef").GetValue();
                        var defName = Traverse.Create(animDef).Field("defName").GetValue<string>();
                        return defName.IndexOf("laydown", StringComparison.OrdinalIgnoreCase) < 0;

                    });
                }
            }

            return animations;
        }

    }
}
