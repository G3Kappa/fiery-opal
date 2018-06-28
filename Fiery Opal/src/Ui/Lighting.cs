using FieryOpal.Src;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Ui
{
    public enum LightEmitterType
    {
        Ambient,// Illuminates the entire map, regardless of the emitter's location.
        Conical,  // Illuminates a section of a circle <) radiating from the emitter.
        Point   // Illuminates only the tile on which the emitter resides.
    }

    public interface ILightEmitter : IOpalGameActor
    {
        LightEmitterType LightEmitterType { get; }
        float LightIntensity { get; }
        float LightRadius { get; }
        float LightSmoothness { get; }
        Color LightColor { get; }

        Vector2 LightDirection { get; }
        int LightAngleWidth { get; }
    }

    public class LightLayer
    {
        public ILightEmitter Source { get; }
        public float[,] Grid { get; }

        public LightingManager Manager;

        protected Point LastPos = new Point(-1337, -420);
        public bool IsDirty => LastPos != Source.LocalPosition;

        public LightLayer(ILightEmitter source, LightingManager parent)
        {
            Source = source;
            Manager = parent;
            Grid = new float[parent.Parent.Width, parent.Parent.Height];
        }

        private float CalcIntensity(Vector2 start, Vector2 v, ILightEmitter emit)
        {
            if (start == v) return emit.LightIntensity;
            return 
                (float)Math.Pow(emit.LightIntensity 
                * (1f / Math.Pow(start.Dist(v) / emit.LightRadius, 2)), 1f/emit.LightSmoothness);
        }

        private void RecalcAmbient()
        {
            for (int x = 0; x < Manager.Parent.Width; x++)
            {
                for (int y = 0; y < Manager.Parent.Height; y++)
                {
                    Grid[x, y] = Source.LightIntensity;
                }
            }
        }

        private void RecalcConical()
        {
            Vector2 rayStart = LastPos.ToVector2() + new Vector2(.5f);

            int alfa = (int)(Math.Atan2(Source.LightDirection.Y, Source.LightDirection.X) * (180f / Math.PI));
            for (float deg = alfa - Source.LightAngleWidth / 2; deg < alfa + Source.LightAngleWidth / 2; deg ++)
            {
                Vector2 rayPos = rayStart;

                double theta = deg * (Math.PI / 180f);
                Vector2 rayDir = new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));

                var rayInfo = Raycaster.CastRay(Manager.Parent, rayStart, rayDir);
                rayInfo.PointsTraversed.ForEach(v =>
                {
                    var p = v.ToPoint();
                    Grid[p.X, p.Y] = CalcIntensity(rayStart, v.ToPoint().ToVector2() + new Vector2(.5f), Source);
                });
            }
        }

        private void RecalcPoint()
        {
            Grid[LastPos.X, LastPos.Y] += Source.LightIntensity;
        }

        public bool Recalc()
        {
            if (!IsDirty) return false;
            LastPos = Source.LocalPosition;
            switch(Source.LightEmitterType)
            {
                case LightEmitterType.Ambient:
                    RecalcAmbient();
                    break;
                case LightEmitterType.Conical:
                    RecalcConical();
                    break;
                case LightEmitterType.Point:
                    RecalcPoint();
                    break;
                default:
                    return false;
            }
            return true;
        }
    }

    public class LightingManager
    {
        public OpalLocalMap Parent { get; }

        protected Dictionary<Guid, LightLayer> Layers { get; }
        protected Color[,] ColorGrid;

        public bool Enabled { get; private set; } = true;
        public void ToggleEnabled(bool? state=null)
        {
            Enabled = state ?? !Enabled;
        }

        public LightingManager(OpalLocalMap parentMap)
        {
            Parent = parentMap;
            Layers = new Dictionary<Guid, LightLayer>();
            ColorGrid = new Color[Parent.Width, Parent.Height];

            parentMap.ActorSpawned += (map, actor) => {
                if (!typeof(ILightEmitter).IsAssignableFrom(actor.GetType())) return;
                Layers[actor.Handle] = new LightLayer(actor as ILightEmitter, this);
            };

            parentMap.ActorDespawned += (map, actor) => {
                if (!typeof(ILightEmitter).IsAssignableFrom(actor.GetType())) return;
                Layers.Remove(actor.Handle);
            };
        }

        public void Update()
        {
            if (!Enabled || Layers.Count == 0) return;

            for (int x = 0; x < Parent.Width; x++)
            {
                for (int y = 0; y < Parent.Height; y++)
                {
                    ColorGrid[x, y] = Color.Black;
                }
            }

            Layers.Values.ForEach(l =>
            {
                bool wasDirty = l.Recalc();
            });

            Layers.Values.Merge(ref ColorGrid);
        }

        public Color Shade(Color c, Point pos)
        {
            return !Enabled ? c : c.BlendLight(ColorGrid[pos.X, pos.Y], 1f);
        }
    }

    public static class LightLayerExtensions
    {
        private static float Lerp(float a, float b, float i)
        {
            return (1 - i) * a + i * b;
        }

        private static float SoftLightBlend(float a, float b, float i)
        {
            return Lerp(a, a * b, i);
        }

        public static Color BlendLight(this Color c, Color toBlend, float intensity)
        {
            return new Color(
                SoftLightBlend(c.R / 255f, toBlend.R / 255f, intensity),
                SoftLightBlend(c.G / 255f, toBlend.G / 255f, intensity),
                SoftLightBlend(c.B / 255f, toBlend.B / 255f, intensity)
            );
        }

        public static Color BlendAdditive(this Color c, Color b)
        {
            return new Color(
                Math.Min(1f, c.R / 255f + b.R / 255f),
                Math.Min(1f, c.G / 255f + b.G / 255f),
                Math.Min(1f, c.B / 255f + b.B / 255f)
            );
        }

        public static void Merge(this IEnumerable<LightLayer> layers, ref Color[,] grid)
        {
            var list = layers.ToList();
            if (list.Count == 0) return;

            int w = list[0].Grid.GetLength(0);
            int h = list[0].Grid.GetLength(1);

            foreach(var l in list)
            {
                Color c = l.Source.LightColor;
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        grid[x, y] = Color.Lerp(grid[x, y], grid[x, y].BlendAdditive(c), l.Grid[x, y]);
                    }
                }
            }
        }
    }
}
