using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors.Animals
{
    class CaveBear : TurnTakingActor
    {
        public override float TurnPriority { get; set; } = 0.1f;

        public CaveBear() : base()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Creatures["CaveBear"], Color.Transparent, 'B'));
            Brain = new WanderingBrain(this);
            Name = "Cave Bear";

            Inventory = new PersonalInventory(0, this);
        }
    }
}
