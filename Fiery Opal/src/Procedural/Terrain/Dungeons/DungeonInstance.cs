using FieryOpal.Src.Procedural.Terrain;
using FieryOpal.Src.Procedural.Terrain.Dungeons;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public class DungeonInstance : INamedObject
    {
        protected const int DUNGEON_WIDTH = 70;
        protected const int DUNGEON_HEIGHT = 70;


        private OpalLocalMap _map = null;
        public OpalLocalMap Map
        {
            get
            {
                if (_map != null) return _map;
                GenerateMap(DUNGEON_WIDTH, DUNGEON_HEIGHT);
                return _map;
            }
            set => _map = value;
        }
        public int Depth { get; }
        public string Name { get; }

        public List<Portal> DownstairPortals { get; } = new List<Portal>();

        public WorldTile WorldRegion { get; }

        protected TerrainGeneratorBase TerrainGenerator;
        protected TerrainDecoratorBase TerrainDecorator;
        public List<DungeonInstance> ConnectedInstances;

        public DungeonInstance(int floor, string name, WorldTile parentRegion, List<DungeonInstance> pointing)
        {
            ConnectedInstances = pointing;
            Depth = floor;
            Name = name;
            WorldRegion = parentRegion;

            TerrainGenerator = new CavesTerrainGenerator(null, Depth);
            TerrainDecorator = new CavesTerrainDecorator(Depth);
        }

        private void MakeUpstairs(Portal p)
        {
            StairTile stairs = (StairTile)OpalTile.GetRefTile<UpstairSkeleton>().Clone();
            stairs.Portal = p.Invert();
            Map.SetTile(p.ToPos.X, p.ToPos.Y, stairs);
        }

        private void MakeDownstairs(Portal p)
        {
            StairTile stairs = (StairTile)OpalTile.GetRefTile<DownstairSkeleton>().Clone();
            stairs.Portal = p;
            Map.SetTile(p.FromPos.X, p.FromPos.Y, stairs);
        }

        public IList<Portal> GenerateDownstairPortals(DungeonInstance to)
        {
            DownstairPortals.Clear();
            int n_portals = 1;
            for (int i = 0; i < n_portals; ++i)
            {
                Point p = new Point(Util.Rng.Next(DUNGEON_WIDTH), Util.Rng.Next(DUNGEON_HEIGHT));
                DownstairPortals.Add(new Portal()
                {
                    FromInstance = this,
                    FromPos = p,
                    ToInstance = to,
                    ToPos = p
                });
            }

            return DownstairPortals;
        }

        public void GenerateMap(int w, int h)
        {
            Map = new OpalLocalMap(w, h, WorldRegion, Util.Str("Dungeon_InstanceNameFmt", Name, Depth));
            Map.SkyColor = Palette.Terrain["FP_DungeonFog"];

            Map.GenerateAnew(TerrainGenerator, TerrainDecorator);

            foreach (var portal in DownstairPortals)
            {
                Portal p = portal;
                var valid_stair_tiles =
                    Map.TilesWithin(null)
                    .Where(t => !t.Item1.Properties.BlocksMovement)
                    .Select(t => t.Item2)
                    .ToList();
                if (valid_stair_tiles.Count == 0)
                {
                    p.FromPos = Point.Zero;
                    Util.LogText("DungeonInstance: Unable to place downstairs.", true);
                }
                else p.FromPos = Util.Choose(valid_stair_tiles);

                MakeDownstairs(p);
            }

            foreach (var instance in ConnectedInstances)
            {
                var UpstairPortals = instance.Map.TilesWithin(null)
                    .Where(t => (t.Item1 as StairTile)?.Portal?.ToInstance == this)
                    .Select(t => (Portal)(t.Item1 as StairTile)?.Portal.Value);

                foreach (var portal in UpstairPortals)
                {
                    var stair = (StairTile)portal.FromInstance.Map.TileAt(portal.FromPos);

                    Portal p = portal;
                    p.ToPos = Util.Choose(
                        Map.TilesWithin(null)
                        .Where(t => !t.Item1.Properties.BlocksMovement)
                        .Select(t => t.Item2)
                        .ToList()
                    );

                    stair.Portal = p;
                    MakeUpstairs(p);
                }
            }

        }
    }

}
