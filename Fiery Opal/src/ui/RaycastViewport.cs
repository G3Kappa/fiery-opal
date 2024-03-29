﻿using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Environment;
using FieryOpal.Src.Procedural.Terrain.Tiles;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
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
            public Point PreviousRayPos;
            public Vector2 StartPos;
        }

        public bool Dirty { get; private set; }
        public bool DrawExtraLabels { get; private set; }
        public float ViewDistance { get; set; }
        public bool DrawTerrainGrid { get; private set; }
        public bool DrawActorBoundaryBoxes { get; private set; }
        public bool DrawAmbientShading { get; private set; }
        public TurnTakingActor Following { get; set; }
        public Texture2D RenderSurface { get; private set; }
        public Texture2D ProjectionTexture { get; private set; }

        private Color[] Backbuffer, Projection;
        private float AspectRatio = 0.5f;

        public Vector2 DirectionVector
        {
            get
            {
                var look = Following.LookingAt;
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
            DrawExtraLabels = Toggle(DrawExtraLabels, state);
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

        private void FillBackbuffer(Color? c = null)
        {
            Projection = Color.TransparentBlack.ToArray(RenderSurface.Width * RenderSurface.Height);
            if (c.HasValue)
            {
                Backbuffer = c.Value.ToArray(RenderSurface.Width * RenderSurface.Height);
            }
            else
            {
                for (int y = 0; y < RenderSurface.Height; y++)
                {
                    Color skyColor = Nexus.DayNightCycle.GetSkyColor(1 - y / (float)RenderSurface.Height);
                    for (int x = 0; x < RenderSurface.Width; x++)
                    {
                        SetBackbufferAt(x, y, skyColor);
                    }
                }
            }
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

        private void SetProjectionAt(int x, int y, Point p, float z, float a=1f)
        {
            if (Util.OOB(x, y, ProjectionTexture.Width, ProjectionTexture.Height)) return;
            int i = y * ProjectionTexture.Width + x;
            Projection[i].R = (byte)(((p.X + .5f) / Target.Width) * 255);
            Projection[i].G = (byte)(((p.Y + .5f) / Target.Height) * 255);
            Projection[i].B = (byte)(z * 255);
            Projection[i].A = (byte)(a * 255);
        }

        private Color GetProjectionAt(int x, int y)
        {
            if (Util.OOB(x, y, ProjectionTexture.Width, ProjectionTexture.Height)) return Color.Magenta;
            return Projection[y * ProjectionTexture.Width + x];
        }

        private void UpdateRenderSurface()
        {
            RenderSurface.SetData(Backbuffer);
            ProjectionTexture.SetData(Projection);
        }

        private Font GetFontByDist(float perpDist, ICustomSpritesheet thing)
        {
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
                if (decos.Count <= 0)
                {
                    return;
                }
                else
                {
                    // And if we do let's draw that as if it were a wall
                    wallPixels = FontTextureCache.GetRecoloredPixels(
                        (spritesheet = GetFontByDist(info.PerpWallDist, decos[0])),
                        (byte)decos[0].FirstPersonGraphics.Glyph,
                        decos[0].FirstPersonGraphics.Foreground,
                        decos[0].FirstPersonGraphics.Background
                    );
                }
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
                SetBackbufferAt(info.Column, y, wallColor);

                bool roofed = Target.TileAt(info.PreviousRayPos)?.Properties.HasCeiling ?? false;
                SetProjectionAt(info.Column, y, info.PreviousRayPos, roofed ? 1 : 0);
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

            // Arrays that contain texture data for the floor and ceiling tiles
            Color[,] floorPixels = null;
            Color[,] ceilingPixels = null;
            // Since ceilings don't change, at least not presently, we can load
            // the texture once and just store it.
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

            // The floor tile changes, however, and some optimizations are made
            // here to reduce the number of array accesses both related to the
            // tiles themselves and their textures.
            OpalTile lastTile = null;
            Point oldFloorPos = new Point(-1, -1);
            // Shading a color is also expensive, and since floors tend to not
            // have too many colors, it's good to cache the last used one.
            Color lastFloorColor = Color.Black, lastShadedFloorColor = Color.Black;
            bool actorsOnTile = false;
            // Draws a circular shadow below each actor, on the floor they're occupying.
            Color[,] shadowPixels = FontTextureCache.MakeShadow(Nexus.Fonts.Spritesheets["Terrain"], 7, new Color(150, 150, 150));
            for (int y = info.DrawEnd; y < RenderSurface.Height; y++)
            {
                float currentDist = RenderSurface.Height / (2f * y - RenderSurface.Height);
                float weight = currentDist / info.PerpWallDist;

                if (weight > 1) continue;

                Vector2 currentFloor = new Vector2(
                    weight * floorWall.X + (1.0f - weight) * info.StartPos.X,
                    weight * floorWall.Y + (1.0f - weight) * info.StartPos.Y
                );
                Point curFloorPos = currentFloor.ToPoint();
                // If we're still iterating the previous tile no need to re-fetch it.
                OpalTile floorTile = lastTile;
                if(oldFloorPos != curFloorPos)
                {
                    floorTile = Target.TileAt(curFloorPos);
                    actorsOnTile = Target.ActorsAt(curFloorPos).Any(a => a.DrawShadow && a.Visible);
                }

                if (floorTile == null) continue;

                Font spritesheet = floorTile.Spritesheet;

                // Only interested in the decimal part of currentFloor as far
                // as the texture is of concern.
                Point floorTex = new Point(
                     (int)((currentFloor.X - (int)currentFloor.X) * spritesheet.Size.X),
                     (int)((currentFloor.Y - (int)currentFloor.Y) * spritesheet.Size.Y)
                );

                // If we stepped over a different floor, we need to load its
                // texture into floorPixels.
                if (lastTile?.Name != floorTile.Name)
                {
                    floorPixels = FontTextureCache.GetRecoloredPixels(
                        spritesheet,
                        (byte)floorTile.Graphics.Glyph,
                        floorTile.Graphics.Foreground,
                        floorTile.Graphics.Background
                    );

                    if(floorTile.Properties.HasCeiling)
                    {
                        ceilingPixels = FontTextureCache.GetRecoloredPixels(
                            spritesheet,
                            (byte)floorTile.Properties.CeilingGraphics.Glyph,
                            floorTile.Properties.CeilingGraphics.Foreground,
                            floorTile.Properties.CeilingGraphics.Background
                        );
                    }
                }

                // The base color before any post processing
                Color floorColor = floorPixels[floorTex.X, floorTex.Y];
                Color ceilingColor = Color.Lerp(ceilingPixels?[floorTex.X, spritesheet.Size.Y - 1 - floorTex.Y] ?? Color.Magenta, Color.Black, .25f);

                // If on, draws a grid around each floor tile
                if (DrawTerrainGrid && (floorTex.X < 1 || floorTex.Y < 1 || floorTex.X > spritesheet.Size.X - 2 || floorTex.Y > spritesheet.Size.Y - 2))
                {
                    floorColor = Color.Magenta;
                    ceilingColor = Color.LawnGreen;
                }

                // If the tile is not empty draw a shadow
                if(actorsOnTile && shadowPixels[floorTex.X, floorTex.Y] != Color.Transparent)
                {
                    floorColor = floorColor.BlendLight(shadowPixels[floorTex.X, floorTex.Y], .75f);
                }

                // Finally, set both floor and ceiling pixels.
                SetBackbufferAt(info.Column, y, floorColor);
                SetProjectionAt(info.Column, y, curFloorPos, 0);

                if (Target.CeilingTile != null)
                {
                    SetBackbufferAt(info.Column, RenderSurface.Height - y, ceilingColor);
                    SetProjectionAt(info.Column, RenderSurface.Height - y, curFloorPos, 0);
                }
                else if(floorTile.Properties.HasCeiling)
                {
                    SetBackbufferAt(info.Column, RenderSurface.Height - y, ceilingColor);
                    SetProjectionAt(info.Column, RenderSurface.Height - y, curFloorPos, 1);
                    SetProjectionAt(info.Column, y, curFloorPos, 1);
                }

                lastTile = floorTile;
                oldFloorPos = curFloorPos;
            }
        }

        public void DrawBillboardSprite(Func<Color[,]> getPixels, Vector2 observerPosition, Point billboardPosition, Vector2 scale, Point textureSize, float vOffset, ref float[] zbuffer, bool ignoreShaders=false)
        {
            float invDet = 1.0f / (PlaneVector.X * DirectionVector.Y - DirectionVector.X * PlaneVector.Y);
            Vector2 spritePosition = (billboardPosition.ToVector2() - observerPosition + new Vector2(.5f));
            Vector2 spriteProjection = new Vector2(
                invDet * (spritePosition.X * DirectionVector.Y - spritePosition.Y * DirectionVector.X),
                invDet * (-spritePosition.X * PlaneVector.Y + spritePosition.Y * PlaneVector.X)
            );
            if (spriteProjection.Y <= 0) return;

            float distance_scaled = (float)billboardPosition.FastDist(observerPosition.ToPoint()) / ViewDistance;

            int spriteScreenX = (int)((RenderSurface.Width / 2) * (1 + spriteProjection.X / spriteProjection.Y));
            int spriteHeight = (int)(Math.Abs((int)(RenderSurface.Height / spriteProjection.Y)) / scale.Y);
            int vMoveScreen = (int)(textureSize.Y * vOffset / spriteProjection.Y);

            //calculate width of the sprite
            int spriteWidth = (int)(Math.Abs((int)(RenderSurface.Height / (spriteProjection.Y))) / scale.X);

            //calculate lowest and highest pixel to fill in current stripe
            Point drawStart = new Point(), drawEnd = new Point();
            drawStart.X = Math.Max(-spriteWidth / 2 + spriteScreenX, 0);
            drawStart.Y = Math.Max(-spriteHeight / 2 + RenderSurface.Height / 2 + vMoveScreen, 0);
            drawEnd.X = Math.Min(spriteWidth / 2 + spriteScreenX, RenderSurface.Width - 1);
            drawEnd.Y = Math.Min(spriteHeight / 2 + RenderSurface.Height / 2 + vMoveScreen, RenderSurface.Height - 1);

            if (drawStart.X >= drawEnd.X || spriteHeight == 0)
                return;

            Color[,] spritePixels = null;

            //loop through every vertical stripe of the sprite on screen
            for (int stripe = drawStart.X; stripe < drawEnd.X; stripe++)
            {
                int texX = (256 * (stripe - (-spriteWidth / 2 + spriteScreenX)) * textureSize.X / spriteWidth) / 256;

                int lineHeight = (int)(RenderSurface.Height / zbuffer[stripe]);
                int drawStartStripe = Math.Max(-lineHeight / 2 + RenderSurface.Height / 2, 0);

                if (spriteProjection.Y < zbuffer[stripe] || drawStart.Y < drawStartStripe)
                {
                    // For every pixel of this stripe
                    for (int y = drawStart.Y; y < drawEnd.Y; y++)
                    {
                        if (spriteProjection.Y >= zbuffer[stripe] && y >= drawStartStripe) break;

                        // Wait for as long as possible to load the pixels to save some cache hits
                        if (spritePixels == null)
                        {
                            spritePixels = getPixels();
                        }

                        int d = (y - vMoveScreen) * 256 - RenderSurface.Height * 128 + spriteHeight * 128;
                        int texY = Math.Max(((d * textureSize.Y) / spriteHeight) / 256, 0);

                        Color spriteColor = spritePixels[texX, texY];

                        // If on, draws a red outline around the projected sprite
                        if (DrawActorBoundaryBoxes && (stripe == drawStart.X || y == drawStart.Y || stripe == drawEnd.X - 1 || y == drawEnd.Y - 1))
                        {
                            spriteColor = Color.Red;
                        }
                        else
                        {
                            // Skip background pixels
                            if (spriteColor.A == 0) continue;

                            // Apply translucency
                            else if(spriteColor.A < 255)
                            {
                                spriteColor = Color.Lerp(GetBackbufferAt(stripe, y), spriteColor, spriteColor.A / 255f);
                            }
                        }

                        bool roofed = Target.TileAt(billboardPosition).Properties.HasCeiling;
                        Color proj = GetProjectionAt(stripe, y);
                        Point projXY = new Point((int)(proj.R / 256f * Target.Width), (int)(proj.G / 256f * Target.Height));
                        Point o = observerPosition.ToPoint();
                        if (
                            !roofed 
                            && proj.B > 0 
                            && (
                                (Target.TileAt(observerPosition.ToPoint())?.Properties.HasCeiling ?? false) 
                                || (o.SquaredEuclidianDistance(billboardPosition) > o.SquaredEuclidianDistance(projXY))
                                )
                         ) continue; // Don't render actors outside of indoors areas if there's a roof

                        SetBackbufferAt(stripe, y, spriteColor);
                        SetProjectionAt(stripe, y, billboardPosition, roofed ? 1 : 0);
                    }
                }
            }
        }

        private void DrawActorLabel(ref float[] zbuffer, Vector2 startPos, IOpalGameActor actor)
        {
            if (String.IsNullOrWhiteSpace(actor.Name)) return;

            DrawBillboardSprite(() =>
            {
                return
                FontTextureCache.MakeLabel(
                    Nexus.Fonts.MainFont,
                    actor.Name,
                    actor.FirstPersonGraphics.Foreground,
                    new Color(0, 0, 0, .25f)
                );
            },
            startPos,
            actor.LocalPosition,
            new Vector2(10f / actor.Name.Length, Nexus.Fonts.MainFont.Size.Y),
            new Point(Nexus.Fonts.MainFont.Size.X * actor.Name.Length, Nexus.Fonts.MainFont.Size.Y),
            -8f - actor.FirstPersonScale.Y * actor.Spritesheet.Size.Y,
            ref zbuffer,
            ignoreShaders: true);
        }

        private void DrawActorHealthbar(ref float[] zbuffer, Vector2 startPos, IOpalGameActor actor)
        {
            if (!typeof(TurnTakingActor).IsAssignableFrom(actor.GetType())) return;

            var tta = actor as TurnTakingActor;
            if (tta.Health >= tta.MaxHealth) return;

            DrawBillboardSprite(() =>
            {
                return
                FontTextureCache.MakeHealthBar(
                    Nexus.Fonts.MainFont,
                    tta.Health / (float)tta.MaxHealth
                );
            },
            startPos,
            actor.LocalPosition,
            new Vector2(2f, Nexus.Fonts.MainFont.Size.Y),
            new Point(Nexus.Fonts.MainFont.Size.X * 20, Nexus.Fonts.MainFont.Size.Y),
            -actor.FirstPersonScale.Y * actor.Spritesheet.Size.Y,
            ref zbuffer,
            ignoreShaders: true);
        }

        private void DrawInteractionMarker(ref float[] zbuffer, Vector2 startPos, IOpalGameActor actor)
        {
            if (actor is IInteractive)
            {
                DrawBillboardSprite(() =>
                {
                    return FontTextureCache.MakeInteractionMarker(
                            Nexus.Fonts.MainFont,
                            25,
                            Palette.Ui["FP_InteractionMarker"]
                    );
                },
                startPos,
                actor.LocalPosition,
                new Vector2(Nexus.Fonts.MainFont.Size.X, Nexus.Fonts.MainFont.Size.Y),
                Nexus.Fonts.MainFont.Size,
                -16f - actor.FirstPersonScale.Y * actor.Spritesheet.Size.Y,
                ref zbuffer,
                ignoreShaders: true);
            }
        }

        private void DrawActorSprites(float[] zbuffer, Vector2 startPos)
        {
            List<IOpalGameActor> actors_within_viewarea = Target.ActorsWithinRing((int)startPos.X, (int)startPos.Y, (int)ViewDistance, 0, true)
                .Where(a =>
                       // Must be visible, duh
                       a.Visible
                       // Must not be a decoration with the DisplayAsBlock property
                       && !(a is DecorationBase && (a as DecorationBase).DisplayAsBlock)
                       // Must derive from OpalActorBase
                       && a is OpalActorBase
                       // Must not be the actor we're following or on the same tile
                       && a.LocalPosition != startPos.ToPoint()
                       && Following.Brain.TileMemory.CanSee(a.LocalPosition)
                       // And must be in a position visible to the actor we're following
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
                Font spritesheet = actor.Spritesheet;
                DrawBillboardSprite(() =>
                {
                    return FontTextureCache.GetRecoloredPixels(
                        spritesheet,
                        (byte)actor.FirstPersonGraphics.Glyph,
                        actor.FirstPersonGraphics.Foreground,
                        Color.Transparent
                    );
                }, 
                startPos,
                actor.LocalPosition,
                actor.FirstPersonScale,
                spritesheet.Size,
                actor.FirstPersonVerticalOffset,
                ref zbuffer);

                DrawActorHealthbar(ref zbuffer, startPos, actor);
                if (DrawExtraLabels)
                {
                    DrawActorLabel(ref zbuffer, startPos, actor);
                    DrawInteractionMarker(ref zbuffer, startPos, actor);
                }
            }
        }

        public override void Print(SadConsole.Console surf, Rectangle viewArea, TileMemory fog = null)
        {
            // Don't waste precious cycles
            if (!Dirty) return;

            if ((RenderSurface?.Width != surf.Width) || RenderSurface.Height != surf.Height)
            {
                RenderSurface = new Texture2D(Global.GraphicsDevice, surf.Width, surf.Height);
                ProjectionTexture = new Texture2D(Global.GraphicsDevice, surf.Width, surf.Height);
                FillBackbuffer(Color.Black);
            }

            ViewDistance = Nexus.Player.ViewDistance;

            // Fill with SkyColor to blend sky with ground
            // Also to make the wall drawing code lighter
            FillBackbuffer(Target.Indoors ? new Color?(Color.Black) : null);

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
                var rayInfo = Raycaster.CastRay(new Point(Target.Width, Target.Height), startPos, rayDir, (p) => Target.TileAt(p)?.Properties.IsBlock ?? false, (int)ViewDistance);
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
                    PreviousRayPos = rayInfo.PointsTraversed.Count > 1 ? rayInfo.PointsTraversed.ElementAt(rayInfo.PointsTraversed.Count - 2).ToPoint() : rayInfo.LastPointTraversed.ToPoint(),
                    StartPos = startPos,
                };

                DrawFloorAndSkyVLine(rcpInfo);
                DrawWallVLine(rcpInfo);
            }

            DrawActorSprites(
                zbuffer,
                startPos
            );

            UpdateRenderSurface();
            Dirty = false;
        }
    }
}
