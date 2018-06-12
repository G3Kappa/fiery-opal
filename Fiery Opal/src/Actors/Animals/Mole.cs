using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors.Animals
{
    class Mole : TurnTakingActor
    {
        public override float TurnPriority { get; set; } = 0.1f;

        public Mole() : base()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Creatures["Mole"], Color.Transparent, 'm'));
            Brain = new WanderingBrain(this);
            Name = "Mole";

            Inventory = new PersonalInventory(0, this);
        }
    }
}
