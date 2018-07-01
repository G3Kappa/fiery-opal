using FieryOpal.Src.Actors.Items;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FieryOpal.Src.Actors
{
    public abstract class TurnTakingActor : OpalActorBase, ITurnTaker, IInventoryHolder, IEquipmentUser
    {
        public abstract float TurnPriority { get; set; }
        public Queue<TurnBasedAction> EnqueuedActions { get; } = new Queue<TurnBasedAction>();
        public TurnBasedAI Brain { get; set; }
        public PersonalInventory Inventory { get; protected set; }
        public PersonalEquipment Equipment { get; protected set; }
        public int Health { get; protected set; }
        public int MaxHealth { get; protected set; }
        public int ViewDistance { get; protected set; }

        public TurnTakingActor() : base()
        {
            MapChanged += (me, old_map) =>
            {
                Brain?.TileMemory.ForgetEverything();
                Brain?.TileMemory.UnseeEverything();
            };

            MaxHealth = Health = 100;
            ViewDistance = 64;
        }

        public void ReceiveDamage(int damage)
        {
            Health -= damage;
            if (Health <= 0)
            {
                Util.LogText(Nexus.Locale.Translation["Actor_Died"].Fmt(Name), false);
                Kill();
            }
            else if (Health > MaxHealth) Health = MaxHealth;
        }

        public virtual IEnumerable<TurnBasedAction> ProcessTurn(int turn, float energy)
        {
            if (EnqueuedActions.Count == 0)
            {
                foreach (var advice in Brain.GiveAdvice(turn, energy)) EnqueuedActions.Enqueue(advice);
            }
            while (EnqueuedActions.Count > 0) yield return EnqueuedActions.Dequeue();
        }

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);

            if (Equipment == null) return;
            foreach(var i in Equipment.GetContents())
            {
                (i as OpalItem).Update(delta);
            }
        }
    }
}
