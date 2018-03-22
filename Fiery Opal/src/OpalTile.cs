using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Linq;
using System.Collections.Generic;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Procedural.Terrain.Biomes;
using System.Runtime.Serialization;

namespace FieryOpal.Src
{
    public struct OpalTileProperties
    {
        public bool IsBlock, IsNatural;
        public float MovementPenalty, Fertility;

        public bool BlocksMovement => IsBlock || MovementPenalty >= 1.0f;

        public OpalTileProperties(bool is_block = false, bool is_natural = true, float movement_penalty = 0f, float fertility = 0f)
        {
            IsBlock = is_block;
            IsNatural = is_natural;
            MovementPenalty = movement_penalty;
            Fertility = fertility;
        }
    }

    public class OpalTile : IDisposable, ICloneable, ICustomSpritesheet
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
        public Font Spritesheet => Program.Fonts.Spritesheets["Terrain"];

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

        private static Dictionary<string, OpalTile> ReferenceTileInstances = new Dictionary<string, OpalTile>();

        public static bool RegisterRefTile(TileSkeleton reference_maker)
        {
            var tile = TileFactory.Make(reference_maker.Make);
            if (ReferenceTileInstances.ContainsKey(tile.InternalName)) return false;
            ReferenceTileInstances[tile.InternalName] = tile;
            return true;
        }

        public static OpalTile GetRefTile<T>()
            where T : TileSkeleton, new()
        {
            var reference_maker = TileSkeleton.Get<T>();
            if (!ReferenceTileInstances.ContainsKey(reference_maker.DefaultName)) return null;
            return ReferenceTileInstances[reference_maker.DefaultName];
        }

        public static OpalTile GetRefTile(TileSkeleton reference_maker)
        {
            if (!ReferenceTileInstances.ContainsKey(reference_maker.DefaultName)) return null;
            return ReferenceTileInstances[reference_maker.DefaultName];
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
    }


    public delegate OpalTile MakeTileDelegate(int id);
    public static class TileFactory
    {
        public static OpalTile Make(MakeTileDelegate d)
        {
            return d(OpalTile.GetFirstFreeId());
        }
    }


    [Serializable]
    public abstract class TileSkeleton : IDisposable
    {
        protected static Dictionary<Type, TileSkeleton> Instances = new Dictionary<Type, TileSkeleton>();

        public virtual OpalTileProperties DefaultProperties { get; private set; }
        public virtual string DefaultName { get; private set; }
        public virtual Cell DefaultGraphics { get; private set; }

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
                OpalTile.RegisterRefTile(Instances[type]);
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
                is_block: false,
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
                is_block: false,
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
                is_block: true,
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
                is_block: base.DefaultProperties.IsBlock,
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
                is_block: base.DefaultProperties.IsBlock,
                is_natural: base.DefaultProperties.IsNatural,
                movement_penalty: 0f,
                fertility: 1f
            );
        public override string DefaultName => "Fertile Soil";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["SoilForeground"], Palette.Terrain["SoilBackground"], '=');
    }

    public class ConstructedWallSkeleton : TileSkeleton
    {
        public override OpalTileProperties DefaultProperties =>
            new OpalTileProperties(
                is_block: true,
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
                is_block: true,
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
            Properties.IsBlock = !isOpen;
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
                is_block: false,
                is_natural: false,
                movement_penalty: -.1f // Slightly favor constructed flooring over natural terrain
            );
        public override string DefaultName => "Constructed Floor";
        public override Cell DefaultGraphics => new Cell(Palette.Terrain["ConstructedFloorForeground"], Palette.Terrain["ConstructedFloorBackground"], '+');
    }
}
