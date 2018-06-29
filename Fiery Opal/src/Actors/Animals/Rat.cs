using FieryOpal.Src.Actors.Items;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors.Animals
{
    class Rat : TurnTakingActor
    {
        public override float TurnPriority { get; set; } = 0.1f;

        public Rat() : base()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Creatures["Rat"], Color.Transparent, 'r'));
            Brain = new WanderingBrain(this);
            Name = "Rat";

            Inventory = new PersonalInventory(0, this);
        }
    }
}
