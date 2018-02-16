using Microsoft.Xna.Framework;
using SadConsole;
using System;

namespace FieryOpal.src.ui
{

    // Translated to C# from http://lodev.org/cgtutor/raycasting.html ; http://lodev.org/cgtutor/raycasting2.html
    public class RaycastViewport : Viewport
    {
        public IOpalGameActor Following { get; protected set; }
        public Vector2 DirectionVector { get; protected set; }
        public Vector2 PlaneVector { get; protected set; }
        public Vector2 Position { get; protected set; }
        public Font RenderFont { get; protected set; }

        public RaycastViewport(OpalLocalMap target, Rectangle view_area, IOpalGameActor following, Font f = null) : base(target, view_area)
        {
            RenderFont = f ?? Program.Font;
            Following = following;
            DirectionVector = new Vector2(-1, 0);
            PlaneVector = new Vector2(0, .66f);
            Position = new Vector2(0, 0);
        }

        public void Rotate(float deg)
        {
            // Rotate both vectors
            DirectionVector = new Vector2((float)Math.Cos(deg) * DirectionVector.X - (float)Math.Sin(deg) * DirectionVector.Y, (float)Math.Sin(deg) * DirectionVector.X + (float)Math.Cos(deg) * DirectionVector.Y);
            PlaneVector = new Vector2((float)Math.Cos(deg) * PlaneVector.X - (float)Math.Sin(deg) * PlaneVector.Y, (float)Math.Sin(deg) * PlaneVector.X + (float)Math.Cos(deg) * PlaneVector.Y);
            // Round them to cut any possible floating point errors short and maintain a constant ratio, just in case
            DirectionVector = new Vector2((float)Math.Round(DirectionVector.X, 0), (float)Math.Round(DirectionVector.Y, 0));
            PlaneVector = new Vector2((float)Math.Round(PlaneVector.X, 5), (float)Math.Round(PlaneVector.Y, 5));
        }

        private void CalcStep(Vector2 rayDir, Vector2 mapPos, Vector2 deltaDist, ref Vector2 stepDir, ref Vector2 sideDist)
        {
            if (rayDir.X < 0)
            {
                stepDir.X = -1;
                sideDist.X = (Position.X - mapPos.X) * deltaDist.X;
            }
            else
            {
                stepDir.X = 1;
                sideDist.X = (mapPos.X + 1 - Position.X) * deltaDist.X;
            }
            if (rayDir.Y < 0)
            {
                stepDir.Y = -1;
                sideDist.Y = (Position.Y - mapPos.Y) * deltaDist.Y;
            }
            else
            {
                stepDir.Y = 1;
                sideDist.Y = (mapPos.Y + 1 - Position.Y) * deltaDist.Y;
            }

        }

        private bool DDA(Vector2 deltaDist, Vector2 stepDir, ref Vector2 mapPos, ref Vector2 sideDist)
        {
            // Wall hit? Side hit?
            bool hit = false, side = false;
            //perform DDA
            while (!hit)
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
                //Check if ray has hit a wall
                var t = Target.TileAt((int)mapPos.X, (int)mapPos.Y);
                if (t == null || t.Properties.BlocksMovement) hit = true;
            }
            return side;
        }

        private void DrawWallVLine(OpalConsoleWindow surface, Vector2 mapPos, int x, float perpWallDist, float wallX, bool side, Vector2 rayDir, int drawStart, int drawEnd, int lineHeight, int viewportHeight)
        {
            OpalTile wallTile = Target.TileAt((int)mapPos.X, (int)mapPos.Y);
            if (wallTile == null)
            {
                for (int y = drawStart; y <= drawEnd; y++)
                {
                    surface.SetCell(x, y, new Cell(Target.SkyColor, Target.SkyColor, ' '));
                }
                return;
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

                Color[,] wallPixels = wallTile.Graphics.GetPixels(RenderFont);
                Color wallColor = texY < 0 ? Target.SkyColor : wallPixels[texX, texY];
                //make color darker for y-sides: R, G and B byte each divided through two with a "shift" and an "and"
                if (side) wallColor = Color.Lerp(wallColor, Color.Black, .25f);
                //shade color with distance
                wallColor = Color.Lerp(wallColor, Target.SkyColor, (float)perpWallDist / 30f);
                surface.SetCell(x, y, new Cell(wallColor, wallColor, ' '));
            }
        }

