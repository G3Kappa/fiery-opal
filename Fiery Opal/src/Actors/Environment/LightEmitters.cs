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
    public class ConicalLightEmitter : OpalActorBase, ILightEmitter
    {
        public ConicalLightEmitter()
        {
            FirstPersonGraphics = Graphics = new ColoredGlyph(new Cell(Color.Red, Color.Transparent, 15));
            Visible = false;
            SetCollision(false);
        }

        public LightEmitterType LightEmitterType => LightEmitterType.Conical;

        public float LightIntensity { get; set; } = 1f;
        public float LightRadius { get; set; } = 10f;
        public float LightSmoothness { get; set; } = .1f;
        public Color LightColor { get; set; } = Util.Choose(new Color[] {
            new Color(255, 0, 0),
            new Color(0, 255, 0),
            new Color(0, 0, 255),
        });

        public Vector2 LightDirection { get; set; } = Util.RandomUnitPoint().ToVector2();
        public int LightAngleWidth { get; set; } = 90;

    }

    public class RadialLightEmitter : ConicalLightEmitter
    {
        public RadialLightEmitter() 
        {
            FirstPersonGraphics = Graphics = new ColoredGlyph(new Cell(Color.Red, Color.Transparent, 15));
            LightAngleWidth = 360;
            LightDirection = Vector2.Zero;
        }
    }

    public class LinearLightEmitter : ConicalLightEmitter
    {
        public LinearLightEmitter()
        {
            FirstPersonGraphics = Graphics = new ColoredGlyph(new Cell(Color.Red, Color.Transparent, 15));
            LightAngleWidth = 2;
            LightDirection = Vector2.Zero;
        }
    }

    public class AmbientLightEmitter : OpalActorBase, ILightEmitter
    {
        public AmbientLightEmitter()
        {
            FirstPersonGraphics = Graphics = new ColoredGlyph(new Cell(Color.Red, Color.Transparent, 15));
            Visible = false;
            SetCollision(false);
        }

        public LightEmitterType LightEmitterType => LightEmitterType.Ambient;

        public float LightIntensity { get; set; } = .25f;
        public float LightRadius { get; set; } = 1f;
        public float LightSmoothness { get; set; } = 1f;
        public Color LightColor { get; set; } = Color.White;

        public Vector2 LightDirection { get; set; } = Vector2.Zero;
        public int LightAngleWidth { get; set; } = 0;
    }
}
