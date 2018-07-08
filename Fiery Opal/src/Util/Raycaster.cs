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

        private static List<Vector2> DDA(Point mapSize, Vector2 deltaDist, Vector2 stepDir, Vector2 pos, Vector2 sideDist, Predicate<Point> isRayBlocked, int maxlen, out bool side)
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
                if (Util.OOB((int)mapPos.X, (int)mapPos.Y, mapSize.X, mapSize.Y))
                {
                    break;
                }
                else if (maxlen > 0 && mapPos.Dist(pos) >= maxlen) break;

                //Check if ray has hit a wall
                if (isRayBlocked(mapPos.ToPoint())) break;
            }

            return traversed;
        }

        public static RayInfo CastRay(Point mapSize, Vector2 position, Vector2 rayDir, Predicate<Point> isRayBlocked, int maxlen=0)
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
            List<Vector2> traversed = DDA(mapSize, deltaDist, stepDir, position, sideDist, isRayBlocked, maxlen, out bool side);
            // Calculate distance projected on camera direction (Euclidean distance will give fisheye effect!)
            float perpWallDist; Vector2 last = traversed.Last();
            if (!side) perpWallDist = (last.X - position.X + (1 - stepDir.X) / 2) / rayDir.X;
            else       perpWallDist = (last.Y - position.Y + (1 - stepDir.Y) / 2) / rayDir.Y;

            if(Util.OOB((int)last.X, (int)last.Y, mapSize.X, mapSize.X))
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

        public static bool IsLineObstructed(Point mapSize, Vector2 a, Vector2 b, Predicate<Point> obstructed)
        {
            Vector2 mapPos = b.ToPoint().ToVector2();

            double angle = Math.Atan2((a - b).Y, (a - b).X);
            Vector2 rayDir = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            var info = CastRay(mapSize, b, rayDir, obstructed);
            return a.Dist(info.LastPointTraversed) < a.Dist(b);
        }
    }
}
