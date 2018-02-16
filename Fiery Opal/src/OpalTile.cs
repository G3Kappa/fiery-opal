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

    public class OpalTile
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

        public static OpalTile DebugGround = new OpalTile(0, "DebugGround", new OpalTileProperties(false), new Cell(Color.Magenta, Color.DarkMagenta, 247));
        public static OpalTile DebugWall = new OpalTile(1, "DebugWall", new OpalTileProperties(false), new Cell(Color.Green, Color.DarkGreen, 8));

        public static OpalTile DungeonWall = new OpalTile(2, "DungeonWall", new OpalTileProperties(false), new Cell(Color.Red, Color.DarkRed, 16));
        public static OpalTile DungeonGround = new OpalTile(3, "DungeonGround", new OpalTileProperties(false), new Cell(Color.White, Color.Gray, 16));
    }
}
