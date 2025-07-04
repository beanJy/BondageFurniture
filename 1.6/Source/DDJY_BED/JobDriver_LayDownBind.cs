using Verse;
using RimWorld;
using System.Collections.Generic;
using Verse.AI;

namespace DDJY_BED
{
    public class JobDriver_LayDownBind : JobDriver_LayDown
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            bool hasBed = this.Bed != null;
            Thing thing;
            // Log.Message("进入LayDownBind");
            if (hasBed)
            {   
                yield return Toils_Bed.ClaimBedIfNonMedical(TargetIndex.A, TargetIndex.None);
                yield return Toils_Bed.GotoBed(TargetIndex.A);
            }
            else if ((thing = this.job.GetTarget(TargetIndex.A).Thing) == null || this.pawn.SpawnedParentOrMe != thing)
            {
                yield return Toils_Goto.GotoCell(TargetIndex.A, PathEndMode.OnCell);
            }
            yield return this.LayDownToil(hasBed);
            yield break;
        }

        public override Toil LayDownToil(bool hasBed)
        {
            return DDJY_Toils_LayDown.LayDown(TargetIndex.A, hasBed, this.LookForOtherJobs, this.CanSleep, this.CanRest, PawnPosture.LayingOnGroundNormal, false);
        }
        public override bool LookForOtherJobs
        {
            get
            {
                return false;
            }
        }


    }
}
