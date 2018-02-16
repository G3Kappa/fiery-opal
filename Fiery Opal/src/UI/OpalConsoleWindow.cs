using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Shapes;
using SadConsole.Surfaces;
using System;

namespace FieryOpal.src.ui
{
    public class OpalConsoleWindow : SadConsole.Console, IPipelineSubscriber<OpalConsoleWindow>
    {
        SadConsole.Console borderSurfaceConsole;

        public Guid Handle { get; }
        public string Caption { get; set; }

        public OpalConsoleWindow(int width, int height, string caption = "Untitled", Font f = null) : base(width - 2, height - 2)
        {
            TextSurface.Font = f ?? Program.Font;

            // Render the border and wrap it inside a console in order to print the caption
            BasicSurface borderSurface = new BasicSurface(width, height, base.textSurface.Font);
            var editor = new SurfaceEditor(borderSurface);

            Box box = Box.GetDefaultBox();
            box.Width = borderSurface.Width;
            box.Height = borderSurface.Height;
            box.Draw(editor);

            borderSurfaceConsole = new SadConsole.Console(borderSurface);

            // Assign a new handle to this window, used by MessagePipelines as addresses
            Handle = Guid.NewGuid();
            // Set the caption
            Caption = caption;
        }

        public override void Draw(TimeSpan delta)
        {
            // Store current position
            Point oldPos = Position;
            // Set the bordered surface the be rendered at this position
            borderSurfaceConsole.Position = oldPos;
            // Add 1,1 to this position so that Print() doesn't need that offset every time
            Position += new Point(1);
            // Print the caption at 1,0 on the bordered surface
            borderSurfaceConsole.Print(1, 0, Caption, Color.White, Color.Black);
            // Draw both surfaces
            borderSurfaceConsole.Draw(delta);
            base.Draw(delta);
            // Restore the position to its intended value
            Position = oldPos;
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
