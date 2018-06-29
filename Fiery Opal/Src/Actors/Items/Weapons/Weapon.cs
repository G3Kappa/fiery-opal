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
    public abstract class Weapon : OpalItem, IEquipable
    {
        public WeaponViewSprite ViewGraphics { get; }

        public Weapon(ColoredString name, WeaponViewSprite viewSprite) : base(name, ItemCategory.Equipment)
        {
            ViewGraphics = viewSprite;
        }

        public virtual EquipmentSlotType SlotType { get; protected set; } = EquipmentSlotType.Hand;
        public virtual int RequiredSlots { get; protected set; } = 1;
        public virtual float AttackDelay { get; protected set; } = Nexus.GameInstance.TurnManager.TimeDilation;

        public virtual void OnEquip(IEquipmentUser actor)
        {
        }

        public virtual void OnUnequip(IEquipmentUser actor)
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
                Util.LogText((ret ? Nexus.Locale.Translation["Weapon_EquipedSuccess"] : Nexus.Locale.Translation["Weapon_EquipedFailure"]).Fmt(ItemInfo.Name), false, Palette.Ui["BoringMessage"]);
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
                Util.LogText((ret ? Nexus.Locale.Translation["Weapon_UnequipedSuccess"] : Nexus.Locale.Translation["Weapon_UnequipedFailure"]).Fmt(ItemInfo.Name), false, Palette.Ui["BoringMessage"]);
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
                Util.LogText(Nexus.Locale.Translation["Equipment_CannotDrop"].Fmt(ItemInfo.Name), false, Palette.Ui["BoringMessage"]);
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
            ChangeLocalMap(m, spawnPos, false);
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
                ApplyDamage(actorsHere, false);
                return HitDelay;
            };
        }

        public virtual void ApplyDamage(List<TurnTakingActor> actors, bool hurtOwner = false)
        {
            actors = actors.Where(a => !a.IsDead).ToList(); // Since this action was enqueued, some targets might have died in the meantime.
            if (!hurtOwner) actors.Remove(FiredFrom.Owner as TurnTakingActor);
            switch (DamageKind)
            {
                case DamageType.Impact:
                    var target = actors.First();
                    target.ReceiveDamage(CalcDamage(CalcPartHit(target)));
                    Kill();
                    break;
                case DamageType.Piercing:
                    foreach (var victim in actors)
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
}
