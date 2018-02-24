using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Linq;
using System.Collections.Generic;
using FieryOpal.src.ui;

namespace FieryOpal.src
{

    public struct OpalTileProperties
    {
        public bool BlocksMovement, IsNatural;
        public float MovementPenalty, Fertility;

        public OpalTileProperties(bool blocks_movement = false, bool is_natural = true, float movement_penalty = 0f, float fertility = 0f)
        {
            BlocksMovement = blocks_movement;
            IsNatural = is_natural;
            MovementPenalty = movement_penalty;
            Fertility = fertility;
        }
    }

    public class OpalTile : IDisposable, ICloneable
    {
        private static Dictionary<int, OpalTile> InstantiatedTiles = new Dictionary<int, OpalTile>();

        public static OpalTile FromId(int id)
        {
            if (!InstantiatedTiles.ContainsKey(id))
            {
                return null;
            }
            return InstantiatedTiles[id];
        }

        public readonly Cell Graphics;
        public OpalTileProperties Properties;
        public readonly string InternalName;
        public readonly int Id;
        public readonly TileSkeleton Skeleton;

        public OpalTile(int id, TileSkeleton skeleton, string name = "Untitled", OpalTileProperties properties = new OpalTileProperties(), Cell graphics = null)
        {
            if (InstantiatedTiles.ContainsKey(id))
            {
                throw new ArgumentException("An OpalTile with the same id already exists!");
            }

            Graphics = graphics ?? new Cell(Color.Magenta, Color.DarkMagenta, 'E');
            Properties = properties;
            InternalName = name;
            Id = id;
            Skeleton = skeleton;

            InstantiatedTiles[Id] = this;
        }

        public static int GetFirstFreeId()
        {
            int id = -1;
            while (InstantiatedTiles.ContainsKey(++id)) ; // Find first unassigned id
            return id;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is OpalTile)
            {
                return (obj as OpalTile).Skeleton.GetType() == Skeleton.GetType();
            }
            return base.Equals(obj);
        }

        public void Dispose()
        {
            InstantiatedTiles.Remove(Id);
        }

        public virtual object Clone()
        {
            return new OpalTile(GetFirstFreeId(), Skeleton, InternalName, Properties, Graphics);
        }

        /* --- NOTES ON TILE HIERARCHY --- 
           * Tiles are defined by TileSkeletons. These expose the default properties used by the Make function.
           * If Make is overridden, the default properties may or may not be used.
           * Each tile owns a reference to the skeleton that created it, so it follows that one can check for
           * ancestry (`if(t.Skeleton is FooSkeleton)`) and type (`if(t.Skeleton.GetType() == typeof(BarSkeleton))`).

           * Ancestry may be useful if one wants to check whether a tile belongs to a given category, although
           * using properties is best, if possible.
           * Type checking works on only one type of skeleton, and it may be useful when implementing
           * tile-specific logic that shouldn't apply to its descendants.
        */
        public static OpalTile Dirt = TileFactory.Make(TileSkeleton.Get<DirtSkeleton>().Make);
        public static OpalTile FertileSoil = TileFactory.Make(TileSkeleton.Get<FertileSoilSkeleton>().Make);
        public static OpalTile Grass = TileFactory.Make(TileSkeleton.Get<GrassSkeleton>().Make);
        public static OpalTile MediumGrass = TileFactory.Make(TileSkeleton.Get<MediumGrassSkeleton>().Make);
        public static OpalTile TallGrass = TileFactory.Make(TileSkeleton.Get<TallGrassSkeleton>().Make);

        public static OpalTile RockFloor = TileFactory.Make(TileSkeleton.Get<NaturalFloorSkeleton>().Make);
        public static OpalTile RockWall = TileFactory.Make(TileSkeleton.Get<NaturalWallSkeleton>().Make);

