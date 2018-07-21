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
    public class Journal : Book
    {
        public Journal() : base("Journal")
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.Black, Color.White, 'J'));
        }

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();
            UnregisterInventoryAction("drop"); // Key item
        }
    }

}
