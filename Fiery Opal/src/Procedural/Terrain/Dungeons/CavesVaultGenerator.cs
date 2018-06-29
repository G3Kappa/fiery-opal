using FieryOpal.Src.Actors.Items;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using FieryOpal.Src.Procedural.Worldgen;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Procedural.Terrain.Dungeons
{
    /// <summary>
    /// Generates an entrance vault for a dungeon.
    /// </summary>
    class CavesVaultGenerator : BuildingGeneratorBase
    {
        public DungeonInstance Dungeon { get; }
        public Portal EntrancePortal { get; private set; }

        public CavesVaultGenerator(DungeonInstance dungeonEntrance)
        {
            Dungeon = dungeonEntrance;
        }

        private bool ValidTile(OpalLocalMap m, Tuple<OpalTile, Point> t)
        {
            return
                !Util.OOB(t.Item2.X, t.Item2.Y, m.Width - 20, m.Height - 20, 20, 20)
                && m.TilesWithinRing(t.Item2.X, t.Item2.Y, 5, 0)
                .All(x => !x.Item1.Properties.BlocksMovement);
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);

            // p is where the stairs to the next map are placed
            var p = Util.Choose(
                m.TilesWithin(null)
                .Where(t => ValidTile(m, t))
                .Select(t => t.Item2).ToList()
            );

            StairTile stairs = (StairTile)OpalTile.GetRefTile<DownstairSkeleton>().Clone();
            OpalTile wall = OpalTile.GetRefTile<NaturalWallSkeleton>();
            OpalTile floor = OpalTile.GetRefTile<RockFloorSkeleton>();

            var shape = GenUtil.MakeRockShape(new Rectangle(p.X - 10, p.Y - 10, 20, 20));
            Workspace.DrawShape(shape, wall);

            // Create passage w/ random walk biased towards going away from p
            var traversed = GenUtil.WeightedRandomWalk(Workspace, p, P => (float)-P.Dist(p), t => t == floor, t => t == wall, floor).ToList();

            Point dir = (traversed.Last() - traversed[traversed.Count - 2]);
            new Torch().ChangeLocalMap(Workspace, Workspace.FirstAccessibleTileInLine(traversed.Last() + dir.ToVector2().Orthogonal().ToPoint(), traversed.Last() + dir.ToVector2().Orthogonal().ToPoint() + dir * new Point(10)));
            new Torch().ChangeLocalMap(Workspace, Workspace.FirstAccessibleTileInLine(traversed.Last() - dir.ToVector2().Orthogonal().ToPoint(), traversed.Last() - dir.ToVector2().Orthogonal().ToPoint() + dir * new Point(10)));

            var dummyInstance = new DungeonInstance(0, "Overworld", m.ParentRegion, new List<DungeonInstance>());
            dummyInstance.Map = m;
            stairs.Portal = EntrancePortal = new Portal()
            {
                FromInstance = dummyInstance,
                FromPos = p,
                ToInstance = Dungeon,
                ToPos = p
            };
            Workspace.SetTile(p.X, p.Y, stairs);
        }
    }
}
