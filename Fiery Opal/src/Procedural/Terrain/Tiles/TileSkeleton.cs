using FieryOpal.Src.Ui;
using SadConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace FieryOpal.Src.Procedural.Terrain.Tiles
{
    [Serializable]
    public abstract class TileSkeleton : IDisposable
    {
        protected static Dictionary<Type, TileSkeleton> Instances = new Dictionary<Type, TileSkeleton>();

        public virtual OpalTileProperties DefaultProperties { get; private set; }
        public virtual string DefaultName { get; private set; }
        public virtual Cell DefaultGraphics { get; private set; }

        public virtual OpalTile Make(int id)
        {
            return new OpalTile(id, this, DefaultName, DefaultProperties, DefaultGraphics);
        }

        protected TileSkeleton()
        {
        }

        private static TileSkeleton Get(Type type, TileSkeleton instance)
        {
            if (!Instances.ContainsKey(type))
            {
                Instances[type] = instance;
                OpalTile.RegisterRefTile(Instances[type]);
            }
            return Instances[type];
        }

        public static TileSkeleton Get<T>()
            where T : TileSkeleton, new()
        {
            return Get(typeof(T), new T());
        }

        public static TileSkeleton FromName(string name)
        {
            return Instances.Values.Where(ts => ts.DefaultName.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }

        public static void PreloadAllSkeletons()
        {
            Type[] typelist = Util.GetTypesInNamespace(Assembly.GetExecutingAssembly(), "FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons");
            foreach (Type t in typelist)
            {
                if (t.IsAbstract) continue;
                var instance = Get(t, (TileSkeleton)Activator.CreateInstance(t));
                Util.LogText("TileSkeleton.PreloadAllSkeletons: Preloaded {0} ({1}).".Fmt(t.Name, instance.DefaultName), true, Palette.Ui["BoringMessage"]);
            }
        }

        public void Dispose()
        {
            Instances.Remove(Instances.Where(kp => kp.Key == this.GetType()).First().Key);
        }
    }
}
