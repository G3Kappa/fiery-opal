using FieryOpal.Src.Procedural;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors.Items
{
    public enum ItemCategory
    {
        Miscellaneous,
        Book,
        Potion,
        Equipment,
    }

    public class ItemInfo<T>
        where T : OpalItem
    {
        public Guid Owner;
        public ItemCategory Category;
        public ColoredString Name;
        public int BaseValue;

        public bool IsUnique;
    }

    public abstract class OpalItem : OpalActorBase, IInteractive
    {
        public virtual ItemInfo<OpalItem> ItemInfo { get; protected set; }
        protected Dictionary<string, Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>, bool>> InventoryActions { get; } = new Dictionary<string, Tuple<Keybind.KeybindInfo, Action<IInventoryHolder>, bool>>();

        public IInventoryHolder Owner { get; private set; } = null;

        protected virtual void DropFrom(IInventoryHolder holder)
        {
            holder.Inventory.Retrieve(this);
            ChangeLocalMap(holder.Map, holder.LocalPosition);
            Owner = null;
            OnDropped(holder);
        }

        public OpalItem(ColoredString name, ItemCategory category = ItemCategory.Miscellaneous)
            : base()
        {
            ItemInfo = new ItemInfo<OpalItem>()
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

        public virtual void OnDropped(IInventoryHolder oldOwner)
        {

        }

        public virtual void OnPickedUp(IInventoryHolder newOwner)
        {

        }

        public bool InteractWith(OpalActorBase actor)
        {
            var a = actor as IInventoryHolder;
            if (a == null) return false;
            bool ret = a.Inventory.Store(this);
            if (ret) ChangeLocalMap(null, new Point());
            Owner = a;
            OnPickedUp(a);
            return ret;
        }

        public void Own(IInventoryHolder newOwner)
        {
            Owner = newOwner;
        }
    }
}
