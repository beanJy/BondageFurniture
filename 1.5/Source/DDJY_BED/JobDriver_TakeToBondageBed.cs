using Verse;
using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using System;

namespace DDJY_BED
{
    public class JobDriver_TakeToBondageBed : JobDriver_TakeToBed
    {
        protected Pawn prisoner
        {
            get
            {
                return (Pawn)this.job.GetTarget(TargetIndex.A).Thing;
            }
        }

        protected Thing bondageBed
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }
        protected override IEnumerable<Toil> MakeNewToils()
        {
            Pawn localPrisoner = this.prisoner;
            Thing localBondageBed = this.bondageBed;
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnAggroMentalStateAndHostile(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A, false, false, false, true);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnForbidden(TargetIndex.B);
            if (prisoner.Dead)
            {
                yield break;
            }
            yield return Toils_General.WaitWith(TargetIndex.B, 60, true, true, false, TargetIndex.None);
            yield return Toils_Reserve.Release(TargetIndex.B);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    if (this.bondageBed.Destroyed)
                    {
                        this.pawn.jobs.EndCurrentJob(JobCondition.Incompletable, true, true);
                        return;
                    }
                    Building_BondageBed building_BondageBed = (Building_BondageBed)this.bondageBed;
                    if (building_BondageBed == null)
                    {
                        throw new Exception("cant find Building_BondageBed");
                    }
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            //Log.Message("TakeToBondageBed");
            yield return Toils_Bed.TuckIntoBed((Building_BondageBed)this.bondageBed, this.pawn, prisoner, false);
            yield break;
        }
    }
}
