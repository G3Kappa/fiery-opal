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
        public static int DEFAULT_WIDTH = 27, DEFAULT_HEIGHT = 29;

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

        private IEnumerable<ColoredString> Chunks(ColoredString s)
        {
            List<ColoredGlyph> glyphs = new List<ColoredGlyph>();
            for (int j = 0; j < s.String.Length; ++j)
            {
                glyphs.Add(s[j]);
                if(glyphs.Count == DEFAULT_WIDTH)
                {
                    yield return new ColoredString(glyphs.ToArray());
                    glyphs.Clear();
                }
            }
        }


        private Point virtualCursor;
        public Point VirtualCursor => virtualCursor;

        private void WriteInternal(ColoredString str, int page = -1, Point? cursorPos = null, bool append = true)
        {
            virtualCursor = cursorPos ?? Point.Zero;

            if ((str.Count / DEFAULT_WIDTH) > 0)
            {
                foreach(var chunk in Chunks(str))
                {
                    if (virtualCursor.Y + (chunk.Count / DEFAULT_WIDTH) > DEFAULT_HEIGHT)
                    {
                        page++;
                        virtualCursor.X = 0;
                        virtualCursor.Y = 0;
                    }
                    WriteInternal(chunk, page, virtualCursor, append);
                    virtualCursor.Y++;
                }
                return;
            }

            while (Pages.Count <= page)
            {
                Pages.Add(new Page()
                {
                    Lines = new List<ColoredString>(),
                    DefaultTextAppearence = new Cell()
                });
            }

            while (Pages[page].Lines.Count <= virtualCursor.Y)
            {
                Pages[page].Lines.Add(new ColoredString(""));
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
                        WriteInternal(str.SubString(j + 1, str.Count - j - 1), page, new Point(virtualCursor.X, virtualCursor.Y + 1), append);
                        return;
                    case '\r':
                        WriteInternal(str.SubString(j + 1, str.Count - j - 1), page, new Point(0, virtualCursor.Y), append);
                        return;
                    case '\t':
                        WriteInternal(str.SubString(j + 1, str.Count - j - 1) + " ".Repeat(TabSize).ToColoredString(str[j].Foreground, str[j].Background), page, new Point(virtualCursor.X, virtualCursor.Y), append);
                        return;
                    case '\b':
                        var oldLine = Pages[page].Lines[virtualCursor.Y];

                        if(oldLine.Count > 0)
                        {
                            Pages[page].Lines[virtualCursor.Y] = oldLine.SubString(0, oldLine.Count - 1);
                            virtualCursor.X--;
                        }
                        else if(virtualCursor.Y > 0)
                        {
                            Pages[page].Lines.RemoveAt(virtualCursor.Y--);
                            oldLine = Pages[page].Lines[virtualCursor.Y];
                            virtualCursor.X = oldLine.Count % DEFAULT_WIDTH;
                            virtualCursor.Y += oldLine.Count / DEFAULT_WIDTH;
                            // todo can't backspace into full previous line
                        }
                        continue;
                }

                var p = Pages[page].Lines[virtualCursor.Y];

                if (j >= lastSpace)
                {
                    WriteInternal(str.SubString(j, str.Count - j), page, new Point(0, virtualCursor.Y + 1), append);
                    return;
                }

                if(virtualCursor.X > p.Count)
                {
                    p = p + " ".Repeat(virtualCursor.X - p.Count).ToColoredString(Color.Transparent, Color.Transparent);
                }

                p = Pages[page].Lines[virtualCursor.Y] = p.Insert(str[j], virtualCursor.X, append);

                if (++virtualCursor.X >= DEFAULT_WIDTH)
                {
                    virtualCursor.X = 0;
                    virtualCursor.Y++;
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
                c.Y = (Pages[page].Lines.Count - 1);
                c.X = Pages[page].Lines[c.Y].Count;
                if(c.X >= DEFAULT_WIDTH)
                {
                    c.Y += c.X / DEFAULT_WIDTH;
                    c.X %= DEFAULT_WIDTH;
                }
            }

            if (c.Y >= DEFAULT_HEIGHT)
            {
                Write(str, page + 1);
                return;
            }

            WriteInternal(str, page, c, append);
        }

        public virtual void OnRead(IInventoryHolder holder)
        {
            var book = OpalDialog.Make<BookDialog>(Name, "", new Point(-1, -1), Nexus.Fonts.Spritesheets["Books"], true);
            book.SetData(this);
            OpalDialog.LendKeyboardFocus(book);
            book.Show();
        }

        public virtual void OnWrite(IInventoryHolder holder)
        {
            var book = OpalDialog.Make<BookDialog>(Name, "", new Point(-1, -1), Nexus.Fonts.Spritesheets["Books"], true);
            book.SetData(this);
            book.WriteMode = true;
            OpalDialog.LendKeyboardFocus(book);
            book.Show();
        }

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();
            RegisterInventoryAction("read", (h) => OnRead(h), new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press, "Read journal"));
            RegisterInventoryAction("write on", (h) => OnWrite(h), new Keybind.KeybindInfo(Keys.W, Keybind.KeypressState.Press, "Write on journal"));
        }
    }
}
