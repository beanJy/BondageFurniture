using Verse;
using RimWorld;
using System.Collections.Generic;
using Verse.AI;
using System;
using UnityEngine;

namespace DDJY_BED
{
    public class JobDriver_GoToUnbind : JobDriver
    {
        protected Thing Thing
        {
            get
            {
                return this.job.GetTarget(TargetIndex.A).Thing;
            }
        }
        protected Thing Target
        {
            get
            {
                return this.job.GetTarget(TargetIndex.B).Thing;
            }
        }


        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return this.pawn.Reserve(this.Target, this.job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOnAggroMentalStateAndHostile(TargetIndex.B);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch).FailOnForbidden(TargetIndex.A);
            Building_BondageBed bondageBed = (Building_BondageBed)this.Thing;
            Pawn prisoner = (Pawn)this.Target;
            if (prisoner.Dead)
            {
                yield break;
            }
            yield return Toils_General.WaitWith(TargetIndex.A, 60, true, true, false, TargetIndex.None);
            yield return Toils_Reserve.Release(TargetIndex.B);
            yield return new Toil
            {
                initAction = delegate ()
                {
                    CompEffectBondageBed compEffectBondageBed = (bondageBed != null) ? bondageBed.TryGetComp<CompEffectBondageBed>() : null;
                    if (compEffectBondageBed == null)
                    {
                        return;
                    }
                    compEffectBondageBed.RemoveHediff(prisoner);
                    prisoner.ownership.UnclaimBed();                
                },
                defaultCompleteMode = ToilCompleteMode.Instant
            };
            yield break;
        }

    }
}
