using FieryOpal.Src.Lib;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{


    /*
        ICE SHEET
        FEATURES: Vast expanses of ice, slightly hilly terrain,
                  Frozen rivers, forzen lakes, river deltas,
                  Points of contact with the sea where the ice cracks
                  ICE CAVES

        ICE CAVES
        FEATURES: Can find ancient fossils, deep down lies a passage
                  that brings you to the CENTER OF THE WORLD. Around
                  this height, "subterranean race" outposts may be found.

        CENTER OF THE WORLD
        FEATURES: The link between north pole and south pole, but also
                  a secret place inhabited by mysterious creatures.

    */

    public class IceSheetTerrainGenerator : BiomeTerrainGenerator
    {
        protected IceSheetTerrainGenerator(WorldTile region) : base(region) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            float[,] icebergNoise = Noise.Calc2D(
                Region.WorldPosition.X * m.Width,
                Region.WorldPosition.Y * m.Height,
                m.Width,
                m.Height,
                .023f,
                2,
                .8f
            );

            float[,] icebergMaskNoise = Noise.Calc2D(
                Region.WorldPosition.X * m.Width,
                Region.WorldPosition.Y * m.Height,
                m.Width,
                m.Height,
                .006f,
                3,
                1f
            );

            Workspace.Iter((s, x, y, t) =>
            {
                if (icebergNoise[x, y] * icebergMaskNoise[x, y] < .5f)
                {
                    s.SetTile(x, y, OpalTile.GetRefTile<IceFloorSkeleton>());
                }
                else
                {
                    s.SetTile(x, y, OpalTile.GetRefTile<IceWallSkeleton>());
                }
                return false;
            });
        }
    }
}
