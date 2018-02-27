using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.actors
{
    public abstract class TurnTakingActor : OpalActorBase, ITurnTaker
    {
        public abstract float TurnPriority { get; }
        public Queue<TurnBasedAction> EnqueuedActions { get; } = new Queue<TurnBasedAction>();
        public TurnBasedAI Brain { get; set; }
        public Vector2 LookingAt { get; set; } = new Vector2(0, 1);

        public TurnTakingActor() : base() { }

        public virtual IEnumerable<TurnBasedAction> ProcessTurn(int turn, float energy)
        {
            if(EnqueuedActions.Count == 0)
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
