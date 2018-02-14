using FieryOpal.src.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using SadConsole.Surfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src
{
    public interface IOpalGameActor
    {
        Point LocalPosition { get; }
        OpalLocalMap Map { get; }
        ColoredGlyph Graphics { get; set; }

        void Update(TimeSpan delta);
        void Draw(TimeSpan delta);

        /// <summary>
        /// Moves the actor a relative amount of units.
        /// </summary>
        /// <param name="rel">The relative amount of units.</param>
        /// <returns>True if the movement succeeded, false otherwise.</returns>
        bool Move(Point rel);

        /// <summary>
        /// Removes the actor from the current map and spawns it at the given coordinates on another map.
        /// </summary>
        /// <param name="new_map">The new OpalLocalMap.</param>
        /// <param name="new_spawn">The spawn coordinates.</param>
        /// <returns></returns>
        bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn);
    }

    public class OpalCreature : IPipelineSubscriber<OpalCreature>, IOpalGameActor
    {
        public Guid Handle { get; }

        private Point localPosition;
        public Point LocalPosition => localPosition;

        private OpalLocalMap map;
        public OpalLocalMap Map => map;

        private ColoredGlyph graphics;
        public ColoredGlyph Graphics { get => graphics; set => graphics = value; }

        public OpalCreature()
        {
            Handle = Guid.NewGuid();
            Graphics = new ColoredGlyph(new Cell(Color.Black, Color.White, '@'));
        }

        public void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalCreature, string> msg, bool is_broadcast)
        {

        }

        public void Update(TimeSpan delta)
        {
            if (Map == null) return;
        }

        public void Draw(TimeSpan delta)
        {
        }

        public bool Move(Point rel)
        {
            var tile = Map.TileAt(LocalPosition.X + rel.X, LocalPosition.Y + rel.Y);
            if (tile == null) return false; //TODO: ChangeLocalMap?
            bool ret = !tile.Properties.BlocksMovement;
            if(ret)
            {
                localPosition = LocalPosition + rel;
            }
            return ret;
        }

        public bool ChangeLocalMap(OpalLocalMap new_map, Point new_spawn)
        {
            var tile = new_map.TileAt(new_spawn.X, new_spawn.Y);
            if (tile == null) return false;
            bool ret = !tile.Properties.BlocksMovement;
            if(ret)
            {
                map = new_map;
                localPosition = new_spawn;
            }
            return ret;
        }
    }

    public static class CellExtension
    {
        private static Dictionary<Tuple<Cell, Font>, Color[,]> Cache = new Dictionary<Tuple<Cell, Font>, Color[,]>();

        public static Color[,] GetPixels(this Cell c, Font f)
        {
            var key = new Tuple<Cell, Font>(c, f);
            if (Cache.ContainsKey(key)) return Cache[key];

            Color[] pixels1d = new Color[f.FontImage.Width * f.FontImage.Height];
            Color[,] pixels2d = new Color[f.Size.X, f.Size.Y];

            int cx = (c.Glyph % 16) * f.Size.X;
            int cy = (c.Glyph / 16) * f.Size.Y;

            Program.FontTexture.GetData(pixels1d);
            for (int x = 0; x < f.Size.X; x++)
            {
                for (int y = 0; y < f.Size.Y; y++)
                {
                    int localindex = (cy + y) * f.FontImage.Width + x + cx;

                    var p = pixels1d[localindex];
                    pixels2d[x, y] = p.A == 0 ? c.Background : c.Foreground;
                }
            }

            Cache[key] = pixels2d;
            return pixels2d;
        }
    }
}
