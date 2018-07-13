using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles
{
    public class DoorTile : OpalTile, IInteractive
    {
        protected bool isOpen = false;
        public bool IsOpen => isOpen;

        public DoorTile(int id, TileSkeleton k, string defaultname, OpalTileProperties props, Cell graphics) : base(id, k, defaultname, props, graphics) { }

        public void Toggle(bool? value = null)
        {
            isOpen = value ?? !isOpen;
            Properties.IsBlock = !isOpen;
        }

        public override object Clone()
        {
            return new DoorTile(GetFirstFreeId(), Skeleton, Name, Properties, Graphics);
        }

        public bool InteractWith(OpalActorBase actor)
        {
            if (!actor.CanMove) return false;
            Toggle();
            actor.Map.Lighting.Update(true);
            return true;
        }
    }
}
