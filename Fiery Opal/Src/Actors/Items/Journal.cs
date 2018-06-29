using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src.Actors.Items
{
    public class Journal : OpalItem
    {
        protected List<List<string>> Contents = new List<List<string>>();

        public Journal() : base("Journal".ToColoredString(), ItemCategory.Book)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.Black, Color.White, 'J'));
        }

        private void Read(IInventoryHolder holder)
        {
            var book = OpalDialog.Make<BookDialog>("Journal", "");
            OpalDialog.LendKeyboardFocus(book);
            book.Show();
        }

        private void Write(IInventoryHolder holder)
        {
            var diag = OpalDialog.Make<DialogueDialog>("Write", "This feature is currently not implemented.");
            diag.AddOption("I wholly understand and submit to the consequences.", null);
            OpalDialog.LendKeyboardFocus(diag);
            diag.Show();
        }

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();
            RegisterInventoryAction("read", (h) => Read(h), new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press, "Read journal"));
            RegisterInventoryAction("write on", (h) => Write(h), new Keybind.KeybindInfo(Keys.W, Keybind.KeypressState.Press, "Write on journal"));

            UnregisterInventoryAction("drop"); // Key item, can't be dropped.
        }
    }
}