        private void DrawFloorAndSkyVLines(OpalConsoleWindow surface, Vector2 mapPos, int x, float perpWallDist, float wallX, bool side, Vector2 rayDir, int drawStart, int drawEnd, int lineHeight, int viewportHeight)
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

            for (int y = drawEnd + viewportHeight % 2; y < viewportHeight; y++)
            {
                currentDist = viewportHeight / (2.0f * y - viewportHeight); //you could make a small lookup table for this instead

                float weight = (currentDist - distPlayer) / (distWall - distPlayer);

                float currentFloorX = weight * floorWall.X + (1.0f - weight) * Position.X;
                float currentFloorY = weight * floorWall.Y + (1.0f - weight) * Position.Y;

                int floorTexX, floorTexY;

                floorTexX = (int)(currentFloorX * texWidth) % texWidth;
                floorTexY = (int)(currentFloorY * texHeight) % texHeight;

                //floor
                OpalTile floorTile = Target.TileAt((int)currentFloorX, (int)currentFloorY);
                if (floorTile == null || floorTexX < 0 || floorTexY < 0) { continue; }

                Color[,] floorPixels = floorTile.Graphics.GetPixels(RenderFont);
                Color floorColor = Color.Lerp(floorPixels[floorTexX, floorTexY], Target.SkyColor, (float)Math.Pow(((viewportHeight) / 2f) / y, 8));

                //surface.SetCell(x, y, new Cell(Color.Blue, Color.Blue, ' '));
                surface.SetCell(x, y, new Cell(floorColor, floorColor, ' '));
                //ceiling (symmetrical!)
                surface.SetCell(x, viewportHeight - y - 1, new Cell(Target.SkyColor, Target.SkyColor, ' '));
            }
            surface.SetCell(x, viewportHeight - (drawEnd + viewportHeight % 2) - 1, new Cell(Target.SkyColor, Target.SkyColor, ' '));
        }

        public override void Print(OpalConsoleWindow surface, Rectangle targetArea)
        {
            Position = new Vector2(Following.LocalPosition.X + .5f, Following.LocalPosition.Y + .5f);

            for (int x = 0; x < targetArea.Width; ++x)
            {
                // -- DECLARATIONS --
                // x-coordinate in camera space
                float cameraX = 2 * x / (float)targetArea.Width - 1;
                if (cameraX == 0) cameraX += .001f; // Prevent middle ray from being exactly perpendicular, giving wrong distances

                // Direction of this ray
                Vector2 rayDir = new Vector2(DirectionVector.X + PlaneVector.X * cameraX, DirectionVector.Y + PlaneVector.Y * cameraX);

                //which box of the map we're in, later gets changed inside the loop
                Vector2 mapPos = new Vector2(Following.LocalPosition.X, Following.LocalPosition.Y);
                //length of ray from current position to next x or y-side
                Vector2 sideDist = new Vector2();
                //what direction to step in x or y-direction (either +1 or -1)
                Vector2 stepDir = new Vector2();
                //length of ray from one x or y-side to next x or y-side
                Vector2 deltaDist = new Vector2(Math.Abs(1 / rayDir.X), Math.Abs(1 / rayDir.Y));
                float perpWallDist;


                // -- ALGORITHM --
                // Calculate stepDir and initial sideDist
                CalcStep(rayDir, mapPos, deltaDist, ref stepDir, ref sideDist);

                // Perform DDA
                bool side = DDA(deltaDist, stepDir, ref mapPos, ref sideDist);

                // Calculate distance projected on camera direction (Euclidean distance will give fisheye effect!)
                if (!side) perpWallDist = (mapPos.X - Position.X + (1 - stepDir.X) / 2) / rayDir.X;
                else perpWallDist = (mapPos.Y - Position.Y + (1 - stepDir.Y) / 2) / rayDir.Y;

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

                DrawFloorAndSkyVLines(surface, mapPos, x, perpWallDist, wallX, side, rayDir, drawStart, drawEnd, lineHeight, targetArea.Height);
                DrawWallVLine(surface, mapPos, x, perpWallDist, wallX, side, rayDir, drawStart, drawEnd, lineHeight, targetArea.Height);
            }
        }
    }
}
