using FieryOpal.src.Ui;
using FieryOpal.Src.Actors;
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
        private struct VLineInfo
        {
            public float PerpWallDist;
            public float PerpendicularWallX;
            public bool SideHit;
            public int Column;
            public int DrawStart;
            public int DrawEnd;
            public int LineHeight;
            public Vector2 RayDir;
            public Point RayPos;
            public Vector2 StartPos;
        }

        public bool Dirty { get; private set; }
        public float ViewDistance { get; set; }
        public TurnTakingActor Following { get; set; }

        private float AspectRatio = 0.5f;

        public Vector2 DirectionVector
        {
            get
            {
                var look = Following.LookingAt.ToUnit();
                if (Vector2.DistanceSquared(look, new Vector2(0, 0)) == 2)
                {
                    return look / 3 * AspectRatio;
                }
                return look / 2 * AspectRatio;
            }
        }
        public Vector2 PlaneVector => DirectionVector.Orthogonal();

        public RaycastViewport(OpalLocalMap target, Rectangle view_area, TurnTakingActor following) : base(target, view_area)
        {
            Following = following;
            ViewDistance = 64f;
            Dirty = true;
        }

        public void FlagForRedraw()
        {
            Dirty = true;
        }

        private Font GetFontByDist(float perpDist, ICustomSpritesheet thing)
        {
            if (perpDist / ViewDistance > .4f)
            {
                return Nexus.Fonts.MainFont;
            }
            return thing.Spritesheet;
        }

        private void DrawWallVLine(SadConsole.Console surface, VLineInfo info)
        {
            Color[,] wallPixels;
            OpalTile wallTile = Target.TileAt(info.RayPos.X, info.RayPos.Y);
            Font spritesheet = null;
            // If there's no wall tile
            if (!wallTile?.Properties.IsBlock ?? true)
            {
                // We might still find a decoration that wants to display as a wall
                var decos = Target.ActorsAt(info.RayPos.X, info.RayPos.Y)
                    .Where(d => (d as DecorationBase)?.DisplayAsBlock ?? false)
                    .ToList();
                if (decos.Count <= 0) return;
                // And if we do let's draw that as if it were a wall
                wallPixels = FontTextureCache.GetRecoloredPixels(
                    (spritesheet = GetFontByDist(info.PerpWallDist, decos[0])),
                    (byte)decos[0].FirstPersonGraphics.Glyph,
                    decos[0].FirstPersonGraphics.Foreground,
                    decos[0].FirstPersonGraphics.Background
                );
            }
            // But if we do have a wall tile, let's draw that
            else
            {
                wallPixels = FontTextureCache.GetRecoloredPixels(
                    (spritesheet = GetFontByDist(info.PerpWallDist, wallTile)),
                    (byte)wallTile.Graphics.Glyph,
                    wallTile.Graphics.Foreground,
                    wallTile.Graphics.Background
                );
            }

            //x coordinate on the texture
            int texX = Math.Max((int)(info.PerpendicularWallX * spritesheet.Size.X), 0);
            if ((!info.SideHit && info.RayDir.X > 0) || (info.SideHit && info.RayDir.Y < 0))
                texX = spritesheet.Size.X - texX - 1;

            for (int y = info.DrawStart; y <= info.DrawEnd; y++)
            {
                //256 and 128 factors to avoid floats
                int d = y * 256 - surface.Height * 128 + info.LineHeight * 128;
                int texY = ((d * spritesheet.Size.Y) / (info.LineHeight + 1)) / 256;
                if (texY < 0) continue;

                Color wallColor = wallPixels[texX, texY];
                //Side walls are slightly darker
                if (info.SideHit) wallColor = Color.Lerp(wallColor, Color.Black, .25f);
                //Distant walls blend in with the sky
                wallColor = Color.Lerp(wallColor, Target.SkyColor, info.PerpWallDist / ViewDistance);
                surface.SetCell(info.Column, y, new Cell(wallColor, wallColor, ' '));
            }
        }

        private void DrawFloorAndSkyVLine(SadConsole.Console surface, VLineInfo info)
        {
            //x, y position of the floor texel at the bottom of the wall
            //4 different wall directions possible
            Vector2 floorWall = new Vector2();
            if (!info.SideHit && info.RayDir.X > 0)
            {
                floorWall.X = info.RayPos.X;
                floorWall.Y = info.RayPos.Y + info.PerpendicularWallX;
            }
            else if (!info.SideHit && info.RayDir.X < 0)
            {
                floorWall.X = info.RayPos.X + 1.0f;
                floorWall.Y = info.RayPos.Y + info.PerpendicularWallX;
            }
            else if (info.SideHit && info.RayDir.Y > 0)
            {
                floorWall.X = info.RayPos.X + info.PerpendicularWallX;
                floorWall.Y = info.RayPos.Y;
            }
            else
            {
                floorWall.X = info.RayPos.X + info.PerpendicularWallX;
                floorWall.Y = info.RayPos.Y + 1.0f;
            }

            OpalTile lastTile = null;
            Color[,] floorPixels = null;
            for (int y = info.DrawEnd; y < surface.Height; y++)
            {
                float currentDist = surface.Height / (2f * y - surface.Height);
                if (currentDist > info.PerpWallDist) continue;

                float weight = currentDist / info.PerpWallDist;
                Vector2 currentFloor = new Vector2();
                currentFloor.X = weight * floorWall.X + (1.0f - weight) * info.StartPos.X;
                currentFloor.Y = weight * floorWall.Y + (1.0f - weight) * info.StartPos.Y;

                OpalTile floorTile = Target.TileAt(currentFloor.ToPoint());
                // Don't waste cycles
                if (floorTile?.Properties.IsBlock ?? true) continue;
                Font spritesheet = floorTile.Spritesheet;

                Point floorTex;
                // Only interested in the decimal part of currentFloor
                floorTex.X = (int)((currentFloor.X - (int)currentFloor.X) * spritesheet.Size.X);
                floorTex.Y = (int)((currentFloor.Y - (int)currentFloor.Y) * spritesheet.Size.Y);

                if (lastTile != floorTile)
                    floorPixels = FontTextureCache.GetRecoloredPixels(
                        spritesheet,
                        (byte)floorTile.Graphics.Glyph,
                        floorTile.Graphics.Foreground,
                        floorTile.Graphics.Background
                    );

                Color floorColor = Color.Lerp(floorPixels[floorTex.X, floorTex.Y], Target.SkyColor, (float)Math.Pow(((surface.Height) / 2f) / y, ViewDistance / 2));
                // Floor
                surface.SetCell(info.Column, y, new Cell(floorColor, floorColor, ' '));
                // Ceiling
                surface.SetCell(info.Column, surface.Height - y - 1, new Cell(Target.SkyColor, Target.SkyColor, ' '));
                lastTile = floorTile;
            }
        }

        private void DrawActorSpriteVLines(SadConsole.Console surface, float[] zbuffer, Vector2 startPos)
        {
            List<IOpalGameActor> actors_within_viewarea = Target.ActorsWithinRing((int)startPos.X, (int)startPos.Y, (int)ViewDistance, 0)
                .Where(a =>
                       // Must be visible, duh
                       a.Visible
                       // Must not be a decoration with the DisplayAsBlock property
                       && !(a is DecorationBase && (a as DecorationBase).DisplayAsBlock)
                       // Must derive from OpalActorBase
                       && a is OpalActorBase
                       // Must not be the actor we're following or them
                       && a.LocalPosition != startPos.ToPoint()
                       // And must be in a visible position
                       && (Math.Sign(a.LocalPosition.X - startPos.X) == Math.Sign(DirectionVector.X)
                       || Math.Sign(a.LocalPosition.Y - startPos.Y) == Math.Sign(DirectionVector.Y))
                ).ToList();
            actors_within_viewarea.Sort((a, b) =>
            {
                var b_dist = a.LocalPosition.SquaredEuclidianDistance(startPos.ToPoint());
                var a_dist = b.LocalPosition.SquaredEuclidianDistance(startPos.ToPoint());
                return a_dist > b_dist ? 1 : (a_dist == b_dist ? 0 : -1);
            });

            foreach (var actor in actors_within_viewarea)
            {
                Vector2 spritePosition = (actor.LocalPosition.ToVector2() - startPos + new Vector2(.5f));
                Vector2 spriteProjection = new Vector2();

                // Required for correct matrix multiplication
                float invDet = 1.0f / (PlaneVector.X * DirectionVector.Y - DirectionVector.X * PlaneVector.Y);
                spriteProjection.X = invDet * (spritePosition.X * DirectionVector.Y - spritePosition.Y * DirectionVector.X);
                spriteProjection.Y = invDet * (-spritePosition.X * PlaneVector.Y + spritePosition.Y * PlaneVector.X);

                if (spriteProjection.Y == 0) continue;

                float distance_scaled = (float)actor.LocalPosition.Dist(startPos) / ViewDistance;
                Font spritesheet = GetFontByDist(distance_scaled * ViewDistance, actor);

                int spriteScreenX = (int)((surface.Width / 2) * (1 + spriteProjection.X / spriteProjection.Y));
                int spriteHeight = (int)(Math.Abs((int)(surface.Height / spriteProjection.Y)) / actor.FirstPersonScale.Y);
                int vMoveScreen = (int)(actor.FirstPersonVerticalOffset * spritesheet.Size.Y / spriteProjection.Y) + spriteHeight / 2;

                //calculate width of the sprite
                int spriteWidth = (int)(Math.Abs((int)(surface.Height / (spriteProjection.Y))) / actor.FirstPersonScale.X);

                //calculate lowest and highest pixel to fill in current stripe
                Point drawStart = new Point(), drawEnd = new Point();
                drawStart.X = Math.Max(-spriteWidth / 2 + spriteScreenX, 0);
                drawStart.Y = Math.Max(-spriteHeight / 2 + surface.Height / 2 + vMoveScreen, 0);
                drawEnd.X = Math.Min(spriteWidth / 2 + spriteScreenX, surface.Width - 1);
                drawEnd.Y = Math.Min(spriteHeight / 2 + surface.Height / 2 + vMoveScreen, surface.Height - 1);

                //loop through every vertical stripe of the sprite on screen
                if (drawStart.X >= drawEnd.X || spriteHeight == 0)
                    continue;

                Color[,] spritePixels = null;
                for (int stripe = drawStart.X; stripe < drawEnd.X; stripe++)
                {
                    int texX = (256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * spritesheet.Size.X / spriteWidth) / 256;
                    //the conditions in the if are:
                    //1) it's in front of camera plane so you don't see things behind you
                    //2) it's on the screen (left)
                    //3) it's on the screen (right)
                    //4) ZBuffer, with perpendicular distance
                    int lineHeight = (int)(surface.Height / zbuffer[stripe]);
                    int drawStartStripe = Math.Max(-lineHeight / 2 + surface.Height / 2, 0);

                    if (spriteProjection.Y > 0 && stripe > 0 && stripe < surface.Width && (spriteProjection.Y < zbuffer[stripe] || drawStart.Y < drawStartStripe))
                    {
                        // For every pixel of this stripe
                        for (int y = drawStart.Y; y < drawEnd.Y; y++)
                        {
                            if (spriteProjection.Y >= zbuffer[stripe] && y >= drawStartStripe) break;
                            if (spritePixels == null) // Wait as long as possible to load the pixels to save some cache hits
                                spritePixels = FontTextureCache.GetRecoloredPixels(
                                    spritesheet,
                                    (byte)actor.FirstPersonGraphics.Glyph,
                                    actor.FirstPersonGraphics.Foreground,
                                    Color.Transparent
                                );

                            int d = (y - vMoveScreen) * 256 - surface.Height * 128 + spriteHeight * 128;
                            int texY = Math.Max(((d * spritesheet.Size.Y) / spriteHeight) / 256, 0);

                            Color spriteColor = spritePixels[texX, texY];
                            if (spriteColor == Color.Transparent) continue;

                            // Shade to sky color with distance from player
                            spriteColor = Color.Lerp(spriteColor, Target.SkyColor, distance_scaled);
                            surface.SetCell(stripe, y, new Cell(spriteColor, spriteColor, ' '));
                        }
                    }
                }
            }
        }

        public override void Print(SadConsole.Console surf, Rectangle viewArea, TileMemory fog = null)
        {
            // Don't waste precious cycles
            if (!Dirty) return;
            // As of now, we're letting this viewport control the fog
            // to display it properly in the topdown viewport. TODO: REFACTOR
            fog.UnseeEverything();
            // Fill with SkyColor to cover up any off-by-one errors
            // Also to make the wall drawing code lighter
            surf.Fill(Target.SkyColor, Target.SkyColor, ' ');

            // Move the ray at the center of the square instead of at the TL corner
            Vector2 startPos = Following.LocalPosition.ToVector2() + new Vector2(.5f);
            AspectRatio = surf.Width / (float)surf.Height;

            float[] zbuffer = new float[surf.Width];
            for (int x = 0; x < surf.Width; ++x)
            {
                // x-coordinate in camera space
                // A small floating point value is added to X to prevent integers
                // from messing up floating point maths. Go figure.
                float cameraX = 2 * (x + .001f) / (float)surf.Width - 1;

                // Direction of this ray
                Vector2 rayDir = new Vector2(DirectionVector.X + PlaneVector.X * cameraX, DirectionVector.Y + PlaneVector.Y * cameraX);
                // Which box of the map we're in, later gets changed inside the loop
                Vector2 mapPos = startPos.ToPoint().ToVector2();
                // Did we hit a side?
                bool side = false;
                float perpWallDist = zbuffer[x] = Raycaster.CastRay(Target, startPos, ref mapPos, rayDir, ref side, fog);

                // Calculate height of line to draw on screen
                int lineHeight = (int)(surf.Height / perpWallDist);

                // Calculate lowest and highest pixel to fill in current stripe
                int drawStart = (int)(Math.Max(-lineHeight / 2f + surf.Height / 2f, 0));
                int drawEnd = (int)(Math.Min(lineHeight / 2f + surf.Height / 2f, surf.Height - 1));

                // Calculate value of wallX (where exactly the wall was hit)
                float wallX = side
                    ? startPos.X + perpWallDist * rayDir.X
                    : startPos.Y + perpWallDist * rayDir.Y;
                wallX -= (float)Math.Floor(wallX);

                VLineInfo rcpInfo = new VLineInfo
                {
                    PerpWallDist = perpWallDist,
                    PerpendicularWallX = wallX,
                    SideHit = side,
                    Column = x,
                    DrawStart = drawStart,
                    DrawEnd = drawEnd,
                    LineHeight = lineHeight,
                    RayDir = rayDir,
                    RayPos = mapPos.ToPoint(),
                    StartPos = startPos,
                };

                DrawFloorAndSkyVLine(surf, rcpInfo);
                DrawWallVLine(surf, rcpInfo);
            }

            DrawActorSpriteVLines(
                surf,
                zbuffer,
                startPos
            );


            Dirty = false;
        }
    }
}
