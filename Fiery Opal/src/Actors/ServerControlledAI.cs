using FieryOpal.src.Multiplayer;
using FieryOpal.Src.Procedural.Terrain.Tiles.Skeletons;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

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
