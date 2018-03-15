using FieryOpal.Src.Actors;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui.Dialogs
{
    class InventoryDialog : OpalDialog
    {
        // How the items are sorted after being grouped together
        public enum SortMode
        {
            Quantity,
            Name,
            Category
        }

        public SortMode SortingMode { get; set; } = SortMode.Quantity;
        public PersonalInventory Inventory { get; set; }

        protected string GroupByItem(Item i) => i.ItemInfo.Name.ToString() + i.ItemInfo.Category.ToString();
        protected int SelectedIndex = 0;
        protected bool Dirty = false;

        public InventoryDialog() : base()
        {
            Borderless = true;

            textSurface.DefaultBackground = Theme.FillStyle.Background = DefaultPalette["Dark"];

            Dirty = true;
        }

        protected ContextMenu<Item> MakeContextMenu()
        {
            var context_menu = Make<ContextMenu<Item>>("Use Item", "", new Point(Width / 2, Height / 4));
            context_menu.Position = Position + new Point(Width / 4 + 1, Height / 3);

            var groups = GroupAndSortInventory();
            var selected_stuff = groups.ElementAtOrDefault(SelectedIndex)?.FirstOrDefault();

            if (selected_stuff == null) return null;

            var options = selected_stuff.EnumerateInventoryActions();
            foreach(var opt in options)
            {
                var key = selected_stuff.GetInventoryActionShortcut(opt);
                context_menu.AddAction(opt, (i) => { i.CallInventoryAction(opt, Inventory.Owner); }, key);
            }

            return context_menu;
        }

        protected IEnumerable<IGrouping<string, Item>> GroupAndSortInventory()
        {
            IEnumerable<IGrouping<string, Item>> grouped_items;
            grouped_items = Inventory.GetContents().GroupBy(GroupByItem);

            switch (SortingMode)
            {
                case SortMode.Quantity:
                    grouped_items = grouped_items.OrderBy(x => -x.Count()).ToList();
                    break;
                case SortMode.Name:
                    grouped_items = grouped_items.OrderBy(x => x.First().ItemInfo.Name.ToString()).ToList();
                    break;
                case SortMode.Category:
                    grouped_items = grouped_items.OrderBy(x => x.First().ItemInfo.Category.ToString()).ToList();
                    break;
            }

            return grouped_items;
        }

        private Item GetFirstSelectedItem()
        {
            var grouped = GroupAndSortInventory();

            return grouped.ElementAt(SelectedIndex).FirstOrDefault();
        }

        protected override void BindKeys()
        {
            base.BindKeys();

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Up, Keybind.KeypressState.Press, "Inventory: previous group"), (info) =>
            {
                SelectedIndex = SelectedIndex = Math.Max(0, --SelectedIndex);
                Dirty = true;
            });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Down, Keybind.KeypressState.Press, "Inventory: next group"), (info) =>
            {
                SelectedIndex = Math.Min(Inventory.GetContents().GroupBy(GroupByItem).Count() - 1, ++SelectedIndex);
                Dirty = true;
            });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Tab, Keybind.KeypressState.Press, "Inventory: change sorting mode"), (info) =>
            {
                SortingMode = (SortMode)(((int)SortingMode + 1) % Enum.GetValues(typeof(SortMode)).Length);
                Dirty = true;
            });
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Enter, Keybind.KeypressState.Press, "Inventory: open context menu for group"), (info) =>
            {
                if (Inventory.Count <= 0) return;

                var context_menu = MakeContextMenu();
                LendKeyboardFocus(context_menu);
                context_menu.Show();
                context_menu.Closed += (e, eh) =>
                {
                    context_menu.ChosenAction?.Invoke(GetFirstSelectedItem());
                    Dirty = true;
                };
                Dirty = true;
            });
        }

        protected override void PrintText(string text)
        {
            return;
        }

        #region Graphics

        // ---
        const int HEADER_HEIGHT = 2;

        const float COL1_WIDTH = .2f;
        const float COL2_WIDTH = .5f;
        const float COL3_WIDTH = .3f;

        private int Col1Width => (int)((Width - 2) * COL1_WIDTH);
        private int Col2Width => (int)((Width - 2) * COL2_WIDTH);
        private int Col3Width => (int)((Width - 2) * COL3_WIDTH);

        private int ElementsPerPage => ((Height - HEADER_HEIGHT - 3) / 2);
        // ---

        private void PrintHeader()
        {
            int tlCornerGlyph = 218;
            int trCornerGlyph = 191;
            int blCornerGlyph = 192;
            int brCornerGlyph = 217;

            int lineGlyph = 196;

            Cell headerStyle = new Cell(DefaultPalette["Light"], DefaultPalette["Dark"]);

            // Print corners
            Print(0, HEADER_HEIGHT, tlCornerGlyph.ToColoredString(headerStyle));
            Print(0, HEADER_HEIGHT + 1, blCornerGlyph.ToColoredString(headerStyle));
            Print(Width - 1, HEADER_HEIGHT, trCornerGlyph.ToColoredString(headerStyle));
            Print(Width - 1, HEADER_HEIGHT + 1, brCornerGlyph.ToColoredString(headerStyle));
            // Print lines
            Print(1, HEADER_HEIGHT, ((char)lineGlyph).Repeat(Width - 2).ToColoredString(headerStyle));
            Print(1, HEADER_HEIGHT + 1, ((char)lineGlyph).Repeat(Width - 2).ToColoredString(headerStyle));
        }
        private void PrintScrollbar(int group_count)
        {
            int leftRailGlyph = 221;
            int rightRailGlyph = 222;
            int cursorGlyph = 221;

            Cell railStyle = new Cell(DefaultPalette["ShadeDark"], DefaultPalette["Dark"]);
            Cell cursorStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Light"]);

            VPrint(0, HEADER_HEIGHT + 2, ((char)leftRailGlyph).Repeat(Height - HEADER_HEIGHT - 3).ToColoredString(railStyle));
            VPrint(Width - 1, HEADER_HEIGHT + 2, ((char)rightRailGlyph).Repeat(Height - HEADER_HEIGHT - 3).ToColoredString(railStyle));

            if (group_count == 0) return;

            int items_per_page = (Height - 5) / 2; // 5 Cells used for layout, items are printed every other row
            int page_count = (int)(group_count / (float)items_per_page) + 1;

            int scrollbarHeight = (int)((1f / page_count) * (Height - HEADER_HEIGHT - 3));
            float nibPos = SelectedIndex / (float)group_count * ((Height - HEADER_HEIGHT - 2) - scrollbarHeight);

            VPrint(Width - 1, HEADER_HEIGHT + 2 + (int)nibPos, ((char)cursorGlyph).Repeat(scrollbarHeight).ToColoredString(cursorStyle));
        }
        private void PrintFooter()
        {
            Cell footerStyle = new Cell(DefaultPalette["Light"], DefaultPalette["Dark"]);

            int footerGlyph = 205;
            Print(0, Height - 1, ((char)footerGlyph).Repeat(Width).ToColoredString(footerStyle));
        }
        private void PrintSingleTab(int x, int y, int width, string text, bool make_it_double)
        {
            Cell tabStyle = new Cell(DefaultPalette["Light"], DefaultPalette["Dark"]);

            int tlCornerGlyph = make_it_double ? 201 : 218;
            int trCornerGlyph = make_it_double ? 187 : 191;
            int vLineGlyph = make_it_double ? 186 : 179;
            int upwardsTGlyph = make_it_double ? 208 : 193;
            int hLineGlyph = make_it_double ? 205 : 196;
            int downarrowGlyph = 31;

            // Start with corners
            Print(x, y, tlCornerGlyph.ToColoredString(tabStyle));
            Print(x + width - 1, y, trCornerGlyph.ToColoredString(tabStyle));
            // Join them to the header
            Print(x, y + 1, vLineGlyph.ToColoredString(tabStyle));
            Print(x + width - 1, y + 1, vLineGlyph.ToColoredString(tabStyle));
            Print(x, y + 2, upwardsTGlyph.ToColoredString(tabStyle));
            Print(x + width - 1, y + 2, upwardsTGlyph.ToColoredString(tabStyle));
            // Join them horizontally
            Print(x + 1, y, ((char)hLineGlyph).Repeat(width - 2).ToColoredString(tabStyle));
            // Print text
            Print(x + (width / 2 - text.Length / 2), y + 1, text.ToColoredString(tabStyle));

            if (make_it_double)
            {
                Print(x + width - 2, y + 1, downarrowGlyph.ToColoredString(tabStyle));
            }
        }
        private void PrintTabs()
        {
            PrintSingleTab(1, 0, Col1Width, "QTY#", SortingMode == SortMode.Quantity);
            PrintSingleTab(1 + Col1Width, 0, Col2Width, "Item Name", SortingMode == SortMode.Name);
            PrintSingleTab(1 + Col1Width + Col2Width, 0, Col3Width, "Category", SortingMode == SortMode.Category);
        }
        private void PrintSlots()
        {
            Cell slotStyle = new Cell(DefaultPalette["ShadeDark"], DefaultPalette["Dark"]);

            int hLineGlyph = 196;
            var line = ((char)hLineGlyph).Repeat(Width - 4);
            line = line.Remove(Col1Width - 3, 4);
            line = line.Insert(Col1Width - 3, "    ");
            line = line.Remove(Col2Width + Col1Width - 3, 4);
            line = line.Insert(Col2Width + Col1Width - 3, "    ");
            line = " " + line.Remove(0, 1);
            line = line.Remove(line.Length - 1, 1) + " ";

            for (int i = 0; i < Height - 5; i += 2)
            {
                Print(2, 4 + i, line.ToColoredString(slotStyle));
            }
        }
        private void PrintItemsAndScrollbar()
        {
            Cell textStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Dark"]);
            IEnumerable<IGrouping<string, Item>> grouped_items = GroupAndSortInventory();

            var sel_idx = SelectedIndex;
            if (SelectedIndex <= ElementsPerPage / 2)
            {
                sel_idx = ElementsPerPage / 2;
            }
            else if (SelectedIndex >= grouped_items.Count() - ElementsPerPage / 2 - 1)
            {
                sel_idx = grouped_items.Count() - ElementsPerPage / 2 - 1;
            }

            for (int j = 0; j < ElementsPerPage; ++j)
            {
                var index = sel_idx + (j - ElementsPerPage / 2);
                var group = grouped_items.ElementAtOrDefault(index);

                if (group == null)
                {
                    if (j < ElementsPerPage / 2)
                    {
                        continue;
                    }
                    else
                        break;
                }

                var name = group.Key;
                var items = group.ToArray();

                Print(Col1Width / 2 - items.Length.ToString().Length / 2, 5 + 2 * j, items.Length.ToString().PadLeft(2, '0').ToColoredString(textStyle));
                SetCell(Col1Width + 3, 5 + 2 * j, items.First().Graphics);
                Print(Col1Width + 5, 5 + 2 * j, items.First().ItemInfo.Name);
                Print(Col1Width + Col2Width + 3, 5 + 2 * j, items.First().ItemInfo.Category.ToString().ToColoredString(textStyle));

                if (SelectedIndex == index)
                {
                    Print(1, 5 + 2 * j, ((char)26).ToString().ToColoredString());
                }
            }

            PrintScrollbar(grouped_items.Count());
        }
        #endregion

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
            if (!Dirty) return;

            Clear();
            PrintHeader();
            PrintTabs();
            PrintFooter();
            PrintSlots();

            if(Inventory != null) PrintItemsAndScrollbar();

            Dirty = false;
        }
    }
}
