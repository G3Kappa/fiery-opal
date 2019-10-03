using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.Src
{
    [Serializable]
    public class TileMemory
    {
        protected HashSet<SerializablePoint> Seen = new HashSet<SerializablePoint>();
        protected HashSet<SerializablePoint> Known = new HashSet<SerializablePoint>();

        [NonSerialized()]
        protected bool IsDisabled = false;
        public bool IsEnabled => !IsDisabled;

        [NonSerialized()]
        private object fogLock = new object();

        public void See(Point p)
        {
            lock (fogLock)
            {
                if (!Seen.Contains(p)) Seen.Add(p);
            }
        }

        public void Learn(Point p)
        {
            lock (fogLock)
            {
                if (!Known.Contains(p)) Known.Add(p);
            }
        }

        public void Unsee(Point p)
        {
            lock (fogLock)
            {
                if (Seen.Contains(p)) Seen.Remove(p);
            }
        }

        public void Forget(Point p)
        {
            lock (fogLock)
            {
                if (Known.Contains(p)) Known.Remove(p);
            }
        }

        public void UnseeEverything()
        {
            lock (fogLock)
            {
                Seen.Clear();
            }
        }

        public void ForgetEverything()
        {
            lock (fogLock)
            {
                Known.Clear();
            }
        }

        public bool CanSee(Point p)
        {
            lock (fogLock)
            {
                if (IsDisabled) return true;
                return Seen.Contains(p);
            }
        }

        public bool KnowsOf(Point p)
        {
            lock (fogLock)
            {
                if (IsDisabled) return true;
                return Known.Contains(p);
            }
        }

        public void Disable()
        {
            lock (fogLock)
            {
                IsDisabled = true;
            }
        }

        public void Enable()
        {
            lock (fogLock)
            {
                IsDisabled = false;
            }
        }

        public void Toggle()
        {
            lock (fogLock)
            {
                IsDisabled = !IsDisabled;
            }
        }

        public string DataFolder => Nexus.GameInstance.World.DataFolder_Regions + "/tilemem";

        public void SaveToDisk(string fileName)
        {
            string fn = $"{DataFolder}/{fileName}";
            Nexus.Serializer.SaveState(this, fn);
        }

        public void LoadFromDisk(string fileName)
        {
            string fn = $"{DataFolder}/{fileName}";
            TileMemory state = (TileMemory)Nexus.Serializer.LoadState(fn);

            Seen = state.Seen;
            Known = state.Known;
        }
    }
}
