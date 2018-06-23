using FieryOpal.Src.Procedural;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors
{
    public enum ItemCategory
    {
        Miscellaneous,
        Book,
        Potion,
        Equipment,
    }

    public struct ItemInfo<T>
        where T : Item
    {
        public Guid Owner;
        public ItemCategory Category;
        public ColoredString Name;
        public int BaseValue;

        public bool IsUnique;
    }

    public abstract class Item : OpalActorBase, IInteractive
    {
        public virtual ItemInfo<Item> ItemInfo { get; protected set; }
        protected Dictionary<string, Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>, bool>> InventoryActions { get; } = new Dictionary<string, Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>, bool>>();

        public IInventoryHolder Owner { get; private set; } = null;

        protected virtual void DropFrom(IInventoryHolder holder)
        {
            holder.Inventory.Retrieve(this);
            ChangeLocalMap(holder.Map, holder.Map.FirstAccessibleTileAround(holder.LocalPosition));
            Owner = null;
        }

        public Item(ColoredString name, ItemCategory category = ItemCategory.Miscellaneous)
            : base()
        {
            ItemInfo = new ItemInfo<Item>()
            {
                Owner = Handle,
                Category = category,
                Name = name,
                BaseValue = (int)Math.Pow(10, (int)category),
                IsUnique = false,
            };
            RegisterInventoryActions();
        }

        public bool RegisterInventoryAction(string action, Action<IInventoryHolder> act, Keybind.KeybindInfo shortcut)
        {
            if (InventoryActions.ContainsKey(action)) return false;
            InventoryActions[action] = new Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>, bool>(shortcut, act, true);
            return true;
        }

        public bool ToggleInventoryAction(string action, bool status)
        {
            if (!InventoryActions.ContainsKey(action)) return false;
            InventoryActions[action] = new Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>, bool>(
                InventoryActions[action].Item1,
                InventoryActions[action].Item2,
                status
            );
            return true;
        }

        public bool CallInventoryAction(string action, IInventoryHolder callee)
        {
            if (!InventoryActions.ContainsKey(action) || !InventoryActions[action].Item3) return false;
            InventoryActions[action].Item2(callee);
            return true;
        }

        public Keybind.KeybindInfo GetInventoryActionShortcut(string action)
        {
            if (!InventoryActions.ContainsKey(action)) return new Keybind.KeybindInfo();
            return InventoryActions[action].Item1;
        }

        public IEnumerable<string> EnumerateInventoryActions()
        {
            foreach (var kvp in InventoryActions.Where(kvp => kvp.Value.Item3)) yield return kvp.Key;
        }

        public virtual bool UnregisterInventoryAction(string action)
        {
            if (!InventoryActions.ContainsKey(action)) return false;
            InventoryActions.Remove(action);
            return true;
        }

        protected virtual void RegisterInventoryActions()
        {
            RegisterInventoryAction("drop", (h) => DropFrom(h), new Keybind.KeybindInfo(Keys.D, Keybind.KeypressState.Press, "Drop item"));
        }

        public bool InteractWith(OpalActorBase actor)
        {
            var a = actor as IInventoryHolder;
            if (a == null) return false;
            bool ret = a.Inventory.Store(this);
            if (ret) ChangeLocalMap(null, new Point());
            Owner = a;
            return ret;
        }

        public void Own(IInventoryHolder newOwner)
        {
            Owner = newOwner;
        }
    }

    public class Journal : Item
    {
        protected List<List<string>> Contents = new List<List<string>>();

        public Journal() : base("Journal".ToColoredString(), ItemCategory.Book)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.Black, Color.White, 'J'));
        }

        private void Read(IInventoryHolder holder)
        {
            var book = OpalDialog.Make<BookDialog>("Journal", "");
            OpalDialog.LendKeyboardFocus(book);
            book.Show();
        }

        private void Write(IInventoryHolder holder)
        {
            var diag = OpalDialog.Make<DialogueDialog>("Write", "This feature is currently not implemented.");
            diag.AddOption("I wholly understand and submit to the consequences.", null);
            OpalDialog.LendKeyboardFocus(diag);
            diag.Show();
        }

        protected override void RegisterInventoryActions()
        {
            base.RegisterInventoryActions();
            RegisterInventoryAction("read", (h) => Read(h), new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press, "Read journal"));
            RegisterInventoryAction("write on", (h) => Write(h), new Keybind.KeybindInfo(Keys.W, Keybind.KeypressState.Press, "Write on journal"));

            UnregisterInventoryAction("drop"); // Key item, can't be dropped.
        }
    }

    public class WorldMap : Item
    {
        public WorldMap() : base("World Map".ToColoredString(), ItemCategory.Book)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.LawnGreen, Color.CornflowerBlue, 'M'));
        }

        private void Read(IInventoryHolder holder)
        {
            var scroll = OpalDialog.Make<WorldMapScrollDialog>("Scroll", "");
            World world = holder.Map.ParentRegion.ParentWorld;

            WorldMapViewport vwp = new WorldMapViewport(world, new Rectangle(0, 0, world.Width, world.Height));
            scroll.Viewport = vwp;
            OpalDialog.LendKeyboardFocus(scroll);
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.G, Keybind.KeypressState.Press, "World Map: Warp to location"), (i) =>
            {
                DateTime now = DateTime.Now;
                var newMap = holder.Map.ParentRegion.ParentWorld.RegionAt(vwp.CursorPosition.X, vwp.CursorPosition.Y).LocalMap;
                holder.ChangeLocalMap(newMap, newMap.FirstAccessibleTileAround(new Point(newMap.Width / 2, newMap.Height / 2)));
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
