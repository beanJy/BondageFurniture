using RimWorld;
using System;
using UnityEngine;
using Verse;
using Verse.AI;

namespace DDJY_BED
{
    public static class DDJY_Toils_LayDown
    {
        public static Toil LayDown(TargetIndex bedOrRestSpotIndex, bool hasBed, bool lookForOtherJobs, bool canSleep = true, bool gainRestAndHealth = true, PawnPosture noBedLayingPosture = PawnPosture.LayingOnGroundNormal, bool deathrest = false)
        {
            Toil layDown = ToilMaker.MakeToil("LayDown");
            layDown.initAction = delegate ()
            {
                Pawn actor = layDown.actor;
                Pawn_PathFollower pather = actor.pather;
                Building_BondageBed building_Bed = (Building_BondageBed)actor.CurJob.GetTarget(bedOrRestSpotIndex).Thing;
                if (pather != null)
                {
                    pather.StopDead();
                }
                JobDriver curDriver = actor.jobs.curDriver;
                if (hasBed)
                {
                    if (!building_Bed.OccupiedRect().Contains(actor.Position))
                    {
                        Log.Error("Can't start LayDown toil because pawn is not in the bed. pawn=" + actor);
                        actor.jobs.EndCurrentJob(JobCondition.Errored, true, true);
                        return;
                    }
                    actor.jobs.posture = PawnPosture.LayingInBed;
                    actor.mindState.lastBedDefSleptIn = building_Bed.def;
                    PortraitsCache.SetDirty(actor);
                }
                else
                {
                    actor.jobs.posture = noBedLayingPosture;
                    actor.mindState.lastBedDefSleptIn = null;
                }
                if (actor.mindState.applyBedThoughtsTick == 0)
                {
                    actor.mindState.applyBedThoughtsTick = Find.TickManager.TicksGame + Rand.Range(2500, 10000);
                    actor.mindState.applyBedThoughtsOnLeave = false;
                }
                if (actor.ownership != null && actor.CurrentBed() != actor.ownership.OwnedBed && !deathrest)
                {
                    ThoughtUtility.RemovePositiveBedroomThoughts(actor);
                }
                CompCanBeDormant comp = actor.GetComp<CompCanBeDormant>();
                if (comp != null)
                {
                    comp.ToSleep();
                }
                //绑定方法
                CompEffectBondageBed compeffectbondagebed = (building_Bed != null) ? building_Bed.TryGetComp<CompEffectBondageBed>() : null;
                if (compeffectbondagebed != null)
                {
                    compeffectbondagebed.AddHediff(actor);
                    actor.jobs.posture = PawnPosture.LayingInBedFaceUp;
                }
            };
            layDown.tickAction = delegate ()
            {
                Pawn actor = layDown.actor;
                Job curJob = actor.CurJob;
                JobDriver curDriver = actor.jobs.curDriver;
                Building_Bed bed = curJob.GetTarget(bedOrRestSpotIndex).Thing as Building_Bed;

                if (!curDriver.asleep)
                {
                    if (canSleep && (RestUtility.CanFallAsleep(actor) || curJob.forceSleep) && (actor.ageTracker.CurLifeStage.canVoluntarilySleep || curJob.startInvoluntarySleep))
                    {
                        curDriver.asleep = true;
                        curJob.startInvoluntarySleep = false;
                    }
                }
                else if (!canSleep || (RestUtility.ShouldWakeUp(actor) && !curJob.forceSleep))
                {
                    curDriver.asleep = false;
                }
                DDJY_Toils_LayDown.ApplyBedRelatedEffects(actor, bed, curDriver.asleep, gainRestAndHealth, deathrest);
                if (lookForOtherJobs && actor.IsHashIntervalTick(211))
                {
                    actor.jobs.CheckForJobOverride();
                    return;
                }
            };
            layDown.defaultCompleteMode = ToilCompleteMode.Never;
            if (hasBed)
            {
                layDown.FailOnBedNoLongerUsable(bedOrRestSpotIndex);
            }
            layDown.AddFinishAction(delegate
            {
                DDJY_Toils_LayDown.FinalizeLayingJob(layDown.actor, hasBed ? ((Building_Bed)layDown.actor.CurJob.GetTarget(bedOrRestSpotIndex).Thing) : null, deathrest);
            });
            return layDown;
        }
        private static void ApplyBedRelatedEffects(Pawn p, Building_Bed bed, bool asleep, bool gainRest, bool deathrest)
        {
            p.GainComfortFromCellIfPossible(false);
            if (asleep && gainRest && p.needs.rest != null)
            {
                float restEffectiveness;
                if (bed != null && bed.def.statBases.StatListContains(StatDefOf.BedRestEffectiveness))
                {
                    restEffectiveness = bed.GetStatValue(StatDefOf.BedRestEffectiveness, true, -1);
                }
                else
                {
                    restEffectiveness = StatDefOf.BedRestEffectiveness.valueIfMissing;
                }
                p.needs.rest.TickResting(restEffectiveness);
            }
            Thing spawnedParentOrMe;
            if (p.IsHashIntervalTick(100) && (spawnedParentOrMe = p.SpawnedParentOrMe) != null && !spawnedParentOrMe.Position.Fogged(spawnedParentOrMe.Map))
            {
                if (asleep && !p.IsColonyMech)
                {
                    FleckDef fleckDef = FleckDefOf.SleepZ;
                    float velocitySpeed = 0.42f;
                    if (p.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Baby || p.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Newborn)
                    {
                        fleckDef = FleckDefOf.SleepZ_Tiny;
                        velocitySpeed = 0.25f;
                    }
                    else if (p.ageTracker.CurLifeStage.developmentalStage == DevelopmentalStage.Child)
                    {
                        fleckDef = FleckDefOf.SleepZ_Small;
                        velocitySpeed = 0.33f;
                    }
                    FleckMaker.ThrowMetaIcon(spawnedParentOrMe.Position, spawnedParentOrMe.Map, fleckDef, velocitySpeed);
                }
                if (gainRest && p.health.hediffSet.GetNaturallyHealingInjuredParts().Any<BodyPartRecord>())
                {
                    FleckMaker.ThrowMetaIcon(spawnedParentOrMe.Position, spawnedParentOrMe.Map, FleckDefOf.HealingCross, 0.42f);
                }
            }
            if (p.mindState.applyBedThoughtsTick != 0 && p.mindState.applyBedThoughtsTick <= Find.TickManager.TicksGame)
            {
                DDJY_Toils_LayDown.ApplyBedThoughts(p, bed);
                p.mindState.applyBedThoughtsTick += 60000;
                p.mindState.applyBedThoughtsOnLeave = true;
            }
            if (ModsConfig.IdeologyActive && bed != null && p.IsHashIntervalTick(2500) && !p.Awake() && (p.IsFreeColonist || p.IsPrisonerOfColony) && !p.IsSlaveOfColony)
            {
                Room room = bed.GetRoom(RegionType.Set_All);
                if (!room.PsychologicallyOutdoors)
                {
                    bool flag = false;
                    foreach (Building_Bed building_Bed in room.ContainedBeds)
                    {
                        foreach (Pawn pawn in building_Bed.CurOccupants)
                        {
                            if (pawn != p && !pawn.Awake() && pawn.IsSlave && !LovePartnerRelationUtility.LovePartnerRelationExists(p, pawn))
                            {
                                p.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptInRoomWithSlave, null, null);
                                flag = true;
                                break;
                            }
                        }
                        if (flag)
                        {
                            break;
                        }
                    }
                }
            }
        }
        private static void FinalizeLayingJob(Pawn pawn, Building_Bed bed, bool deathrest)
        {
            if (pawn.mindState.applyBedThoughtsOnLeave)
            {
                DDJY_Toils_LayDown.ApplyBedThoughts(pawn, bed);
            }
            if (deathrest)
            {
                DDJY_Toils_LayDown.UpdateDeathrestThoughtIndex(pawn);
            }
        }
        private static void ApplyBedThoughts(Pawn actor, Building_Bed bed)
        {
            if (actor.needs.mood == null)
            {
                return;
            }
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInBedroom);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInBarracks);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOutside);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptOnGround);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInCold);
            actor.needs.mood.thoughts.memories.RemoveMemoriesOfDef(ThoughtDefOf.SleptInHeat);
            float ambientTemperature = actor.AmbientTemperature;
            float num = actor.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin, null);
            float num2 = actor.def.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax, null);
            if (ModsConfig.BiotechActive && actor.genes != null)
            {
                foreach (Gene gene in actor.genes.GenesListForReading)
                {
                    if (gene.Active)
                    {
                        num += gene.def.statOffsets.GetStatOffsetFromList(StatDefOf.ComfyTemperatureMin);
                        num2 += gene.def.statOffsets.GetStatOffsetFromList(StatDefOf.ComfyTemperatureMax);
                    }
                }
            }
            if (ambientTemperature < num)
            {
                actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptInCold, null, null);
            }
            if (ambientTemperature > num2)
            {
                actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptInHeat, null, null);
            }
            if (!actor.IsWildMan())
            {
                if (actor.GetRoom(RegionType.Set_All).PsychologicallyOutdoors)
                {
                    actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptOutside, null, null);
                }
                if (bed == null || bed.CostListAdjusted().Count == 0)
                {
                    actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtDefOf.SleptOnGround, null, null);
                }
            }
            if (bed != null && bed == actor.ownership.OwnedBed && !bed.ForPrisoners && !actor.story.traits.HasTrait(TraitDefOf.Ascetic))
            {
                ThoughtDef thoughtDef = null;
                if (bed.GetRoom(RegionType.Set_All).Role == RoomRoleDefOf.Bedroom)
                {
                    thoughtDef = ThoughtDefOf.SleptInBedroom;
                }
                else if (bed.GetRoom(RegionType.Set_All).Role == RoomRoleDefOf.Barracks)
                {
                    thoughtDef = ThoughtDefOf.SleptInBarracks;
                }
                if (thoughtDef != null)
                {
                    int scoreStageIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(bed.GetRoom(RegionType.Set_All).GetStat(RoomStatDefOf.Impressiveness));
                    if (thoughtDef.stages[scoreStageIndex] != null)
                    {
                        actor.needs.mood.thoughts.memories.TryGainMemory(ThoughtMaker.MakeThought(thoughtDef, scoreStageIndex), null);
                    }
                }
            }
            actor.Notify_AddBedThoughts();
        }
        private static void UpdateDeathrestThoughtIndex(Pawn actor)
        {
            if (actor.needs.mood == null || !ModsConfig.BiotechActive)
            {
                return;
            }
            Pawn_GeneTracker genes = actor.genes;
            Gene_Deathrest gene_Deathrest = (genes != null) ? genes.GetFirstGeneOfType<Gene_Deathrest>() : null;
            if (gene_Deathrest == null)
            {
                return;
            }
            Room room = actor.GetRoom(RegionType.Set_All);
            if (room == null)
            {
                gene_Deathrest.chamberThoughtIndex = -1;
                return;
            }
            if (!actor.IsWildMan() && room.PsychologicallyOutdoors)
            {
                gene_Deathrest.chamberThoughtIndex = 0;
                return;
            }
            if (actor.story.traits.HasTrait(TraitDefOf.Ascetic))
            {
                gene_Deathrest.chamberThoughtIndex = -1;
                return;
            }
            gene_Deathrest.chamberThoughtIndex = RoomStatDefOf.Impressiveness.GetScoreStageIndex(room.GetStat(RoomStatDefOf.Impressiveness)) + 1;
        }

        private const int TicksBetweenSleepZs = 100;
        private const int GetUpOrStartJobWhileInBedCheckInterval = 211;
        private const int SlavesInSleepingRoomCheckInterval = 2500;
        private const float VelocityForAdults = 0.42f;
        private const float VelocityForBabies = 0.25f;
        private const float VelocityForChildren = 0.33f;
    }
}

