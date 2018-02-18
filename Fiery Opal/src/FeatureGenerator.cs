using Microsoft.Xna.Framework;
using Simplex;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FieryOpal.src
{
    public interface IOpalFeatureGenerator
    {
        /// <summary>
        /// Yield the generated tile at coordinates X, Y.
        /// </summary>
        /// <param name="x">The tile's X coordinate.</param>
        /// <param name="y">The tile's Y coordinate.</param>
        /// <returns></returns>
        OpalTile Get(int x, int y);
        /// <summary>
        /// Yield the generated decoration at coordinates X, Y.
        /// </summary>
        /// <param name="x">The decoration's X coordinate.</param>
        /// <param name="y">The decoration's Y coordinate.</param>
        /// <returns></returns>
        IDecoration GetDecoration(int x, int y);
        /// <summary>
        /// Optionally called if the generator must rely on state in order to yield a tile for a given point.
        /// </summary>
        /// <param name="m">The requesting map in its current state.</param>
        void Generate(OpalLocalMap m);
    }

    public class BasicTerrainGenerator : IOpalFeatureGenerator
    {
        protected float LayerHeight;
        protected float Openness;

        protected OpalLocalMap Tiles; // Used as wrapper for the extension methods

        public BasicTerrainGenerator(float layer_height = 1, float openness = 0.5f)
        {
            LayerHeight = layer_height;
            Openness = openness;
        }

        private void ReplaceDebugTiles()
        {
            // Replace debug tiles with actual tiles
            Tiles.Iter((self, x, y, t) =>
            {
                if (t == OpalTile.DebugGround)
                {
                    self.SetTile(x, y, OpalTile.DungeonGround);
                }
                else if (t == OpalTile.DebugWall)
                {
                    self.SetTile(x, y, OpalTile.DungeonWall);
                }

                return false;
            });
        }

        private void ConnectEnclosedAreas(Random rng)
        {

            // Determine each enclosed area
            List<IEnumerable<Point>> enclosed_areas = new List<IEnumerable<Point>>();
            Tiles.Iter((self, x, y, t) =>
            {
                if (t != OpalTile.DungeonGround) return false;

                var area = self.FloodFill(x, y, OpalTile.DebugGround).ToList();
                enclosed_areas.Add(area);

                return false;
            });
            
            if (enclosed_areas.Count <= 1) return; // Done


            Dictionary<int, List<int>> connections = new Dictionary<int, List<int>>();
            int remaining_connections = enclosed_areas.Count;
            Point[] circle_centers = new Point[enclosed_areas.Count];
            int radius = 2; // Tiles
            const int MAX_RADIUS = 32; 
            Dictionary<int, List<int>> is_connected = new Dictionary<int, List<int>>();

            for (int i = 0; i < enclosed_areas.Count; ++i)
            {
                int j = rng.Next(enclosed_areas[i].Count());
                circle_centers[i] = enclosed_areas[i].ElementAt(j);
                connections[i] = new List<int>();
            }
            while (remaining_connections > 0 && radius <= MAX_RADIUS)
            {
                for (int i = 0; i < circle_centers.Length; ++i)
                {
                    if (connections[i].Count >= 2) continue;
                    Point p1 = circle_centers[i];
                    for (int j = 0; j < circle_centers.Length; ++j)
                    {
                        if (i == j || connections[j].Count >= 2 || connections[j].Contains(i)) continue;
                        Point p2 = circle_centers[j];

                        if (Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2)) <= 2 * radius)
                        {
                            connections[i].Add(j);
                            connections[j].Add(i);

                            for (int a = 0; a < enclosed_areas.Count; ++a)
                            {
                                int r = rng.Next(enclosed_areas[a].Count());
                                circle_centers[a] = enclosed_areas[a].ElementAt(r);
                            }

                            remaining_connections--;
                            Tiles.DrawLine(p1, p2, OpalTile.DebugGround, thickness: rng.Next(3, 5)).ToList();
                        }
                    }
                }
                radius *= 2;
            }
        }

        private void ApplyErosion(int EROSION_PASSES, float EROSION_FAIL_CHANCE, float SPONTANEOUS_EROSION_CHANCE, Random rng)
        {
            for (int k = 0; k < EROSION_PASSES; ++k)
            {
                // Apply erosion to remove the hard edges
                Tiles.Iter((self, x, y, T) =>
                {
                    if (!T.Properties.BlocksMovement) return false;
                    if (rng.NextDouble() < EROSION_FAIL_CHANCE) return false;

                    var neighbours = self.Neighbours(x, y);
                    var count = neighbours.Where(t => t.Item1 == OpalTile.DungeonWall || t.Item1 == OpalTile.DungeonGround).Count();


                    if(count < 5 && rng.NextDouble() < SPONTANEOUS_EROSION_CHANCE)
                    {
                        self.SetTile(x, y, rng.NextDouble() < .5 ? OpalTile.DebugGround : OpalTile.DebugWall);
                    }
                    else if (count == 3 /* Rough edges */ || count <= 2 /* Lone pillars */)
                    {
                        if (count == 4 && x < Tiles.Width - 1 && y < Tiles.Height - 1 && x > 0)
                        {
                            if (!(!self.TileAt(x + 1, y).Properties.BlocksMovement && !self.TileAt(x, y + 1).Properties.BlocksMovement && self.TileAt(x + 1, y + 1).Properties.BlocksMovement)
                                && !(!self.TileAt(x - 1, y).Properties.BlocksMovement && !self.TileAt(x, y + 1).Properties.BlocksMovement && self.TileAt(x - 1, y + 1).Properties.BlocksMovement))
                            {
                                return false;
                            }
                        }

                        self.SetTile(x, y, OpalTile.DebugGround);
                    }
                    else if (count == 7 /* Rough corners */)
                    {
                        var lone_ground = self.Neighbours(x, y).Where(t => t.Item1 != OpalTile.DungeonWall && t.Item1 != OpalTile.DungeonGround).ToArray()[0].Item2;
                        if (self.Neighbours(lone_ground.X, lone_ground.Y).Where(t => t.Item1 == OpalTile.DebugGround || t.Item1 == OpalTile.DebugGround).Count() > 3) return false;

                        self.SetTile(lone_ground.X, lone_ground.Y, OpalTile.DebugWall);
                    }

                    return false;
                });
            }
        }

        public void Generate(OpalLocalMap m)
        {
            Tiles = new OpalLocalMap(m.Width, m.Height);

            // 0. Create a rectangle as big as the map and assign it to I
            // 1. Split I into two new rectangles by a random vertical line (if Width > Height, otherwise horizontal) such that no new rectangle is too small.
            // 2. If either rectangle would be too small even if the line were to be centered, then pop I from the stack and go to step 1.
            // 3. Otherwise, push the first rectangle onto the stack,
            // 4. Assign the second rectangle to I
            // 5. Go to step 1 while the stack contains more than 0 rectangles
            const float RECT_TOO_SMALL = 1 / 20f;
            const float PARTITION_RAND = .3f;
            float PARTITION_REMOVAL_PERCENTAGE = 1 - Openness;
            const int EROSION_PASSES = 100;
            const float EROSION_FAIL_CHANCE = .1f;
            const float SPONTANEOUS_EROSION_CHANCE = .01f;

            List<Rectangle> partitions = new List<Rectangle>();
            Stack<Rectangle> partition_stack = new Stack<Rectangle>();
            Random rng = Util.GlobalRng;

            Rectangle I = new Rectangle(0, 0, m.Width, m.Height);
            do
            {
                bool ver = I.Width > I.Height; // Vertical cut?
                int xRandOffset = ver ? (int)(PARTITION_RAND * I.Width / 2) - rng.Next((int)(PARTITION_RAND * I.Width)) : 0;
                int yRandOffset = ver ? 0 : (int)(PARTITION_RAND * I.Height / 2) - rng.Next((int)(PARTITION_RAND * I.Height));

                int r1W = (ver ? I.Width / 2 : I.Width) + xRandOffset;
                int r1H = (ver ? I.Height : I.Height / 2) + yRandOffset;

                Rectangle r1 = new Rectangle(I.X, I.Y, r1W, r1H);
                Rectangle r2 = new Rectangle(I.X + (ver ? r1W : 0), I.Y + (ver ? 0 : r1H), (ver ? r1W : I.Width), (ver ? I.Height : r1H));

                if (r1.Width / (float)m.Width <= RECT_TOO_SMALL || r2.Width / (float)m.Width <= RECT_TOO_SMALL
                    || r1.Height / (float)m.Height <= RECT_TOO_SMALL || r2.Height / (float)m.Height <= RECT_TOO_SMALL)
                {
                    partitions.Add(I);

                    if (partition_stack.Count == 0) break;
                    I = partition_stack.Pop();
                    continue;
                }

                partition_stack.Push(r1);
                I = r2;
            }
            while (true);

            // Remove some random partitions
            int partitions_to_remove = (int)(partitions.Count * PARTITION_REMOVAL_PERCENTAGE);

            while (partitions_to_remove-- > 0)
            {
                partitions.RemoveAt(rng.Next(partitions.Count));
            }

            // Set tiles
            Tiles.Iter((self, x, y, T) =>
            {
                self.SetTile(x, y, OpalTile.DungeonWall);
                foreach (Rectangle r in partitions)
                {
                    if (r.Contains(x, y))
                    {
                        self.SetTile(x, y, OpalTile.DungeonGround);
                        break;
                    }
                }
                return false;
            });
            
            ConnectEnclosedAreas(rng);
            ApplyErosion(EROSION_PASSES, EROSION_FAIL_CHANCE, SPONTANEOUS_EROSION_CHANCE, rng);
            ConnectEnclosedAreas(rng);
            ReplaceDebugTiles();

        }

        public OpalTile Get(int x, int y)
        {
            return Tiles.TileAt(x, y);
        }

        public IDecoration GetDecoration(int x, int y)
        {
            return null;
        }
    }
    public class BasicTerrainDecorator : IOpalFeatureGenerator
    {
        float[,] Noise;
        private OpalLocalMap CurrentMap;

        public void Generate(OpalLocalMap m)
        {
            Noise = Simplex.Noise.Calc2D(Util.GlobalRng.Next(0, 1000), Util.GlobalRng.Next(0, 1000), m.Width, m.Height, .065f);
            float[,] mask = Simplex.Noise.Calc2D(Util.GlobalRng.Next(0, 1000), Util.GlobalRng.Next(0, 1000), m.Width, m.Height, .0055f);
            m.Iter((self, x, y, t) =>
            {
                Noise[x, y] *= (mask[x, y] / 255f);
                Noise[x, y] /= 255f;
                return false;
            });
            CurrentMap = m;
        }

        public OpalTile Get(int x, int y)
        {
            if (CurrentMap.TileAt(x, y).Properties.BlocksMovement) return null;

            return new[] { OpalTile.DungeonGround, OpalTile.Grass, OpalTile.ThickGrass } [(int)(Noise[x, y]  * 3)];
        }

        public IDecoration GetDecoration(int x, int y)
        {
            if (CurrentMap.TileAt(x, y).Properties.BlocksMovement) return null;

            if (Noise[x, y] > .5f)
            {
                if(Noise[x, y] > .7f)
                {
                    return new Plant();
                }
                return new Sapling();
            }
            return null;
        }
    }
}
