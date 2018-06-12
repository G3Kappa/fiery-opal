using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors.Animals
{
    class CaveLeech : TurnTakingActor
    {
        public override float TurnPriority { get; set; } = 0.1f;

        public CaveLeech() : base()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Creatures["CaveLeech"], Color.Transparent, 'l'));
            Brain = new WanderingBrain(this);
            Name = "Cave Leech";

            Inventory = new PersonalInventory(0, this);
        }
    }
}
