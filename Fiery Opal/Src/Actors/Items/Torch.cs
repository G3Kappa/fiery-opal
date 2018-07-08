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

        private static WeaponViewSprite MakeViewSprite(Color? c = null)
        {
            WeaponViewSprite wvs = new WeaponViewSprite();
            wvs.SpritesheetIndex = 1;
            wvs.Color = c ?? Color.Orange;
            wvs.Scale = new Vector2(1.25f, 1.25f);
            wvs.Offset = new Vector2(.75f, .33f);
            return wvs;
        }

        public Torch() : this(Color.Orange) { }

        public Torch(Color color) : base("Torch".ToColoredString(color), MakeViewSprite(color))
        {
            LightSourceOuter = new RadialLightEmitter()
            {
                LightColor = color,
                LightIntensity = 30f,
                LightRadius = 12,
            };

            LightSourceInner = new RadialLightEmitter()
            {
                LightColor = color.BlendAdditive(Color.White, false, .33f),
                LightIntensity = 5f,
                LightRadius = 5,
            };

            Spritesheet = Nexus.Fonts.Spritesheets["Weapons"];
            FirstPersonGraphics = new ColoredGlyph(new Cell(color, Color.Transparent, 1));
            Graphics = new ColoredGlyph(new Cell(color, Color.Transparent, 161));
            Name = "Torch";

            MapChanged += HandleSpawnOnMapChange;

            FirstPersonScale = new Vector2(2f, 2f);
            FirstPersonVerticalOffset = 6f;
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
            actor.PositionChanged -= FollowActor;
            actor.MapChanged -= HandleSpawnOnMapChange;
            actor.Map.Despawn(LightSourceOuter);
            actor.Map.Despawn(LightSourceInner);
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
