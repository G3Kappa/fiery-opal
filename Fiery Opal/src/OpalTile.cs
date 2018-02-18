using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;

namespace FieryOpal.src
{

    public struct OpalTileProperties
    {
        public readonly bool BlocksMovement;

        public OpalTileProperties(bool blocks_movement = false)
        {
            BlocksMovement = blocks_movement;
        }
    }

    public class OpalTile : IDisposable
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
        public readonly OpalTileProperties Properties;
        public readonly string InternalName;
        public readonly int Id;

        public OpalTile(int id, string name, OpalTileProperties properties, Cell graphics)
        {
            if (InstantiatedTiles.ContainsKey(id))
            {
                throw new ArgumentException("An OpalTile with the same id already exists!");
            }

            Graphics = graphics;
            Properties = properties;
            InternalName = name;
            Id = id;

            InstantiatedTiles[Id] = this;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is OpalTile)
            {
                return (obj as OpalTile).Id == Id;
            }
            return base.Equals(obj);
        }

        public void Dispose()
        {
            InstantiatedTiles.Remove(Id);
        }

        public static OpalTile DebugGround = new OpalTile(0, "DebugGround", new OpalTileProperties(false), new Cell(Color.Magenta, Color.DarkMagenta, 247));
        public static OpalTile DebugWall = new OpalTile(1, "DebugWall", new OpalTileProperties(true), new Cell(Color.Green, Color.DarkGreen, 8));

        public static OpalTile DungeonGround = new OpalTile(2, "DungeonGround", new OpalTileProperties(false), new Cell(Color.Green, Color.ForestGreen, '.'));
        public static OpalTile DungeonWall = new OpalTile(3, "DungeonWall", new OpalTileProperties(true), new Cell(Color.DimGray, Color.Gray, 176));
        public static OpalTile StairsUp = new OpalTile(4, "StairsUp", new OpalTileProperties(false), new Cell(Color.FloralWhite, Color.Gray, 60));
        public static OpalTile StairsDown = new OpalTile(5, "StairsDown", new OpalTileProperties(false), new Cell(Color.FloralWhite, Color.Gray, 62));

        public static OpalTile Sand = new OpalTile(6, "Soil", new OpalTileProperties(false), new Cell(Color.Beige, Color.SandyBrown, '~'));
        public static OpalTile Grass = new OpalTile(7, "Grass", new OpalTileProperties(false), new Cell(Color.ForestGreen, Color.Green, ','));
        public static OpalTile ThickGrass = new OpalTile(8, "ThickGrass", new OpalTileProperties(false), new Cell(Color.ForestGreen, Color.DarkGreen, ';'));
    }
}
