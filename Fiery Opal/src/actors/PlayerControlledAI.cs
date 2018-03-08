using FieryOpal.src.ui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.actors
{
    public enum PlayerAction
    {
        Wait = 0,
        MoveU = 1,
        MoveD = 2,
        MoveL = 3,
        MoveR = 4,

        TurnL = 5,
        TurnR = 6,

        Interact = 7,
        OpenInventory = 8
    }

    public class PlayerActionsKeyConfiguration
    {
        Dictionary<PlayerAction, Keybind.KeybindInfo> Config;

        public PlayerActionsKeyConfiguration()
        {
            Config = new Dictionary<PlayerAction, Keybind.KeybindInfo>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="action"></param>
        /// <param name="k"></param>
        /// <returns>True if the key was re-assigned, false if it was assigned.</returns>
        public bool AssignKey(PlayerAction action, Keybind.KeybindInfo k)
        {
            bool ret = Config.ContainsKey(action);
            Config[action] = k;
            return ret;
        }

        public Keybind.KeybindInfo GetInfo(PlayerAction action)
        {
            if (!Config.ContainsKey(action)) throw new Exception();

            return Config[action];
        }

        public bool IsValid()
        {
            foreach(PlayerAction key in Enum.GetValues(typeof(PlayerAction)))
            {
                if (!Config.ContainsKey(key)) return false;
            }
            return true;
        }
    }

    class PlayerControlledAI : TurnBasedAI
    {
        private bool isHandlingDialog => OpalDialog.CurrentDialogCount > 0;

        public PlayerActionsKeyConfiguration KeyConfig;
        public MessagePipeline<OpalGame> InternalMessagePipeline { get; }

        public PlayerControlledAI(TurnTakingActor player, PlayerActionsKeyConfiguration keyconfig) : base(player)
        {
            if(!keyconfig?.IsValid() ?? true)
            {
                throw new ArgumentException("Invalid key configuration.");
            }
            KeyConfig = keyconfig;
            InternalMessagePipeline = new MessagePipeline<OpalGame>();

            Body.Inventory.Store(new Journal());

            Body.Inventory.ItemStored += (item) => Util.Log("Got ".ToColoredString() + item.ItemInfo.Name, false);
            Body.Inventory.ItemRetrieved += (item) => Util.Log("Dropped ".ToColoredString() + item.ItemInfo.Name, false);
        }

        public void BindKeys()
        {
            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.Wait), (info) => { Wait(5); });

            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.MoveU), (info) => { MoveRelative(0, -1); });
            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.MoveD), (info) => { MoveRelative(0, 1); });
            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.MoveL), (info) => { MoveRelative(-1, 0); });
            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.MoveR), (info) => { MoveRelative(1, 0); });

            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.TurnL), (info) => { Turn(-(float)Math.PI / 4); });
            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.TurnR), (info) => { Turn((float)Math.PI / 4); });

            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.Interact), (info) => { Interact(); });
            Keybind.BindKey(KeyConfig.GetInfo(PlayerAction.OpenInventory), (info) => { OpenInventory(); });
        }

        public override IEnumerable<TurnBasedAction> GiveAdvice(int turn, float energy)
        {
            yield break;
        }

        /// <summary>
        /// Makes the game's TurnManager begin a new turn and optionally sends a message to the OpalGameWindows that host the game.
        /// </summary>
        /// <param name="msgForWindow"></param>
        private void InputHandled(string msgForWindow = null)
        {
            InternalMessagePipeline.Broadcast(null, (g) => {
                if (msgForWindow != null) g.InternalMessagePipeline.Broadcast(null, (c) => msgForWindow);
                return "PlayerInputHandled";
            });
        }

        private void Interact()
        {
            var pos = Body.LocalPosition + Util.NormalizedStep(Body.LookingAt);

            var interactive = Body.Map.ActorsAt(pos.X, pos.Y).FirstOrDefault(a => a is IInteractive);
            if (interactive == null) return;
            Body.EnqueuedActions.Enqueue(() =>
            {
                (interactive as IInteractive).InteractWith(Body);
                InputHandled("FlagRaycastViewportForRedraw");
                return 0f;
            });
            InputHandled();
        }

        private void OpenInventory()
        {
            if (isHandlingDialog) return;

            var inventory_window = OpalDialog.Make<InventoryDialog>("Inventory", "");
            inventory_window.Inventory = Body.Inventory;
            OpalDialog.LendKeyboardFocus(inventory_window);
            inventory_window.Show();
        }

        private void Turn(float angle)
        {
            Body.EnqueuedActions.Enqueue(() =>
            {
                Body.Turn(angle);
                // Here "PlayerInputHandled" isn't received in time for the rotation update,
                // but the parameterless call will fix everything harmlessly.
                InputHandled("UpdateRaycastWindowRotation");
                return 1 /20f;
            });
            InputHandled();
        }

        private void Wait(float turns)
        {
            if (turns <= 0f) return;

            Body.EnqueuedActions.Enqueue(() => { return turns; });
            for (int i = 0; i < (int)turns - 1; ++i)
            {
                InputHandled();
            }
            InputHandled("FlagRaycastViewportForRedraw");
        }

        private void MoveRelative(int x, int y)
        {
            Point to = new Point();
            if (x == 0 && y == -1) to = Util.NormalizedStep(Body.LookingAt);
            else if (x == -1 && y == 0) to = Util.NormalizedStep(-Body.LookingAt.Orthogonal());
            else if (x == 0 && y == 1) to = Util.NormalizedStep(-Body.LookingAt);
            else if (x == 1 && y == 0) to = Util.NormalizedStep(Body.LookingAt.Orthogonal());

            Point _ = new Point();
            if (!Body.CanMoveTo(to, ref _, false)) return;

            Body.EnqueuedActions.Enqueue(() =>
            {
                Body.MoveTo(to);
                return 1 / 4f;
            });

            InputHandled("FlagRaycastViewportForRedraw");
        }
    }
}
