using System.Collections.Generic;

namespace FieryOpal.Src.Actors
{
    public abstract class TurnBasedAI
    {
        protected TurnTakingActor Body;
        public TurnBasedAI(TurnTakingActor actor)
        {
            Body = actor;
        }
        public abstract IEnumerable<TurnBasedAction> GiveAdvice(int turn, float energy);
    }

    public class BrainDummy : TurnBasedAI
    {
        public BrainDummy(TurnTakingActor actor) : base(actor) { }
        public override IEnumerable<TurnBasedAction> GiveAdvice(int turn, float energy) { yield break; }
    }

    public class WanderingBrain : TurnBasedAI
    {
        public WanderingBrain(TurnTakingActor actor) : base(actor) { }
        public override IEnumerable<TurnBasedAction> GiveAdvice(int turn, float energy)
        {
            yield return () => { Body.MoveTo(Util.RandomUnitPoint()); return 1.0f; };
        }
    }
}
