using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace FieryOpal.Src.Ui.Dialogs
{
    public class BookDialog : OpalDialog
    {
        protected string Text;
        public string Contents => Text;

        protected List<string> Pages { get; } = new List<string>();

        private int current_page = 0;
        public int CurrentPage
        {
            get => current_page;
            set
            {
                current_page = value;
                Dirty = true;
            }
        }

        private int bookmarked_page = -1;
        public int BookmarkedPage
        {
            get => bookmarked_page;
            set
            {
                if (bookmarked_page == value)
                {
                    bookmarked_page = -1;
                }
                else
                {
                    bookmarked_page = value;
                }

                Dirty = true;
            }
        }

        private bool Dirty = false;

        new protected static Palette DefaultPalette = new Palette(
            new[] {
                new Tuple<string, Color>("CoverCornerForeground", new Color(255, 191, 0)),
                new Tuple<string, Color>("CoverCornerBackground", new Color(140, 70, 0)),

                new Tuple<string, Color>("CoverBorderForeground", new Color(140, 35, 0)),
                new Tuple<string, Color>("CoverBorderBackground", new Color(140, 70, 0)),

                new Tuple<string, Color>("HiddenPagesForeground", new Color(158, 134, 100)),
                new Tuple<string, Color>("HiddenPagesBackground", new Color(191, 171, 143)),
                new Tuple<string, Color>("RidgeBackground", new Color(207, 196, 168)),
                new Tuple<string, Color>("RidgeForeground", new Color(203, 188, 150)),
                new Tuple<string, Color>("CurrentPagesForeground", new Color(168, 144, 110)),
                new Tuple<string, Color>("CurrentPagesBackground", new Color(221, 221, 195)),

                new Tuple<string, Color>("BookmarkForeground", new Color(186, 17, 45)),
                new Tuple<string, Color>("BookmarkBackground", new Color(166, 17, 45)),
            }
        );

        private void PrintFrame(int w, int h, bool bookmark = false)
        {
            int cornerGlyph = 4; // ♦
            int hBorderGlyph = 196; // ─
            int vBorderGlyph = 179; // │
            int downwardsTGlyph = 194; // ┬
            int upwardsTGlyph = 193; // ┴

            int dblTLCorner = 201;
            int dblTRCorner = 187;
            int dblBLCorner = 200;
            int dblBRCorner = 188;
            int hPagesGlyph = 205;
            int vPagesGlyph = 186;
            int doubleDownwardsTGlyph = 203;
            int doubleUpwardsTGlyph = 202;

            int bookmarkGlyph = 222;
            int bookmarkEndGlyph = 171;
            int bookmarkStartGlyph = 172;

            int ridgeGlyph = 222;


            var fg = Color.Magenta;
            var bg = Color.Magenta;
            for (int y = 0; y < h - 2; ++y)
            {

                if (y <= 0 || y >= h - 3)
                {
                    fg = DefaultPalette["CoverCornerForeground"];
                    bg = DefaultPalette["CoverCornerBackground"];
                }
                else
                {
                    fg = DefaultPalette["CoverBorderForeground"];
                    bg = DefaultPalette["CoverBorderBackground"];
                }

                Print(0, y + 1, ((char)vBorderGlyph).ToString(), fg, bg);
                Print(w - 1, y + 1, ((char)vBorderGlyph).ToString(), fg, bg);

                fg = DefaultPalette["CurrentPagesForeground"];
                bg = DefaultPalette["CurrentPagesBackground"];
                Print(2, y + 2, " ".Repeat(w - 2), fg, bg);

                fg = DefaultPalette["HiddenPagesForeground"];
                bg = DefaultPalette["HiddenPagesBackground"];
                Print(1, y + 2, ((char)vPagesGlyph).ToString(), fg, bg);
                Print(w - 2, y + 2, ((char)vPagesGlyph).ToString(), fg, bg);

                fg = DefaultPalette["RidgeForeground"];
                bg = DefaultPalette["RidgeBackground"];
                Print(w / 2, y + 2, ((char)ridgeGlyph).ToString(), fg, bg);

                if (!bookmark) continue;

                fg = DefaultPalette["BookmarkForeground"];
                bg = DefaultPalette["BookmarkBackground"];
                Print(w / 2, y + 2, ((char)bookmarkGlyph).ToString(), fg, bg);
            }

            for (int x = 0; x < w; ++x)
            {
                int glyph = 2;

                if (x == 0 || x == w - 1)
                {
                    glyph = cornerGlyph;
                }
                else if (x == w / 2) glyph = downwardsTGlyph;
                else glyph = hBorderGlyph;

                if (x <= 1 || x >= w - 2)
                {
                    fg = DefaultPalette["CoverCornerForeground"];
                    bg = DefaultPalette["CoverCornerBackground"];
                }
                else
                {
                    fg = DefaultPalette["CoverBorderForeground"];
                    bg = DefaultPalette["CoverBorderBackground"];
                }

                Print(x, 0, ((char)glyph).ToString(), fg, bg);

                if (glyph == downwardsTGlyph) glyph = upwardsTGlyph;

                Print(x, h - 1, ((char)glyph).ToString(), fg, bg);

                fg = DefaultPalette["HiddenPagesForeground"];
                bg = DefaultPalette["HiddenPagesBackground"];
                if (x >= 1 && x <= w - 2)
                {
                    if (x == 1) glyph = dblTLCorner;
                    else if (x == w - 2) glyph = dblTRCorner;
                    else if (x == w / 2) glyph = doubleDownwardsTGlyph;
                    else glyph = hPagesGlyph;

                    Print(x, 1, ((char)glyph).ToString(), fg, bg);

                    if (glyph == doubleDownwardsTGlyph) glyph = doubleUpwardsTGlyph;
                    else if (glyph == dblTLCorner) glyph = dblBLCorner;
                    else if (glyph == dblTRCorner) glyph = dblBRCorner;

                    Print(x, h - 2, ((char)glyph).ToString(), fg, bg);
                }
            }

            if (!bookmark) return;

            fg = DefaultPalette["BookmarkBackground"];
            Print(w / 2, h - 2, ((char)bookmarkEndGlyph).ToString(), fg, null);
            Print(w / 2, 1, ((char)bookmarkStartGlyph).ToString(), fg, null);
        }

        SadConsole.Console LeftPage, RightPage;

        public BookDialog()
            : base()
        {
            Borderless = true;
            CurrentPage = 0;

            PrintFrame(Width, Height);

            LeftPage = new SadConsole.Console((Width - 11) / 2, Height - 8);
            LeftPage.Position = new Point(5, 5);
            RightPage = new SadConsole.Console((Width - 11) / 2, Height - 8);
            RightPage.Position = new Point(8 + (Width - 11) / 2 + (1 - Height % 2), 5);
        }

        public override void Draw(TimeSpan delta)
        {
            if (Dirty)
            {
                Clear();
                PrintFrame(Width, Height, BookmarkedPage == CurrentPage);
                Impaginate();
                FlipToPage(CurrentPage);
                Dirty = false;
            }

            base.Draw(delta);
            LeftPage.Draw(delta);
            RightPage.Draw(delta);
        }

        public override void Show(bool modal)
        {
            LeftPage.Position += Position;
            RightPage.Position += Position;
            FlipToPage(0);
            base.Show(modal);
        }

        public void Impaginate()
        {
            Pages.Clear();

            var page_size = LeftPage.Width * (LeftPage.Height - 2);
            var len = 0;
            while (len < Text.Length)
            {
                Pages.Add(Text.Substring(len, Math.Min(page_size, Text.Length - len)));
                len += page_size;
            }
        }

        public void Write(string str)
        {
            Text += str;
        }

        public void EndPage()
        {
            Impaginate();
            int spaces_needed = LeftPage.Width * (LeftPage.Height - 2) - Pages[Pages.Count - 1].Length;

            // In this case we want to leave a blank page
            if (spaces_needed == 0) spaces_needed = LeftPage.Width * (LeftPage.Height - 2);

            Write(' '.Repeat(spaces_needed));
        }

        public void NewLine()
        {
            Impaginate();
            Write(' '.Repeat(LeftPage.Width - Pages[Pages.Count - 1].Length % LeftPage.Width));
        }

        public void FlipToPage(int page)
        {
            if (page < 0 || Pages.Count <= page) return;
            CurrentPage = page;

            string l_pg = String.Format("{0}/{1}", CurrentPage + 1, Pages.Count);
            string r_pg = String.Format("{0}/{1}", CurrentPage + 2, Pages.Count);

            LeftPage.Fill(DefaultPalette["CurrentPagesForeground"], DefaultPalette["CurrentPagesBackground"], ' ');
            RightPage.Fill(DefaultPalette["CurrentPagesForeground"], DefaultPalette["CurrentPagesBackground"], ' ');

            Print(3, LeftPage.Position.Y + LeftPage.Height - 2, l_pg);
            if (CurrentPage + 2 <= Pages.Count)
            {
                Print(RightPage.Position.X + RightPage.Width - r_pg.Length - 1, RightPage.Position.Y + RightPage.Height - 2, r_pg);
            }
            else
            {
                Print(RightPage.Position.X + RightPage.Width - r_pg.Length - 1, RightPage.Position.Y + RightPage.Height - 2, " ".Repeat(r_pg.Length));
            }

            LeftPage.Print(0, 0, Pages[page]);
            if (Pages.Count <= page + 1) return;
            RightPage.Print(0, 0, Pages[page + 1]);

            Dirty = true;
        }

        protected override void PrintText(string text)
        {
            //base.PrintText(text);
            Write(text);
            Dirty = true;
        }

        protected override void BindKeys()
        {
            base.BindKeys();

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Left, Keybind.KeypressState.Press, "Book: previous page"), (info) => FlipToPage(CurrentPage - 2));
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Right, Keybind.KeypressState.Press, "Book: next page"), (info) => FlipToPage(CurrentPage + 2));
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.B, Keybind.KeypressState.Press, "Book: bookmark this page", ctrl: true), (info) => BookmarkedPage = CurrentPage);
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.B, Keybind.KeypressState.Press, "Book: jump to bookmark"), (info) => FlipToPage(BookmarkedPage));
        }

        public override void Hide()
        {
            if (LeftPage != null) LeftPage.Position -= Position;
            if (RightPage != null) RightPage.Position -= Position;
            base.Hide();
        }
    }
}
