using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using SadConsole.Input;

namespace FieryOpal.src
{
    public delegate void KeybindTriggered(Keybind.KeybindInfo kb);

    public class Keybind
    {
        public enum KeypressState
        {
            Press = 0,
            Down = 1,
            Release = 2
        }

        public struct KeybindInfo
        {
            Keys MainKey;
            bool CtrlDown;
            bool ShiftDown;
            bool AltDown;

            KeypressState State;

            public KeybindInfo(Keys k, KeypressState state, bool ctrl = false, bool shift = false, bool alt = false)
            {
                MainKey = k;
                CtrlDown = ctrl;
                ShiftDown = shift;
                AltDown = alt;
                State = state;
            }
        }

        protected static Dictionary<KeybindInfo, KeybindTriggered> Delegates = new Dictionary<KeybindInfo, KeybindTriggered>();

        public static bool BindKey(KeybindInfo kb, KeybindTriggered onkbtriggered)
        {
            if (!Delegates.ContainsKey(kb))
            {
                Delegates.Add(kb, onkbtriggered);
                return true;
            }
            return false;
        }

        public static bool UnbindKey(KeybindInfo kb)
        {
            if(Delegates.ContainsKey(kb))
            {
                Delegates.Remove(kb);
                return true;
            }
            return false;
        }

        private static void FireEvent(KeybindInfo info)
        {
            if (Delegates.ContainsKey(info))
            {
                Delegates[info](info);
            }
        }

        public static void Update()
        {
            bool ctrl  = SadConsole.Global.KeyboardState.IsKeyDown(Keys.LeftControl) || SadConsole.Global.KeyboardState.IsKeyDown(Keys.RightControl);
            bool shift = SadConsole.Global.KeyboardState.IsKeyDown(Keys.LeftShift)   || SadConsole.Global.KeyboardState.IsKeyDown(Keys.RightShift);
            bool alt   = SadConsole.Global.KeyboardState.IsKeyDown(Keys.LeftAlt)     || SadConsole.Global.KeyboardState.IsKeyDown(Keys.RightAlt);

            foreach (var k in SadConsole.Global.KeyboardState.KeysDown)
            {
                var info = new KeybindInfo(k.Key, KeypressState.Down, ctrl, shift, alt);
                FireEvent(info);
            }

            foreach (var k in SadConsole.Global.KeyboardState.KeysPressed)
            {
                var info = new KeybindInfo(k.Key, KeypressState.Press, ctrl, shift, alt);
                FireEvent(info);
            }

            foreach (var k in SadConsole.Global.KeyboardState.KeysReleased)
            {
                var info = new KeybindInfo(k.Key, KeypressState.Release, ctrl, shift, alt);
                FireEvent(info);
            }
        }

    }
}
