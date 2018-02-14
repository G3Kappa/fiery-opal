using FieryOpal.src.UI;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if(InstantiatedTiles.ContainsKey(id))
            {
                throw new ArgumentException("An OpalTile with the same id already exists!");
            }

            Graphics = graphics;
            Properties = properties;
            InternalName = name;
            Id = id;

            InstantiatedTiles[Id] = this;
        }

        public static OpalTile TileWater = new OpalTile(0, "Water", new OpalTileProperties(false), new Cell(Color.Green, Color.DarkGreen, ','));
        public static OpalTile TileWall = new OpalTile(1, "Wall", new OpalTileProperties(true), new Cell(Color.LightGray, Color.Gray, '#'));
        public static OpalTile TileGrass = new OpalTile(2, "Grass", new OpalTileProperties(false), new Cell(Color.Cyan, Color.DarkBlue, 247));
    }

    public interface IOpalFeatureGenerator
    {
        OpalTile Generate(int x, int y, OpalLocalMap m);
    }

    public class BasicTerrainGenerator : IOpalFeatureGenerator
    {

        public OpalTile Generate(int x, int y, OpalLocalMap m)
        {
            float[,] noise = Simplex.Noise.Calc2D(x, y, 1, 1, .02f);

            return OpalTile.FromId((int)(noise[0, 0] / 256 * 3));
        }
    }

    public class OpalLocalMap
    {
        protected int[,] TerrainGrid { get; private set; }
        public List<IOpalGameActor> Actors { get; private set; }

        public int Width { get; }
        public int Height { get; }

        public OpalLocalMap(int width, int height)
        {
            TerrainGrid = new int[width, height];
            Actors = new List<IOpalGameActor>(); 
            Width = width;
            Height = height;
        }

        public virtual void Generate(params IOpalFeatureGenerator[] generators)
        {
            foreach(var gen in generators)
            {
                for(int x = 0; x < Width; ++x)
                {
                    for(int y = 0; y < Height; ++y)
                    {
                        OpalTile output = gen.Generate(x, y, this);
                        TerrainGrid[x, y] = output.Id;
                    }
                }
            }
        }

        public void Update(TimeSpan delta)
        {
            foreach(var actor in Actors)
            {
                actor.Update(delta);
            }
        }

        public OpalTile TileAt(int x, int y)
        {
            if(x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return null;
            }
            return OpalTile.FromId(TerrainGrid[x, y]);
        }

        public IEnumerable<IOpalGameActor> ActorsAt(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Width || y >= Height)
            {
                return new List<IOpalGameActor>();
            }
            return Actors.Where(act => act.LocalPosition == new Point(x, y));
        }

        public IEnumerable<Tuple<OpalTile, Point>> TilesWithin(Rectangle r)
        {
            for(int x = r.X; x < r.Width + r.X; ++x)
            {
                for(int y = r.Y; y < r.Height + r.Y; ++y)
                {
                    OpalTile t = TileAt(x, y);
                    if(t != null)
                    {
                        yield return new Tuple<OpalTile, Point>(t, new Point(x, y));
                    }
                }
            }
        }

        public IEnumerable<IOpalGameActor> ActorsWithin(Rectangle r)
        {
            for (int x = r.X; x < r.Width + r.X; ++x)
            {
                for (int y = r.Y; y < r.Height + r.Y; ++y)
                {
                    IEnumerable<IOpalGameActor> actors = ActorsAt(x, y);
                    foreach(var act in actors)
                    {
                        yield return act;
                    }
                }
            }
        }
    }

    public class Viewport
    {
        public OpalLocalMap Target { get; protected set; }

        protected Rectangle viewArea;
        public Rectangle ViewArea
        {
            get { return viewArea; }
            set
            {
                var oldValue = viewArea;
                viewArea = value;
                ViewAreaChanged(oldValue);
            }
        }

        public Viewport(OpalLocalMap target, Rectangle view_area)
        {
            ViewArea = view_area;
            Target = target;
        }

        protected virtual void ViewAreaChanged(Rectangle oldViewArea)
        {

        }

        public virtual void Print(OpalConsoleWindow surface, Rectangle targetArea)
        {
            var tiles = Target.TilesWithin(ViewArea);
            foreach(var tuple in tiles)
            {
                OpalTile t = tuple.Item1;
                Point pos = tuple.Item2 - new Point(ViewArea.X, ViewArea.Y);
                if(pos.X >= targetArea.Width || pos.Y >= targetArea.Height)
                {
                    continue;
                }
                surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, t.Graphics);
            }

            var actors = Target.ActorsWithin(ViewArea);
            foreach (var act in actors)
            {
                Point pos = act.LocalPosition - new Point(ViewArea.X, ViewArea.Y);
                if (pos.X >= targetArea.Width || pos.Y >= targetArea.Height)
                {
                    continue;
                }
                surface.SetCell(targetArea.X + pos.X, targetArea.Y + pos.Y, act.Graphics);
            }
        }
    }

    // Translated to C# from http://lodev.org/cgtutor/raycasting.html ; http://lodev.org/cgtutor/raycasting2.html
    public class RaycastViewport : Viewport
    {
        public IOpalGameActor Following { get; protected set; }
        public Vector2 DirectionVector { get; protected set; }
        public Vector2 PlaneVector { get; protected set; }
        public Vector2 Position { get; protected set; }

        public RaycastViewport(OpalLocalMap target, Rectangle view_area, IOpalGameActor following) : base(target, view_area)
        {
            Following = following;
            DirectionVector = new Vector2(-1, 0);
            PlaneVector = new Vector2(0, .5f);
            Position = new Vector2(0, 0);
        }

        private void PrintVLine(int x, int drawStart, int drawEnd, Cell cell, OpalConsoleWindow surface)
        {
            var m = Math.Min(drawStart, drawEnd);
            var M = Math.Max(drawStart, drawEnd);
            if (m < 0 || M < 0) return;
            for (int y = m; y <= M; ++y)
            {
                surface.SetCell(x, y, cell);
            }
        }

        public void Rotate(float deg)
        {
            DirectionVector = new Vector2((float)Math.Cos(deg) * DirectionVector.X - (float)Math.Sin(deg) * DirectionVector.Y, (float)Math.Sin(deg) * DirectionVector.X + (float)Math.Cos(deg) * DirectionVector.Y);
            PlaneVector = new Vector2((float)Math.Cos(deg) * PlaneVector.X - (float)Math.Sin(deg) * PlaneVector.Y, (float)Math.Sin(deg) * PlaneVector.X + (float)Math.Cos(deg) * PlaneVector.Y);
        }

        private Color DarkerColor(Color c, float lerp = .25f)
        {
            return Color.Lerp(c, Color.Black, lerp);
        }

        public override void Print(OpalConsoleWindow surface, Rectangle targetArea)
        {
            Position = new Vector2(Following.LocalPosition.X, Following.LocalPosition.Y);
            const int texWidth = 10, texHeight = 10;

            for (int x = 0; x < targetArea.Width; ++x)
            {
                // x-coordinate in camera space
                float cameraX = 2 * x / (float)targetArea.Width - 1;

                // Direction of this ray
                Vector2 rayDir = new Vector2(DirectionVector.X + PlaneVector.X * cameraX, DirectionVector.Y + PlaneVector.Y * cameraX);

                //which box of the map we're in
                Point mapPos = Following.LocalPosition;

                //length of ray from current position to next x or y-side
                Vector2 sideDist = new Vector2();

                //length of ray from one x or y-side to next x or y-side
                Vector2 deltaDist = new Vector2(Math.Abs(1 / rayDir.X), Math.Abs(1 / rayDir.Y));

                float perpWallDist;

                //what direction to step in x or y-direction (either +1 or -1)
                Point stepDir = new Point();

                // Wall hit? Side hit?
                bool hit = false, side = false;

                //calculate step and initial sideDist
                if (rayDir.X < 0)
                {
                    stepDir.X = -1;
                    sideDist.X = (Position.X - mapPos.X) * deltaDist.X;
                }
                else
                {
                    stepDir.X = 1;
                    sideDist.X = ((Position.X - mapPos.X) + 1.0f) * deltaDist.X;
                }
                if (rayDir.Y < 0)
                {
                    stepDir.Y = -1;
                    sideDist.Y = (Position.Y - mapPos.Y) * deltaDist.Y;
                }
                else
                {
                    stepDir.Y = 1;
                    sideDist.Y = ((Position.Y - mapPos.Y) + 1.0f) * deltaDist.Y;
                }

                //perform DDA
                while (!hit)
                {
                    //maxtries++;
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
                    //Check if ray has hit a wall
                    var t = Target.TileAt(mapPos.X, mapPos.Y);
                    if (t == null) break;
                    if (t.Properties.BlocksMovement) hit = true;
                }

                //Calculate distance projected on camera direction (Euclidean distance will give fisheye effect!)
                if (!side) perpWallDist = ((mapPos.X - Position.X) + (1 - stepDir.X) / 2) / rayDir.X;
                else perpWallDist = ((mapPos.Y - Position.Y) + (1 - stepDir.Y) / 2) / rayDir.Y;

                //Calculate height of line to draw on screen
                int lineHeight = (int)(targetArea.Height / perpWallDist);

                //calculate lowest and highest pixel to fill in current stripe
                int drawStart = -lineHeight / 2 + targetArea.Height / 2;
                if (drawStart < 0) drawStart = 0;
                int drawEnd = lineHeight / 2 + targetArea.Height / 2;
                if (drawEnd >= targetArea.Height) drawEnd = targetArea.Height - 1;

                //calculate value of wallX
                float wallX; //where exactly the wall was hit
                if (!side) wallX = Position.Y + perpWallDist * rayDir.Y;
                else wallX = Position.X + perpWallDist * rayDir.X;
                wallX -= (float)Math.Floor(wallX);

                OpalTile wallTile = Target.TileAt(mapPos.X, mapPos.Y);

                //x coordinate on the texture
                int texX = (int)(wallX * (float)(texWidth));
                if (!side && rayDir.X > 0) texX = texWidth - texX - 1;
                if (side && rayDir.Y < 0) texX = texWidth - texX - 1;

                for (int y = drawStart; y <= drawEnd; y++)
                {
                    int d = y * 256 - targetArea.Height * 128 + lineHeight * 128;  //256 and 128 factors to avoid floats
                                                                   // TODO: avoid the division to speed this up
                    int texY = ((d * texHeight) / (lineHeight + 1)) / 256;
                    Color[,] wallPixels = wallTile.Graphics.GetPixels(Program.Font);
                    Color wallColor = wallPixels[texX, texY];
                    //make color darker for y-sides: R, G and B byte each divided through two with a "shift" and an "and"
                    if (side) wallColor = DarkerColor(wallColor);
                    //shade color with distance
                    wallColor = DarkerColor(wallColor, perpWallDist / 30f);
                    surface.SetCell(x, y, new Cell(wallColor, wallColor, ' '));
                }

                // FLOOR CASTING
                Vector2 floorWall = new Vector2(); //x, y position of the floor texel at the bottom of the wall
                                                   //4 different wall directions possible
                if (!side && rayDir.X > 0)
                {
                    floorWall.X = mapPos.X;
                    floorWall.Y = mapPos.Y + wallX;
                }
                else if (!side && rayDir.X < 0)
                {
                    floorWall.X = mapPos.X + 1.0f;
                    floorWall.Y = mapPos.Y + wallX;
                }
                else if (side && rayDir.Y > 0)
                {
                    floorWall.X = mapPos.X + wallX;
                    floorWall.Y = mapPos.Y;
                }
                else
                {
                    floorWall.X = mapPos.X + wallX;
                    floorWall.Y = mapPos.Y + 1.0f;
                }

                float distWall, distPlayer, currentDist;

                distWall = perpWallDist;
                distPlayer = 0.0f;

                if (drawEnd < 0) drawEnd = targetArea.Height; //becomes < 0 when the integer overflows
                                                              //draw the floor from drawEnd to the bottom of the screen
                for (int y = drawEnd; y < targetArea.Height; y++)
                {
                    currentDist = targetArea.Height / (2.0f * (y + 1) - targetArea.Height); //you could make a small lookup table for this instead

                    float weight = (currentDist - distPlayer) / (distWall - distPlayer);

                    float currentFloorX = weight * floorWall.X + (1.0f - weight) * Position.X;
                    float currentFloorY = weight * floorWall.Y + (1.0f - weight) * Position.Y;

                    int floorTexX, floorTexY;

                    floorTexX = (int)(currentFloorX * texWidth) % texWidth;
                    floorTexY = (int)(currentFloorY * texHeight) % texHeight;

                    //floor
                    OpalTile floorTile = Target.TileAt((int)currentFloorX, (int)currentFloorY);
                    Color[,] floorPixels = floorTile.Graphics.GetPixels(Program.Font);
                    Color floorColor = DarkerColor(floorPixels[floorTexX, floorTexY], currentDist / 30f);

                    surface.SetCell(x, y, new Cell(floorColor, floorColor, ' '));
                    //ceiling (symmetrical!)
                    surface.SetCell(x, targetArea.Height - y - 1, new Cell(Color.CornflowerBlue, DarkerColor(Color.CornflowerBlue, currentDist / 30f), ' '));
                    //buffer[h - y][x] = texture[6][texWidth * floorTexY + floorTexX];
                }
            }
        }
    }

    // Here the pipeline is used to control the game remotely and to let it propagate messages to the parent window(s).
    public class OpalGame : IPipelineSubscriber<OpalGame>
    {
        public MessagePipeline<OpalConsoleWindow> InternalMessagePipeline { get; protected set; }

        public Viewport Viewport { get; set; }
        public Guid Handle { get; }

        public OpalCreature Player = new OpalCreature();

        public OpalGame(Viewport defaultViewport)
        {
            Handle = Guid.NewGuid();
            Player = new OpalCreature();

            Viewport = defaultViewport;
            Viewport = new RaycastViewport(Viewport.Target, Viewport.ViewArea, Player);

            Player.ChangeLocalMap(Viewport.Target, new Point(Viewport.Target.Width / 2, Viewport.Target.Height / 2));
            Viewport.Target.Actors.Add(Player);
            InternalMessagePipeline = new MessagePipeline<OpalConsoleWindow>();
        }

        public virtual void Update(TimeSpan delta)
        {
            Viewport.Target.Update(delta);
        }

        public virtual void Draw(TimeSpan delta)
        {
            // Tell any subscribed OpalGameWindow to render the viewport.
            InternalMessagePipeline.Broadcast(null, new Func<OpalConsoleWindow, string>(
                w => 
                {
                    w.Clear();
                    if (Viewport.Target.Actors.Count > 0)
                    {
                        Viewport.ViewArea = new Rectangle(Viewport.Target.Actors[0].LocalPosition.X - w.Width / 2, Viewport.Target.Actors[0].LocalPosition.Y - w.Height / 2, w.Width, w.Height);
                    }
                    Viewport.Print(w, new Rectangle(new Point(0, 0), new Point(w.Width, w.Height)));
                    return "ViewportRefresh";
                }
                ));
        }

        public void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalGame, string> msg, bool is_broadcast)
        {
            string performed_action = msg(this);
            switch(performed_action)
            {
                case "RequestInfo": // Is a FWD from an OpalInfoWindow connected to a MessagePipeline<OpalConsoleWindow>
                    var info_pipeline = MessagePipeline<OpalConsoleWindow>.GetPipeline(pipeline_handle);
                    // Using a Forward in order to pass our Handle despite not being an OpalConsoleWindow
                    info_pipeline.Forward<OpalConsoleWindow>(pipeline_handle, Handle, sender_handle, new Func<OpalConsoleWindow, string>(
                        w => 
                        {
                            OpalInfoWindow info_window = (OpalInfoWindow)w;

                            OpalInfoWindow.GameInfo info = new OpalInfoWindow.GameInfo
                            {
                                PlayerName = "Kappa",
                                PlayerTitle = "Human",
                                PlayerLevel = 1,
                                PlayerHp = 2,
                                PlayerMaxHp = 4,
                                PlayerLocalPosition = Viewport.Target.Actors[0].LocalPosition
                            };

                            info_window.ReceiveInfoUpdateFromGame(Handle, ref info);
                            return "ServeInfo";
                        }
                        ));
                    break;
                default:
                    break;
            }
        }
    }
}
