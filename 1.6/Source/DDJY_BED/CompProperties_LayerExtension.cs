using System.Collections.Generic;
using Verse;

namespace DDJY_BED
{
    public class CompProperties_LayerExtension : CompProperties
    {
        public CompProperties_LayerExtension()
        {
            this.compClass = typeof(CompLayerExtension);
        }

        public List<CompProperties_LayerExtension.GA> gas;

        public class GA
        {
            public AltitudeLayer altitudeLayer;

            public GraphicData graphicData;
        }
        public bool keepFaceUp;
    }
}
