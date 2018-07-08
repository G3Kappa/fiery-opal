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
    public class Flashlight : Torch
    {
        public Flashlight() : base()
        {
            ViewGraphics.SpritesheetIndex = (byte)'i';
            ViewGraphics.Color = Color.Yellow;
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.Yellow, Color.Transparent, 'i'));
            Name = "Flashlight";
            ItemInfo.Name = Name.ToColoredString(Color.Yellow);

            LightSourceInner.LightColor = Color.White;
            LightSourceOuter.LightColor = Color.Yellow;

            LightSourceInner.LightAngleWidth = 60;
            LightSourceOuter.LightAngleWidth = 60;

            LightSourceInner.LightRadius = 2;
            LightSourceOuter.LightRadius = 12;

            LightSourceInner.LightIntensity = 24f;
            LightSourceOuter.LightIntensity = 24f;
        }

        public override void Update(TimeSpan delta)
        {
            base.Update(delta);
            if (Owner == null) return;
            LightSourceInner.LightDirection = LightSourceOuter.LightDirection = Owner.LookingAt;
        }
    }
}
