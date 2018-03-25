using FieryOpal.Src.Procedural.Worldgen;
using FieryOpal.Src.Ui;
using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Actors
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
        OpenInventory = 8,
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
                Util.Err("Invalid player key configuration. Check cfg/keys.cfg.");
#if DEBUG
                throw new ArgumentException("Invalid player key configuration.");
#endif
            }
            KeyConfig = keyconfig;
            InternalMessagePipeline = new MessagePipeline<OpalGame>();

            Body.Inventory.Store(new Journal());
            Body.Inventory.Store(new WorldMap());

            Body.Inventory.ItemStored += (item) => Util.Log(Util.Str("Player_ItemPickedUp", item.ItemInfo.Name), false);
            Body.Inventory.ItemRetrieved += (item) => Util.Log(Util.Str("Player_ItemDropped", item.ItemInfo.Name), false);
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
#if DEBUG
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.F2, Keybind.KeypressState.Press, "Debug: Toggle fog", ctrl: true), (info) => {
                TileMemory.Toggle();
                Util.Log(
                    ("-- " + (TileMemory.IsEnabled ? "Enabled " : "Disabled") + " fog.").ToColoredString(Palette.Ui["DebugMessage"]),
                    false
                );
            });
#endif

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
            var interaction_rect = new Rectangle(
                Body.LocalPosition - new Point(1), 
                new Point(3)
            );

            var interactives = 
                Body.Map.ActorsWithin(interaction_rect)
                .Where(a => a is IInteractive && a != Body)
                .Select(a => a as IInteractive)
                .ToList();
            interactives.AddRange(
                Body.Map.TilesWithin(interaction_rect)
                .Where(t => t.Item1 is IInteractive)
                .Select(t => t.Item1 as IInteractive)
                .ToList()
            );

            if(interactives.Count == 0)
            {
                Util.Log(Util.Str("Player_NoAvailableInteractions").ToColoredString(Palette.Ui["BoringMessage"]), false);
                return;
            }
            else if(interactives.Count == 1)
            {
                Body.EnqueuedActions.Enqueue(() =>
                {
                    interactives.First().InteractWith(Body);
                    return 0f;
                });
                InputHandled("FlagRaycastViewportForRedraw");
                return;
            }

            ContextMenu<IInteractive> dialog =
                OpalDialog.Make<ContextMenu<IInteractive>>(
                    Util.Str("Player_ChooseInteractionTitle"), ""
                );
            dialog.CloseOnESC = true;
            dialog.Closed += (e, eh) =>
            {
                dialog.ChosenAction?.Invoke(null);
                InputHandled("FlagRaycastViewportForRedraw");
            };

            char key = 'A';
            foreach(var itr in interactives)
            {
                Keybind.KeybindInfo info = new Keybind.KeybindInfo(
                    (Keys)(key++),
                    Keybind.KeypressState.Press,
                    Util.Str("Player_InteractWithHelpText", itr.Name)
                );
                dialog.AddAction(itr.Name, (_) => {
                    Body.EnqueuedActions.Enqueue(() =>
                    {
                        itr.InteractWith(Body);
                        return 0f;
                    });
                }, info);
            }

            OpalDialog.LendKeyboardFocus(dialog);
            dialog.Show();
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

            Body.EnqueuedActions.Enqueue(() =>
            {
                Body.MoveTo(to);
                return 1 / 4f;
            });

            InputHandled("FlagRaycastViewportForRedraw");
        }
    }
}
