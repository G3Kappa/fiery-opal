using FieryOpal.Src;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FieryOpal.src.Procedural.CellularAutomata
{

    class CellularAutomation<T>
    {
        public delegate bool CellularPredicate(CellState state, T t);

        public struct CellState
        {
            public Point Size;
            private int[,] m_Values;

            public int GetValue(Point p)
            {
                if (!Util.OOB(p.X, p.Y, Size.X, Size.Y)) return m_Values[p.X, p.Y];
                else throw new ArgumentOutOfRangeException("p");
            }

            public void SetValue(Point p, int i)
            {
                if (!Util.OOB(p.X, p.Y, Size.X, Size.Y)) m_Values[p.X, p.Y] = i;
                else throw new ArgumentOutOfRangeException("p");
            }

            public CellState(int w, int h)
            {
                Size = new Point(w, h);
                m_Values = new int[w, h];
            }
        }

        public class Cell
        {
            private CellState m_State;
            public CellState State
            {
                get => m_State;
                set
                {
                    var oldState = m_State;
                    m_State = value;
                    StateChanged(oldState);
                }
            }

            private void StateChanged(CellState oldState)
            {
                for (int x = 0; x < State.Size.X; ++x)
                {
                    for (int y = 0; y < State.Size.Y; ++y)
                    {
                        var p = new Point(x, y);
                        int bit = State.GetValue(p);
                        if (Reactions.ContainsKey(bit)) Reactions[bit](p);
                    }
                }
            }

            protected Dictionary<int, Action<Point>> Reactions;

            public Cell(CellState baseState)
            {
                State = baseState;
            }

            public void AddReaction(int stateBit, Action<Point> action)
            {
                Reactions.Add(stateBit, action);
            }
        }

        // Rule:
        //     - Predicate (Point)
        //     |- True -> new CellState
        //     |- False -> keep CellState
        public class Rule
        {
            public CellularPredicate Condition;
            public CellState NewState;

            public Rule(CellularPredicate condition, CellState newState)
            {
                Condition = condition;
                NewState = newState;
            }

            public CellState Evaluate(CellState curState, T t)
            {
                bool ret = Condition(curState, t);
                if (ret)
                {
                    return NewState;
                }
                return curState;
            }
        }

        protected List<Rule> Rules = new List<Rule>();

        public void AddRule(CellularPredicate condition, CellState newState)
        {
            Rules.Add(new Rule(condition, newState));
        }

        public void RemoveRule(CellularPredicate condition)
        {
            Rules.RemoveAll(r => r.Condition == condition);
        }
    }
}
