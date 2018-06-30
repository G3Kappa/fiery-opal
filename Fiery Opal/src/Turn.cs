using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src
{
    /*
        TURN-BASED ACTIONS:
        A TBA is a wrapper that encapsulates some game logic and returns the number of turns this logic "used up".
        For example, let's say that moving a creature takes exactly one turn.
        Inside your creature's code, you'll create a TBA as follows:
        () => { Move(x, y); return 1.0f; }
        But if you're coding a dash-like ability, and want to move five squares in a turn, your TBA will look like:
        () => { Move(x, y); return 1/5f; }
        When a creature's actions exceed one turn in duration, this duration will be treated as a cooldown instead.
        Say that an action takes two turns to complete: you'll perform it instantaneously, but your turn will end AND
        you will skip the next turn. If one such action's duration isn't an integer, you'll be able to take the first
        turn you would take as if the duration was rounded down, but you'll only be able to use up 1 - (d-(int)d) turns
        before undergoing further cooldown.
    */
    public delegate float TurnBasedAction();

    public interface ITurnTaker
    {
        Guid Handle { get; }
        float TurnPriority { get; } // Lower => take turn earlier. Can be implemented as 1f/Speed.

        IEnumerable<TurnBasedAction> ProcessTurn(int turn, float energy); // Actual time = turn + (1 - energy)
    }

    public class TurnManager
    {
        public float CurrentTime { get; private set; } = 0f; // CurrentTurn + (.0f -> 1f)
        public float CurrentPlayerDelay { get; private set; } = 0f;
        public int CurrentTurn => (int)CurrentTime;

        public float TimeDilation { get; private set; } = 1 / 40f;

        protected Dictionary<Guid, float> Accumulator;

        public delegate void TurnStartedEventHandler(TurnManager sender, float turn);
        public event TurnStartedEventHandler TurnStarted;
        public delegate void TurnEndedEventHandler(TurnManager sender, float turn);
        public event TurnEndedEventHandler TurnEnded;

        public TurnManager()
        {
            ResetAccumulator();
        }

        private bool AccumulatorResetFlag = false;
        public void ResetAccumulator()
        {
            Accumulator = new Dictionary<Guid, float>();
            AccumulatorResetFlag = true;
        }

        public void BeginTurn(OpalLocalMap map)
        {
            var turnTakers = map.ActorsWithin(null).Where(a => a is ITurnTaker).OrderBy(tt => (tt as ITurnTaker).TurnPriority).ToList();
            Dictionary<Guid, Queue<TurnBasedAction>> actions = new Dictionary<Guid, Queue<TurnBasedAction>>();
            foreach (var taker in turnTakers)
            {
                if (!Accumulator.ContainsKey(taker.Handle)) Accumulator[taker.Handle] = 0.0f;
                actions[taker.Handle] = new Queue<TurnBasedAction>();
                var intentions = (taker as ITurnTaker).ProcessTurn(CurrentTurn, 1 - Accumulator[taker.Handle]);
                foreach (var intention in intentions)
                {
                    actions[taker.Handle].Enqueue(intention);
                }
            }

            CurrentTime -= TimeDilation;

            var accKeys = Accumulator.Keys.ToList();
            TurnStarted?.Invoke(this, CurrentTurn);

            float t = 0;
            for (; t < 1; t += TimeDilation)
            {
                // If the player is dead allow no further processing of turns.
                // Unless they somehow come back to life, that is.
                if (Nexus.Player.IsDead)
                {
                    Util.LogText("You died.", false);
                    return;
                }

                // If the player's key is not contained in actions, it might be
                // that they just left the map while the turn hasn't finished.
                if (actions.ContainsKey(Nexus.Player.Handle) && actions[Nexus.Player.Handle].Count == 0 && Accumulator[Nexus.Player.Handle] == .0f)
                {
                    // If the player has no more actions to perform but hasn't completed their turn yet,
                    // end the current turn now and let them make another move.
                    break;
                }

                foreach (var key in accKeys)
                {
                    Accumulator[key] = Math.Max(Accumulator[key] - TimeDilation, 0);
                }

                foreach (var kvp in actions)
                {
                    if (kvp.Value.Count == 0) continue;
                    if (Accumulator[kvp.Key] > 0) continue;

                    var cost = kvp.Value.Dequeue().Invoke();
                    if(AccumulatorResetFlag) // Triggered when the player changes map
                    {
                        Accumulator[Nexus.Player.Handle] = cost;
                        break;
                    }

                    Accumulator[kvp.Key] += cost + TimeDilation * (cost - 1);
                }

                CurrentTime = (float)Math.Round(CurrentTime + TimeDilation, 3);
            }

            Nexus.DayNightCycle.Update(t);
            Nexus.DayNightCycle.UpdateLocal(map);

            TurnEnded?.Invoke(this, CurrentTime);
            CurrentPlayerDelay = Accumulator[Nexus.Player.Handle];
            if(AccumulatorResetFlag)
            {
                AccumulatorResetFlag = false;
            }
            else if (CurrentPlayerDelay > 0)
            {
                BeginTurn(map);
            }
        }
    }
}
