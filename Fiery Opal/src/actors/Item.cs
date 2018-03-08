using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.actors
{
    public enum ItemCategory
    {
        Miscellaneous,
        Potion,
        Book,
        Armor,
        Weapon,
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
        protected Dictionary<string, Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>>> InventoryActions { get; } = new Dictionary<string, Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>>>();

        private void DropFrom(IInventoryHolder holder)
        {
            holder.Inventory.Retrieve(this);
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
            InventoryActions[action] = new Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>>(shortcut, act);
            return true;
        }

        public bool CallInventoryAction(string action, IInventoryHolder callee)
        {
            if (!InventoryActions.ContainsKey(action)) return false;
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
            foreach (var key in InventoryActions.Keys) yield return key;
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
            return ret;
        }
    }

    public delegate void ItemContainerContentsChanged(Item i);

    public class ItemContainer<T>
        where T : Item
    {
        protected Dictionary<Guid, T> Contents;
        protected int capacity = 1;
        public int Capacity => capacity;

        public int Count { get; private set; } = 0;

        public event ItemContainerContentsChanged ItemRetrieved;
        public event ItemContainerContentsChanged ItemStored;

        public virtual bool IsRetrievable(T item)
        {
            return Contents.ContainsKey(item.Handle);
        }

        public ItemContainer(int cap)
        {
            Contents = new Dictionary<Guid, T>();
            capacity = cap;
        }

        public virtual bool Retrieve(T item)
        {
            if (!IsRetrievable(item)) return false;
            Contents.Remove(item.Handle);
            Count--;
            ItemRetrieved?.Invoke(item);
            return true;
        }

        public virtual bool Store(T item)
        {
            if (Contents.Count >= Capacity || Contents.ContainsKey(item.Handle)) return false;
            Contents[item.Handle] = item;
            Count++;
            ItemStored?.Invoke(item);
            return true;
        }

        public IEnumerable<T> GetContents()
        {
            foreach (var value in Contents.Values)
            {
                yield return value;
            }
        }
    }

    public class PersonalInventory : ItemContainer<Item>
    {
        public IInventoryHolder Owner;

        public PersonalInventory(int cap, IInventoryHolder person) : base(cap)
        {
            Owner = person;
        }
    }

    public class Journal : Item
    {
        protected List<List<string>> Contents = new List<List<string>>();

        public Journal() : base("Journal".ToColoredString(), ItemCategory.Book)
        {
            Graphics = FirstPersonGraphics = new ColoredGlyph(new Cell(Color.Gold, Color.CornflowerBlue, 'J'));
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
}
