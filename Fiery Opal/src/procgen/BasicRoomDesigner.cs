using FieryOpal.src.actors;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.procgen
{
    public class BasicRoomDesigner : BuildingDesigner
    {
        public BasicRoomDesigner(Rectangle area) : base(area)
        {
        }

        protected override void GenerateOntoWorkspace()
        {
        }
    }
}
