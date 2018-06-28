using FieryOpal.Src.Multiplayer;
using System.Collections.Generic;

namespace FieryOpal.Src.Actors
{
    class ServerControlledAI : TurnBasedAI
    {
        public MessagePipeline<OpalGame> InternalMessagePipeline { get; }

        public ServerControlledAI(TurnTakingActor player, OpalClient client) : base(player)
        {
            InternalMessagePipeline = new MessagePipeline<OpalGame>();

            Body.TurnPriority = 0;

            Body.Inventory.Store(new Journal());
            Body.Inventory.Store(new WorldMap());

            Body.Inventory.ItemStored += (item) => Util.LogText(Util.Str("Player_ItemPickedUp", item.ItemInfo.Name), false);
            Body.Inventory.ItemRetrieved += (item) => Util.LogText(Util.Str("Player_ItemDropped", item.ItemInfo.Name), false);
        }

        public override IEnumerable<TurnBasedAction> GiveAdvice(int turn, float energy)
        {
            yield break;
        }
    }
}
