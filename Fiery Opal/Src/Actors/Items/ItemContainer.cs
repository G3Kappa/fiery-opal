﻿using System;
using System.Collections.Generic;

namespace FieryOpal.Src.Actors.Items
{
    public delegate void ItemContainerContentsChanged(OpalItem i);

    public class ItemContainer<T>
        where T : OpalItem
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

    public class PersonalInventory : ItemContainer<OpalItem>
    {
        public IInventoryHolder Owner;

        public PersonalInventory(int cap, IInventoryHolder person) : base(cap)
        {
            Owner = person;
        }
    }
}
