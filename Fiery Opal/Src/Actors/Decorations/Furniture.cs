using FieryOpal.Src.Ui;
using SadConsole;

namespace FieryOpal.Src.Actors.Decorations
{
    public class Furniture : DecorationBase
    {
        public override bool BlocksMovement => true;
        public override bool DrawShadow => true;
    }

    public class Chair : Furniture
    {
        public Chair()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Terrain["WoodenStuffForeground"],
                    Palette.Terrain["WoodenStuffBackground"],
                    'h'
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
        }
    }

    public class Table : Furniture
    {
        public Table()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Terrain["WoodenStuffForeground"],
                    Palette.Terrain["WoodenStuffBackground"],
                    194
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
        }
    }

    public class Vase : Furniture
    {
        public Vase()
        {
            Graphics =
                FirstPersonGraphics =
                new ColoredGlyph(
                    new Cell(Palette.Terrain["Vase01Foreground"],
                    Palette.Terrain["Vase01Background"],
                    246
                ));

            FirstPersonScale = new Microsoft.Xna.Framework.Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
        }
    }
}