        public static OpalTile ConstructedFloor = TileFactory.Make(TileSkeleton.Get<ConstructedFloorSkeleton>().Make);
        public static OpalTile ConstructedWall = TileFactory.Make(TileSkeleton.Get<ConstructedWallSkeleton>().Make);
        public static OpalTile Door = TileFactory.Make(TileSkeleton.Get<DoorSkeleton>().Make);
    }


    public delegate OpalTile MakeTileDelegate(int id);
    public static class TileFactory
    {
        public static OpalTile Make(MakeTileDelegate d)
        {
            return d(OpalTile.GetFirstFreeId());
        }
    }
    public abstract class TileSkeleton : IDisposable
    {
        protected static Dictionary<Type, TileSkeleton> Instances = new Dictionary<Type, TileSkeleton>();

        public abstract OpalTileProperties DefaultProperties { get; }
        public abstract string DefaultName { get; }
        public abstract Cell DefaultGraphics { get; }

        public virtual OpalTile Make(int id)
        {
            return new OpalTile(id, this, DefaultName, DefaultProperties, DefaultGraphics);
        }

        protected TileSkeleton() { }

        public static TileSkeleton Get<T>()
            where T : TileSkeleton, new ()
        {
            Type type = typeof(T);
            if (!Instances.ContainsKey(type))
            {
                Instances[type] = new T();
            }
            return Instances[type];
        }

        public void Dispose()
        {
            Instances.Remove(Instances.Where(kp => kp.Key == this.GetType()).First().Key);
        }
    }

    public class DebugFloorSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: false,
                is_natural: false,
                movement_penalty: 0f,
                fertility: 0f
            );
        public override string DefaultName => "<Debug Floor>";
        private Cell graphics = new Cell(Palette.Terrain[""], Palette.Terrain[""], '?');
        public override Cell DefaultGraphics => graphics;

        public DebugFloorSkeleton SetGraphics(Cell c)
        {
            graphics = c;
            return this;
        }
    }

    public class NaturalFloorSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: false,
                is_natural: true,
                movement_penalty: 0f,
                fertility: 0f
            );
        public override string DefaultName => "Rock Floor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["RockFloorForeground"], Palette.Terrain["RockFloorBackground"], '.');
    }

    public class NaturalWallSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: true,
                is_natural: true,
                movement_penalty: 0f,
                fertility: 0f
            );
        public override string DefaultName => "Rock Wall";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["RockWallForeground"], Palette.Terrain["RockWallBackground"], 177);
    }

    public class DirtSkeleton : NaturalFloorSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: base.DefaultProperties.BlocksMovement,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: .1f,
                fertility: .2f
            );
        public override string DefaultName => "Dirt";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DirtForeground"], Palette.Terrain["DirtBackground"], '.');
    }
    public class FertileSoilSkeleton : DirtSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: base.DefaultProperties.BlocksMovement,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: 0f,
                fertility: 1f
            );
        public override string DefaultName => "Fertile Soil";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["SoilForeground"], Palette.Terrain["SoilBackground"], '=');
    }
    public class GrassSkeleton : FertileSoilSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: base.DefaultProperties.BlocksMovement,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: .1f,
                fertility: .5f
            );
        public override string DefaultName => "Grass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["GrassForeground"], Palette.Terrain["GrassBackground"], ',');
    }
    public class MediumGrassSkeleton : GrassSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: base.DefaultProperties.BlocksMovement,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: 0.3f,
                fertility: .6f
            );
        public override string DefaultName => "Medium Grass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["MediumGrassForeground"], Palette.Terrain["MediumGrassBackground"], ':');
    }
    public class TallGrassSkeleton : MediumGrassSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: base.DefaultProperties.BlocksMovement,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: 0.5f,
                fertility: .3f
            );
        public override string DefaultName => "Tall Grass";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["TallGrassForeground"], Palette.Terrain["TallGrassBackground"], ':');
    }

    public class ConstructedWallSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: true,
                is_natural: false,
                movement_penalty: 0f // Unneeded since it blocks movement
            );
        public override string DefaultName => "Constructed Wall";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["ConstructedWallForeground"], Palette.Terrain["ConstructedWallBackground"], 176);
    }

    public class DoorSkeleton : ConstructedWallSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: true,
                is_natural: false,
                movement_penalty: 0f
            );
        public override string DefaultName => "Door";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["DoorForeground"], Palette.Terrain["DoorBackground"], 197);

        public override OpalTile Make(int id)
        {
            return new Door(id, this, DefaultName, DefaultProperties, DefaultGraphics);
        }
    }

    public class Door : OpalTile
    {
        protected bool isOpen = false;
        public bool IsOpen => isOpen;

        public Door(int id, TileSkeleton k, string defaultname, OpalTileProperties props, Cell graphics) : base(id, k, defaultname, props, graphics) { }

        public void Toggle()
        {
            isOpen = !isOpen;
            Properties.BlocksMovement = !isOpen;
        }

        public override object Clone()
        {
            return new Door(GetFirstFreeId(), Skeleton, InternalName, Properties, Graphics);
        }
    }

    public class ConstructedFloorSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                blocks_movement: false,
                is_natural: false,
                movement_penalty: -.1f // Slightly favor constructed flooring over natural terrain
            );
        public override string DefaultName => "Constructed Floor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["ConstructedFloorForeground"], Palette.Terrain["ConstructedFloorBackground"], '+');
    }
}
