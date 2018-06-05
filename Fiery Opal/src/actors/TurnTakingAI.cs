using ConvNetSharp.Core;
using ConvNetSharp.Core.Layers.Double;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors
{
    public abstract class TurnBasedAI
    {
        public TurnTakingActor Body { get; protected set; }
        public TileMemory TileMemory { get; }

        public TurnBasedAI(TurnTakingActor actor)
        {
            Body = actor;
            TileMemory = new TileMemory();
        }

        public abstract IEnumerable<TurnBasedAction> GiveAdvice(int turn, float energy);
        public virtual void Update(GameTime gt)
        {

        }
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

    #region --- NEURAL AI ---
    public abstract class SensoryUnit
    {
        public SensoryUnit()
        {

        }

        public abstract double[] GetSensoryData(NeuralBrain brain);
        public abstract int OutputWidth { get; }
        public abstract int OutputHeight { get; }
        public abstract int OutputDepth { get; }

        public int OutputVolume => OutputWidth * OutputHeight * OutputDepth;
    }

    #region -- SENSORY UNITS --
    // Sight converts the NxN tile grid around a creature into a movement penalty matrix.
    // It also provides another NxN matrix filled with 0s for empty tiles or a 0-1 number that indicates the color of the topmost actor of that cell.
    // Thus, it provides a NxNx2 matrix to the CNN.
    public class SightUnit : SensoryUnit
    {
        public int Radius { get; }
        public override int OutputWidth => Radius;
        public override int OutputHeight => Radius;
        public override int OutputDepth => 2;

        public SightUnit(int radius)
        {
            Radius = radius;
        }

        private double ConvertColor(Color c)
        {
            return c.GetHue() / 360.0;
        }

        public override double[] GetSensoryData(NeuralBrain brain)
        {
            var map = brain.Body.Map;

            var tiles = map.TilesWithin(new Rectangle(
                brain.Body.LocalPosition.X - Radius / 2 - 1,
                brain.Body.LocalPosition.Y - Radius / 2 - 1,
                Radius,
                Radius
            ), true);

            var movementMatrix = tiles
            .Select(t => (t.Item1?.Properties.BlocksMovement ?? true) ? 1d : t.Item1.Properties.MovementPenalty)
            .ToList();

            IEnumerable<IOpalGameActor> actorsAt = null;
            var entityMatrix = tiles
            .Select(t => (actorsAt = map.ActorsAt(t.Item2.X, t.Item2.Y)).Count() == 0 ? 0 : ConvertColor(actorsAt.First().Graphics.Foreground)); // TODO: Implement limit?

            movementMatrix.AddRange(entityMatrix);
            return movementMatrix.ToArray();
        }
    }

    // Smell provides a direction vector that points towards the currently tracked actor or item
    public class SmellUnit : SensoryUnit
    {
        public OpalActorBase TrackedActor { get; protected set; }
        public override int OutputWidth => 1;
        public override int OutputHeight => 1;
        public override int OutputDepth => 3;
        public int Radius { get; }

        public SmellUnit(int radius)
        {
            Radius = radius;
        }

        public void Track(OpalActorBase act)
        {
            TrackedActor = act;
        }

        public override double[] GetSensoryData(NeuralBrain brain)
        {
            double numActors = SmellActors(brain).Count / (Math.PI * Radius * Radius);

            if (TrackedActor?.LocalPosition.Dist(brain.Body.LocalPosition) > Radius) TrackedActor = null;

            if (TrackedActor == null) return new double[] { 0, 0, numActors };
            Vector2 dir = (TrackedActor.LocalPosition - brain.Body.LocalPosition).ToVector2();
            dir.Normalize();
            if (Double.IsNaN(dir.X) || Double.IsNaN(dir.Y)) dir = Vector2.Zero;
            return new double[] { dir.X, dir.Y, numActors };
        }

        public List<OpalActorBase> SmellActors(NeuralBrain brain)
        {
            return brain.Body.Map
                .ActorsWithinRing(
                    brain.Body.LocalPosition.X,
                    brain.Body.LocalPosition.Y,
                    Radius,
                    0
                 )
                .Where(a => a is OpalActorBase)
                .Select(a => a as OpalActorBase)
                .ToList();
        }
    }
    #endregion

    public class DecisionMaker
    {
        public SightUnit Sight { get; }
        public Net<double> Network { get; } = new Net<double>();

        public enum DecisionType : int
        {

        }

        public DecisionMaker(SightUnit s)
        {
            Sight = s;
            Network.AddLayer(new InputLayer(Sight.OutputWidth, Sight.OutputHeight, Sight.OutputDepth));
        }
    }

    public class NeuralBrain : TurnBasedAI
    {
        public NeuralBrain(TurnTakingActor actor)
            : base(actor)
        {
        }

        public override IEnumerable<TurnBasedAction> GiveAdvice(int turn, float energy)
        {
            var ret = new List<TurnBasedAction>();
            return ret;
        }
    }

    #endregion
}
