using FieryOpal.Src.Audio;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SadConsole;
using SadConsole.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FieryOpal.Src.Ui.Dialogs
{
    public class FormDialog : OpalDialog
    {
        public class Question
        {
            public ColoredString Text { get; set; }
            public int MaxAnswerLength { get; }
            public Func<Question, string, bool> ValidateAnswer { get; }
            public string AnswerGiven { get; protected set; } = null;
            public List<string> PresetAnswers { get; protected set; } = null;
            public int SelectedPreset { get; private set; } = -1;
            public bool FreeInput { get; protected set; } = true;
            public Point MarginTopBottom = Point.Zero;

            private string _LastError = null;
            public void SetError(string msg)
            {
                _LastError = msg;
            }

            public void ClearError()
            {
                _LastError = null;
            }

            public string LastError => _LastError;

            public Question(ColoredString text, int ansLen, Func<Question, string, bool> validate, bool freeInput = true, IEnumerable<string> presetAnswers = null, Point? tbMargin = null)
            {
                Text = text;
                MaxAnswerLength = ansLen;
                ValidateAnswer = validate;
                PresetAnswers = presetAnswers?.ToList() ?? new List<string>();
                FreeInput = freeInput;
                MarginTopBottom = tbMargin ?? Point.Zero;
                ChooseNextPreset();
            }

            public bool Validate(string ans)
            {
                if (ans == null || ans.Length > MaxAnswerLength && MaxAnswerLength > 0) return false;

                if (ValidateAnswer?.Invoke(this, ans) ?? true)
                {
                    AnswerGiven = ans;
                    return true;
                }
                return false;
            }

            public void ClearAnswer()
            {
                AnswerGiven = null;
            }

            public virtual void ChooseNextPreset()
            {
                if (PresetAnswers.Count > 0)
                {
                    SelectedPreset = SelectedPreset - 1;
                    if (SelectedPreset < 0) SelectedPreset = 0;

                    AnswerGiven = PresetAnswers[SelectedPreset];
                    ClearError();
                }
            }

            public virtual void ChoosePrevPreset()
            {
                if (PresetAnswers.Count > 0)
                {
                    SelectedPreset = SelectedPreset + 1;
                    if (SelectedPreset >= PresetAnswers.Count) SelectedPreset = PresetAnswers.Count - 1;

                    AnswerGiven = PresetAnswers[SelectedPreset];
                    ClearError();
                }
            }
        }

        public class MultilineQuestion : Question
        {
            public MultilineQuestion(ColoredString text, int ansCols, int ansRows, Func<Question, string, bool> validate, bool freeInput = true, IEnumerable<string> presetAnswers = null, Point? tbMargin = null) : base(text, ansCols * ansRows, validate, freeInput, presetAnswers, tbMargin)
            {

            }
        }

        public class RandomAnswerQuestion : Question
        {
            public Func<string> PresetGenerator { get; } = null;

            public RandomAnswerQuestion(ColoredString text, int ansLen, Func<Question, string, bool> validate, bool freeInput, Func<string> generatePresetAnswer, Point? tbMargin = null) : base(text, ansLen, validate, freeInput, null, tbMargin)
            {
                PresetGenerator = generatePresetAnswer;
                PresetAnswers.Add("_unused");
            }

            public override void ChooseNextPreset()
            {
                AnswerGiven = PresetGenerator?.Invoke() ?? AnswerGiven;
                ClearError();
            }

            public override void ChoosePrevPreset()
            {
                ChooseNextPreset();
            }
        }

        public class ActionButtonQuestion : Question
        {
            public event Action<string> ButtonPressed;

            public ActionButtonQuestion(ColoredString text, string buttonText, Point? tbMargin = null) : base(text, buttonText.Length, (q, s) => true, false, null, tbMargin)
            {
                Validate(buttonText);
            }

            public void PressButton()
            {
                ButtonPressed?.Invoke(AnswerGiven);
            }
        }

        protected List<Question> Questions { get; } = new List<Question>();
        protected int SelectedQuestion = 0;

        public Action<FormDialog> OnRedraw;

        public FormDialog() : base()
        {
            Clear();
        }

        public void AddQuestion(Question q)
        {
            Questions.Add(q);
            Invalidate();
        }

        public bool IsFullyValidated()
        {
            foreach(Question q in Questions)
            {
                if (!q.Validate(q.AnswerGiven)) return false;
            }
            return true;
        }

        public override void Invalidate()
        {
            base.Invalidate();
            OnRedraw?.Invoke(this);

            Cell inactiveTextStyle = new Cell(Palette.Ui["LGRAY"], Palette.Ui["BLACK"]);
            Cell activeTextStyle = new Cell(Palette.Ui["WHITE"], Palette.Ui["BLACK"]);
            Cell activeInputStyle = new Cell(Palette.Ui["WHITE"], Palette.Ui["DGRAY"]);
            Cell inactiveInputStyle = new Cell(Palette.Ui["WHITE"], Palette.Ui["BLACK"]);
            Cell errorStyle = new Cell(Palette.Ui["RED"], Palette.Ui["BLACK"]);

            ColoredString presetArrowL = "<".ToColoredString(inactiveTextStyle);
            ColoredString presetArrowR = ">".ToColoredString(inactiveTextStyle);

            ColoredString presetButtonL = "[".ToColoredString(inactiveTextStyle);
            ColoredString presetButtonR = "]".ToColoredString(inactiveTextStyle);

            int y = 1;
            int x = 1;
            int i = 0;

            Fill(Palette.Ui["DGRAY"], Palette.Ui["BLACK"], ' ');

            foreach(Question q in Questions)
            {
                q.Validate(q.AnswerGiven ?? "");
                var inputStyle = (i == SelectedQuestion) ? activeInputStyle : inactiveInputStyle;

                y += q.MarginTopBottom.X;

                if (i == SelectedQuestion)
                {
                    Print(x, y, q.Text);
                }
                else
                {
                    Print(x, y, q.Text.Recolor(inactiveTextStyle.Foreground, inactiveTextStyle.Background));
                }

                if(q.PresetAnswers.Count > 0)
                {
                    if(i == SelectedQuestion)
                    {
                        var l = (q.SelectedPreset == -1 || q.SelectedPreset > 0) ? presetArrowL.Recolor(activeTextStyle.Foreground, activeTextStyle.Background) : presetArrowL;
                        var r = (q.SelectedPreset == -1 || q.SelectedPreset < q.PresetAnswers.Count - 1) ? presetArrowR.Recolor(activeTextStyle.Foreground, activeTextStyle.Background) : presetArrowR;

                        Print(x + q.Text.Count + 1, y, l + " ".Repeat(q.MaxAnswerLength).ToColoredString(inputStyle) + r);
                    }
                    else
                    {
                        Print(x + q.Text.Count + 2, y, " ".Repeat(q.MaxAnswerLength).ToColoredString(inputStyle));
                    }
                }
                else if(q is ActionButtonQuestion)
                {
                    if(i != SelectedQuestion) inputStyle = inactiveTextStyle;
                    var l = (i == SelectedQuestion) ? presetButtonL.Recolor(activeTextStyle.Foreground, activeTextStyle.Background) : presetButtonL;
                    var r = (i == SelectedQuestion) ? presetButtonR.Recolor(activeTextStyle.Foreground, activeTextStyle.Background) : presetButtonR;

                    Print(x + q.Text.Count + 1, y, l + " ".Repeat(q.MaxAnswerLength).ToColoredString(inputStyle) + r);
                }
                else
                {
                    Print(x + q.Text.Count + 1, y, " ".Repeat(q.MaxAnswerLength).ToColoredString(inputStyle));
                }

                if(q.AnswerGiven != null)
                {
                    int xofs = (q is ActionButtonQuestion || q.PresetAnswers.Count > 0) ? 1 : 0;
                    Print(x + q.Text.Count + 1 + xofs, y, q.AnswerGiven.ToColoredString(inputStyle));
                    if (i == SelectedQuestion && q.FreeInput && q.AnswerGiven.Length < q.MaxAnswerLength)
                    {
                        Print(x + q.Text.Count + 1 + xofs + q.AnswerGiven.Length, y, "_".ToColoredString(inputStyle));
                    }
                }
                else
                {
                    if (q.FreeInput && i == SelectedQuestion) Print(x + q.Text.Count + 1 + (q.PresetAnswers.Count > 0 ? 1 : 0), y, "_".ToColoredString(inputStyle));
                }

                if (q.LastError != null)
                {
                    Print(x, y + 1, q.LastError.ToColoredString(errorStyle));
                    y++;
                }

                y++;
                y += q.MarginTopBottom.Y;

                i++;
            }
        }

        protected override void BindKeys()
        {
            base.BindKeys();
            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Up, Keybind.KeypressState.Press, "Form Dialog: Navigate up (questions)"), (info) =>
            {
                SelectedQuestion = SelectedQuestion - 1;
                if (SelectedQuestion < 0) SelectedQuestion = Questions.Count - 1;
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiBlip);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Down, Keybind.KeypressState.Press, "Form Dialog: Navigate down (questions)"), (info) =>
            {
                SelectedQuestion = (SelectedQuestion + 1) % Questions.Count;
                SFXManager.PlayFX(SFXManager.SoundEffectType.UiBlip);
                Invalidate();
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Left, Keybind.KeypressState.Press, "Form Dialog: Select next preset answer (question)"), (info) =>
            {
                if (Questions.Count > 0)
                {
                    var q = Questions[SelectedQuestion];
                    int p = q.SelectedPreset;
                    if(!(q is ActionButtonQuestion) && p == -1 || p >= 1) SFXManager.PlayFX(SFXManager.SoundEffectType.UiBlip);

                    q.ChooseNextPreset();
                    Invalidate();
                }
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Right, Keybind.KeypressState.Press, "Form Dialog: Select previous preset answer (question)"), (info) =>
            {
                if (Questions.Count > 0)
                {
                    var q = Questions[SelectedQuestion];
                    int p = q.SelectedPreset;
                    if (!(q is ActionButtonQuestion) && p == -1 || p < q.PresetAnswers.Count - 1) SFXManager.PlayFX(SFXManager.SoundEffectType.UiBlip);

                    Questions[SelectedQuestion].ChoosePrevPreset();
                    Invalidate();
                }
            });

            Keybind.BindKey(new Keybind.KeybindInfo(Keys.Enter, Keybind.KeypressState.Press, "Form Dialog: Press selected button"), (info) =>
            {
                if (Questions.Count > 0)
                {
                    (Questions[SelectedQuestion] as ActionButtonQuestion)?.PressButton();
                }
            });
        }

        public override bool ProcessKeyboard(SadConsole.Input.Keyboard info)
        {
            var q = Questions[SelectedQuestion];
            if (!q.FreeInput) return false;
            // Check each key pressed.
            foreach (var key in info.KeysPressed)
            {
                // If the character associated with the key pressed is a printable character, print it
                if (key.Character != '\0')
                {
                    q.Validate((q.AnswerGiven ?? "") + key.Character.ToString());
                    Invalidate();
                }
                // Special character - BACKSPACE
                else if (key.Key == Keys.Back)
                {
                    if(q.AnswerGiven != null)
                    {
                        if(q.AnswerGiven.Length <= 1)
                        {
                            q.ClearAnswer();
                        }
                        else
                        {
                            q.Validate(q.AnswerGiven.Substring(0, q.AnswerGiven.Length - 1));
                        }
                        Invalidate();
                    }
                }
            }
            return false;
        }

        public override void Draw(TimeSpan delta)
        {
            base.Draw(delta);
        }
    }
}
