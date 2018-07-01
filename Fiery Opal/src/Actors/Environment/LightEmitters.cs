using FieryOpal.Src;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Actors.Environment
{
    public abstract class LightEmitterBase : OpalActorBase, ILightEmitter
    {
        public event Action<ILightEmitter> LightEmitterDataChanged;

        public LightEmitterBase()
        {
            FirstPersonGraphics = Graphics = new ColoredGlyph(new Cell(Color.Magenta, Color.Transparent, 15));
            Visible = false;
            SetCollision(false);

            PositionChanged += (a, p, _) => { LightEmitterDataChanged?.Invoke(this); };
        }

        private LightEmitterType _type = LightEmitterType.Conical;
        public LightEmitterType LightEmitterType { get => _type; set { _type = value; LightEmitterDataChanged?.Invoke(this); } }

        private float _intensity = 1f;
        public float LightIntensity { get => _intensity; set { _intensity = value; LightEmitterDataChanged?.Invoke(this); } }

        private float _lightRadius = 5f;
        public float LightRadius { get => _lightRadius; set { _lightRadius = value; LightEmitterDataChanged?.Invoke(this); } }

        private Color _color = Color.White;
        public Color LightColor { get => _color; set { _color = value; LightEmitterDataChanged?.Invoke(this); } }

        private Vector2 _direction = Vector2.Zero;
        public Vector2 LightDirection { get => _direction; set { _direction = value; LightEmitterDataChanged?.Invoke(this); } }

        private int _angle = 90;
        public int LightAngleWidth { get => _angle; set { _angle = value; LightEmitterDataChanged?.Invoke(this); } }
    }

    public class ConicalLightEmitter : LightEmitterBase
    {
        public ConicalLightEmitter()
        {
            LightAngleWidth = 90;
            LightDirection = Vector2.Zero;
            LightEmitterType = LightEmitterType.Conical;
        }
    }

    public class RadialLightEmitter : ConicalLightEmitter
    {
        public RadialLightEmitter() 
        {
            LightAngleWidth = 360;
            LightDirection = Vector2.Zero;
        }
    }

    public class LinearLightEmitter : ConicalLightEmitter
    {
        public LinearLightEmitter()
        {
            LightAngleWidth = 2;
            LightDirection = Vector2.Zero;
        }
    }

    public class AmbientLightEmitter : LightEmitterBase
    {
        public AmbientLightEmitter()
        {
            LightEmitterType = LightEmitterType.Ambient;
        }
    }

}
