using Microsoft.Xna.Framework;
using SadConsole;

namespace FieryOpal.Src.Ui
{
    /// <summary>
    /// Defines the properties for the "viewmodel" of a given weapon.
    /// These include the sprite's scale, offset and color, which are then
    /// rendered on top of a RaycastViewport. Additionally, each weapon
    /// may use more than one sprite in order to display state.
    /// </summary>
    public class WeaponViewSprite
    {
        public Font Spritesheet { get; }

        /// <summary>
        /// The Spritesheet index of the current sprite for this weapon.
        /// </summary>
        public byte SpritesheetIndex { get; set; }

        /// <summary>
        /// An X,Y vector that displaces the viewmodel relative to its centered position.
        /// </summary>
        public Vector2 Offset { get; set; }
        /// <summary>
        /// An X,Y vector that scales the viewmodel relative to its centered position.
        /// </summary>
        public Vector2 Scale { get; set; }

        /// <summary>
        /// An RGB color that is multiplied onto the grayscale sprite, leaving any non-grey pixels untouched.
        /// </summary>
        public Color Color { get; set; }

        public WeaponViewSprite()
        {
            Spritesheet = Nexus.Fonts.Spritesheets["Weapons"];
        }

        public void DrawOnto(SadConsole.Console surface)
        {
            var font = Nexus.Fonts.Spritesheets["Weapons"];

            Color[,] pixels = FontTextureCache.GetRecoloredPixels(
                                    font,
                                    SpritesheetIndex,
                                    Color,
                                    Color.Transparent
                                );

            Point scaledSize = new Point(
                                    (int)((surface.Width / 4.0) * Scale.X),
                                    (int)((surface.Width / 4.0) * Scale.Y)
                               );

            for (int x = 0; x < scaledSize.X; ++x)
            {
                int viewportX = surface.Width / 2 - scaledSize.X / 2 + x + (int)(Offset.X * Scale.X);
                for (int y = 0; y < scaledSize.Y; ++y)
                {
                    int viewportY = surface.Height - scaledSize.Y + y + (int)(Offset.Y * Scale.Y);
                    Point c = new Point(
                        (int)(x / (double)scaledSize.X * font.Size.X),
                        (int)(y / (double)scaledSize.Y * font.Size.Y)
                    );

                    if (Util.OOB(viewportX, viewportY, surface.Width, surface.Height)) continue;
                    if (pixels[c.X, c.Y].A == 0) continue;

                    surface.SetCell(viewportX, viewportY, new Cell(pixels[c.X, c.Y], pixels[c.X, c.Y], ' '));
                }
            }
        }
    }
}
