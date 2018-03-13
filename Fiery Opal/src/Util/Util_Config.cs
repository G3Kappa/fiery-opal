using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using System;
using System.Linq;
using System.Collections.Generic;
using static FieryOpal.Src.Procedural.GenUtil;
using Microsoft.Xna.Framework.Input;
using FieryOpal.src;

namespace FieryOpal.Src
{
    public static partial class Util
    {
        public static PlayerActionsKeyConfiguration LoadDefaultKeyconfig()
        {
            var cfg = new PlayerActionsKeyConfiguration();
            cfg.AssignKey(PlayerAction.Wait, new Keybind.KeybindInfo(Keys.OemPeriod, Keybind.KeypressState.Press, "Player: Wait"));

            cfg.AssignKey(PlayerAction.MoveU, new Keybind.KeybindInfo(Keys.W, Keybind.KeypressState.Press, "Player: Walk forwards"));
            cfg.AssignKey(PlayerAction.MoveD, new Keybind.KeybindInfo(Keys.S, Keybind.KeypressState.Press, "Player: Walk backwards"));
            cfg.AssignKey(PlayerAction.MoveL, new Keybind.KeybindInfo(Keys.A, Keybind.KeypressState.Press, "Player: Strafe left"));
            cfg.AssignKey(PlayerAction.MoveR, new Keybind.KeybindInfo(Keys.D, Keybind.KeypressState.Press, "Player: Strafe right"));

            cfg.AssignKey(PlayerAction.TurnL, new Keybind.KeybindInfo(Keys.Q, Keybind.KeypressState.Press, "Player: Turn left"));
            cfg.AssignKey(PlayerAction.TurnR, new Keybind.KeybindInfo(Keys.E, Keybind.KeypressState.Press, "Player: Turn right"));

            cfg.AssignKey(PlayerAction.Interact, new Keybind.KeybindInfo(Keys.Space, Keybind.KeypressState.Press, "Player: Interact"));
            cfg.AssignKey(PlayerAction.OpenInventory, new Keybind.KeybindInfo(Keys.I, Keybind.KeypressState.Press, "Player: Open inventory"));

            return cfg;
        }

        public static FontConfigInfo LoadDefaultFontConfig()
        {
            return new FontConfigLoader().LoadFile("gfx/fonts.cfg");
        }
    }

    public static partial class Extensions
    {
    }
}
