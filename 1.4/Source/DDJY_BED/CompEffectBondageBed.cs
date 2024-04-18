using System.Collections.Generic;
using RimWorld;
using Verse;

namespace DDJY_BED
{
    public class CompEffectBondageBed : CompUseEffect
    {
        public void AddHediff(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            HediffDef hediffBed = DDJY_HediffDefOf.DDJY_Hediff_BondageBed;
            List<BodyPartRecord> partsWithDef = usedBy.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Arm);
            if (partsWithDef != null)
            {
                foreach (BodyPartRecord bodyPartRecord in partsWithDef)
                {
                    if (bodyPartRecord != null && !usedBy.health.hediffSet.PartIsMissing(bodyPartRecord))
                    {
                        usedBy.health.AddHediff(hediffBed, bodyPartRecord, null, null);
                    }
                }
            }
            List<BodyPartRecord> partsWithDef2 = usedBy.RaceProps.body.GetPartsWithDef(BodyPartDefOf.Leg);
            if (partsWithDef2 == null)
            {
                return;
            }
            foreach (BodyPartRecord bodyPartRecord2 in partsWithDef2)
            {
                if (bodyPartRecord2 != null && !usedBy.health.hediffSet.PartIsMissing(bodyPartRecord2))
                {
                    usedBy.health.AddHediff(hediffBed, bodyPartRecord2, null, null);
                }
            }
        }

        public void RemoveHediff(Pawn usedBy)
        {
            base.DoEffect(usedBy);
            HediffDef hediffBed = DDJY_HediffDefOf.DDJY_Hediff_BondageBed;
            IEnumerable<Hediff> allHediffs = new List<Hediff>(usedBy.health.hediffSet.hediffs);
            foreach (Hediff hediff in allHediffs)
            {
                if(hediff.def == hediffBed)
                {
                    usedBy.health.RemoveHediff(hediff); 
                }
            }
        }
    }
}