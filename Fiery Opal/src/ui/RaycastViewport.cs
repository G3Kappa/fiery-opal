using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui
{

    // Translated to C# from http://lodev.org/cgtutor/raycasting.html ; http://lodev.org/cgtutor/raycasting2.html
    public class RaycastViewport : LocalMapViewport
    {
        public IOpalGameActor Following { get; protected set; }
        public Vector2 DirectionVector { get; set; }
        public Vector2 PlaneVector { get; set; }
        public Vector2 Position { get; protected set; }
        public Font RenderFont { get; protected set; }
        public bool Dirty { get; private set; }
        public float ViewDistance { get; set; }

        public RaycastViewport(OpalLocalMap target, Rectangle view_area, IOpalGameActor following, Font f = null) : base(target, view_area)
        {
            RenderFont = f ?? Program.Font;
            Following = following;
            DirectionVector = new Vector2(0, 1);
            PlaneVector = new Vector2(-1, 0);
            Position = new Vector2(0, 0);
            Dirty = true;
            ViewDistance = 64f;
        }

        public void FlagForRedraw()
        {
            Dirty = true;
        }

        public void Rotate(float deg)
        {
            // Rotate both vectors
            DirectionVector = new Vector2((float)Math.Cos(deg) * DirectionVector.X - (float)Math.Sin(deg) * DirectionVector.Y, (float)Math.Sin(deg) * DirectionVector.X + (float)Math.Cos(deg) * DirectionVector.Y);
            PlaneVector = new Vector2((float)Math.Cos(deg) * PlaneVector.X - (float)Math.Sin(deg) * PlaneVector.Y, (float)Math.Sin(deg) * PlaneVector.X + (float)Math.Cos(deg) * PlaneVector.Y);
            // Round them to cut any possible floating point errors short and maintain a constant ratio, just in case
            DirectionVector = new Vector2((float)Math.Round(DirectionVector.X, 0), (float)Math.Round(DirectionVector.Y, 0));
            PlaneVector = new Vector2((float)Math.Round(PlaneVector.X, 5), (float)Math.Round(PlaneVector.Y, 5));

            if(deg != 0) Dirty = true;
        }

        private void CalcStep(Vector2 rayDir, Vector2 position, Vector2 mapPos, Vector2 deltaDist, ref Vector2 stepDir, ref Vector2 sideDist)
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

        private bool DDA(Vector2 deltaDist, Vector2 stepDir, ref Vector2 mapPos, ref Vector2 sideDist, bool update_fog = true)
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
                var t = Target.TileAt((int)mapPos.X, (int)mapPos.Y);
                if (update_fog)
                {
                    Target.Fog.See(new Point((int)mapPos.X, (int)mapPos.Y));
                    Target.Fog.Learn(new Point((int)mapPos.X, (int)mapPos.Y));
                }
                //Check if ray has hit a wall
                if (t == null || t.Properties.BlocksMovement && !(t is Door && (t as Door).IsOpen))
                {
                    return side;
                }
                // Check if it hit a decoration that should render as a wall
                var decos = Target.ActorsAt((int)mapPos.X, (int)mapPos.Y).Where(d => d is DecorationBase && (d as DecorationBase).DisplayAsBlock);
                if (decos.Count() > 0)
                {
                    return side;
                }
            }
        }

        private void DrawWallVLine(SadConsole.Console surface, Vector2 mapPos, int x, float perpWallDist, float wallX, bool side, Vector2 rayDir, int drawStart, int drawEnd, int lineHeight, int viewportHeight)
        {
            //Fog.Clear(new Point((int)mapPos.X, (int)mapPos.Y));
            OpalTile wallTile = Target.TileAt((int)mapPos.X, (int)mapPos.Y);
            Color[,] wallPixels;
            if (wallTile == null || !wallTile.Properties.BlocksMovement)
            {
                var decos = Target.ActorsAt((int)mapPos.X, (int)mapPos.Y).Where(d => d is DecorationBase && (d as DecorationBase).DisplayAsBlock).ToList();
                if(decos.Count > 0)
                {
                    wallPixels = FontTextureCache.GetRecoloredPixels(RenderFont, (byte)decos[0].FirstPersonGraphics.Glyph, decos[0].FirstPersonGraphics.Foreground, decos[0].FirstPersonGraphics.Background);
                }
                else
                {
                    for (int y = drawStart; y <= drawEnd; y++)
                    {
                        surface.SetCell(x, y, new Cell(Target.SkyColor, Target.SkyColor, ' '));
                    }
                    return;
                }
            }
            else
            {
                wallPixels = FontTextureCache.GetRecoloredPixels(RenderFont, (byte)wallTile.Graphics.Glyph, wallTile.Graphics.Foreground, wallTile.Graphics.Background);
            }

            int texWidth = RenderFont.Size.X, texHeight = RenderFont.Size.Y;

            //x coordinate on the texture
            int texX = Math.Max((int)(wallX * texWidth), 0);
            if (!side && rayDir.X > 0) texX = texWidth - texX - 1;
            if (side && rayDir.Y < 0) texX = texWidth - texX - 1;

            for (int y = drawStart; y <= drawEnd; y++)
            {
                int d = y * 256 - viewportHeight * 128 + lineHeight * 128;  //256 and 128 factors to avoid floats
                                                                            // TODO: avoid the division to speed this up
                int texY = ((d * texHeight) / (lineHeight + 1)) / 256;

                Color wallColor = texY < 0 ? Target.SkyColor : wallPixels[texX, texY];
                //make color darker for y-sides: R, G and B byte each divided through two with a "shift" and an "and"
                if (side) wallColor = Color.Lerp(wallColor, Color.Black, .25f);
                //shade color with distance
                wallColor = Color.Lerp(wallColor, Target.SkyColor, (float)perpWallDist / ViewDistance);
                //apply lighting
                /*
                foreach(var t in lighting)
                {
                    wallColor = Color.Lerp(wallColor, t.Item1, (float)t.Item2);
                }
                */
                surface.SetCell(x, y, new Cell(wallColor, wallColor, ' '));
            }
        }
        private void DrawFloorAndSkyVLine(SadConsole.Console surface, Vector2 mapPos, int x, float perpWallDist, float wallX, bool side, Vector2 rayDir, int drawStart, int drawEnd, int lineHeight, int viewportHeight)
        {
            int texWidth = RenderFont.Size.X, texHeight = RenderFont.Size.Y;

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

            if (drawEnd < 0) drawEnd = viewportHeight; //becomes < 0 when the integer overflows

            OpalTile lastTile = null;
            Color[,] lastPixels = null;
            for (int y = drawEnd + viewportHeight % 2; y < viewportHeight; y++)
            {
                currentDist = viewportHeight / (2.0f * y - viewportHeight); //you could make a small lookup table for this instead
                if (currentDist > perpWallDist) continue;

                float weight = (currentDist - distPlayer) / (distWall - distPlayer);

                float currentFloorX = weight * floorWall.X + (1.0f - weight) * Position.X;
                float currentFloorY = weight * floorWall.Y + (1.0f - weight) * Position.Y;
                
                int floorTexX, floorTexY;

                floorTexX = (int)((currentFloorX - (int)currentFloorX) * texWidth);
                floorTexY = (int)((currentFloorY - (int)currentFloorY) * texHeight);

                //floor
                OpalTile floorTile = Target.TileAt((int)currentFloorX, (int)currentFloorY);
                if (floorTile == null || floorTexX < 0 || floorTexY < 0 || floorTile.Properties.BlocksMovement) { continue; }
                //Fog.Clear(new Point((int)currentFloorX, (int)currentFloorY));
                Color[,] floorPixels;
                if (lastTile != floorTile)
                {
                    floorPixels = FontTextureCache.GetRecoloredPixels(RenderFont, (byte)floorTile.Graphics.Glyph, floorTile.Graphics.Foreground, floorTile.Graphics.Background);
                }
                else floorPixels = lastPixels;

                Color floorColor = Color.Lerp(floorPixels[floorTexX, floorTexY], Target.SkyColor, (float)Math.Pow(((viewportHeight) / 2f) / y, ViewDistance / 2));
                
                surface.SetCell(x, y, new Cell(floorColor, floorColor, ' '));
                //ceiling (symmetrical!)
                surface.SetCell(x, viewportHeight - y - 1, new Cell(Target.SkyColor, Target.SkyColor, ' '));
                /*
                if (floorTile.Properties.IsNatural)
                {
                }
                else
                {
                    surface.SetCell(x, viewportHeight - y - 1, new Cell(floorColor, floorColor, ' '));
                }
                */
                lastTile = floorTile;
                lastPixels = floorPixels;
            }
            if (viewportHeight - (drawEnd + viewportHeight % 2) - 1 < 0) return;
            surface.SetCell(x, viewportHeight - (drawEnd + viewportHeight % 2) - 1, new Cell(Target.SkyColor, Target.SkyColor, ' '));
        }
        private void DrawActorSpriteVLines(SadConsole.Console surface, float[] zbuffer, int viewportWidth, int viewportHeight)
        {

            List<IOpalGameActor> actors_within_viewarea = Target.ActorsWithinRing((int)Position.X, (int)Position.Y, (int)ViewDistance, 0)
                .Where(a => 
                       a.Visible 
                       && !(a is DecorationBase && (a as DecorationBase).DisplayAsBlock)
                       && a is OpalActorBase
                       && a != Following
                       && (Math.Sign(a.LocalPosition.X - Position.X) == Math.Sign(DirectionVector.X)
                       || Math.Sign(a.LocalPosition.Y - Position.Y) == Math.Sign(DirectionVector.Y))
                ).ToList();
            actors_within_viewarea.Sort((a, b) => {
                var b_dist = Math.Pow(a.LocalPosition.X + .5 - Position.X, 2) + Math.Pow(a.LocalPosition.Y + .5 - Position.Y, 2);
                var a_dist = Math.Pow(b.LocalPosition.X + .5 - Position.X, 2) + Math.Pow(b.LocalPosition.Y + .5 - Position.Y, 2);
                return a_dist > b_dist ? 1 : (a_dist == b_dist ? 0 : -1);
            });

            foreach (var actor in actors_within_viewarea)
            {
                Vector2 spritePosition = (new Vector2(actor.LocalPosition.X + .5f, actor.LocalPosition.Y + .5f) - Position);
                Vector2 spriteProjection = new Vector2();

                float invDet = 1.0f / (PlaneVector.X * DirectionVector.Y - DirectionVector.X * PlaneVector.Y); //required for correct matrix multiplication
                spriteProjection.X = invDet * (spritePosition.X * DirectionVector.Y - spritePosition.Y * DirectionVector.X);
                spriteProjection.Y = invDet * (-spritePosition.X * PlaneVector.Y + spritePosition.Y * PlaneVector.X);
                if (spriteProjection.Y == 0) continue;

                float distance_scaled = (float)actor.LocalPosition.Dist(Position) / ViewDistance;

                if (distance_scaled > .4f)
                {
                    RenderFont = Program.Font;
                }
                int spriteScreenX = (int)((viewportWidth / 2) * (1 + spriteProjection.X / spriteProjection.Y));
                int spriteHeight = (int)(Math.Abs((int)(viewportHeight / spriteProjection.Y)) / actor.FirstPersonScale.Y); //using "transformY" instead of the real distance prevents fisheye
                int vMoveScreen = (int)(actor.FirstPersonVerticalOffset * RenderFont.Size.Y / spriteProjection.Y);

                //calculate lowest and highest pixel to fill in current stripe
                int drawStartY = -spriteHeight / 2 + viewportHeight / 2 + vMoveScreen;
                if (drawStartY < 0) drawStartY = 0;
                int drawEndY = spriteHeight / 2 + viewportHeight / 2 + vMoveScreen;
                if (drawEndY >= viewportHeight) drawEndY = viewportHeight - 1;

                //calculate width of the sprite
                int spriteWidth = (int)(Math.Abs((int)(viewportHeight / (spriteProjection.Y))) / actor.FirstPersonScale.X);
                int drawStartX = -spriteWidth / 2 + spriteScreenX;
                if (drawStartX < 0) drawStartX = 0;
                int drawEndX = spriteWidth / 2 + spriteScreenX;
                if (drawEndX >= viewportWidth) drawEndX = viewportWidth - 1;

                //loop through every vertical stripe of the sprite on screen

                int texWidth = RenderFont.Size.X, texHeight = RenderFont.Size.Y;
                if (drawStartX >= drawEndX || spriteHeight == 0)
                {
                    if (distance_scaled > .4f)
                    {
                        RenderFont = Program.HDFont;
                    }
                    continue;
                }
                Color[,] spritePixels = null;
                for (int stripe = drawStartX; stripe < drawEndX; stripe++)
                {
                    int texX = (int)(256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * texWidth / spriteWidth) / 256;
                    //the conditions in the if are:
                    //1) it's in front of camera plane so you don't see things behind you
                    //2) it's on the screen (left)
                    //3) it's on the screen (right)
                    //4) ZBuffer, with perpendicular distance
                    int lineHeight = (int)(viewportHeight / zbuffer[stripe]);
                    int drawStart = Math.Max(-lineHeight / 2 + viewportHeight / 2, 0);

                    if (spriteProjection.Y > 0 && stripe > 0 && stripe < viewportWidth && (spriteProjection.Y < zbuffer[stripe] || drawStartY < drawStart))
                    {
                        for (int y = drawStartY; y < drawEndY; y++) //for every pixel of the current stripe
                        {
                            if (spriteProjection.Y >= zbuffer[stripe] && y >= drawStart) break;
                            if (spritePixels == null) // Wait as long as possible to load the pixels to save some cache hits
                            {
                                spritePixels = FontTextureCache.GetRecoloredPixels(RenderFont, (byte)actor.FirstPersonGraphics.Glyph, actor.FirstPersonGraphics.Foreground, Color.Transparent);

                            }

                            int d = (y - vMoveScreen) * 256 - viewportHeight * 128 + spriteHeight * 128; //256 and 128 factors to avoid floats
                            int texY = ((d * texHeight) / spriteHeight) / 256;

                            Color spriteColor = spritePixels[texX, texY >= 0 ? texY : 0];
                            if (spriteColor == Color.Transparent)
                            {
                                continue;
                            }

                            // Shade to sky color with distance from player
                            spriteColor = Color.Lerp(spriteColor, Target.SkyColor, distance_scaled);


                            surface.SetCell(stripe, y, new Cell(spriteColor, spriteColor, ' '));
                        }
                    }
                }

                if (distance_scaled > .4f)
                {
                    RenderFont = Program.HDFont;
                }
            }
        }

        private Tuple<Color, double> CalcLighting(Vector2 mapPos, ILightSource l)
        {
            //Util.Log(new ColoredString(mapPos.ToString()), true);
            // Cast a ray from the light to mapPos
            Vector2 lightRayPos = new Vector2(l.LocalPosition.X + .5f, l.LocalPosition.Y + .5f), initialPos = lightRayPos;
            // Direction is mapPos - lightPos, normalized
            Vector2 lightRayDir = new Vector2((mapPos.X + .5f) - lightRayPos.X, (mapPos.Y + .5f) - lightRayPos.Y);
            bool _side = false;

            float dist = CastRay(initialPos, ref lightRayPos, lightRayDir, ref _side);


            // If there are no obstructions
            var ls = (l as ILightSource);
            double brightness = ls.LightIntensity / (4 * Math.PI * Math.Pow(dist, 2));
            if (lightRayPos.Dist(mapPos) <= 2)
            {
                // Calculate the light intensity at this distance and yield it along with the source color
                return new Tuple<Color, double>(ls.LightColor, brightness);
            }
            else
            {
                // Util.Log(new ColoredString(String.Format("Dist Rejected: {0} vs {1} ({2})", lightRayPos, mapPos, lightRayPos.Dist(mapPos)), new Cell(Color.Red)), true);
                return new Tuple<Color, double>(Target.SkyColor, 0);
            }

        }

        public float CastRay(Vector2 position, ref Vector2 mapPos, Vector2 rayDir, ref bool side)
        {
            //length of ray from current position to next x or y-side
            Vector2 sideDist = new Vector2();
            //what direction to step in x or y-direction (either +1 or -1)
            Vector2 stepDir = new Vector2();
            //length of ray from one x or y-side to next x or y-side
            Vector2 deltaDist = new Vector2(Math.Abs(1 / rayDir.X), Math.Abs(1 / rayDir.Y));
            float perpWallDist;

            // Calculate stepDir and initial sideDist
            CalcStep(rayDir, position, mapPos, deltaDist, ref stepDir, ref sideDist);
            // Perform DDA
            side = DDA(deltaDist, stepDir, ref mapPos, ref sideDist);
            // Calculate distance projected on camera direction (Euclidean distance will give fisheye effect!)
            if (!side) perpWallDist = (mapPos.X - position.X + (1 - stepDir.X) / 2) / rayDir.X;
            else perpWallDist = (mapPos.Y - position.Y + (1 - stepDir.Y) / 2) / rayDir.Y;

            return perpWallDist;
        }

        public float CastRay(Vector2 position, ref Vector2 mapPos, Point rayDir, ref bool side)
        {
            return CastRay(position, ref mapPos, new Vector2(rayDir.X, rayDir.Y), ref side);
        }

        public override void Print(SadConsole.Console surface, Rectangle targetArea)
        {
            if (!Dirty) return;
            Target.Fog.UnseeEverything();
            Position = new Vector2(Following.LocalPosition.X + .5f, Following.LocalPosition.Y + .5f);

            var oldD = DirectionVector;
            var oldP = PlaneVector;

            if(DirectionVector.LengthSquared() == 2)
            {
                DirectionVector /= 2f;
                PlaneVector /= 2f;
            }
            else
            {
                DirectionVector /= 1.5f;
                PlaneVector /= 1.5f;
            }

            Target.Fog.See(Following.LocalPosition);
            Target.Fog.Learn(Following.LocalPosition);
            float[] zbuffer = new float[targetArea.Width];
            for (int x = 0; x < targetArea.Width; ++x)
            {
                // -- DECLARATIONS --
                // x-coordinate in camera space
                float cameraX = 2 * x / (float)targetArea.Width - 1;
                if (cameraX == 0) cameraX += .001f; // Prevent middle ray from being exactly perpendicular, giving wrong distances

                // Direction of this ray
                Vector2 rayDir = new Vector2(DirectionVector.X + PlaneVector.X * cameraX, DirectionVector.Y + PlaneVector.Y * cameraX);
                // Which box of the map we're in, later gets changed inside the loop
                Vector2 mapPos = new Vector2(Following.LocalPosition.X, Following.LocalPosition.Y);
                // Did we hit a side?
                bool side = false;
                float perpWallDist = zbuffer[x] = CastRay(Position, ref mapPos, rayDir, ref side);

                /*
                // If we hit a wall, calc the correct lighting
                List<Tuple<Color, double>> lighting = new List<Tuple<Color, double>>();
                if (Target.TileAt((int)mapPos.X, (int)mapPos.Y) != null)
                {
                    var lights = Target.ActorsWithin(null).Where(a => a is ILightSource).ToList();
                    foreach(var l in lights)
                    {
                        var color_dist = CalcLighting(mapPos, l as ILightSource);
                        lighting.Add(color_dist);
                    }
                }
                */

                // Calculate height of line to draw on screen
                int lineHeight = (int)(targetArea.Height / perpWallDist);

                // Calculate lowest and highest pixel to fill in current stripe
                int drawStart = Math.Max(-lineHeight / 2 + targetArea.Height / 2, 0);
                int drawEnd = Math.Min(lineHeight / 2 + targetArea.Height / 2, targetArea.Height - 1);

                // Calculate value of wallX (where exactly the wall was hit)
                float wallX;

                if (!side) wallX = Position.Y + perpWallDist * rayDir.Y;
                else wallX = Position.X + perpWallDist * rayDir.X;

                wallX -= (float)Math.Floor(wallX);

                DrawFloorAndSkyVLine(surface, mapPos, x, perpWallDist, wallX, side, rayDir, drawStart, drawEnd, lineHeight, targetArea.Height);
                if (perpWallDist > ViewDistance / 4)
                {
                    RenderFont = Program.Font;
                }
                DrawWallVLine(surface, mapPos, x, perpWallDist, wallX, side, rayDir, drawStart, drawEnd, lineHeight, targetArea.Height);
                if (perpWallDist > ViewDistance / 4)
                {
                    RenderFont = Program.HDFont;
                }
            }
            DrawActorSpriteVLines(surface, zbuffer, targetArea.Width, targetArea.Height);
            Dirty = false;
            DirectionVector = oldD;
            PlaneVector = oldP;
        }
    }
}
