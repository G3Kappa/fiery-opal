using FieryOpal.Src.Procedural.Terrain.Tiles;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src
{
    public struct OpalTileProperties
    {
        public bool IsBlock, IsNatural, HasRoof;
        public float MovementPenalty, Fertility;

        public bool BlocksMovement => IsBlock || MovementPenalty >= 1.0f;

        public OpalTileProperties(bool is_block = false, bool is_natural = true, float movement_penalty = 0f, float fertility = 0f, bool has_roof = false)
        {
            IsBlock = is_block;
            IsNatural = is_natural;
            MovementPenalty = movement_penalty;
            Fertility = fertility;
            HasRoof = has_roof;
        }
    }

    public class OpalTile : IDisposable, ICloneable, ICustomSpritesheet, INamedObject
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
        public readonly int Id;
        public readonly TileSkeleton Skeleton;
        public Font Spritesheet => Nexus.Fonts.Spritesheets["Terrain"];

        public string Name { get; private set; }

        public OpalTile(int id, TileSkeleton skeleton, string name = "Untitled", OpalTileProperties properties = new OpalTileProperties(), Cell graphics = null)
        {
            if (InstantiatedTiles.ContainsKey(id))
            {
                throw new ArgumentException("An OpalTile with the same id already exists!");
            }

            Graphics = graphics ?? new Cell(Color.Magenta, Color.DarkMagenta, 'E');
            Properties = properties;
            Name = name;
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
            return new OpalTile(GetFirstFreeId(), Skeleton, Name, Properties, Graphics);
        }

        private static Dictionary<string, OpalTile> ReferenceTileInstances = new Dictionary<string, OpalTile>();

        public static bool RegisterRefTile(TileSkeleton reference_maker)
        {
            var tile = TileFactory.Make(reference_maker.Make);
            if (ReferenceTileInstances.ContainsKey(tile.Name)) return false;
            ReferenceTileInstances[tile.Name] = tile;
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

        public static OpalTile GetRefTileByRefId(int ref_id)
        {
            var values = ReferenceTileInstances.Values;
            if (ref_id < 0 || ref_id >= values.Count) return null;
            return values.ElementAt(ref_id);
        }

        public static int GetRefId(OpalTile t)
        {
            if (t == null) return -1;
            return ReferenceTileInstances.Keys.ToList().IndexOf(t.Skeleton.DefaultName);
        }

        public static int ReferenceTileCount => ReferenceTileInstances.Count;

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
}
