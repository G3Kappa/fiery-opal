using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src
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
        public int CurrentTurn => (int)CurrentTime;

        public float TimeDilation { get; private set; } = 1 / 40f;

        protected Dictionary<Guid, float> Accumulator;

        public TurnManager()
        {
            ResetAccumulator();
        }

        public void ResetAccumulator()
        {
            Accumulator = new Dictionary<Guid, float>();
        }

        public void BeginTurn(OpalLocalMap map, Guid playerGuid)
        {
            // Don't allow time to flow if the player still has unresolved dialogs
            if (OpalDialog.CurrentDialogCount > 0) return;

            var turnTakers = map.ActorsWithin(null).Where(a => a is ITurnTaker).OrderBy(tt => (tt as ITurnTaker).TurnPriority).ToList();
            Dictionary<Guid, Queue<TurnBasedAction>> actions = new Dictionary<Guid, Queue<TurnBasedAction>>();
            foreach (var taker in turnTakers)
            {
                if (!Accumulator.ContainsKey(taker.Handle)) Accumulator[taker.Handle] = 0.0f;
                actions[taker.Handle] = new Queue<TurnBasedAction>();
                var intentions = (taker as ITurnTaker).ProcessTurn(CurrentTurn, 1 - Accumulator[taker.Handle]);
                foreach(var intention in intentions)
                {
                    actions[taker.Handle].Enqueue(intention);
                }
            }

            for (float t = 0; t < 1; t += TimeDilation)
            {
                if (actions[playerGuid].Count == 0 && Accumulator[playerGuid] == .0f)
                {
                    // If the player has no more actions to perform but hasn't completed their turn yet,
                    // end the current turn now and let them make another move.
                    return;
                }
                foreach (var kvp in actions)
                {
                    if (Accumulator[kvp.Key] <= t && kvp.Value.Count > 0)
                    {
                        var cost = kvp.Value.Dequeue().Invoke();
                        Accumulator[kvp.Key] += cost - TimeDilation;
                    }
                    else Accumulator[kvp.Key] = Math.Max(Accumulator[kvp.Key] - TimeDilation, 0);
                }
                CurrentTime = (float)Math.Round(CurrentTime + TimeDilation, 3);
            }

            // If some actions are left uninvoked, discard them. The AI will re-evaluate.
        }
    }
}
