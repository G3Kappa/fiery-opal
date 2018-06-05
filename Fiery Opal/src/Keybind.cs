using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace FieryOpal.Src
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
            public Keys MainKey;
            public bool CtrlDown;
            public bool ShiftDown;
            public bool AltDown;

            public KeypressState State;
            public string HelpText;

            public KeybindInfo(Keys k, KeypressState state, string help_text, bool ctrl = false, bool shift = false, bool alt = false)
            {
                MainKey = k;
                CtrlDown = ctrl;
                ShiftDown = shift;
                AltDown = alt;
                State = state;
                HelpText = help_text;
            }

            public override string ToString()
            {
                var fmt = "{0}{1}{2}";
                char first_char = MainKey.ToString()[0];
                string shift_mod = ((first_char >= 'a' && first_char <= 'z') || (first_char >= 'A' && first_char <= 'Z')) ? MainKey.ToString().ToUpper() : "shift+" + MainKey.ToString().ToLower();

                var s = String.Format(fmt, CtrlDown ? "ctrl+" : "", AltDown ? "alt+" : "", ShiftDown ? shift_mod : MainKey.ToString().ToLower());

                return s;
            }

            private KeybindInfo Stripped()
            {
                if (HelpText.Length == 0) return this;
                return new KeybindInfo(MainKey, State, "", CtrlDown, ShiftDown, AltDown);
            }

            public override int GetHashCode()
            {
                int hash = MainKey.GetHashCode();
                hash = (hash * 17) + CtrlDown.GetHashCode();
                hash = (hash * 17) + ShiftDown.GetHashCode();
                hash = (hash * 17) + AltDown.GetHashCode();
                hash = (hash * 17) + State.GetHashCode();
                return hash;
            }

            public override bool Equals(object obj)
            {
                KeybindInfo? info = obj as KeybindInfo?;
                if (!info.HasValue) return false;

                return info.Value.GetHashCode() == GetHashCode();
            }

            public static KeybindInfo Invalid = new KeybindInfo(Keys.None, KeypressState.Release, "");
        }

        protected static Stack<Dictionary<KeybindInfo, KeybindTriggered>> Delegates = new Stack<Dictionary<KeybindInfo, KeybindTriggered>>();

        private static Dictionary<KeybindInfo, KeybindTriggered> currentDelegates = new Dictionary<KeybindInfo, KeybindTriggered>();
        protected static Dictionary<KeybindInfo, KeybindTriggered> CurrentDelegates
        {
            get => currentDelegates;
            set => currentDelegates = value;
        }

        public static bool BindKey(KeybindInfo kb, KeybindTriggered onkbtriggered)
        {
            if (!CurrentDelegates.ContainsKey(kb))
            {
                CurrentDelegates.Add(kb, onkbtriggered);
                return true;
            }
            return false;
        }

        public static bool UnbindKey(KeybindInfo kb)
        {
            if (CurrentDelegates.ContainsKey(kb))
            {
                CurrentDelegates.Remove(kb);
                return true;
            }
            return false;
        }

        private static void FireEvent(KeybindInfo info)
        {
            if (CurrentDelegates.ContainsKey(info))
            {
                CurrentDelegates[info](info);
            }
        }

        private static void BindPermanent()
        {
            BindKey(new KeybindInfo(Keys.F10, KeypressState.Press, "Show this dialog"), (info) => ShowCurrentKeybindsDialog());
        }

        public static void PushState()
        {
            Delegates.Push(CurrentDelegates);
            CurrentDelegates = new Dictionary<KeybindInfo, KeybindTriggered>();
            BindPermanent();
        }

        public static void PopState()
        {
            CurrentDelegates = Delegates.Pop();
        }

        public static void Update()
        {
            bool ctrl = SadConsole.Global.KeyboardState.IsKeyDown(Keys.LeftControl) || SadConsole.Global.KeyboardState.IsKeyDown(Keys.RightControl);
            bool shift = SadConsole.Global.KeyboardState.IsKeyDown(Keys.LeftShift) || SadConsole.Global.KeyboardState.IsKeyDown(Keys.RightShift);
            bool alt = SadConsole.Global.KeyboardState.IsKeyDown(Keys.LeftAlt) || SadConsole.Global.KeyboardState.IsKeyDown(Keys.RightAlt);

            foreach (var k in SadConsole.Global.KeyboardState.KeysDown)
            {
                var info = new KeybindInfo(k.Key, KeypressState.Down, "", ctrl, shift, alt);
                FireEvent(info);
            }

            foreach (var k in SadConsole.Global.KeyboardState.KeysPressed)
            {
                var info = new KeybindInfo(k.Key, KeypressState.Press, "", ctrl, shift, alt);
                FireEvent(info);
            }

            foreach (var k in SadConsole.Global.KeyboardState.KeysReleased)
            {
                var info = new KeybindInfo(k.Key, KeypressState.Release, "", ctrl, shift, alt);
                FireEvent(info);
            }
        }

        private static bool IsCKDShown = false;
        public static void ShowCurrentKeybindsDialog()
        {
            if (IsCKDShown) return;
            var dialog = OpalDialog.Make<ContextMenu<Keybind>>("Current Key Mapping", "");
            dialog.BindActions = false;
            foreach (var bind in CurrentDelegates.Keys)
            {
                if (bind.HelpText.EndsWith("this dialog")) continue;

                dialog.AddAction(bind.HelpText, (kb) => { }, bind);
            }
            OpalDialog.LendKeyboardFocus(dialog);
            foreach (var bind in CurrentDelegates.Keys)
            {
                dialog.AddAction(bind.HelpText, (kb) => { }, bind);
            }
            dialog.Show();
            dialog.Closed += (e, eh) =>
            {
                IsCKDShown = false;
            };
            IsCKDShown = true;
        }

    }
}
