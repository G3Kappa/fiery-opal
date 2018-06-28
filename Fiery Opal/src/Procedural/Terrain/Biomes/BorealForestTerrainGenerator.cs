using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;

namespace FieryOpal.Src.Procedural.Terrain.Biomes
{
    public class BorealForestTerrainGenerator : BiomeTerrainGenerator
    {
        protected BorealForestTerrainGenerator(WorldTile worldPos) : base(worldPos) { }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            Workspace.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, OpalTile.GetRefTile<GrassSkeleton>());
                return false;
            });

            var poisson = Lib.PoissonDiskSampler.SampleRectangle(Vector2.Zero, new Vector2(m.Width, m.Height), 3.33f);
            foreach (Vector2 v in poisson)
            {
                Actors.Plant tree = new Actors.Pine();
                tree.ChangeLocalMap(Workspace, v.ToPoint(), true);
            }
        }
    }
}
