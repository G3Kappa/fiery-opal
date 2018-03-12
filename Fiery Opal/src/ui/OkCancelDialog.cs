using Microsoft.Xna.Framework;
using SadConsole.Controls;
using System;

namespace FieryOpal.Src.Ui
{
    public class OkCancelDialog : OpalDialog
    {
        public enum OpalDialogResult
        {
            STILL_OPEN = -1,
            CLOSED = 0,
            CANCEL = 1,
            OK = 2
        }

        public OpalDialogResult Result { get; protected set; }
        public Action<OpalDialogResult> OnResult;

        protected Button OkButton, CancelButton;

        private void CreateButtons(string ok_text, string cancel_text)
        {
            OkButton = new Button(ok_text.Length + 4);
            OkButton.Position = new Point(Width - (ok_text.Length + 5), Height - 2);
            OkButton.Text = ok_text;
            OkButton.Theme = DialogButtonTheme;
            OkButton.Click += (btn, args) =>
            {
                Result = OpalDialogResult.OK;
                Hide();
            };
            OkButton.EndCharacterLeft = '[';
            OkButton.EndCharacterRight = ']';
            Add(OkButton);

            CancelButton = new Button(cancel_text.Length + 4);
            CancelButton.Position = new Point(Width - (cancel_text.Length + 4 + ok_text.Length + 6), Height - 2);
            CancelButton.Text = cancel_text;
            CancelButton.Theme = DialogButtonTheme;
            CancelButton.Click += (btn, args) =>
            {
                Result = OpalDialogResult.CANCEL;
                Hide();
            };
            Add(CancelButton);
            CancelButton.EndCharacterLeft = '[';
            CancelButton.EndCharacterRight = ']';
            FocusedControl = CancelButton; // Force redraw with new theme

            Result = OpalDialogResult.STILL_OPEN;
            Closed += (e, f) =>
            {
                if (Result == OpalDialogResult.STILL_OPEN) Result = OpalDialogResult.CLOSED;
                if (OnResult != null) OnResult.Invoke(Result);
            };
        }

        public OkCancelDialog() : base()
        {
            CreateButtons("Ok", "Cancel");
        }
    }
}
