using FieryOpal.Src.Procedural;
using FieryOpal.Src.Ui;
using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Actors
{
    public class Humanoid : TurnTakingActor, IInteractive, ISocialCreature
    {
        public Humanoid() : base()
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Palette.Creatures["Humanoid"], Color.Transparent, '@'));
            Brain = new WanderingBrain(this);

            Name = "Dude";

            Inventory = new PersonalInventory(10, this);
            Equipment = new PersonalEquipment((t) =>
            {
                switch (t)
                {
                    case EquipmentSlotType.Arm:
                        return 2;
                    case EquipmentSlotType.Hand:
                        return 2;

                    case EquipmentSlotType.Leg:
                        return 2;
                    case EquipmentSlotType.Foot:
                        return 2;

                    case EquipmentSlotType.Head:
                        return 1;
                    case EquipmentSlotType.Torso:
                        return 1;

                    default: return 0;
                }
            });
        }

        public override float TurnPriority { get; set; } = .2f;


        public bool InteractWith(OpalActorBase actor)
        {
            return true;
        }
    }
}
