using FieryOpal.Src;
using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.src.Actors.Decorations
{
    class Boulders : DecorationBase
    {
        public override Font Spritesheet => Nexus.Fonts.Spritesheets["Terrain"];
        public override bool BlocksMovement => true;

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
            FirstPersonVerticalOffset = 2f;
        }
    }

    class Fungus : DecorationBase
    {
        public override Font Spritesheet => Nexus.Fonts.Spritesheets["Vegetation"];
        public override bool BlocksMovement => false;

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
            FirstPersonVerticalOffset = 1.35f;
        }
    }
}
