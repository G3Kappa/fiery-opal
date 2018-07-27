using FieryOpal.Src.Audio;
using FieryOpal.Src.Procedural;
using FieryOpal.Src.Ui.Dialogs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Controls;
using SadConsole.Effects;
using SadConsole.Input;
using SadConsole.Renderers;
using SadConsole.Shapes;
using SadConsole.Surfaces;
using System;
using System.Diagnostics;

namespace FieryOpal.Src.Ui.Windows
{
    public class PerlinFire : CellEffectBase
    {
        public Color ForegroundColor, BackgroundColor;

        public PerlinFire() : base()
        {
            IsFinished = false;
            RemoveOnFinished = false;
            Permanent = false;
            ForegroundColor = Color.White;
            BackgroundColor = Color.Black;
        }

        private Vector4 XYZS = new Vector4();
        private Vector4 Coord = new Vector4();

        public void SetXYZS(float x, float y, float z, float s)
        {
            XYZS = new Vector4(x, y, z, s);
        }

        private float _timeElapsed = 0f;
        public override void Update(double gameTimeSeconds)
        {
            _timeElapsed += (float)gameTimeSeconds * 4;
            if (XYZS.X == -1)
            {
                Coord.X = _timeElapsed;
            }
            else Coord.X = XYZS.X;
            if (XYZS.Y == -1)
            {
                Coord.Y = 1337 + _timeElapsed;
            }
            else Coord.Y = .5f * XYZS.Y + _timeElapsed;
            if (XYZS.Z == -1)
            {
                Coord.Z = 1337 * 7 + _timeElapsed / 2;
            }
            else Coord.Z = XYZS.Z;
            if (XYZS.W == -1)
            {
                Coord.W = .25f;
            }
            else Coord.W = XYZS.W;
        }

        public override bool Apply(Cell cell)
        {
            cell.RestoreState();
            if (cell.State == null)
                cell.SaveState();

            var oldForeground = cell.Foreground;
            var oldBackground = cell.Background;

            float n = Lib.Noise.CalcPixel3D(Coord.X, Coord.Y, Coord.Z, Coord.W) / 256f;
            cell.Foreground = Color.Lerp(oldForeground, ForegroundColor, n);
            if(cell.Glyph != ' ') cell.Background = Color.Lerp(oldBackground, BackgroundColor, n);

            return oldForeground != cell.Foreground || oldBackground != cell.Background;
        }

        public override ICellEffect Clone()
        {
            var ret = new PerlinFire()
            {
                _timeElapsed = _timeElapsed,
                ForegroundColor = ForegroundColor,
                BackgroundColor = BackgroundColor
            };
            ret.SetXYZS(XYZS.X, XYZS.Y, XYZS.Z, XYZS.W);
            return ret;
        }
    }

    public class MainMenuWindow : OpalConsoleWindow
    {
        protected int SelectedAction = 0;

        public MainMenuWindow(int width, int height) : base(width - 2, height - 2, "Main Menu", Nexus.Fonts.Spritesheets["Books"])
        {
            Fill(Color.White, Palette.Ui["BLACK"], ' ');
            //Borderless = true;

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = "v{0}".Fmt(fvi.FileVersion);

            Print(width - 4 - version.Length - 1, height - 6, version.ToColoredString(Palette.Ui["DGRAY"]));
            Print(1, height - 6, "Made by Kevin Giovinazzo".ToColoredString(Palette.Ui["DGRAY"]));

            Print(3, 2, "'||''''|                                 .|''''|,                 '||`".ToColoredString(Color.MonoGameOrange, Palette.Ui["BLACK"]));
            Print(3, 3, " ||  .    ''                             ||    ||                  || ".ToColoredString(Color.MonoGameOrange, Palette.Ui["BLACK"]));
            Print(3, 4, " ||''|    ||  .|''|, '||''| '||  ||`     ||    || '||''|,  '''|.   || ".ToColoredString(Color.MonoGameOrange, Palette.Ui["BLACK"]));
            Print(3, 5, " ||       ||  ||..||  ||     `|..||      ||    ||  ||  || .|''||   || ".ToColoredString(Color.MonoGameOrange, Palette.Ui["BLACK"]));
            Print(3, 6, ".||.     .||. `|...  .||.        ||      `|....|'  ||..|' `|..||. .||.".ToColoredString(Color.MonoGameOrange, Palette.Ui["BLACK"]));
            Print(3, 7, "                              ,  |'                ||                 ".ToColoredString(Color.MonoGameOrange, Palette.Ui["BLACK"]));
            Print(3, 8, "                               ''                 .||                 ".ToColoredString(Color.MonoGameOrange, Palette.Ui["BLACK"]));

            PerlinFire perlin = new PerlinFire()
            {
                ForegroundColor = Color.Yellow,
                BackgroundColor = Palette.Ui["BLACK"]
            };

            for(int x = 0; x < 70; ++x)
            {
                for(int y = 0; y < 7; ++y)
                {
                    var fx = perlin.Clone() as PerlinFire;
                    fx.SetXYZS(x, y, -1, .2f);
                    SetEffect(x + 3, 2 + y, fx);
                }
            }


            Cell borderStyle = new Cell(Palette.Ui["DGRAY"], Palette.Ui["BLACK"]);
            Cell captionStyle = new Cell(Palette.Ui["LGRAY"], Palette.Ui["BLACK"]);
            RedrawBorder(borderStyle, captionStyle);

            BindEvents();
            Invalidate();
        }

