using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src
{
    public static class Raycaster
    {
        public struct RayInfo
        {
            public float ProjectedDistance;
            public List<Vector2> PointsTraversed;
            public bool SideHit;
            public Vector2 LastPointTraversed;
        }

        private static void CalcStep(Vector2 rayDir, Vector2 position, Vector2 deltaDist, ref Vector2 stepDir, ref Vector2 sideDist)
        {
            Vector2 mapPos = position.ToPoint().ToVector2();
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

        private static List<Vector2> DDA(OpalLocalMap target, Vector2 deltaDist, Vector2 stepDir, Vector2 pos, Vector2 sideDist, out bool side)
        {
            // Wall hit? Side hit?
            //perform DDA
            List<Vector2> traversed = new List<Vector2>();
            Vector2 mapPos = pos.ToPoint().ToVector2();
            traversed.Add(pos);
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

                traversed.Add(mapPos);
                if (Util.OOB((int)mapPos.X, (int)mapPos.Y, target.Width, target.Height))
                {
                    break;
                }


                //Check if ray has hit a wall
                var t = target.TileAt(mapPos.ToPoint());
                if (t?.Properties.IsBlock ?? false) break;

                // Check if it hit a decoration that should render as a block
                var decos = target.ActorsAt(mapPos.ToPoint())
                    .Where(d => 
                        d is DecorationBase 
                        && (d as DecorationBase).DisplayAsBlock
                    )
                ;
                if (decos.Count() > 0) break;
            }

            return traversed;
        }

        public static RayInfo CastRay(OpalLocalMap target, Vector2 position, Vector2 rayDir)
        {
            //length of ray from current position to next x or y-side
            Vector2 sideDist = new Vector2();
            //what direction to step in x or y-direction (either +1 or -1)
            Vector2 stepDir = new Vector2();
            //length of ray from one x or y-side to next x or y-side
            Vector2 deltaDist = new Vector2(Math.Abs(1 / rayDir.X), Math.Abs(1 / rayDir.Y));

            // Calculate stepDir and initial sideDist
            CalcStep(rayDir, position, deltaDist, ref stepDir, ref sideDist);
            // Perform DDA
            List<Vector2> traversed = DDA(target, deltaDist, stepDir, position, sideDist, out bool side);
            // Calculate distance projected on camera direction (Euclidean distance will give fisheye effect!)
            float perpWallDist; Vector2 last = traversed.Last();
            if (!side) perpWallDist = (last.X - position.X + (1 - stepDir.X) / 2) / rayDir.X;
            else       perpWallDist = (last.Y - position.Y + (1 - stepDir.Y) / 2) / rayDir.Y;

            if(Util.OOB((int)last.X, (int)last.Y, target.Width, target.Height))
            {
                traversed.RemoveAt(traversed.Count - 1);
            }

            return new RayInfo()
            {
                ProjectedDistance = perpWallDist,
                PointsTraversed = traversed,
                SideHit = side,
                LastPointTraversed = last
            };
        }

        public static bool IsLineObstructed(OpalLocalMap target, Vector2 a, Vector2 b)
        {
            Vector2 mapPos = b.ToPoint().ToVector2();

            double angle = Math.Atan2((a - b).Y, (a - b).X);
            Vector2 rayDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            var info = CastRay(target, b, rayDir);
            return a.Dist(info.LastPointTraversed) < a.Dist(b);
        }
    }
}
