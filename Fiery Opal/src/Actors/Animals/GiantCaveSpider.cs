using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors.Animals
{
    class GiantCaveSpider : TurnTakingActor
    {
        public override float TurnPriority { get; set; } = 0.1f;

        public GiantCaveSpider() : base()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Creatures["GiantCaveSpider"], Color.Transparent, 'S'));
            Brain = new WanderingBrain(this);
            Name = "Giant Cave Spider";

            Inventory = new PersonalInventory(0, this);
        }
    }
}
