using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Renderers;
using SadConsole.Shapes;
using SadConsole.Surfaces;
using System;

namespace FieryOpal.Src.Ui.Windows
{
    public class OpalConsoleWindow : SadConsole.Window, IPipelineSubscriber<OpalConsoleWindow>
    {
        public SadConsole.Surfaces.BasicSurface BorderSurface { get; }
        public SadConsole.Surfaces.BasicSurface CaptionSurface { get; }

        public Guid Handle { get; }
        public string Caption { get; set; }
        public bool Borderless { get; set; }

        public OpalConsoleWindow(int width, int height, string caption = "Untitled", Font f = null) : base(width - 2, height - 2)
        {
            TextSurface.Font = f ?? Nexus.Fonts.MainFont;

            CaptionSurface = new BasicSurface(caption.Length, 1, TextSurface.Font);
            new SurfaceEditor(CaptionSurface).Print(0, 0, caption.ToColoredString(Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]));

            BorderSurface = new BasicSurface(width, height, TextSurface.Font);

            var editor = new SurfaceEditor(BorderSurface);

            Box box = Box.GetDefaultBox();
            box.Width = BorderSurface.Width;
            box.Height = BorderSurface.Height;
            box.Foreground = Palette.Ui["DefaultForeground"];
            box.BorderBackground = Palette.Ui["DefaultBackground"];
            box.Draw(editor);

            // The following line is requierd to avoid a NPE
            ((ControlsConsoleRenderer)Renderer).Controls = new System.Collections.Generic.List<SadConsole.Controls.ControlBase>();

            // Assign a new handle to this window, used by MessagePipelines as addresses
            Handle = Guid.NewGuid();
            // Set the caption
            Caption = caption;
            Borderless = false;

            Theme = new SadConsole.Themes.WindowTheme()
            {
                FillStyle = new Cell(Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]),
                BorderStyle = new Cell(Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]),
                TitleStyle = new Cell(Palette.Ui["DefaultForeground"], Palette.Ui["DefaultBackground"]),
            };

            Redraw();
        }

        public override void Draw(TimeSpan delta)
        {
            if (!Borderless)
            {
                base.Renderer.Render(BorderSurface);
                base.Renderer.Render(CaptionSurface);
                Global.DrawCalls.Add(new DrawCallSurface(BorderSurface, Position, UsePixelPositioning));
                Global.DrawCalls.Add(new DrawCallSurface(CaptionSurface, Position + new Point(1, 0), UsePixelPositioning));
            }

            // Store current position
            Point oldPos = Position;
            Position += new Point(1);
            base.Draw(delta);
            // Restore the position to its intended value
            Position = oldPos;
        }

        public void VPrint(int x, int y, ColoredString s)
        {
            for (int j = 0; j < s.Count; ++j)
            {
                Print(x, y + j, s.SubString(j, 1));
            }
        }

        public virtual void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalConsoleWindow, string> action, bool is_broadcast)
        {
            string performed_action = action.Invoke(this);
        }

        /// <summary>
        /// Called when RegisterWindow of a WindowManager is called on this window.
        /// </summary>
        /// <param name="wm">The WindowManager that just registered this Window.</param>
        public virtual void OnWindowManagerRegistration(WindowManager wm) { }

        /// <summary>
        /// Called when UnregisterWindow of a WindowManager is called on this window.
        /// </summary>
        /// <param name="wm">The WindowManager that just unregistered this Window.</param>
        public virtual void OnWindowManagerUnregistration(WindowManager wm) { }
    }

}
