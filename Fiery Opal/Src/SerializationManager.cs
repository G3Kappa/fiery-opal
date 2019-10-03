using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src
{
    [Serializable]
    public struct SerializablePoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public SerializablePoint(int x, int y)
        {
            X = x; Y = y;
        }

        public static implicit operator SerializablePoint(Point p)
        {
            return new SerializablePoint(p.X, p.Y);
        }

        public static implicit operator Point(SerializablePoint p)
        {
            return new Point(p.X, p.Y);
        }
    }

    [Serializable]
    public class SerializableCell
    {
        public uint Foreground { get; set; }
        public uint Background { get; set; }
        public int Glyph { get; set; }
        public bool IsVisible { get; set; }

        public SerializableCell(Cell c)
        {
            if(c == null)
            {
                Foreground = Background = 0;
                Glyph = 0;
                IsVisible = false;
                return;
            }

            Foreground = c.Foreground.PackedValue;
            Background = c.Background.PackedValue;
            Glyph = c.Glyph;
            IsVisible = c.IsVisible;
        }

        public static implicit operator SerializableCell(Cell c)
        {
            return new SerializableCell(c);
        }

        public static implicit operator Cell(SerializableCell c)
        {
            return new Cell(new Color(c.Foreground), new Color(c.Background), c.Glyph)
            {
                IsVisible = c.IsVisible
            };
        }
    }

    public class SerializationManager
    {
        public SerializationManager()
        {
        }

        public void SaveState(object obj, string path)
        {
            Stream stream = File.Open(path, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Close();
        }

        public object LoadState(string path)
        {
            Stream stream = File.Open(path, FileMode.Open);
            BinaryFormatter formatter = new BinaryFormatter();

            object state = formatter.Deserialize(stream);
            stream.Close();

            return state;
        }
    }
}
