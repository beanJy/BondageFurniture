using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;
using System;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.Code;
using DDJY_BED;
using System.Runtime.InteropServices;

namespace DDJY_BED.Patch
{
    //HarmonyLib初始化
    [StaticConstructorOnStartup]
    public class PatchMain
    {

        static PatchMain()
        {
            PatchMain.instance.PatchAll(Assembly.GetExecutingAssembly());
        }


        public static Harmony instance = new Harmony("DDJY_BED");
    }
    //当 bed 为 bondagebed 时,Toils_Bed.TuckIntoBed 中的 LayDown 替换为 DDJY_BindLayDown
    [HarmonyPatch(typeof(Pawn_JobTracker), "Notify_TuckedIntoBed")]
    internal class Pawn_JobTracker_Notify_TuckedIntoBed_Patch
    {
        private static bool Prefix(Pawn_JobTracker __instance, Building_Bed bed)
        {
            if (bed is Building_BondageBed)
            {
                FieldInfo pawnField = AccessTools.Field(typeof(Pawn_JobTracker), "pawn");
                if (pawnField != null)
                {
                    Pawn pawn = (Pawn)pawnField.GetValue(__instance);
                    pawn.Position = RestUtility.GetBedSleepingSlotPosFor(pawn, bed);
                    pawn.Notify_Teleported(false, true);
                    pawn.stances.CancelBusyStanceHard();
                    JobDef jobDef = DDJY_JobDefOf.DDJY_LayDownBind;
                    __instance.StartJob(JobMaker.MakeJob(jobDef, bed), JobCondition.InterruptForced, null, false, true, null, new JobTag?(JobTag.TuckedIntoBed), false, false, null, true, true);
                    return false;
                }
            }
            return true;
        }
    }

    //bondagebed 不会被自动分配给 pawn
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

        //    //public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        //    //{
        //    //    var codes = new List<CodeInstruction>(instructions);
        //    //    CodeInstruction ldfldSleepr = null;
        //    //    var label1 = generator.DefineLabel();

        //    //    for (var i = 0; i < codes.Count; i++)
        //    //    {   
        //    //        yield return codes[i];
        //    //        if( ldfldSleepr == null && codes[i].opcode == OpCodes.Stfld && codes[i].operand is FieldInfo fieldInfo && fieldInfo.Name == "sleeper")
        //    //        {
        //    //            //Log.Message(codes[i].operand.ToString());
        //    //            ldfldSleepr = new CodeInstruction(OpCodes.Ldfld, codes[i].operand);
        //    //        }
        //    //        if (i > 1)
        //    //        {
        //    //            if
        //    //            (
        //    //                codes[i - 2].opcode == OpCodes.Ldfld && codes[i - 2].operand is FieldInfo fieldInfo1 && fieldInfo1.Name == "sleeper" &&
        //    //                codes[i - 1].opcode == OpCodes.Call && codes[i - 1].operand is MethodInfo methodInfo2 && methodInfo2.Name == "CurrentBed" &&
        //    //                codes[i].opcode == OpCodes.Callvirt && codes[i].operand is MethodInfo methodInfo1 && methodInfo1.Name == "get_Medical" 

        //    //            )
        //    //            {
        //    //                yield return new CodeInstruction(OpCodes.Brtrue_S, label1);
        //    //                yield return new CodeInstruction(OpCodes.Ldloc_0);
        //    //                yield return ldfldSleepr;
        //    //                yield return new CodeInstruction(OpCodes.Ldfld, typeof(Pawn).GetField("ownership"));
        //    //                yield return new CodeInstruction(OpCodes.Callvirt, typeof(Pawn_Ownership).GetProperty("OwnedBed").GetMethod);
        //    //                yield return new CodeInstruction(OpCodes.Isinst, typeof(Building_BondageBed));
        //    //                codes[i + 2].labels.Add(label1);
        //    //            }
        //    //        }
        //    //    }
        //    //}
    }
    //招募自动解绑
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

    //调整人物位置
    [HarmonyPatch(typeof(PawnRenderer), "GetBodyPos")]
    internal class PawnRenderer_GetBodyPos
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
                        codes[i - 4].ToString() == "ldloc.s 5 (UnityEngine.Vector3)" &&
                        codes[i - 3].ToString() == "ldloc.s 4 (System.Single)" &&
                        codes[i - 2].ToString() == "call static UnityEngine.Vector3 UnityEngine.Vector3::op_Multiply(UnityEngine.Vector3 a, System.Single d)" &&
                        codes[i - 1].ToString() == "call static UnityEngine.Vector3 UnityEngine.Vector3::op_Subtraction(UnityEngine.Vector3 a, UnityEngine.Vector3 b)" &&
                        codes[i].ToString() == "stloc.0 NULL"
                    )
                    {
                        yield return Ldloc_1;
                        yield return Isinst[typeof(Building_BondageBed)];
                        yield return Brfalse[label1]; // 如果对象引用为空，则跳转到标签 label1
                        ////对象引用不为空，可以进行后续操作
                        yield return Ldloc_1;
                        yield return Call[typeof(ThingCompUtility).GetMethod("TryGetComp", new Type[] { typeof(Thing) }).MakeGenericMethod(typeof(CompLayerExtension))]; // 调用 TryGetComp<CompLayerExtension>() 方法
                        yield return Ldloc_0; // 加载局部变量0 (__result) 的值
                        yield return Call[typeof(CompLayerExtension).GetMethod("ChangePawnDrawOffset")]; // 调用 ChangePawnDrawOffset(__result) 方法
                        yield return Stloc_0; // 将结果存储回局部变量0 (__result)

                        yield return new CodeInstruction(OpCodes.Nop).WithLabels(label1);

                    }
                }
            }
        }

    }
}