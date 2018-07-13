using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Actors.Decorations
{
    class Boulders : DecorationBase
    {
        public override Font Spritesheet => Nexus.Fonts.Spritesheets["Terrain"];

        public Boulders()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Terrain["DirtForeground"],
                    Palette.Terrain["DirtBackground"],
                    236
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
            SetCollision(true);
        }
    }

    class Fungus : DecorationBase
    {
        public override Font Spritesheet => Nexus.Fonts.Spritesheets["Vegetation"];

        public Fungus()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Vegetation["Fungus"],
                    Palette.Vegetation["Fungus"],
                    237
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 4f;
            SetCollision(false);
        }
    }
}
