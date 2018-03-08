using FieryOpal.src.procgen;
using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.actors
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
