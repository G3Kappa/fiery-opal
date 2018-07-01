using FieryOpal.Src.Actors.Environment;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors.Items
{
    public class Torch : Weapons.Weapon, IDecoration
    {
        public override EquipmentSlotType SlotType => EquipmentSlotType.Hand;
        public override int RequiredSlots => 1;

        bool IDecoration.BlocksMovement => false;

        protected RadialLightEmitter LightSourceInner, LightSourceOuter;

        private static WeaponViewSprite MakeViewSprite()
        {
            WeaponViewSprite wvs = new WeaponViewSprite();
            wvs.SpritesheetIndex = 161;
            wvs.Color = Color.Orange;
            wvs.Scale = new Vector2(1.25f, 1.25f);
            wvs.Offset = new Vector2(.75f, .5f);
            return wvs;
        }

        public Torch() : base("Torch".ToColoredString(Color.Orange), MakeViewSprite())
        {
            LightSourceOuter = new RadialLightEmitter()
            {
                LightColor = Color.DarkOrange,
                LightIntensity = 30f,
                LightRadius = 12f,
            };

            LightSourceInner = new RadialLightEmitter()
            {
                LightColor = new Color(255, 255, 200),
                LightIntensity = 30f,
                LightRadius = 4f,
            };

            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.Orange, Color.Transparent, 161));
            Name = "Torch";

            MapChanged += HandleSpawnOnMapChange;
        }
        
        protected virtual void FollowActor(IOpalGameActor a, Point oldPos, bool mapChanged=false)
        {
            LightSourceOuter.MoveTo(a.LocalPosition, true);
            LightSourceInner.MoveTo(a.LocalPosition, true);
        }

        private void HandleSpawnOnMapChange(IOpalGameActor a, OpalLocalMap oldMap)
        {
            LightSourceOuter.ChangeLocalMap(a.Map, a.LocalPosition, false);
            LightSourceInner.ChangeLocalMap(a.Map, a.LocalPosition, false);
        }

        public override void OnUnequip(IEquipmentUser actor)
        {
            base.OnUnequip(actor);
            actor.Map.Despawn(LightSourceOuter);
            actor.Map.Despawn(LightSourceInner);
            actor.PositionChanged -= FollowActor;
            actor.MapChanged -= HandleSpawnOnMapChange;
        }

        public override void OnEquip(IEquipmentUser actor)
        {
            base.OnEquip(actor);
            LightSourceOuter.ChangeLocalMap(actor.Map, actor.LocalPosition, false);
            LightSourceInner.ChangeLocalMap(actor.Map, actor.LocalPosition, false);
            actor.PositionChanged += FollowActor;
            actor.MapChanged += HandleSpawnOnMapChange;
        }

        public override void OnDropped(IInventoryHolder oldOwner)
        {
            base.OnDropped(oldOwner);
            LightSourceOuter.ChangeLocalMap(oldOwner.Map, oldOwner.LocalPosition, false);
            LightSourceInner.ChangeLocalMap(oldOwner.Map, oldOwner.LocalPosition, false);
        }

        public override void OnPickedUp(IInventoryHolder newOwner)
        {
            base.OnPickedUp(newOwner);
            newOwner.Map.Despawn(LightSourceOuter);
            newOwner.Map.Despawn(LightSourceInner);
        }

        public override void Attack(Point direction)
        {
            //
        }
    }
}
