using FieryOpal.Src.Actors.Environment;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors.Decorations
{
    public class Furniture : DecorationBase
    {
        public override bool DrawShadow => true;

        public Furniture()
        {
            SetCollision(true);
        }
    }

    public class Chair : Furniture
    {
        public Chair() : base()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Terrain["WoodenStuff"],
                    Color.Transparent,
                    'h'
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
        }
    }

    public class Table : Furniture
    {
        public Table() : base()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Terrain["WoodenStuff"],
                    Color.Transparent,
                    194
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
        }
    }

    public class Closet : Furniture
    {
        public Closet() : base()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Terrain["WoodenStuff"],
                    Color.Transparent,
                    227
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(1.5f, 1f);
            FirstPersonVerticalOffset = 2f;
        }
    }

    public class Vase : Furniture
    {
        public Vase() : base()
        {
            Graphics =
            new ColoredGlyph(
                new Cell(Palette.Terrain["Vase01"],
                Color.Transparent,
                246
            ));

            FirstPersonGraphics =
            new ColoredGlyph(
                new Cell(Palette.Terrain["Vase01"],
                Color.Transparent,
                246
            ));

            if (Util.CoinToss())
            {
                FirstPersonGraphics.Glyph = 173;
                FirstPersonGraphics.Foreground = Graphics.Foreground = Palette.Terrain["Vase02"];
            }

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
        }
    }

    public class CeilingLamp : Furniture
    {
        protected RadialLightEmitter LightSource;

        public CeilingLamp() : base()
        {
            Spritesheet = Nexus.Fonts.Spritesheets["Items"];
            FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, 2));
            Graphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, 0));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(1.5f, 1.5f);
            FirstPersonVerticalOffset = -5f;

            LightSource = new RadialLightEmitter()
            {
                LightColor = Color.LightGoldenrodYellow,
                LightIntensity = 8f,
                LightRadius = 6,
            };

            Name = "Ceiling Lamp";
            MapChanged += HandleSpawnOnMapChange;
        }

        private void HandleSpawnOnMapChange(IOpalGameActor a, OpalLocalMap oldMap)
        {
            LightSource.ChangeLocalMap(a.Map, a.LocalPosition, false);
        }
    }
}
