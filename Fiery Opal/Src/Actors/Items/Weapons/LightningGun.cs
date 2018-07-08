using FieryOpal.Src.Actors.Environment;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors.Items.Weapons
{

    public class Lightning : Projectile
    {
        public float BranchingRadius { get; }
        private RadialLightEmitter LightEmitter { get; }

        public Lightning(OpalLocalMap m, Point spawnPos, Vector2 direction, Freezzino spawner, float branchRadius = 2.5f)
            : base(m, spawnPos, direction, spawner, DamageType.Piercing)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(
                new Cell(Color.Cyan, Color.Transparent, 'z')
            );

            BranchingRadius = branchRadius;

            LightEmitter = new RadialLightEmitter();
            LightEmitter.LightIntensity = 1f;
            LightEmitter.LightRadius = 1;
            LightEmitter.LightColor = Color.Cyan;
            LightEmitter.ChangeLocalMap(m, spawnPos, false);

            m.ActorDespawned += (_, a) =>
            {
                if (a == this) m.Despawn(LightEmitter);
            };
        }

        public override float TurnPriority { get; set; } = 0f;
        public override float HitDelay => Nexus.GameInstance.TurnManager.TimeDilation / 10;

        public override IEnumerable<TurnBasedAction> ProcessTurn(int turn, float energy)
        {
            List<TurnTakingActor> actorsHere = Map.ActorsAt(LocalPosition.X, LocalPosition.Y)
                .Where(a => a is TurnTakingActor && !(a is Projectile))
                .Select(a => a as TurnTakingActor)
                .ToList();

            foreach (var a in base.ProcessTurn(turn, energy)) yield return a;
            yield return () => { Kill(); return HitDelay; };
        }

        public override int CalcDamage(EquipmentSlotType partHit)
        {
            return 7;
        }

        public override EquipmentSlotType CalcPartHit(TurnTakingActor a)
        {
            return EquipmentSlotType.Torso;
        }
    }

    public class Freezzino : Weapon
    {
        private static ColoredString DefaultName = new ColoredString(
            "Freezzino",
            new Cell(Color.Cyan, Color.Transparent)
        );

        public override Font Spritesheet => Nexus.Fonts.Spritesheets["Weapons"];
        public override float AttackDelay => 0f;

        public int BaseRayLength { get; }

        private static WeaponViewSprite MakeViewSprite()
        {
            WeaponViewSprite wvs = new WeaponViewSprite();
            wvs.SpritesheetIndex = 0;
            wvs.Color = Color.AliceBlue;
            wvs.Scale = new Vector2(1.25f, 1.25f);
            wvs.Offset = new Vector2(0, 0);
            return wvs;
        }

        public override void Attack(Point direction)
        {
            if (Owner == null) return;

            Point rayEnd = Owner.LocalPosition + direction * new Point(BaseRayLength);
            var line = Util.BresenhamLine(Owner.LocalPosition + direction, rayEnd).ToList();
            foreach (Point p in line)
            {
                if (Owner.Map.TileAt(p)?.Properties.IsBlock ?? true) break;
                var child = new Lightning(Owner.Map, p, (Owner.LocalPosition - p).ToVector2(), this, 5f);
            }
        }

        public Freezzino(int baseRayLen) : base(DefaultName, MakeViewSprite())
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(
                new Cell(Color.Cyan, Color.Blue, ViewGraphics.SpritesheetIndex)
            );
            Graphics.GlyphCharacter = 'W';
            FirstPersonVerticalOffset = -2f;
            BaseRayLength = baseRayLen;
            Name = "Freezzino";
        }

        public Freezzino() : this(5)
        {

        }
    }
}