        private void BindEvents()
        {
            NewGamePressed += () =>
            {
                FormDialog diag = OpalDialog.Make<FormDialog>("Test", "Prova", new Point(Width + 2, Height + 2), Nexus.Fonts.Spritesheets["Books"]);
                diag.Position = Position;

                bool stringHandler(FormDialog.Question q, string s)
                {
                    if (String.IsNullOrWhiteSpace(s))
                    {
                        q.SetError("You must enter a valid, non-whitespace name.");
                        return false;
                    }
                    if (s.Length > 16)
                    {
                        q.SetError("Your name cannot be longer than 16 characters.");
                        return false;
                    }
                    q.ClearError();
                    return true;
                }

                var nameQuestion = new FormDialog.RandomAnswerQuestion(
                    "Enter your name:  ".ToColoredString(Palette.Ui["WHITE"], Palette.Ui["BLACK"]),
                    16,
                    stringHandler,
                    true,
                    () =>
                    {
                        return new DeityNameGenerator().GetName(null);
                    }
                );
                diag.AddQuestion(nameQuestion);

                var classQuestion = new FormDialog.Question(
                    "Select your class:".ToColoredString(Palette.Ui["WHITE"], Palette.Ui["BLACK"]),
                    16,
                    stringHandler,
                    false,
                    new[] { "Knight", "Archer", "Wizard" }
                );
                diag.AddQuestion(classQuestion);

                var startGameQuestion = new FormDialog.ActionButtonQuestion(
                    "Begin your adventure:".ToColoredString(Palette.Ui["LCYAN"], Palette.Ui["BLACK"]),
                    "Start Game",
                    tbMargin: new Point(1, 1)
                );

                diag.OnRedraw += (d) =>
                {
                    if (!diag.IsFullyValidated())
                    {
                        startGameQuestion.Text = startGameQuestion.Text.Recolor(Palette.Ui["ErrorMessage"], Palette.Ui["BLACK"]);
                        startGameQuestion.SetError("Fill in all the other inputs first.");
                    }
                    else
                    {
                        startGameQuestion.Text = startGameQuestion.Text.Recolor(Palette.Ui["LGREEN"], Palette.Ui["BLACK"]);
                        startGameQuestion.ClearError();
                    }
                };

                startGameQuestion.ButtonPressed += (text) =>
                {
                    if (diag.IsFullyValidated())
                    {
                        diag.Hide();
                        SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                        NewGameStarted?.Invoke(new NewGameInfo()
                        {
                            PlayerName = nameQuestion.AnswerGiven,
                            PlayerClass = classQuestion.AnswerGiven
                        });
                    }
                    else
                    {
                        SFXManager.PlayFX(SFXManager.SoundEffectType.UiError);
                    }
                };

                diag.AddQuestion(startGameQuestion);

                OpalDialog.LendKeyboardFocus(diag);
                diag.Show();
            };

            ExitPressed += () =>
            {
                OkCancelDialog areYouSure = OpalDialog.Make<OkCancelDialog>("Quitting Fiery Opal", "Are you sure you want to quit?");
                areYouSure.Show();
                areYouSure.OnResult += (res) =>
                {
                    if (res == OkCancelDialog.OpalDialogResult.OK)
                    {
                        SadConsole.Game.Instance.Exit();
                    }
                };
            };
        }

        private static string ACTION_FMT_SELECTED = "{2:LRED}{0:WHITE}{1:WHITE}";
        private static string ACTION_FMT_NOT_SELECTED = "{2}{0:RED}{1:LGRAY}";
        private static string TOOLTIP_FMT = "{0:LGRAY}";
        private string[] ACTIONS = { "New Game", "Continue", "Options", "About", "Quit" };
        private string[] TOOLTIPS = {
            "Start a new adventure in a randomly generated world.",
            "Load an adventure that's already in progress.",
            "Impose your will over the default settings.",
            "Skim over all the boring stuff.",
            "Lift the curse."
        };


