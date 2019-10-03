using FieryOpal.Src.Procedural;
using FieryOpal.Src.Ui;
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
    public class WorldMap : OpalItem
    {
        public WorldMap() : base("World Map".ToColoredString(), ItemCategory.Book)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.LawnGreen, Color.CornflowerBlue, 'M'));
        }

        private void Read(IInventoryHolder holder)
        {
            var scroll = OpalDialog.Make<WorldMapScrollDialog>("Scroll", "", new Point(-1, -1), Nexus.Fonts.MainFont, true);
            World world = holder.Map.ParentRegion.ParentWorld;

            WorldMapViewport vwp = new WorldMapViewport(world, new Rectangle(0, 0, world.Width, world.Height));
            scroll.Viewport = vwp;
            OpalDialog.LendKeyboardFocus(scroll);
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.G, Keybind.KeypressState.Press, "World Map: Warp to location"), (i) =>
            {
                DateTime now = DateTime.Now;
                var newMap = holder.Map.ParentRegion.ParentWorld.RegionAt(vwp.CursorPosition.X, vwp.CursorPosition.Y).LocalMap;
                holder.ChangeLocalMap(newMap, new Point(newMap.Width / 2, newMap.Height / 2));
                Util.LogText(String.Format("Map successfully generated. ({0:0.00}s)", (DateTime.Now - now).TotalSeconds), true);
                scroll.Hide();
            });

            scroll.MoveCursor(holder.Map.ParentRegion.WorldPosition.X, holder.Map.ParentRegion.WorldPosition.Y);
            scroll.Viewport.Markers.Add(
                holder.Map.ParentRegion.WorldPosition,
                new Cell(
                    Palette.Ui["DefaultForeground"],
                    Palette.Ui["DefaultBackground"],
                    '@'
                )
            );

            scroll.Show();
            scroll.Closed += (e, eh) =>
            {
                Keybind.UnbindKey(new Keybind.KeybindInfo(Keys.G, Keybind.KeypressState.Press, ""));
            };
        }

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();
            RegisterInventoryAction("view", (h) => Read(h), new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press, "View world map"));

            UnregisterInventoryAction("drop"); // Key item, can't be dropped.
        }
    }
}
