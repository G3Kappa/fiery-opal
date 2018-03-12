using FieryOpal.Src.Procedural;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors
{
    public class Humanoid : TurnTakingActor, IInteractive, ISocialCreature
    {
        public Humanoid() : base()
        {
            Graphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
            FirstPersonGraphics = new ColoredGlyph(new Cell(Color.White, Color.Transparent, '@'));
            Brain = new WanderingBrain(this);

            Inventory = new PersonalInventory(10, this);
        }

        public override float TurnPriority => 0;


        public bool InteractWith(OpalActorBase actor)
        {
            return true;
        }
    }
}
