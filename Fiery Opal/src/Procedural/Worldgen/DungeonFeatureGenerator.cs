using FieryOpal.src.Procedural.Terrain.Dungeons;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Linq;
using System.Collections.Generic;
using FieryOpal.Src.Procedural.Terrain;

namespace FieryOpal.Src.Procedural.Worldgen
{
    public struct Portal
    {
        public DungeonInstance FromInstance;
        public Point FromPos;
        public DungeonInstance ToInstance;
        public Point ToPos;

        public Portal Invert()
        {
            return new Portal()
            {
                FromInstance = ToInstance,
                FromPos = ToPos,
                ToInstance = FromInstance,
                ToPos = FromPos
            };
        }
    }

    public class StairTile : OpalTile, IInteractive
    {
        public Portal? Portal { get; set; }

        public StairTile(int id, StairSkeleton skeleton, string name = "Untitled", OpalTileProperties properties = new OpalTileProperties(), Cell graphics = null)
            : base(id, skeleton, name, properties, graphics)
        {
        }

        public bool InteractWith(OpalActorBase actor)
        {
            if (Portal?.ToInstance != null)
            {
                actor.ChangeLocalMap(Portal.Value.ToInstance.Map, Portal.Value.ToPos);
                if (actor.IsPlayer)
                {
                    Util.Log(Util.Str("Player_UsingStairs", Portal.Value.ToInstance.Map.Name, Portal.Value.ToPos), false);
                }
                return true;
            }
            if (actor.IsPlayer)
            {
                Util.Log(Util.Str("Player_StairsUnconnected"), false);
            }
            return false;
        }

        public override object Clone()
        {
            return new StairTile(GetFirstFreeId(), (StairSkeleton)Skeleton, Name, Properties, Graphics);
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
        public List<Portal> UpstairPortals { get; }
        public int Depth { get; }
        public string Name { get; }

        public List<Portal> DownstairPortals { get; } = new List<Portal>();
        
        public WorldTile WorldRegion { get; }

        protected TerrainGeneratorBase TerrainGenerator;

        public DungeonInstance(int floor, string name, WorldTile parentRegion, List<Portal> pointing)
        {
            UpstairPortals = pointing;
            Depth = floor;
            Name = name;
            WorldRegion = parentRegion;

            TerrainGenerator = new DungeonTerrainGenerator(null, Depth);
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
            Map.SetTile(p.ToPos.X, p.ToPos.Y, stairs);
        }

        public IList<Portal> GenerateDownstairPortals(DungeonInstance to)
        {
            DownstairPortals.Clear();
            int n_portals = 3;
            for (int i = 0; i < n_portals; ++i)
            {
                Point p = new Point(Util.GlobalRng.Next(DUNGEON_WIDTH), Util.GlobalRng.Next(DUNGEON_HEIGHT));
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

            Map.GenerateAnew(TerrainGenerator);
            foreach(var portal in UpstairPortals)
            {
                MakeUpstairs(portal);
            }

            foreach(var portal in DownstairPortals)
            {
                MakeDownstairs(portal);
            }
        }
    }
    
    public class DungeonFeatureGenerator : VillageFeatureGenerator, INamedObject
    {
        public DungeonTerrainGenerator TerrainGenerator;

        public string Name { get; }
        public int Depth { get; }

        public DungeonFeatureGenerator()
        {
            BaseGraphics.Glyph = 143;
            BaseGraphics.Foreground = Palette.Terrain["World_DungeonForeground"];

            var lairname = new GoodDeityGenerator().Generate().Name;
            Name = "Lair of {0}".Fmt(lairname.Substring(0, lairname.Length - 1));

            Depth = Util.GlobalRng.Next(3, 30);
        }

        protected Dictionary<int, DungeonInstance> Instances = new Dictionary<int, DungeonInstance>();
        protected DungeonInstance GetInstance(int floor, WorldTile parent)
        {
            if (Instances.ContainsKey(floor)) return Instances[floor];

            DungeonInstance instance = new DungeonInstance(floor, Name, parent, new List<Portal>());
            return (Instances[floor] = instance);
        }
        
        protected override void GenerateLocal(OpalLocalMap m, WorldTile parent)
        {
            parent = new WorldTile(parent.ParentWorld, new Point(-2, -2));

            Instances.Clear();
            var prevInst = GetInstance(0, parent);
            var vaultGen = new DungeonVaultGenerator(prevInst);
            m.CallLocalGenerator(vaultGen);
            prevInst.UpstairPortals.Add(vaultGen.EntrancePortal);

            for(int i = 1; i < Depth - 1; ++i)
            {
                var inst = GetInstance(i, parent);
                prevInst.GenerateDownstairPortals(inst).ToList();
                inst.UpstairPortals.AddRange(prevInst.DownstairPortals);
                prevInst = inst;
            }
        }
    }
}
