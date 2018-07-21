using FieryOpal.Src.Actors.Items;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui.Dialogs
{
    public class BookDialog : OpalDialog
    {
        public Book Data { get; private set; }

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

        public BookDialog()
            : base()
        {
            Borderless = true;
            Dirty = true;
            CurrentPage = 0;
            VirtualCursor.DisableWordBreak = true; // Done internally
            VirtualCursor.PrintAppearance = new Cell(Palette.Ui["BLACK"], DefaultPalette["CurrentPagesBackground"]);
        }

        public void SetData(Book b)
        {
            Data = b;
            foreach(var p in Data.Pages)
            {
                for (int i = 0; i < p.Lines.Count; i++)
                {
                    p.Lines[i] = p.Lines[i].Recolor(null, DefaultPalette["CurrentPagesBackground"], true);
                }
            }
        }

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
            int bookmarkEndGlyph = 172;
            int bookmarkStartGlyph = 171;

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

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
            if (Dirty)
            {
                Dirty = false;

                Clear();
                PrintFrame(Width, Height, BookmarkedPage == CurrentPage);

                if (Data == null) return;

                // Draw left page
                if (Data.Pages.Count <= CurrentPage) return;

                VirtualCursor.Position = new Point(7, 6);
                foreach (var l in Data.Pages[CurrentPage].Lines)
                {
                    VirtualCursor.Print(l);
                    VirtualCursor.Position = new Point(7, VirtualCursor.Position.Y + 1);
                }

                var cp = "{0}".Fmt(CurrentPage);
                VirtualCursor.Position = new Point(3, 3);
                VirtualCursor.Print(
                    cp.ToColoredString(
                        DefaultPalette["CurrentPagesForeground"], 
                        DefaultPalette["CurrentPagesBackground"]
                    )
                );

                // Draw right page
                if (Data.Pages.Count <= CurrentPage + 1) return;

                VirtualCursor.Position = new Point(Width / 2 + 6, 6);
                foreach (var l in Data.Pages[CurrentPage + 1].Lines)
                {
                    VirtualCursor.Print(l);
                    VirtualCursor.Position = new Point(Width / 2 + 6, VirtualCursor.Position.Y + 1);
                }

                var np = "{0}".Fmt(CurrentPage + 1);
                VirtualCursor.Position = new Point(Width - 3 - np.Length, 3);
                VirtualCursor.Print(
                    np.ToColoredString(
                        DefaultPalette["CurrentPagesForeground"],
                        DefaultPalette["CurrentPagesBackground"]
                    )
                );
            }
        }

        public override void Show(bool modal)
        {
            base.Show(modal);
        }


        protected override void BindKeys()
        {
            base.BindKeys();

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.PageDown, Keybind.KeypressState.Press, "Book: next page"), (info) =>
            {
                CurrentPage = CurrentPage + 2 < (Data?.Pages.Count ?? 0) ? CurrentPage + 2 : CurrentPage;
                Dirty = true;
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.PageUp, Keybind.KeypressState.Press, "Book: previous page"), (info) =>
            {
                CurrentPage = CurrentPage - 2 >= 0 ? CurrentPage - 2 : CurrentPage;
                Dirty = true;
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.B, Keybind.KeypressState.Press, "Book: set bookmark", true), (info) =>
            {
                if (BookmarkedPage != CurrentPage) BookmarkedPage = CurrentPage;
                else BookmarkedPage = -1;
                Dirty = true;
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.B, Keybind.KeypressState.Press, "Book: flip to bookmark", false, true), (info) =>
            {
                if (BookmarkedPage > 0)
                {
                    CurrentPage = BookmarkedPage;
                    Dirty = true;
                }
            });
        }

        public override void Hide()
        {
            base.Hide();
        }
    }
}
