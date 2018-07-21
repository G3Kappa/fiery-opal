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
    public class Book : OpalItem
    {
        public const int DEFAULT_WIDTH = 40, DEFAULT_HEIGHT = 50;

        public bool EnableWordWrap { get; set; } = true;
        public int TabSize { get; set; } = 4;

        public struct Page
        {
            public List<ColoredString> Lines { get; set; }
            public Cell DefaultTextAppearence { get; set; }
        }

        public List<Page> Pages = new List<Page>();

        public Book(string title="Book") : base(title.ToColoredString(), ItemCategory.Book)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.Black, Color.White, 'B'));
        }

        private void WriteInternal(ColoredString str, int page = -1, Point? cursorPos = null, bool append = true)
        {
            Point c = cursorPos ?? Point.Zero;

            while (Pages.Count <= page)
            {
                Pages.Add(new Page()
                {
                    Lines = new List<ColoredString>(),
                    DefaultTextAppearence = new Cell()
                });
            }

            while (Pages[page].Lines.Count <= c.Y)
            {
                Pages[page].Lines.Add(new ColoredString(""));
            }

            if(c.Y + (str.Count / DEFAULT_WIDTH) >= DEFAULT_HEIGHT)
            {
                WriteInternal(str, page + 1, Point.Zero, append);
                return;
            }

            if (str.Count == 0) return;

            var min = Math.Min(str.Count, DEFAULT_WIDTH);
            int lastSpace = EnableWordWrap && min == DEFAULT_WIDTH ? str.String.Substring(0, min).LastIndexOf(' ') + 1 : min;
            if (lastSpace == 0) lastSpace = min;

            for (int j = 0; j < str.String.Length; ++j)
            {
                char ch = str[j].GlyphCharacter;
                switch (ch)
                {
                    case '\n':
                        WriteInternal(str.SubString(j + 1, str.Count - j - 1), page, new Point(c.X, c.Y + 1), append);
                        return;
                    case '\r':
                        WriteInternal(str.SubString(j + 1, str.Count - j - 1), page, new Point(0, c.Y), append);
                        return;
                    case '\t':
                        WriteInternal(" ".Repeat(TabSize).ToColoredString() + str.SubString(j + 1, str.Count - j - 1), page, new Point(0, c.Y), append);
                        return;
                }

                var p = Pages[page].Lines[c.Y];
                var s = new ColoredString(new[] { str[j] });

                if (j >= lastSpace)
                {
                    WriteInternal(str.SubString(j, str.Count - j), page, new Point(0, c.Y + 1), append);
                    return;
                }

                p = Pages[page].Lines[c.Y] = p.Insert(str[j], c.X, append);

                if (++c.X >= DEFAULT_WIDTH)
                {
                    c.X = 0;
                    c.Y++;
                }
            }
        }

        public void Write(ColoredString str, int page = -1, Point? cursorPos = null, bool append=true)
        {
            if (str.Count == 0) return;

            if (page < 0)
            {
                if (Pages.Count > 0) page = Pages.Count - 1;
                else page = 0;

            }

            while (Pages.Count <= page)
            {
                Pages.Add(new Page()
                {
                    Lines = new List<ColoredString>(),
                    DefaultTextAppearence = new Cell()
                });
            }

            Point c = cursorPos ?? new Point();
            if(!cursorPos.HasValue)
            {
                if(Pages[page].Lines.Count == 0)
                {
                    Pages[page].Lines.Add(new ColoredString(""));
                }
                c.Y = Pages[page].Lines.Count - 1;
                if (Pages[page].Lines.Last().Count == 0)
                {
                }
                c.X = Pages[page].Lines.Last().Count;
            }

            if(c.Y >= DEFAULT_HEIGHT)
            {
                Write(str, page + 1);
                return;
            }

            WriteInternal(str, page, c, append);
        }

        public virtual void OnRead(IInventoryHolder holder)
        {
            var book = OpalDialog.Make<BookDialog>(Name, "", new Point(DEFAULT_WIDTH * 2 + 17, DEFAULT_HEIGHT + 12), Nexus.Fonts.Spritesheets["Books"]);
            book.SetData(this);
            OpalDialog.LendKeyboardFocus(book);
            book.Show();
        }

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();
            RegisterInventoryAction("read", (h) => OnRead(h), new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press, "Read journal"));
        }
    }
}
