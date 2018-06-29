using FieryOpal.Src.Actors.Items;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors.Animals
{
    class CaveBat : TurnTakingActor
    {
        public override float TurnPriority { get; set; } = 0.1f;

        public CaveBat() : base()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Creatures["CaveBat"], Color.Transparent, 'b'));
            Brain = new WanderingBrain(this);
            Name = "Cave Bat";
            IsFlying = true;

            Inventory = new PersonalInventory(0, this);
        }
    }
}
