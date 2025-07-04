using HarmonyLib;
using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using PipeSystem;
using VNPE;
using System.Reflection;
using System;
using System.Text;
using System.Xml.Linq;

namespace DDJY_BED.Patch
{
    //HarmonyLib初始化
    [StaticConstructorOnStartup]
    public class VNPE_Patch
    {
    
        static VNPE_Patch()
        {
            if (ModsConfig.ActiveModsInLoadOrder.Any(mod =>
                mod.PackageId.Equals("vanillaexpanded.vnutriente", StringComparison.OrdinalIgnoreCase)))
            {
                instance.PatchCategory("VNPE");
            }
        }
    
    
        public static Harmony instance = new Harmony("VNPE_Patch");
    }
    
    //添加营养膏消耗
    [HarmonyPatchCategory("VNPE")]
    [HarmonyPatch(typeof(Building_BondageBed))]
    [HarmonyPatch("TickRare")]
    public static class Building_BondageBed_TickRare_Patch
    {
        public static void Postfix(Building_BondageBed __instance)
        {
            var pipeNet = __instance.GetComp<CompResource>()?.PipeNet;
            if (pipeNet == null || pipeNet.Stored <= 0f)
                return;
    
            List<Pawn> occupants = __instance.CurOccupants.ToList();
            foreach (var pawn in occupants)
            {
                if (pawn.needs?.food == null)
                    continue;
    
                if (pawn.needs.food.CurLevelPercentage <= 0.26f)
                {
                    pipeNet.DrawAmongStorage(1f, pipeNet.storages);
    
                    Thing nutrientPaste = ThingMaker.MakeThing(ThingDefOf.MealNutrientPaste);
                    var compIngredients = nutrientPaste.TryGetComp<CompIngredients>();
                    if (compIngredients != null)
                    {
                        foreach (var storage in pipeNet.storages)
                        {
                            var compRegister = storage.parent.TryGetComp<CompRegisterIngredients>();
                            if (compRegister != null)
                            {
                                foreach (var ingredient in compRegister.ingredients)
                                {
                                    compIngredients.RegisterIngredient(ingredient);
                                }
                            }
                        }
                    }
                    float nutritionGained = nutrientPaste.Ingested(pawn, pawn.needs.food.NutritionWanted);
                    pawn.needs.food.CurLevel += nutritionGained;
                    pawn.records.AddTo(RecordDefOf.NutritionEaten, nutritionGained);
                }
            }
        }
    }
    //面板信息
    [HarmonyPatchCategory("VNPE")]
    [HarmonyPatch(typeof(Building_BondageBed))]
    [HarmonyPatch("GetInspectString")]
    public static class Patch_BondageBed_GetInspectString
    {
        public static void Postfix(ref string __result, Building_BondageBed __instance)
        {
            var comp = __instance.GetComp<CompResource>();
            if (comp != null && comp.PipeNet != null)
            {
                float stored = comp.PipeNet.Stored;
                string name = comp.PipeNet.def.resource.name;
                string unit = comp.PipeNet.def.resource.unit;
                StringBuilder pipeSystem_Stored = new StringBuilder();
                pipeSystem_Stored.AppendInNewLine("\n" + "PipeSystem_Stored".Translate(name) + stored.ToString("F0") + " " + unit);
                __result += pipeSystem_Stored;
            }
        }
    }
}