        bool InGame = false;
        public void SetLabels(bool inGame)
        {
            InGame = inGame;
            ACTIONS[0]  = inGame ? "Resume" : "New Game";
            TOOLTIPS[0] = inGame ? "Back to your adventure." : "Start a new adventure in a randomly generated world.";

            ACTIONS[1]  = inGame ? "Save and quit" : "Continue";
            TOOLTIPS[1] = inGame ? "Save your progress and go back to the main menu." : "Load an adventure that's already in progress.";

            ACTIONS[4]  = inGame ? "Delet this" : "Quit";
            TOOLTIPS[4] = inGame ? "Go back in time and kill your character's grandfather." : "Lift the curse.";

            Invalidate();
        }

        private static ICellEffect HighlightSelected = new Fade()
        {
            FadeForeground = true,
            FadeBackground = false,
            DestinationForeground = Palette.Ui["LGRAY"],
            DestinationBackground = Palette.Ui["BLACK"],
            FadeDuration = 1,
            AutoReverse = false,
            Repeat = true,
            RemoveOnFinished = true
        };

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
        }

        public override void Invalidate()
        {
            HighlightSelected.Restart();
            for (int i = 0; i < ACTIONS.Length; i++)
            {
                var fmt = SelectedAction == i ? ACTION_FMT_SELECTED : ACTION_FMT_NOT_SELECTED;
                var s = SelectedAction == i ? "@:" : ".";

                var cs = fmt.FmtC(null, null, ACTIONS[i][0], ACTIONS[i].Substring(1).PadRight(14), s);
                for (int x = 0; x < cs.String.Trim().Length - s.Length; ++x)
                {
                    SetEffect(5 + x, 12 + i * 2, SelectedAction == i ? HighlightSelected : null);
                }

                Print(3, 12 + i * 2, cs);

                if (SelectedAction == i)
                {
                    Print(1, Height - 4, TOOLTIP_FMT.FmtC(null, null, TOOLTIPS[i].PadRight(Width - 6)));
                }
            }

        }

        public override void Update(TimeSpan time)
        {
            base.Update(time);
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            return true;
        }

        public override void Show(bool modal)
        {
            base.Show(modal);
            Keybind.PushState();

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.N, Keybind.KeypressState.Press, "Main Menu: New Game"), (info) =>
            {
                if (InGame) return;
                NewGamePressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.C, Keybind.KeypressState.Press, "Main Menu: Continue"), (info) =>
            {
                if (InGame) return;
                ContinuePressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.O, Keybind.KeypressState.Press, "Main Menu: Options"), (info) =>
            {
                OptionsPressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.A, Keybind.KeypressState.Press, "Main Menu: About"), (info) =>
            {
                AboutPressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.E, Keybind.KeypressState.Press, "Main Menu: Exit"), (info) =>
            {
                if (InGame) return;
                ExitPressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.R, Keybind.KeypressState.Press, "Main Menu: Resume"), (info) =>
            {
                if (!InGame) return;
                ResumePressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.S, Keybind.KeypressState.Press, "Main Menu: Save and quit"), (info) =>
            {
                if (!InGame) return;
                SaveAndQuitPressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.D, Keybind.KeypressState.Press, "Main Menu: Delete character"), (info) =>
            {
                if (!InGame) return;
                DeletePressed?.Invoke();
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Up, Keybind.KeypressState.Press, "Main Menu: Navigate up"), (info) =>
            {
                SelectedAction = SelectedAction - 1;
                if (SelectedAction < 0) SelectedAction = ACTIONS.Length - 1;
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiBlip);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Down, Keybind.KeypressState.Press, "Main Menu: Navigate down"), (info) =>
            {
                SelectedAction = (SelectedAction + 1) % ACTIONS.Length;
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiBlip);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Enter, Keybind.KeypressState.Press, "Main Menu: Invoke selected option"), (info) =>
            {
                switch(SelectedAction)
                {
                    case 0:
                        if (InGame) ResumePressed?.Invoke();
                        else NewGamePressed?.Invoke();
                        break;
                    case 1:
                        if (InGame) SaveAndQuitPressed?.Invoke();
                        else ContinuePressed?.Invoke();
                        break;
                    case 2:
                        OptionsPressed?.Invoke();
                        break;
                    case 3:
                        AboutPressed?.Invoke();
                        break;
                    case 4:
                        if (InGame) DeletePressed?.Invoke();
                        else ExitPressed?.Invoke();
                        break;
                }
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Escape, Keybind.KeypressState.Press, "Main Menu (In-Game): Close"), (info) =>
            {
                if (InGame)
                {
                    ResumePressed?.Invoke();
                    SFXManager.PlayFX(SFXManager.SoundEffectType.UiSuccess);
                    Invalidate();
                }
            });
        }

        public override void Hide()
        {
            base.Hide();
            Keybind.PopState();
        }

        public event Action NewGamePressed, ContinuePressed, ResumePressed, SaveAndQuitPressed, OptionsPressed, AboutPressed, ExitPressed, DeletePressed;

        public struct NewGameInfo
        {
            public string PlayerName;
            public string PlayerClass;
        }
        public event Action<NewGameInfo> NewGameStarted;
    }
}
