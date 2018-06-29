using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private Texture2D _texture;
        public Texture2D AsTexture2D(int viewportWidth)
        {
            if (_texture != null) return _texture;

            var font = Nexus.Fonts.Spritesheets["Weapons"];

            Color[,] pixels = FontTextureCache.GetRecoloredPixels(
                                    font,
                                    SpritesheetIndex,
                                    Color,
                                    Color.Transparent
                                );

            int scale = (viewportWidth / font.Size.X) / 2;
            _texture = new Texture2D(Global.GraphicsDevice, pixels.GetLength(0) * (int)(scale * Scale.X), pixels.GetLength(1) * (int)(scale * Scale.Y));
            _texture.SetData(pixels.ResizeNearestNeighbor(font.Size.X * (int)(scale * Scale.X), font.Size.Y * (int)(scale * Scale.Y)).Flatten());
            return _texture;
        }
    }
}
