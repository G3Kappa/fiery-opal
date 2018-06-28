using Microsoft.Xna.Framework;
using System;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class BiomeTransitioner : BiomeTerrainGenerator
    {

        public BiomeTransitioner(WorldTile worldPos) : base(worldPos) { }

        private static Tuple<Rectangle, Rectangle> GetTransitionRects(Point outer, Point inner)
        {
            int RegionW = Nexus.InitInfo.RegionWidth;
            int RegionH = Nexus.InitInfo.RegionHeight;

            int TransitionW = RegionW / 8;
            int TransitionH = RegionH / 8;

            Rectangle innerRect, outerRect;
            if (outer.Y == inner.Y)
            {
                innerRect = new Rectangle(RegionW - TransitionW, 0, TransitionW, RegionH);
                outerRect = new Rectangle(0, 0, TransitionW, RegionH);
                if (outer.X < inner.X)
                {
                    var temp = innerRect;
                    innerRect = outerRect;
                    outerRect = temp;
                }
            }
            else
            {
                innerRect = new Rectangle(0, RegionH - TransitionH, RegionW, TransitionH);
                outerRect = new Rectangle(0, 0, RegionW, TransitionH);
                if (outer.Y < inner.Y)
                {
                    var temp = innerRect;
                    innerRect = outerRect;
                    outerRect = temp;
                }
            }

            return new Tuple<Rectangle, Rectangle>(outerRect, innerRect);
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);
        }
    }
}
