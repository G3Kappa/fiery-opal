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

            Nexus.Quests.GetActiveQuests().ForEach(q =>
            {
                book.Write(q.Name);
                book.NewLine();
                book.Write(q.Descrption);
                book.NewLine();
                foreach (var o in q.GetObjectives())
                {
                    book.Write("  ".ToColoredString() + o.Descrption);
                    book.NewLine();
                }
            });

            OpalDialog.LendKeyboardFocus(book);
            book.Show();
        }

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();
            RegisterInventoryAction("read", (h) => Read(h), new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press, "Read journal"));

            UnregisterInventoryAction("drop"); // Key item, can't be dropped.
        }
    }
}
