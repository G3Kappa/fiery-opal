using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.actors
{
    public abstract class Item : OpalActorBase
    {
        public Guid Owner;
    }

    public class ItemContainer<T>
        where T : Item
    {
        protected Dictionary<Guid, T> Contents;
        protected int capacity = 1;
        public int Capacity => capacity;

        public virtual bool IsRetrievable(T item)
        {
            return Contents.ContainsKey(item.Handle);
        }

        public ItemContainer(int cap)
        {
            capacity = cap;
        }

        public virtual bool Retrieve(T item)
        {
            if (!IsRetrievable(item)) return false;
            Contents.Remove(item.Handle);
            return true;
        }

        public virtual bool Store(T item)
        {
            if (Contents.Count >= Capacity || Contents.ContainsKey(item.Handle)) return false;
            Contents[item.Handle] = item;
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

    public class Book : Item
    {

    }

    public class Compass : Item
    {

    }

    public abstract class Parchment : Item
    {

    }

    public class Scroll : Parchment
    {

    }

    public class MapScroll : Scroll
    {

    }
}
