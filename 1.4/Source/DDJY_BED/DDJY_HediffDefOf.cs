using RimWorld;
using Verse;

namespace DDJY_BED
{
    [DefOf]
    public static class DDJY_HediffDefOf
    {
        static DDJY_HediffDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DDJY_HediffDefOf));
        }

        public static HediffDef DDJY_Hediff_BondageBed;
    }
}
