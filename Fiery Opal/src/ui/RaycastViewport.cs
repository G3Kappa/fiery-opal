using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Environment;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
        public bool PrintLabels { get; private set; }
        public float ViewDistance { get; set; }
        public bool DrawTerrainGrid { get; private set; }
        public bool DrawActorBoundaryBoxes { get; private set; }
        public bool DrawAmbientShading { get; private set; }
        public TurnTakingActor Following { get; set; }
        public Texture2D RenderSurface { get; private set; }

        private Color[] Backbuffer;
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

        public void FlagForRedraw()
        {
            Dirty = true;
        }
        private bool Toggle(bool value, bool? state)
        {
            if (!state.HasValue)
            {
                return !value;
            }
            else return state.Value;
        }
        public void ToggleLabels(bool? state = null)
        {
            PrintLabels = Toggle(PrintLabels, state);
        }
        public void ToggleActorBoundaryBoxes(bool? state = null)
        {
            DrawActorBoundaryBoxes = Toggle(DrawActorBoundaryBoxes, state);
        }
        public void ToggleTerrainGrid(bool? state = null)
        {
            DrawTerrainGrid = Toggle(DrawTerrainGrid, state);
        }
        public void ToggleAmbientShading(bool? state = null)
        {
            DrawAmbientShading = Toggle(DrawAmbientShading, state);
        }
        private void FillBackbuffer(Color c)
        {
            Backbuffer = c.ToArray(RenderSurface.Width * RenderSurface.Height);
        }
        private void SetBackbufferAt(int x, int y, Color c)
        {
            if (Util.OOB(x, y, RenderSurface.Width, RenderSurface.Height)) return;
            Backbuffer[y * RenderSurface.Width + x] = c;
        }
        private Color GetBackbufferAt(int x, int y)
        {
            if (Util.OOB(x, y, RenderSurface.Width, RenderSurface.Height)) return Color.Magenta;
            return Backbuffer[y * RenderSurface.Width + x];
        }
        private void UpdateRenderSurface()
        {
            RenderSurface.SetData(Backbuffer);
        }
        private Font GetFontByDist(float perpDist, ICustomSpritesheet thing)
        {
            return thing.Spritesheet;
            if (perpDist / ViewDistance > .4f)
            {
                return Nexus.Fonts.MainFont;
            }
            return thing.Spritesheet;
        }


        public RaycastViewport(OpalLocalMap target, Rectangle view_area, TurnTakingActor following) : base(target, view_area)
        {
            Following = following;
            ViewDistance = 64f;
            Dirty = true;
            DrawActorBoundaryBoxes = false;
            DrawTerrainGrid = false;
            DrawAmbientShading = true;
        }

        private void DrawWallVLine(VLineInfo info)
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
                int d = y * 256 - RenderSurface.Height * 128 + info.LineHeight * 128;
                int texY = ((d * spritesheet.Size.Y) / (info.LineHeight + 1)) / 256;
                if (texY < 0) continue;

                Color wallColor = wallPixels[texX, texY];
                //Side walls are slightly darker
                if (info.SideHit) wallColor = Color.Lerp(wallColor, Color.Black, .25f);
                // lighting
                wallColor = Target.Lighting.Shade(wallColor, info.RayPos);

                //Distant walls blend in with the sky
                if (DrawAmbientShading) wallColor = Color.Lerp(wallColor, Target.SkyColor, info.PerpWallDist / ViewDistance);
                SetBackbufferAt(info.Column, y, wallColor);
            }
        }

        private void DrawFloorAndSkyVLine(VLineInfo info)
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
            Point oldFloorPos = new Point(-1,-1);

            Color[,] ceilingPixels = null;
            if(Target.CeilingTile != null)
            {
                var reft = OpalTile.GetRefTile(Target.CeilingTile);
                ceilingPixels = FontTextureCache.GetRecoloredPixels(
                    reft.Spritesheet,
                    (byte)reft.Graphics.Glyph,
                    reft.Graphics.Foreground,
                    reft.Graphics.Background
                );
            }

            for (int y = info.DrawEnd; y < RenderSurface.Height; y++)
            {
                float currentDist = RenderSurface.Height / (2f * y - RenderSurface.Height);
                if (currentDist > info.PerpWallDist) continue;

                float weight = currentDist / info.PerpWallDist;
                Vector2 currentFloor = new Vector2();
                currentFloor.X = weight * floorWall.X + (1.0f - weight) * info.StartPos.X;
                currentFloor.Y = weight * floorWall.Y + (1.0f - weight) * info.StartPos.Y;
                Point curFloorPos = currentFloor.ToPoint();

                OpalTile floorTile = curFloorPos == oldFloorPos ? lastTile : Target.TileAt(curFloorPos);
                // Don't waste cycles
                if (floorTile?.Properties.IsBlock ?? true) continue;
                Font spritesheet = floorTile.Spritesheet;

                Point floorTex;
                // Only interested in the decimal part of currentFloor
                floorTex.X = (int)((currentFloor.X - (int)currentFloor.X) * spritesheet.Size.X);
                floorTex.Y = (int)((currentFloor.Y - (int)currentFloor.Y) * spritesheet.Size.Y);

                if (lastTile?.Name != floorTile.Name)
                    floorPixels = FontTextureCache.GetRecoloredPixels(
                        spritesheet,
                        (byte)floorTile.Graphics.Glyph,
                        floorTile.Graphics.Foreground,
                        floorTile.Graphics.Background
                    );

                Color floorColor = floorPixels[floorTex.X, floorTex.Y];
                if (DrawTerrainGrid && (floorTex.X < 1 || floorTex.Y < 1 || floorTex.X > spritesheet.Size.X - 2 || floorTex.Y > spritesheet.Size.Y - 2))
                {
                    floorColor = Color.Green;
                }
                floorColor = Target.Lighting.Shade(floorColor, curFloorPos);

                if (DrawAmbientShading) floorColor = Color.Lerp(
                    floorColor,
                    Target.SkyColor,
                    (float)Math.Pow((RenderSurface.Height / 2f) / y, ViewDistance / 3)
                );

                SetBackbufferAt(info.Column, y, floorColor);

                // Ceiling
                if(ceilingPixels == null)
                {
                    SetBackbufferAt(info.Column, RenderSurface.Height - y, Target.SkyColor);
                }
                else
                {
                    Color ceilingColor = ceilingPixels[floorTex.X, spritesheet.Size.Y - 1 - floorTex.Y];
                    ceilingColor = Target.Lighting.Shade(ceilingColor, curFloorPos);
                    if (DrawAmbientShading) ceilingColor = Color.Lerp(
                        ceilingColor,
                        Target.SkyColor,
                        (float)Math.Pow((RenderSurface.Height / 2f) / y, ViewDistance / 3)
                     );

                    SetBackbufferAt(info.Column, RenderSurface.Height - y, ceilingColor);
                }
                lastTile = floorTile;
                oldFloorPos = curFloorPos;
            }
        }

        private void DrawActorLabel(float[] zbuffer, Vector2 startPos, IOpalGameActor actor)
        {
            if (!PrintLabels || String.IsNullOrEmpty(actor.Name)) return;

            Color[,] labelPixels = 
                FontTextureCache.MakeLabel(
                    Nexus.Fonts.MainFont,
                    actor.Name,
                    actor.FirstPersonGraphics.Foreground,
                    Color.Transparent
                );

            Vector2 spritePosition = (actor.LocalPosition.ToVector2() - startPos + new Vector2(.5f));
            Vector2 spriteProjection = new Vector2();

            // Required for correct matrix multiplication
            float invDet = 1.0f / (PlaneVector.X * DirectionVector.Y - DirectionVector.X * PlaneVector.Y);
            spriteProjection.X = invDet * (spritePosition.X * DirectionVector.Y - spritePosition.Y * DirectionVector.X);
            spriteProjection.Y = invDet * (-spritePosition.X * PlaneVector.Y + spritePosition.Y * PlaneVector.X);

            if (spriteProjection.Y == 0) return;

            float distance_scaled = (float)actor.LocalPosition.Dist(startPos) / ViewDistance;
            Font spritesheet = Nexus.Fonts.MainFont;

            int spriteScreenX = (int)((RenderSurface.Width / 2) * (1 + spriteProjection.X / spriteProjection.Y));
            int spriteHeight = (int)(Math.Abs((int)(RenderSurface.Height / spriteProjection.Y)) / 3 /*FP_SCALE*/);
            if (spriteHeight < 6) return;
            if (spriteHeight > spritesheet.Size.Y * 2) spriteHeight = spritesheet.Size.Y * 2;

            int vMoveScreen = (int)(actor.FirstPersonVerticalOffset * spritesheet.Size.Y / spriteProjection.Y) - spriteHeight / 2;

            //calculate width of the sprite
            int spriteWidth;
            if (spriteHeight == spritesheet.Size.Y * 2) spriteWidth = Math.Min(spritesheet.Size.X * 2 * actor.Name.Length, spritesheet.Size.X * 2 * 8);
            else spriteWidth = (int)(Math.Abs((int)(RenderSurface.Height / spriteProjection.Y)) / 6 /*2*FP_SCALE*/) * actor.Name.Length;

            //calculate lowest and highest pixel to fill in current stripe
            Point drawStart = new Point(), drawEnd = new Point();
            drawStart.X = Math.Max(-spriteWidth / 2 + spriteScreenX, 0);
            drawStart.Y = Math.Max(-spriteHeight / 2 + RenderSurface.Height / 2 + vMoveScreen, 0);
            drawEnd.X = Math.Min(spriteWidth / 2 + spriteScreenX, RenderSurface.Width - 1);
            drawEnd.Y = Math.Min(spriteHeight / 2 + RenderSurface.Height / 2 + vMoveScreen, RenderSurface.Height - 1);

            if (drawStart.X >= drawEnd.X || spriteHeight == 0)
                return;

            //loop through every vertical stripe of the sprite on screen
            for (int stripe = drawStart.X; stripe < drawEnd.X; stripe++)
            {
                int texX = (256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * spritesheet.Size.X * actor.Name.Length /*MULT*/ / spriteWidth) / 256;
                //the conditions in the if are:
                //1) it's in front of camera plane so you don't see things behind you
                //2) it's on the screen (left)
                //3) it's on the screen (right)
                //4) ZBuffer, with perpendicular distance
                int lineHeight = (int)(RenderSurface.Height / zbuffer[stripe]);
                int drawStartStripe = Math.Max(-lineHeight / 2 + RenderSurface.Height / 2, 0);

                if (spriteProjection.Y > 0 && stripe > 0 && stripe < RenderSurface.Width && (spriteProjection.Y < zbuffer[stripe] || drawStart.Y < drawStartStripe))
                {
                    // For every pixel of this stripe
                    for (int y = drawStart.Y; y < drawEnd.Y; y++)
                    {
                        if (spriteProjection.Y >= zbuffer[stripe] && y >= drawStartStripe) break;

                        int d = (y - vMoveScreen) * 256 - RenderSurface.Height * 128 + spriteHeight * 128;
                        int texY = Math.Max(((d * spritesheet.Size.Y) / spriteHeight) / 256, 0);

                        Color spriteColor = labelPixels[texX, texY];
                        if(spriteColor == Color.Transparent)
                        {
                            spriteColor = Color.Lerp(Color.Black, GetBackbufferAt(stripe, y), distance_scaled * 10);
                        }


                        // Shade to sky color with distance from player
                        if (DrawAmbientShading) spriteColor = Color.Lerp(spriteColor, Target.SkyColor, distance_scaled);
                        SetBackbufferAt(stripe, y, spriteColor);
                    }
                }
            }
        }

        private void DrawActorSpriteVLines(float[] zbuffer, Vector2 startPos)
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

                int spriteScreenX = (int)((RenderSurface.Width / 2) * (1 + spriteProjection.X / spriteProjection.Y));
                int spriteHeight = (int)(Math.Abs((int)(RenderSurface.Height / spriteProjection.Y)) / actor.FirstPersonScale.Y);
                int vMoveScreen = (int)(actor.FirstPersonVerticalOffset * spriteHeight / spriteProjection.Y) + spriteHeight / 2;

                //calculate width of the sprite
                int spriteWidth = (int)(Math.Abs((int)(RenderSurface.Height / (spriteProjection.Y))) / actor.FirstPersonScale.X);

                //calculate lowest and highest pixel to fill in current stripe
                Point drawStart = new Point(), drawEnd = new Point();
                drawStart.X = Math.Max(-spriteWidth / 2 + spriteScreenX, 0);
                drawStart.Y = Math.Max(-spriteHeight / 2 + RenderSurface.Height / 2 + vMoveScreen, 0);
                drawEnd.X = Math.Min(spriteWidth / 2 + spriteScreenX, RenderSurface.Width - 1);
                drawEnd.Y = Math.Min(spriteHeight / 2 + RenderSurface.Height / 2 + vMoveScreen, RenderSurface.Height - 1);

                if (drawStart.X >= drawEnd.X || spriteHeight == 0)
                    continue;

                Color[,] spritePixels = null;
                //loop through every vertical stripe of the sprite on screen
                for (int stripe = drawStart.X; stripe < drawEnd.X; stripe++)
                {
                    int texX = (256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * spritesheet.Size.X / spriteWidth) / 256;
                    //the conditions in the if are:
                    //1) it's in front of camera plane so you don't see things behind you
                    //2) it's on the screen (left)
                    //3) it's on the screen (right)
                    //4) ZBuffer, with perpendicular distance
                    int lineHeight = (int)(RenderSurface.Height / zbuffer[stripe]);
                    int drawStartStripe = Math.Max(-lineHeight / 2 + RenderSurface.Height / 2, 0);

                    if (spriteProjection.Y > 0 && stripe > 0 && stripe < RenderSurface.Width && (spriteProjection.Y < zbuffer[stripe] || drawStart.Y < drawStartStripe))
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

                            int d = (y - vMoveScreen) * 256 - RenderSurface.Height * 128 + spriteHeight * 128;
                            int texY = Math.Max(((d * spritesheet.Size.Y) / spriteHeight) / 256, 0);

                            Color spriteColor = spritePixels[texX, texY];
                            if (DrawActorBoundaryBoxes && (stripe == drawStart.X || y == drawStart.Y || stripe == drawEnd.X - 1 || y == drawEnd.Y - 1))
                            {
                                spriteColor = Color.Red;
                            }
                            if (spriteColor == Color.Transparent) continue;

                            // Shade to sky color with distance from player
                            spriteColor = Target.Lighting.Shade(spriteColor, actor.LocalPosition);
                            if (DrawAmbientShading) spriteColor = Color.Lerp(spriteColor, Target.SkyColor, distance_scaled);
                            SetBackbufferAt(stripe, y, spriteColor);
                        }
                    }
                }
                DrawActorLabel(zbuffer, startPos, actor);
            }
        }

        public override void Print(SadConsole.Console surf, Rectangle viewArea, TileMemory fog = null)
        {
            // Don't waste precious cycles
            if (!Dirty) return;

            if ((RenderSurface?.Width != surf.Width) || RenderSurface.Height != surf.Height)
            {
                RenderSurface = new Texture2D(Global.GraphicsDevice, surf.Width, surf.Height);
            }
            
            // Fill with SkyColor to cover up any off-by-one errors
            // Also to make the wall drawing code lighter
            FillBackbuffer(Target.SkyColor);

            // Make sure the lighting system is up to date
            Target.Lighting.Update();
            fog.UnseeEverything();

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
                var rayInfo = Raycaster.CastRay(Target, startPos, rayDir);
                float perpWallDist = zbuffer[x] = rayInfo.ProjectedDistance;
                // Update TileMemory
                rayInfo.PointsTraversed.ForEach(v => {
                    var p = v.ToPoint();
                    fog.Learn(p);
                    fog.See(p);
                });

                // Calculate height of line to draw on screen
                int lineHeight = (int)(surf.Height / perpWallDist);

                // Calculate lowest and highest pixel to fill in current stripe
                int drawStart = (int)(Math.Max(-lineHeight / 2f + surf.Height / 2f, 0));
                int drawEnd = (int)(Math.Min(lineHeight / 2f + surf.Height / 2f, surf.Height - 1));

                // Calculate value of wallX (where exactly the wall was hit)
                float wallX = rayInfo.SideHit
                    ? startPos.X + perpWallDist * rayDir.X
                    : startPos.Y + perpWallDist * rayDir.Y;
                wallX -= (float)Math.Floor(wallX);

                VLineInfo rcpInfo = new VLineInfo
                {
                    PerpWallDist = perpWallDist,
                    PerpendicularWallX = wallX,
                    SideHit = rayInfo.SideHit,
                    Column = x,
                    DrawStart = drawStart,
                    DrawEnd = drawEnd,
                    LineHeight = lineHeight,
                    RayDir = rayDir,
                    RayPos = rayInfo.LastPointTraversed.ToPoint(),
                    StartPos = startPos,
                };

                DrawFloorAndSkyVLine(rcpInfo);
                DrawWallVLine(rcpInfo);
            }

            DrawActorSpriteVLines(
                zbuffer,
                startPos
            );

            UpdateRenderSurface();
            Dirty = false;
        }
    }
}
