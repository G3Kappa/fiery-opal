using FieryOpal.src.Ui;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using FieryOpal.Src.Ui.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors
{
    public abstract class Weapon : Item, IEquipable
    {
        public WeaponViewSprite ViewGraphics { get; }

        public Weapon(ColoredString name, WeaponViewSprite viewSprite) : base(name, ItemCategory.Equipment)
        {
            ViewGraphics = viewSprite;
        }

        public EquipmentSlotType SlotType => EquipmentSlotType.Hand;
        public virtual int RequiredSlots { get; protected set; } = 1;
        public virtual float AttackDelay { get; protected set; } = Nexus.GameInstance.TurnManager.TimeDilation;

        public virtual void OnEquip(TurnTakingActor actor)
        {
        }

        public virtual void OnUnequip(TurnTakingActor actor)
        {
        }

        public abstract void Attack(Point direction);

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();

            RegisterInventoryAction("equip", (h) => Equip(h), new Keybind.KeybindInfo(Keys.E, Keybind.KeypressState.Press, "Equip weapon"));
            RegisterInventoryAction("unequip", (h) => Unequip(h), new Keybind.KeybindInfo(Keys.E, Keybind.KeypressState.Press, "Unequip weapon"));

            ToggleInventoryAction("unequip", false);
        }

        private void Equip(IInventoryHolder h)
        {
            if (!(h is TurnTakingActor)) return;
            TurnTakingActor a = h as TurnTakingActor;
            bool ret = a.Equipment.TryEquip(this, a);
            if (a.IsPlayer)
            {
                Util.Log((ret ? Nexus.Locale.Translation["Weapon_EquipedSuccess"] : Nexus.Locale.Translation["Weapon_EquipedFailure"]).Fmt(ItemInfo.Name), false, Palette.Ui["BoringMessage"]);
            }
            if (ret)
            {
                ToggleInventoryAction("equip", false);
                ToggleInventoryAction("unequip", true);
                FlagRaycastViewportsForRedraw();
            }
        }

        private void Unequip(IInventoryHolder h)
        {
            if (!(h is TurnTakingActor)) return;
            TurnTakingActor a = h as TurnTakingActor;
            bool ret = a.Equipment.TryUnequip(this, a);
            if (a.IsPlayer)
            {
                Util.Log((ret ? Nexus.Locale.Translation["Weapon_UnequipedSuccess"] : Nexus.Locale.Translation["Weapon_UnequipedFailure"]).Fmt(ItemInfo.Name), false, Palette.Ui["BoringMessage"]);
            }
            if (ret)
            {
                ToggleInventoryAction("equip", true);
                ToggleInventoryAction("unequip", false);
                FlagRaycastViewportsForRedraw();
            }
        }

        private void FlagRaycastViewportsForRedraw()
        {
            Nexus.GameInstance.InternalMessagePipeline.Broadcast(null, cw =>
            {
                OpalGameWindow w = cw as OpalGameWindow;
                var rc = w.Viewport as RaycastViewport;
                if (rc != null)
                {
                    rc.FlagForRedraw();
                }
                return "ViewportRefresh";
            });
        }

        protected override void DropFrom(IInventoryHolder holder)
        {
            if ((holder as TurnTakingActor)?.Equipment.IsEquiped(this) ?? false)
            {
                Util.Log(Nexus.Locale.Translation["Equipment_CannotDrop"].Fmt(ItemInfo.Name), false, Palette.Ui["BoringMessage"]);
                return;
            }
            base.DropFrom(holder);
        }
    }

    public abstract class Projectile : TurnTakingActor
    {
        public enum DamageType
        {
            Impact, // Damages biggest actor on cell and dies.
            Piercing, // Damages all actors on cell and doesn't die.
            Explosive, // Damages all actors on cell and nearby cells and dies.
        }

        public abstract int CalcDamage(EquipmentSlotType partHit);
        public abstract EquipmentSlotType CalcPartHit(TurnTakingActor a);
        public abstract float HitDelay { get; }
        public Weapon FiredFrom { get; }

        public Vector2 Direction { get; protected set; }
        public DamageType DamageKind { get; }

        public Projectile(OpalLocalMap m, Point spawnPos, Vector2 direction, Weapon spawner, DamageType dt)
        {
            Direction = direction;
            ChangeLocalMap(m, spawnPos);
            DamageKind = dt;
            FiredFrom = spawner;
        }

        public override IEnumerable<TurnBasedAction> ProcessTurn(int turn, float energy)
        {
            List<TurnTakingActor> actorsHere = Map.ActorsAt(LocalPosition.X, LocalPosition.Y)
                .Where(a => a is TurnTakingActor && !(a is Projectile))
                .Select(a => a as TurnTakingActor)
                .ToList();

            if (actorsHere.Count == 0) yield break;
            yield return () =>
            {
                ApplyDamage(actorsHere);
                return HitDelay;
            };
        }

        public virtual void ApplyDamage(List<TurnTakingActor> actors)
        {
            actors = actors.Where(a => !a.IsDead).ToList(); // Since this action was enqueued, some targets might have died in the meantime. 
            switch (DamageKind)
            {
                case DamageType.Impact:
                    var target = actors.First();
                    target.ReceiveDamage(CalcDamage(CalcPartHit(target)));
                    Kill();
                    break;
                case DamageType.Piercing:
                    foreach(var victim in actors)
                    {
                        victim.ReceiveDamage(CalcDamage(CalcPartHit(victim)));
                    }
                    break;
                case DamageType.Explosive:
                    foreach (var victim in actors)
                    {
                        victim.ReceiveDamage(CalcDamage(CalcPartHit(victim)));
                    }
                    Kill();
                    break;
            }
        }
    }

    public class Lightning : Projectile
    {
        public float BranchingRadius { get; }

        public Lightning(OpalLocalMap m, Point spawnPos, Vector2 direction, Freezzino spawner, float branchRadius=2.5f) 
            : base(m, spawnPos, direction, spawner, DamageType.Piercing)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(
                new Cell(Color.Cyan, Color.Transparent, 'z')
            );

            BranchingRadius = branchRadius;
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

        public override void ApplyDamage(List<TurnTakingActor> actors)
        {
            var branchTargets = Map.ActorsWithinRing(LocalPosition.X, LocalPosition.Y, (int)BranchingRadius, 1)
                .Where(a => 
                a is TurnTakingActor 
                && Map.ActorsAt(a.LocalPosition.X, a.LocalPosition.Y).Where(b => b is Lightning).Count() == 0 
                && a.LocalPosition != FiredFrom.Owner.LocalPosition
                && !actors.Contains(a)
                && Util.BresenhamLine(LocalPosition, a.LocalPosition).All(p => !Map.TileAt(p).Properties.IsBlock)
                )
                .ToList();

            if (branchTargets.Count > 0)
            {
                var target = Util.Choose(branchTargets);
                var line = Util.BresenhamLine(LocalPosition, target.LocalPosition);
                foreach(Point p in line)
                {
                    var child = new Lightning(Map, p, (p - LocalPosition).ToVector2(), (Freezzino)FiredFrom, BranchingRadius);
                }
            }

            Util.Log("Hit", false);
            base.ApplyDamage(actors);
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
            wvs.SpritesheetIndex = 1;
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

        public Freezzino(int baseRayLen = 5) : base(DefaultName, MakeViewSprite())
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(
                new Cell(Color.Cyan, Color.Blue, ViewGraphics.SpritesheetIndex)
            );
            Graphics.GlyphCharacter = 'W';
            FirstPersonVerticalOffset = -2f;
            BaseRayLength = baseRayLen;
        }
    }
}
