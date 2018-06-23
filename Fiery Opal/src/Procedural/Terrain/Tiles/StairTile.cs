using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using FieryOpal.Src.Procedural.Worldgen;
using SadConsole;

namespace FieryOpal.Src.Procedural.Terrain.Tiles
{
    public class StairTile : OpalTile, IInteractive
    {
        public Portal? Portal { get; set; }

        public StairTile(int id, StairSkeleton skeleton, string name = "Untitled", OpalTileProperties properties = new OpalTileProperties(), Cell graphics = null)
            : base(id, skeleton, name, properties, graphics)
        {
        }

        public bool InteractWith(OpalActorBase actor)
        {
            if (Portal?.ToInstance != null)
            {
                if (actor.Map.TileAt(actor.LocalPosition.X, actor.LocalPosition.Y) != this && actor.IsPlayer)
                {
                    Util.LogText(Util.Str("Player_StairsTooFar"), false);
                    return false;
                }

                actor.ChangeLocalMap(Portal.Value.ToInstance.Map, Portal.Value.ToPos);
                if (actor.IsPlayer)
                {
                    Util.LogText(Util.Str("Player_UsingStairs", Portal.Value.ToInstance.Map.Name, Portal.Value.ToPos), false);
                }
                return true;
            }
            if (actor.IsPlayer)
            {
                Util.LogText(Util.Str("Player_StairsUnconnected"), false);
            }
            return false;
        }

        public override object Clone()
        {
            return new StairTile(GetFirstFreeId(), (StairSkeleton)Skeleton, Name, Properties, Graphics);
        }
    }
}
