using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors
{
    public class Plant : DecorationBase
    {
        public override Font Spritesheet => Nexus.Fonts.Spritesheets["Vegetation"];

        public override bool BlocksMovement => true;
        protected static Color[] PossibleColors;
        protected static int[] PossibleGlyphs;

        public Plant()
        {
            PossibleGlyphs = new[] { 5, 6, 23, 24 };
            PossibleColors = new[] {
                Palette.Vegetation["GenericPlant1"],
                Palette.Vegetation["GenericPlant2"],
                Palette.Vegetation["GenericPlant3"],
            };

            /* Dead Tree */
            if (Util.Rng.NextDouble() > .8f)
            {
                PossibleGlyphs = new[] { (byte)'v' + 16 };
                PossibleColors = new[] {
                    Palette.Vegetation["GenericPlant4"],
                };
            }

            FirstPersonVerticalOffset = 4.5f * -3 / 2f;
            FirstPersonScale = new Vector2(.5f, 1 / 3f);

            SetGraphics();
        }

        protected void SetGraphics()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(PossibleColors[Util.Rng.Next(PossibleColors.Length)], Color.Transparent, PossibleGlyphs[Util.Rng.Next(PossibleGlyphs.Length)]));
        }
    }

    public class Sapling : Plant
    {
        public override bool BlocksMovement => false;

        public Sapling()
        {
            PossibleGlyphs = new[] { 231, 147, 252 };

            PossibleColors = new[] {
                Palette.Vegetation["GenericPlant1"],
                Palette.Vegetation["GenericPlant2"],
                Palette.Vegetation["GenericPlant3"],
            };

            FirstPersonVerticalOffset = 2;
            FirstPersonScale = new Vector2(1.5f, 3f);

            SetGraphics();

            if (Graphics.Glyph == 245)
            {
                FirstPersonVerticalOffset += .5f;
            }
        }
    }

    public class Mushroom : Plant /* inb4 */
    {
        public override bool BlocksMovement => false;

        public Mushroom()
        {
            PossibleGlyphs = new[] { 130, 129, 28, 29 };
            PossibleColors = new[] { Color.White };

            FirstPersonVerticalOffset = 2.4f;
            FirstPersonScale = new Vector2(2.4f, 2.4f);

            SetGraphics();
        }
    }

}
