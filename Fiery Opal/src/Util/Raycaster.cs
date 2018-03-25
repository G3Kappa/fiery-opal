using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src
{
    class Raycaster
    {
        private static void CalcStep(Vector2 rayDir, Vector2 position, Vector2 mapPos, Vector2 deltaDist, ref Vector2 stepDir, ref Vector2 sideDist)
        {
            if (rayDir.X < 0)
            {
                stepDir.X = -1;
                sideDist.X = (position.X - mapPos.X) * deltaDist.X;
            }
            else
            {
                stepDir.X = 1;
                sideDist.X = (mapPos.X + 1 - position.X) * deltaDist.X;
            }
            if (rayDir.Y < 0)
            {
                stepDir.Y = -1;
                sideDist.Y = (position.Y - mapPos.Y) * deltaDist.Y;
            }
            else
            {
                stepDir.Y = 1;
                sideDist.Y = (mapPos.Y + 1 - position.Y) * deltaDist.Y;
            }
        }

        private static bool DDA(OpalLocalMap target, Vector2 deltaDist, Vector2 stepDir, ref Vector2 mapPos, ref Vector2 sideDist, TileMemory fog = null)
        {
            // Wall hit? Side hit?
            bool side = false;
            //perform DDA
            while (true)
            {
                //jump to next map square, OR in x-direction, OR in y-direction
                if (sideDist.X < sideDist.Y)
                {
                    sideDist.X += deltaDist.X;
                    mapPos.X += stepDir.X;
                    side = false;
                }
                else
                {
                    sideDist.Y += deltaDist.Y;
                    mapPos.Y += stepDir.Y;
                    side = true;
                }
                var t = target.TileAt((int)mapPos.X, (int)mapPos.Y);
                if (fog != null)
                {
                    fog.See(new Point((int)mapPos.X, (int)mapPos.Y));
                    fog.Learn(new Point((int)mapPos.X, (int)mapPos.Y));
                }
                //Check if ray has hit a wall
                if (t == null || t.Properties.IsBlock && !(t is DoorTile && (t as DoorTile).IsOpen))
                {
                    return side;
                }
                // Check if it hit a decoration that should render as a wall
                var decos = target.ActorsAt((int)mapPos.X, (int)mapPos.Y).Where(d => d is DecorationBase && (d as DecorationBase).DisplayAsBlock);
                if (decos.Count() > 0)
                {
                    return side;
                }
            }
        }

        /// <summary>
        /// Casts a ray and returns the distance from that ray to the nearest solid object.
        /// </summary>
        /// <param name="target">The map in which the ray is going to be cast.</param>
        /// <param name="position">The initial position of the ray.</param>
        /// <param name="mapPos">The initial map location of the ray (position rounded down). Will later reflect where the ray stopped.</param>
        /// <param name="rayDir">A vector indicating the direction of the ray.</param>
        /// <param name="side">Did we hit a wall straight-on or from the side?</param>
        /// <returns>The distance from the nearest perpendicular wall OR solid actor.</returns>
        public static float CastRay(OpalLocalMap target, Vector2 position, ref Vector2 mapPos, Vector2 rayDir, ref bool side, TileMemory fog = null)
        {
            //length of ray from current position to next x or y-side
            Vector2 sideDist = new Vector2();
            //what direction to step in x or y-direction (either +1 or -1)
            Vector2 stepDir = new Vector2();
            //length of ray from one x or y-side to next x or y-side
            Vector2 deltaDist = new Vector2(Math.Abs(1 / rayDir.X), Math.Abs(1 / rayDir.Y));
            float perpWallDist;

            if(fog != null)
            {
                // See this position now since the ray won't
                fog.See(position.ToPoint());
                fog.Learn(position.ToPoint());
            }

            // Calculate stepDir and initial sideDist
            CalcStep(rayDir, position, mapPos, deltaDist, ref stepDir, ref sideDist);
            // Perform DDA
            side = DDA(target, deltaDist, stepDir, ref mapPos, ref sideDist, fog);
            // Calculate distance projected on camera direction (Euclidean distance will give fisheye effect!)
            if (!side) perpWallDist = (mapPos.X - position.X + (1 - stepDir.X) / 2) / rayDir.X;
            else perpWallDist = (mapPos.Y - position.Y + (1 - stepDir.Y) / 2) / rayDir.Y;

            return perpWallDist;
        }
    }
}
