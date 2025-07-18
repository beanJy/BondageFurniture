﻿using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace DDJY_BED
{
    public class CompLayerExtension : ThingComp
    {
        public CompProperties_LayerExtension Props
        {
            get
            {
                return (CompProperties_LayerExtension)this.props;
            }
        }
        public override void PostDraw()
        {
            base.PostDraw();
            if (this.Props.gas == null || this.Props.gas.Count == 0)
            {
                return;
            }
            foreach (CompProperties_LayerExtension.GA ga in this.Props.gas)
            {
                ga.graphicData.Graphic.Draw(GenThing.TrueCenter(this.parent.Position, this.parent.Rotation, this.parent.def.size, ga.altitudeLayer.AltitudeFor()), this.parent.Rotation, this.parent, 0f);
            }
        }
        public Rot4 ChangePawnRotation(Rot4 rotation)
        {
            if(this.Props.keepFaceUp == true)
            {
                rotation.AsInt = 0;
            }
            return rotation;
        }
    }
}