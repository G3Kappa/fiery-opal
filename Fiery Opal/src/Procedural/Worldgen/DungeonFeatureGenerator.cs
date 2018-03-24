using FieryOpal.src.Procedural.Terrain.Dungeons;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public class StairTile : OpalTile, IInteractive
    {
        public Tuple<OpalLocalMap, Point> Exit { get; set; }

        public StairTile(int id, StairSkeleton skeleton, string name = "Untitled", OpalTileProperties properties = new OpalTileProperties(), Cell graphics = null)
            : base(id, skeleton, name, properties, graphics)
        {
        }

        public bool InteractWith(OpalActorBase actor)
        {
            if(Exit?.Item1 != null)
            {
                actor.ChangeLocalMap(Exit.Item1, Exit.Item2);
                if(actor.IsPlayer)
                {
                    Util.Log("Moving to {0} at {1}.".Fmt(Exit.Item1.Name, Exit.Item2), false);
                }
                return true;
            }
            if (actor.IsPlayer)
            {
                Util.Log("These stairs lead to nowhere.", false);
            }
            return false;
        }

        public override object Clone()
        {
            return new StairTile(GetFirstFreeId(), (StairSkeleton)Skeleton, InternalName, Properties, Graphics);
        }
    }

    public abstract class StairSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties => new OpalTileProperties(
            is_block: false,
            is_natural: false,
            movement_penalty: 0
        );
        public override Cell DefaultGraphics => new Cell(Color.Red, Color.Blue, 'X');
        public override string DefaultName => "Stairs";

        public override OpalTile Make(int id)
        {
            return new StairTile(id, this, DefaultName, DefaultProperties, DefaultGraphics);
        }
    }

    public class DownstairSkeleton : StairSkeleton
    {
        public override OpalTileProperties DefaultProperties => base.DefaultProperties;
        public override Cell DefaultGraphics => new Cell(Color.White, Color.Black, '<');
        public override string DefaultName => "Downstairs";
    }

    public class UpstairSkeleton : StairSkeleton
    {
        public override OpalTileProperties DefaultProperties => base.DefaultProperties;
        public override Cell DefaultGraphics => new Cell(Color.White, Color.Black, '>');
        public override string DefaultName => "Upstairs";
    }

    internal class DungeonInstanceGenerator : WorldFeatureGenerator
    {
        public WorldTile ParentRegion;

        public DungeonInstanceGenerator(DungeonTerrainGenerator tg, WorldTile parent, int floor)
        {
            ParentRegion = parent;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            StairTile stairs = (StairTile)OpalTile.GetRefTile<DownstairSkeleton>().Clone();
            // Make down stairs on the previous map
            stairs.Exit = new Tuple<OpalLocalMap, Point>(m, new Point(0, 0));
            ParentRegion.LocalMap.SetTile(0, 0, stairs);
            // Make up stairs on this map
            stairs = (StairTile)OpalTile.GetRefTile<UpstairSkeleton>().Clone();
            stairs.Exit = new Tuple<OpalLocalMap, Point>(ParentRegion.LocalMap, new Point(0, 0));
            m.SetTile(0, 0, stairs);

            m.SkyColor = Palette.Terrain["FP_DungeonFog"];
            m.Name = "Dungeon Instance";
        }

        protected override IEnumerable<Point> MarkRegions(World w)
        {
            yield return ParentRegion.WorldPosition;
        }
    }

    public class DungeonFeatureGenerator : VillageFeatureGenerator
    {
        public DungeonTerrainGenerator TerrainGenerator;

        public DungeonFeatureGenerator()
        {
            BaseGraphics.Glyph = 159 - 16;
            BaseGraphics.Foreground = Palette.Terrain["World_DungeonForeground"];
        }

        protected virtual WorldTile GetInstance(WorldTile parent, int floor)
        {
            WorldTile region = new WorldTile(parent.ParentWorld, new Point(-2, -2));
            DungeonInstanceGenerator inst_gen = new DungeonInstanceGenerator(TerrainGenerator, parent, floor);
            inst_gen.GetMarkedRegions(null);
            region.FeatureGenerators.Add(inst_gen);
            return region;
        }

        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            TerrainGenerator = new DungeonTerrainGenerator(parent, 100);

            // Generate whole dungeon structure, then link it to m with a vault
            // Structure is: Region -> Map -> Region -> ...
            var first = GetInstance(parent, 0);
            first.LocalMap.GetType();
            parent = first;

            for (int i = 1; i < TerrainGenerator.Depth; ++i)
            {
                var floor = GetInstance(parent, i);
                parent = floor;
            }
        }
    }
}
