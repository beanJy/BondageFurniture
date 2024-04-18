using RimWorld;
using Verse;

namespace DDJY_BED
{
    [DefOf]
    public static class DDJY_JobDefOf
    {
        static DDJY_JobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DDJY_JobDefOf));
        }

        public static JobDef DDJY_TakeToBondageBed;
        public static JobDef DDJY_LayDownBind;
        public static JobDef DDJY_GoToUnbind;
    }
}

