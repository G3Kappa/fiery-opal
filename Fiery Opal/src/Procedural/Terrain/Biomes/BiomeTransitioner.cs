using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class BiomeTransitioner : BiomeTerrainGenerator
    {
        /*
         *  --------- | ---------
         *  |      T| T |T      |
         *  |      T| T |T      |
         *  |      T| T |T      |
         *  --------- | ---------
         * Biome transitioning applies a mask of a fixed size at the edge of two tiles.
         * This mask evenly blends the terrain and decorations of both biomes.
         * */

        protected const int TRANSITION_W = WorldTile.REGION_WIDTH / 8;
        protected const int TRANSITION_H = WorldTile.REGION_HEIGHT / 8;

        public BiomeTransitioner(WorldTile worldPos) : base(worldPos) { }

        private static Tuple<Rectangle, Rectangle> GetTransitionRects(Point outer, Point inner)
        {
            Rectangle innerRect, outerRect;
            if(outer.Y == inner.Y)
            {
                innerRect = new Rectangle(WorldTile.REGION_WIDTH - TRANSITION_W, 0, TRANSITION_W, WorldTile.REGION_HEIGHT);
                outerRect = new Rectangle(0, 0, TRANSITION_W, WorldTile.REGION_HEIGHT);
                if(outer.X < inner.X)
                {
                    var temp = innerRect;
                    innerRect = outerRect;
                    outerRect = temp;
                }
            }
            else
            {
                innerRect = new Rectangle(0, WorldTile.REGION_HEIGHT - TRANSITION_H, WorldTile.REGION_WIDTH, TRANSITION_H);
                outerRect = new Rectangle(0, 0, WorldTile.REGION_WIDTH, TRANSITION_H);
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
