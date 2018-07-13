using FieryOpal.Src;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Ui
{
    public enum LightEmitterType
    {
        Conical,  // Illuminates a section of a circle <) radiating from the emitter.
        Point   // Illuminates only the tile on which the emitter resides.
    }

    public interface ILightEmitter : IOpalGameActor
    {
        LightEmitterType LightEmitterType { get; }
        float LightIntensity { get; }
        int LightRadius { get; }
        Color LightColor { get; }

        Vector2 LightDirection { get; }
        int LightAngleWidth { get; }

        event Action<ILightEmitter> LightEmitterDataChanged;
        void ForceUpdateLightEmitter();
    }

    public class LightLayer
    {
        public ILightEmitter Source { get; }
        public float[,] Grid { get; private set; }

        public LightingManager Manager;

        protected Point LastPos = new Point();
        public bool IsDirty { get; private set; } = true;

        public LightLayer(ILightEmitter source, LightingManager parent)
        {
            Source = source;
            Manager = parent;

            switch(source.LightEmitterType)
            {
                case LightEmitterType.Conical:
                    Grid = new float[2 * source.LightRadius, 2 * source.LightRadius];
                    source.LightEmitterDataChanged += (s) =>
                    {
                        Grid = new float[2 * s.LightRadius, 2 * s.LightRadius];
                    };
                    break;
                case LightEmitterType.Point:
                    Grid = new float[1, 1];
                    break;
            }

            source.LightEmitterDataChanged += (s) =>
            {
                IsDirty = true;
            };
        }

        public float CalcIntensity(Point start, Point v, ILightEmitter emit)
        {
            if (start == v) return emit.LightIntensity;

            var dist = (float)start.Dist(v) + 1;
            if (dist > emit.LightRadius) dist += emit.LightRadius * (dist - emit.LightRadius);

            var intensity = emit.LightIntensity / (dist * dist);
            var retq = intensity.Quantize(32);
            return retq;
        }

        private void Fill(float val)
        {
            int w = Grid.GetLength(0);
            int h = Grid.GetLength(1);
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Grid[x, y] = val;
                }
            }
        }

        private void RecalcConical()
        {
            Vector2 rayStart = LastPos.ToVector2() + new Vector2(.5f);

            int alfa = (int)(Math.Atan2(Source.LightDirection.Y, Source.LightDirection.X) * (180f / Math.PI));
            for (float deg = alfa - Source.LightAngleWidth / 2 + 0.01f; deg < alfa + Source.LightAngleWidth / 2; deg+=3.6f)
            {
                Vector2 rayPos = rayStart;

                double theta = deg * (Math.PI / 180f);
                Vector2 rayDir = new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta));

                var rayInfo = Raycaster.CastRay(new Point(Manager.Parent.Width, Manager.Parent.Height), rayStart, rayDir, (p) => (Manager.Parent.TileAt(p)?.Properties.IsBlock ?? false));
                foreach(Vector2 v in rayInfo.PointsTraversed)
                {
                    var p = v.ToPoint() - (Source.LocalPosition - new Point(Source.LightRadius));
                    if (Util.OOB(p.X, p.Y, Grid.GetLength(0), Grid.GetLength(1))) break;
                    if (Manager.Parent.TileAt(v.ToPoint())?.Properties.IsBlock ?? true) break;

                    Grid[p.X, p.Y] = CalcIntensity(rayStart.ToPoint(), v.ToPoint(), Source);
                }
            }
        }

        private void RecalcPoint()
        {
            Grid[0, 0] = Source.LightIntensity;
        }

        public bool Recalc(bool force = false)
        {
            if (!force && !IsDirty) return false;
            Fill(0);
            LastPos = Source.LocalPosition;
            switch(Source.LightEmitterType)
            {
                case LightEmitterType.Conical:
                    RecalcConical();
                    break;
                case LightEmitterType.Point:
                    RecalcPoint();
                    break;
                default:
                    return false;
            }
            IsDirty = false;
            return true;
        }
    }

    public class LightingManager
    {
        public OpalLocalMap Parent { get; }

        protected Dictionary<Guid, LightLayer> Layers { get; }
        protected Color[] ColorGrid;
        public Texture2D LightMap { get; protected set; }

        public bool Enabled { get; private set; } = true;
        public void ToggleEnabled(bool? state=null)
        {
            Enabled = state ?? !Enabled;
        }

        public LightingManager(OpalLocalMap parentMap)
        {
            Parent = parentMap;
            Layers = new Dictionary<Guid, LightLayer>();
            ColorGrid = new Color[Parent.Width * Parent.Height];
            LightMap = new Texture2D(Global.GraphicsDevice, Parent.Width, Parent.Height);

            parentMap.ActorSpawned += (map, actor) => {
                if (!typeof(ILightEmitter).IsAssignableFrom(actor.GetType())) return;

                var emit = actor as ILightEmitter;
                Layers[actor.Handle] = new LightLayer(emit, this);
            };

            parentMap.ActorDespawned += (map, actor) => {
                if (!typeof(ILightEmitter).IsAssignableFrom(actor.GetType())) return;
                Layers.Remove(actor.Handle);
                Update(true);
            };

        }

        public void Update(bool force=false)
        {
            if (!Enabled) return;

            bool anyDirty = false;
            Layers.Values.ForEach(l =>
            {
                bool wasDirty = l.Recalc(force);
                if (wasDirty) anyDirty = true;
            });

            if(force || anyDirty)
            {
                Layers.Values.Merge(ref ColorGrid, new Point(Parent.Width, Parent.Height), Parent.AmbientLightIntensity);
                Util.LogText("Lighting.Update: Updated.", true);
            }
            LightMap.SetData(ColorGrid);
        }

        public Color ApplyShading(Color c, Point pos)
        {
            var t = Parent.TileAt(pos);
            var l = ColorGrid[pos.X + pos.Y * Parent.Width];
            var ai = (t?.Properties.IsBlock ?? true) || t.Properties.HasCeiling ? .5f : Parent.AmbientLightIntensity;
            var ambient = new Color(ai, ai, ai);
            return !Enabled ? c : c.BlendMultiply(Color.Lerp(ambient, l.BlendAdditive(ambient), l.A / 255f));
        }

        public Color GetLightingAt(Point pos)
        {
            return ColorGrid[pos.Y * Parent.Width + pos.X];
        }

        public float CalcFaceIntensity(Vector2 floorWall)
        {
            return
            Math.Min(
                Layers.Sum(l =>
                    {
                        if (Raycaster.IsLineObstructed(
                            new Point(Parent.Width, Parent.Height),
                            floorWall,
                            l.Value.Source.LocalPosition.ToVector2(),
                            (p) => Parent.TileAt(p)?.Properties.IsBlock ?? true)
                        )
                        {
                            return 0f;
                        }

                        return l.Value.CalcIntensity(l.Value.Source.LocalPosition, floorWall.ToPoint(), l.Value.Source);
                    }
                ),
                1
           );
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
            Vector3 vc = c.ToVector3();
            Vector3 vb = toBlend.ToVector3();

            return new Color(
                SoftLightBlend(vc.X, vb.X, intensity),
                SoftLightBlend(vc.Y, vb.Y, intensity),
                SoftLightBlend(vc.Z, vb.Z, intensity)
            );
        }

        public static Color BlendAdditive(this Color c, Color b, bool alpha=true, float pct=1)
        {
            Vector4 vc = c.ToVector4();
            Vector4 vb = b.ToVector4();

            return new Color(
                Math.Min(1f, vc.X + pct * vb.X),
                Math.Min(1f, vc.Y + pct * vb.Y),
                Math.Min(1f, vc.Z + pct * vb.Z),
                alpha ? Math.Min(1f, vc.W + pct * vb.W) : vc.W
            );
        }

        public static Color BlendSubtractive(this Color c, Color b, bool alpha = true)
        {
            Vector4 vc = c.ToVector4();
            Vector4 vb = b.ToVector4();

            return new Color(
                Math.Min(1f, vc.X - vb.X),
                Math.Min(1f, vc.Y - vb.Y),
                Math.Min(1f, vc.Z - vb.Z),
                alpha ? Math.Min(1f, vc.W - vb.W) : vc.W
            );
        }

        public static Color BlendMultiply(this Color c, Color b, bool alpha = true)
        {
            Vector4 vc = c.ToVector4();
            Vector4 vb = b.ToVector4();

            return new Color(
                Math.Min(1f, vc.X * vb.X),
                Math.Min(1f, vc.Y * vb.Y),
                Math.Min(1f, vc.Z * vb.Z),
                alpha ? Math.Min(1f, vc.W * vb.W) : vc.W
            );
        }

        public static float ApplyContrast(this float f, float contrast)
        {
            int iContrast = (int)(contrast * 255);
            int iF = (int)(f * 255);

            float k = (259f * (iContrast + 255)) / (255f * (259 - iContrast));
            return ((k * (iF - 128) + 128).Clamp(0, 255)) / 255f;
        }

        public static void Merge(this IEnumerable<LightLayer> layers, ref Color[] grid, Point size, float ambientLight=1f)
        {
            var list = layers.ToList();

            int w = size.X;
            int h = size.Y;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    int i = y * w + x;
                    grid[i] = Color.TransparentBlack;
                    foreach (var l in list)
                    {
                        Color c = l.Source.LightColor;

                        int lw = l.Grid.GetLength(0);
                        int lh = l.Grid.GetLength(1);

                        int lx = x - l.Source.LocalPosition.X;
                        int ly = y - l.Source.LocalPosition.Y;

                        if(l.Source.LightEmitterType == LightEmitterType.Conical)
                        {
                            lx += l.Source.LightRadius;
                            ly += l.Source.LightRadius;
                        }

                        if(Util.OOB(lx, ly, lw, lh))
                        {
                            continue;
                        }

                        grid[i] = Color.Lerp(grid[i], grid[i].BlendAdditive(c), l.Grid[lx, ly]);
                    }
                }
            }
        }
    }
}
