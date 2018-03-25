using FieryOpal.Src;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Procedural.Worldgen;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.Procedural.Terrain.Dungeons
{
    /// <summary>
    /// Generates an entrance vault for a dungeon.
    /// </summary>
    class DungeonVaultGenerator : BuildingGeneratorBase
    {
        public DungeonInstance Dungeon;
        public Point EntranceLocation;

        public Portal EntrancePortal;

        public DungeonVaultGenerator(DungeonInstance dungeonEntrance)
        {
            Dungeon = dungeonEntrance;
        }

        private bool ValidTile(OpalTile t)
        {
            return true;
        }

        public override void Generate(OpalLocalMap m)
        {
            base.Generate(m);
            
            // p is where the stairs to the next map are placed
            var p = EntranceLocation = Util.Choose(
                m.TilesWithin(null)
                .Where(t => !Util.OOB(t.Item2.X, t.Item2.Y, m.Width - 2, m.Height - 2, 2, 2) && ValidTile(t.Item1))
                .Select(t => t.Item2).ToList()
            );

            StairTile stairs = (StairTile)OpalTile.GetRefTile<DownstairSkeleton>().Clone();
            OpalTile wall = OpalTile.GetRefTile<NaturalWallSkeleton>();
            OpalTile door = (DoorTile)OpalTile.GetRefTile<DoorSkeleton>().Clone();

            Workspace.Iter((s, x, y, t) =>
            {
                s.SetTile(x, y, wall);
                return false;
            }, new Rectangle(p - new Point(1), new Point(3)));
            Workspace.SetTile(p.X, p.Y + 1, door);

            var dummyInstance = new DungeonInstance(0, "Overworld", m.ParentRegion, new List<Portal>());
            dummyInstance.Map = m;
            stairs.Portal = EntrancePortal = new Portal()
            {
                FromInstance = dummyInstance,
                FromPos = p,
                ToInstance = Dungeon,
                ToPos = EntranceLocation
            };
            Workspace.SetTile(p.X, p.Y, stairs);
        }
    }
}
