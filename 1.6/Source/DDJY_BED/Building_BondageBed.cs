using Verse;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse.AI;
using System.Text;
using UnityEngine;


namespace DDJY_BED
{
    public class Building_BondageBed : Building_Bed
    {
        //按钮图标
        public override IEnumerable<Gizmo> GetGizmos()
        {
            Faction currentFaction = this.factionInt;
            this.factionInt = null;
            foreach (Gizmo gizmo in base.GetGizmos())
            {
                yield return gizmo;
            }
            IEnumerator<Gizmo> enumerator = null;
            this.factionInt = currentFaction;
            yield break;
            yield break;
        }
        //不显示占有者状态
        public override void DrawGUIOverlay() { return; }
        //放置初始化建筑
        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.ForPrisoners = true;
            base.Medical = false;
            base.SpawnSetup(map, respawningAfterLoad);
        }
        //移除时执行
        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            if (this.OwnersForReading.Count > 0)
            {
                foreach (Pawn pawn in this.OwnersForReading)
                {
                    CompEffectBondageBed compEffectBondageBed = this.GetComp<CompEffectBondageBed>();
                    if (compEffectBondageBed != null)
                    {
                        compEffectBondageBed.RemoveHediff(pawn);
                        //this.occupants = null;
                    }
                }
            }
            Building_BondageBed building_bed = null;
            base.DeSpawn(mode);

        }
        //面板显示内容
        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (this.def.building.bed_humanlike)
            {
                stringBuilder.AppendInNewLine("ForPrisonerUse".Translate());
            }
            return stringBuilder.ToString();
        }
        //右键菜单
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn myPawn)
        {
            if (myPawn.RaceProps.Humanlike && !myPawn.Drafted && base.Faction == Faction.OfPlayer && RestUtility.CanUseBedEver(myPawn, this.def))
            {
                if (this.OwnersForReading.Count > 0)
                {
                    foreach (Pawn pawn in this.OwnersForReading)
                    {
                        Action action = delegate ()
                        {
                            Job job = JobMaker.MakeJob(DDJY_JobDefOf.DDJY_GoToUnbind, this, pawn);
                            job.count = 1;
                            myPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                            myPawn.mindState.ResetLastDisturbanceTick();
                        };
                        yield return new FloatMenuOption("Unbind".Translate() + pawn.Name.ToStringShort + ", " + pawn.story.TitleShort, action, MenuOptionPriority.High, null, null, 0f, null, null, true, 0);
                    }
                }
                else
                {
                    List<Pawn> BindablePawnList = this.BindablePawnList(myPawn.Map.mapPawns.PrisonersOfColonySpawned);
                    if (!(BindablePawnList.Count > 0))
                    {
                        yield return new FloatMenuOption("NoBindablePrisoners".Translate(), null, MenuOptionPriority.High, null, null, 0f, null, null, true, 0);
                    }
                    else
                    {
                        foreach (Pawn prisoners in BindablePawnList)
                        {
                            Action action = delegate ()
                            {
                                if (myPawn.CanReserveAndReach(this, PathEndMode.ClosestTouch, Danger.Deadly, this.SleepingSlotsCount, -1, null, true))
                                {
                                    Job job = JobMaker.MakeJob(DDJY_JobDefOf.DDJY_TakeToBondageBed, prisoners, this);
                                    job.count = 1;
                                    myPawn.jobs.TryTakeOrderedJob(job, new JobTag?(JobTag.Misc), false);
                                    myPawn.mindState.ResetLastDisturbanceTick();
                                }
                            };
                            yield return new FloatMenuOption("Bind".Translate() + prisoners.Name.ToStringShort + ", " + prisoners.story.TitleShort, action, MenuOptionPriority.High, null, null, 0f, null, null, true, 0);
                        }
                    }
                }
            }
        }
        //自定义 右键菜单 人物列表
        private List<Pawn> BindablePawnList(List<Pawn> list)
        {
            List<Pawn> newList = new List<Pawn>();
            foreach (Pawn p in list)
            {
                if (!(p.CurrentBed() is Building_BondageBed) && p.IsPrisoner)
                {
                    newList.Add(p);
                }
            }
            return newList;
        }
        public override void TickRare()
        {
            base.TickRare();
        }
    }
}