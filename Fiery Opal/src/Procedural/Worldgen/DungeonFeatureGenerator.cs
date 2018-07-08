using FieryOpal.Src.Procedural.Terrain.Dungeons;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public class DungeonFeatureGenerator : VillageFeatureGenerator, INamedObject
    {
        public string Name { get; }
        public int Depth { get; }

        public DungeonFeatureGenerator()
        {
            BaseGraphics.Glyph = 143;
            BaseGraphics.Foreground = Palette.Terrain["World_DungeonForeground"];

            var lairname = new GoodDeityGenerator().Generate().Name;
            Name = "Lair of {0}".Fmt(lairname.Substring(0, lairname.Length - 1));

            Depth = Util.Rng.Next(1, 5);
        }

        protected Dictionary<int, DungeonInstance> Instances = new Dictionary<int, DungeonInstance>();
        protected DungeonInstance GetInstance(int floor, WorldTile parent)
        {
            if (Instances.ContainsKey(floor)) return Instances[floor];

            DungeonInstance instance = new DungeonInstance(floor, Name, parent, new List<DungeonInstance>());
            return (Instances[floor] = instance);
        }

        protected override IEnumerable<Point> MarkRegions(World w)
        {
            var allowedBiomes = new[] { BiomeType.Mountain, BiomeType.Hill };
            Point p;
            do
            {
                p = new Point(Util.Rng.Next(w.Width), Util.Rng.Next(w.Height));
            }
            while (!allowedBiomes.Contains(w.RegionAt(p.X, p.Y).Biome.Type));
            yield return p;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            parent = new WorldTile(parent.ParentWorld, new Point(-2, -2));

            Instances.Clear();
            var prevInst = GetInstance(0, parent);
            var vaultGen = new CavesVaultGenerator(prevInst);
            m.CallLocalGenerator(vaultGen, false);
            prevInst.ConnectedInstances.Add(vaultGen.EntrancePortal.FromInstance);

            for (int i = 1; i < Depth - 1; ++i)
            {
                var inst = GetInstance(i, parent);
                inst.ConnectedInstances.Add(prevInst);
                prevInst.GenerateDownstairPortals(inst).ToList();
                prevInst = inst;
            }
        }
    }
}
