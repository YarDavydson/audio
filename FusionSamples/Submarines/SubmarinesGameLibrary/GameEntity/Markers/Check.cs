﻿using SubmarinesWars.SubmarinesGameLibrary.Field;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubmarinesWars.SubmarinesGameLibrary.GameEntity.Markers
{
    public class Check : Marker
    {
        public Check(Cell cell)
            : base(LogicService.check, cell)
        { }

        internal override VisibleObject Copy(VisibleObject parent)
        {
            Marker marker = new Check(Cell);
            marker.Parent = (EntityCollection)parent;
            return marker;
        }
    }
}
