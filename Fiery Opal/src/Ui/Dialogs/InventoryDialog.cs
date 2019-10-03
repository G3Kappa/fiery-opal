using FieryOpal.Src.Actors;
using FieryOpal.Src.Actors.Items;
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

        public enum SortDirection
        {
            Ascending,
            Descending,
        }

        public SortMode SortingMode { get; set; } = SortMode.Category;
        public SortDirection SortingDirection { get; set; } = SortDirection.Ascending;
        public PersonalInventory Inventory { get; set; }

        protected string GroupByItem(OpalItem i) => i.ItemInfo.Name.ToString() + i.ItemInfo.Category.ToString();
        protected int SelectedIndex = 0;
        protected bool Dirty = false;

        private string BaseCaption;

        public InventoryDialog() : base()
        {
            Borderless = true;

            textSurface.DefaultBackground = Theme.FillStyle.Background = DefaultPalette["Dark"];
            BaseCaption = Caption;
            Dirty = true;
        }

        protected ContextMenu<OpalItem> MakeContextMenu()
        {
            var context_menu = Make<ContextMenu<OpalItem>>("Use Item", "", new Point(Width / 2, Height / 4));
            context_menu.Position = Position + new Point(Width / 4 + 1, Height / 3);

            var groups = GroupAndSortInventory();
            var selected_stuff = groups.ElementAtOrDefault(SelectedIndex)?.FirstOrDefault();

            if (selected_stuff == null) return null;

            var options = selected_stuff.EnumerateInventoryActions();
            foreach (var opt in options)
            {
                var key = selected_stuff.GetInventoryActionShortcut(opt);
                context_menu.AddAction(opt, (i) => { i.CallInventoryAction(opt, Inventory.Owner); }, key);
            }

            return context_menu;
        }

        protected IEnumerable<IGrouping<string, OpalItem>> GroupAndSortInventory()
        {
            IEnumerable<IGrouping<string, OpalItem>> grouped_items;
            grouped_items = Inventory.GetContents().GroupBy(GroupByItem);

            switch (SortingMode)
            {
                case SortMode.Quantity:
                    if(SortingDirection == SortDirection.Ascending)
                        grouped_items = grouped_items.OrderBy(x => x.Count()).ToList();
                    else
                        grouped_items = grouped_items.OrderByDescending(x => x.Count()).ToList();
                    break;
                case SortMode.Name:
                    if (SortingDirection == SortDirection.Ascending)
                        grouped_items = grouped_items.OrderBy(x => x.First().ItemInfo.Name.ToString()).ToList();
                    else
                        grouped_items = grouped_items.OrderByDescending(x => x.First().ItemInfo.Name.ToString()).ToList();
                    break;
                case SortMode.Category:
                    if (SortingDirection == SortDirection.Ascending)
                        grouped_items = grouped_items.OrderBy(x => x.First().ItemInfo.Category.ToString()).ToList();
                    else
                        grouped_items = grouped_items.OrderByDescending(x => x.First().ItemInfo.Category.ToString()).ToList();
                    break;
            }

            return grouped_items;
        }

        private OpalItem GetFirstSelectedItem()
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
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Tab, Keybind.KeypressState.Press, "Inventory: cycle sorting mode"), (info) =>
            {
                SortingMode = (SortMode)(((int)SortingMode + 1) % Enum.GetValues(typeof(SortMode)).Length);
                Dirty = true;
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Multiply, Keybind.KeypressState.Press, "Inventory: cycle sorting direction"), (info) =>
            {
                SortingDirection = (SortDirection)(((int)SortingDirection + 1) % Enum.GetValues(typeof(SortDirection)).Length);
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

        private int ElementsPerPage => ((Height - HEADER_HEIGHT - 3) / 2);
        // ---

        private void PrintHeader()
        {
            int lineGlyph = 196;
            Cell headerStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Dark"]);

            // Print lines
            Print(0, HEADER_HEIGHT, ((char)lineGlyph).Repeat(Width).ToColoredString(headerStyle));
        }
        private void PrintScrollbar(int group_count)
        {
            int leftRailGlyph = 221;
            int rightRailGlyph = 222;
            int cursorGlyph = 221;

            Cell railStyle = new Cell(DefaultPalette["ShadeDark"], DefaultPalette["ShadeLight"]);
            Cell cursorStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Light"]);

            VPrint(0, HEADER_HEIGHT + 1, ((char)leftRailGlyph).Repeat(Height - HEADER_HEIGHT - 2).ToColoredString(railStyle));
            VPrint(Width - 1, HEADER_HEIGHT + 1, ((char)rightRailGlyph).Repeat(Height - HEADER_HEIGHT - 2).ToColoredString(railStyle));

            if (group_count == 0) return;

            int items_per_page = (Height - 5) / 2; // 5 Cells used for layout, items are printed every other row
            int page_count = (int)(group_count / (float)items_per_page) + 1;

            int scrollbarHeight = (int)((1f / page_count) * (Height - HEADER_HEIGHT - 2));
            float nibPos = SelectedIndex / (float)group_count * ((Height - HEADER_HEIGHT - 2) - scrollbarHeight);

            VPrint(Width - 1, HEADER_HEIGHT + 1 + (int)nibPos, ((char)cursorGlyph).Repeat(scrollbarHeight).ToColoredString(cursorStyle));
        }
        private void PrintFooter()
        {
            Cell footerStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Dark"]);

            int footerGlyph = 205;
            Print(0, Height - 1, ((char)footerGlyph).Repeat(Width).ToColoredString(footerStyle));
        }
        private void PrintSingleTab(int x, int y, int width, string text, bool selected)
        {
            Cell tabStyle = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Dark"]);
            Cell labelStyle = new Cell(Palette.Ui["LCYAN"], DefaultPalette["Dark"]);

            int tlCornerGlyph = 218;
            int trCornerGlyph = 191;
            int vLineGlyph = 179;
            int blCornerGlyph = selected ? 217 : 193;
            int brCornerGlyph = selected ? 192 : 193;
            int hLineGlyph = 196;

            //text = text.PadLeft(width / 2 + text.Length / 2).PadRight(width - 2);

            if(selected)
            {
                text = text.Substring(0, text.Length - 1) + (SortingDirection == SortDirection.Ascending ? (char)30 : (char)31);
            }

            // Start with corners
            Print(x, y, tlCornerGlyph.ToColoredString(tabStyle));
            Print(x + width - 1, y, trCornerGlyph.ToColoredString(tabStyle));
            // Join them to the header
            Print(x, y + 1, vLineGlyph.ToColoredString(tabStyle));
            Print(x + width - 1, y + 1, vLineGlyph.ToColoredString(tabStyle));
            Print(x, y + 2, blCornerGlyph.ToColoredString(tabStyle));
            Print(x + width - 1, y + 2, brCornerGlyph.ToColoredString(tabStyle));
            // Join them horizontally
            Print(x + 1, y, ((char)hLineGlyph).Repeat(width - 2).ToColoredString(tabStyle));
            Print(x + 1, y + 2, (selected ? ' ' : (char)196).Repeat(width - 2).ToColoredString(tabStyle));
            // Print text
            Print(x + 1, y + 1, text.ToColoredString(selected ? labelStyle : tabStyle));

            if (selected)
            {
                //Print(x + width - 2, y + 1, downarrowGlyph.ToColoredString(labelStyle));
            }
        }

        private static string h1 = "#  ", h2 = "Name".PadRight(16), h3 = "Category".PadRight(12);
        private void PrintTabs()
        {
            PrintSingleTab(1, 0, h1.Length + 2, h1, SortingMode == SortMode.Quantity);
            PrintSingleTab(2 + h1.Length + 2, 0, h2.Length + 2, h2, SortingMode == SortMode.Name);
            PrintSingleTab(3 + h1.Length + h2.Length + 4, 0, h3.Length + 2, h3, SortingMode == SortMode.Category);
        }

        private void PrintItemsAndScrollbar()
        {
            Cell textStyleNormal = new Cell(DefaultPalette["Light"], DefaultPalette["Dark"]);
            Cell textStyleHighlighted = new Cell(DefaultPalette["Light"], Palette.Ui["CYAN"]);
            Cell tabStyle = new Cell(DefaultPalette["ShadeDark"], DefaultPalette["Dark"]);
            Cell sepStyleHighlighted = new Cell(DefaultPalette["ShadeLight"], DefaultPalette["Dark"]);
            IEnumerable<IGrouping<string, OpalItem>> grouped_items = GroupAndSortInventory();

            var sel_idx = SelectedIndex;
            if (SelectedIndex <= ElementsPerPage / 2)
            {
                sel_idx = ElementsPerPage / 2;
            }
            else if (SelectedIndex >= grouped_items.Count() - ElementsPerPage / 2 - 1)
            {
                sel_idx = grouped_items.Count() - ElementsPerPage / 2 - 1;
            }

            int selected_j = 0;
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

                if (SelectedIndex == index) selected_j = j;

                var textStyle = SelectedIndex != index ? textStyleNormal : textStyleHighlighted;
                Print(1, 4 + j, " ".Repeat(Width - 2).ToColoredString(textStyle));

                var name = group.Key;
                var items = group.ToArray();

                //Print(2, 4 + j, " ".Repeat(Width - 2).ToColoredString(textStyle));
                Print(2 + h1.Length / 2 - items.Length.ToString().Length / 2, 4 + j, items.Length.ToString().ToColoredString(textStyle));
                SetCell(2 + h1.Length + 3, 4 + j, items.First().Graphics);
                Print(2 + h1.Length + 5, 4 + j, items.First().ItemInfo.Name.Recolor(null, textStyle.Background));
                Print(2 + h1.Length + h2.Length + 6, 4 + j, items.First().ItemInfo.Category.ToString().ToColoredString(textStyle));

            }

            var sep = "|".Repeat(Height - 4).ToColoredString(tabStyle);
            var sepHighlighted = "|".Repeat(Height - 4).ToColoredString(sepStyleHighlighted);
            VPrint(3 + h1.Length, 3, new[] { SortMode.Quantity, SortMode.Name }.Contains(SortingMode) ? sepHighlighted : sep);
            VPrint(6 + h1.Length + h2.Length, 3, new[] { SortMode.Name, SortMode.Category }.Contains(SortingMode) ? sepHighlighted : sep);
            VPrint(9 + h1.Length + h2.Length + h3.Length, 3, new[] { SortMode.Category }.Contains(SortingMode) ? sepHighlighted : sep);

            SetBackground(3 + h1.Length, 4 + selected_j, textStyleHighlighted.Background);
            SetBackground(6 + h1.Length + h2.Length, 4 + selected_j, textStyleHighlighted.Background);
            SetBackground(9 + h1.Length + h2.Length + h3.Length, 4 + selected_j, textStyleHighlighted.Background);

            PrintScrollbar(grouped_items.Count());
        }
        #endregion

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
            if (!Dirty) return;

            Fill(Color.White, DefaultPalette["Dark"], ' ');
            PrintHeader();
            PrintTabs();
            PrintFooter();

            if (Inventory != null) PrintItemsAndScrollbar();

            Dirty = false;
        }
    }
}
