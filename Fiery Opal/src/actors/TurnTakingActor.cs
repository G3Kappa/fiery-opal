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
        public Vector2 LookingAt { get; set; } = new Vector2(0, 1);
        public PersonalInventory Inventory { get; protected set; }
        public PersonalEquipment Equipment { get; protected set; }
        public int Health { get; protected set; }
        public int MaxHealth { get; protected set; }

        public TurnTakingActor() : base()
        {
            MapChanged += (me, old_map) =>
            {
                Brain?.TileMemory.ForgetEverything();
                Brain?.TileMemory.UnseeEverything();
            };

            MaxHealth = Health = 100;
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

        public void Turn(float deg)
        {
            LookingAt = new Vector2((float)Math.Cos(deg) * LookingAt.X - (float)Math.Sin(deg) * LookingAt.Y, (float)Math.Sin(deg) * LookingAt.X + (float)Math.Cos(deg) * LookingAt.Y);
            // Round them to cut any possible floating point errors short and maintain a constant ratio, just in case
            LookingAt = new Vector2((float)Math.Round(LookingAt.X, 0), (float)Math.Round(LookingAt.Y, 0));
        }
    }
}
