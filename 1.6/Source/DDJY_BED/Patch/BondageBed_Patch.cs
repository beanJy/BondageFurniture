using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.Code;

namespace DDJY_BED.Patch
{
    //HarmonyLib初始化
    [StaticConstructorOnStartup]
    public class Building_BondageBed_Patch
    {

        static Building_BondageBed_Patch()
        {
            instance.PatchCategory("Building_BondageBed");
        }


        public static Harmony instance = new Harmony("Building_BondageBed_Patch");
    }

    //bondagebed 不会被自动分配给 pawn
    [HarmonyPatchCategory("Building_BondageBed")]
    [HarmonyPatch(typeof(RestUtility), "IsValidBedFor")]
    internal class RestUtility_IsValidBedFor
    {
        private static bool Prefix(ref bool __result, Thing bedThing, Pawn sleeper)
        {
            if (!(bedThing is Building_BondageBed))
            {
                return true;
            }
            __result = false;
            return false;
        }
    }

    //pawn从床上意外掉落可被送回床上
    [HarmonyPatchCategory("Building_BondageBed")]
    [HarmonyPatch(typeof(RestUtility), "FindBedFor", new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus?) })]
    internal class RestUtility_FindBedFor_Patch
    {
        private static bool Prefix(ref Building_Bed __result, Pawn sleeper, Pawn traveler, bool checkSocialProperness, bool ignoreOtherReservations = false, GuestStatus? guestStatus = null)
        {
            if (sleeper.ownership.OwnedBed != null && sleeper.ownership.OwnedBed is Building_BondageBed)
            {
                __result = sleeper.ownership.OwnedBed;
                return false;
            }
            return true;
        }
    }
    //招募自动解绑
    [HarmonyPatchCategory("Building_BondageBed")]
    [HarmonyPatch(typeof(RestUtility), "CanUseBedNow", new Type[] { typeof(Pawn), typeof(Pawn), typeof(bool), typeof(bool), typeof(GuestStatus?) })]
    internal class RestUtility_CanUseBedNow_Patch
    {
        private static bool Prefix(Thing bedThing, Pawn sleeper)
        {
            if (bedThing is Building_BondageBed && !sleeper.IsPrisonerOfColony)
            {
                CompEffectBondageBed compEffectBondageBed = (bedThing != null) ? bedThing.TryGetComp<CompEffectBondageBed>() : null;
                compEffectBondageBed.RemoveHediff(sleeper);
            }
            return true;
        }
    }

    //调整人物角度
    [HarmonyPatchCategory("Building_BondageBed")]
    [HarmonyPatch(typeof(PawnRenderer), "BodyAngle")]
    internal class PawnRenderer_BodyAngle
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {

            var label1 = generator.DefineLabel();
            var codes = new List<CodeInstruction>(instructions);
            for (var i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (i > 4)
                {
                    if
                    (
                        codes[i - 3].ToString() == "call System.Int32 Verse.Rot4::get_AsInt()" &&
                        codes[i - 2].opcode == OpCodes.Ldc_I4_2 &&
                        codes[i - 1].opcode == OpCodes.Add &&
                        codes[i].ToString() == "call System.Void Verse.Rot4::set_AsInt(System.Int32 value)"
                    )
                    {

                        yield return Ldloc_0;
                        yield return Isinst[typeof(Building_BondageBed)];
                        yield return Brfalse[label1]; // 如果对象引用为空，则跳转到标签 label1
                        ////对象引用不为空，可以进行后续操作
                        yield return Ldloc_0;
                        yield return Call[typeof(ThingCompUtility).GetMethod("TryGetComp", new Type[] { typeof(Thing) }).MakeGenericMethod(typeof(CompLayerExtension))]; // 调用 TryGetComp<CompLayerExtension>() 方法
                        yield return Ldloc_S[4]; // 加载局部变量4 (rotation) 的值
                        yield return Call[typeof(CompLayerExtension).GetMethod("ChangePawnRotation")]; // 调用 ChangePawnRotation(rotation) 方法
                        yield return Stloc_S[4]; // 将结果存储回局部变量4 (rotation)

                        yield return new CodeInstruction(OpCodes.Nop).WithLabels(label1);
                    }
                }
            }
        }

    }
    //给睡眠状态添加束缚状态和睡眠姿势
    [HarmonyPatchCategory("Building_BondageBed")]
    [HarmonyPatch(typeof(Toils_LayDown), nameof(Toils_LayDown.LayDown))]
    public static class Patch_Toils_LayDown_LayDown
    {
        public static void Postfix(ref Toil __result, TargetIndex bedOrRestSpotIndex)
        {
            Toil localToil = __result;
            Action oldInit = localToil.initAction;
            localToil.initAction = () =>
            {
                oldInit?.Invoke();
                Pawn actor = localToil.actor;
                if (actor == null || actor.CurJob == null)
                    return;
                Thing bedThing = actor.CurJob.GetTarget(bedOrRestSpotIndex).Thing;
                if (bedThing is Building_BondageBed bondageBed)
                { 
                    CompEffectBondageBed compeffectbondagebed = bedThing.TryGetComp<CompEffectBondageBed>();
                    if (compeffectbondagebed != null)
                    {
                        compeffectbondagebed.AddHediff(actor);
                        actor.jobs.posture = PawnPosture.LayingInBedFaceUp;
                    }
                }
            };
        }
    }
    //束缚床上停止寻找工作
    [HarmonyPatchCategory("Building_BondageBed")]
    [HarmonyPatch(typeof(JobDriver_LayDown))]
    [HarmonyPatch("LookForOtherJobs", MethodType.Getter)]
    public static class Patch_JobDriver_LayDown_LookForOtherJobs_Getter
    {
        public static void Postfix(JobDriver_LayDown __instance, ref bool __result)
        {
            var bed = __instance.Bed;
            if (bed is Building_BondageBed)
            {
                __result = false;
            }
        }
    }
}