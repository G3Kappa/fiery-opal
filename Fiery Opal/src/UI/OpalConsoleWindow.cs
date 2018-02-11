using Microsoft.Xna.Framework;
using SadConsole;
using SadConsole.Shapes;
using SadConsole.Surfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FieryOpal.src.UI
{
    public class OpalConsoleWindow : SadConsole.Console, IPipelineSubscriber<OpalConsoleWindow>
    {
        BasicSurface borderSurface;

        public Guid Handle { get; }
        public string Caption { get; set; }

        public OpalConsoleWindow(int width, int height, string caption = "Untitled") : base(width, height)
        {
            // Render the border
            borderSurface = new BasicSurface(width, height, base.textSurface.Font);

            var editor = new SurfaceEditor(borderSurface);

            Box box = Box.GetDefaultBox();
            box.Width = borderSurface.Width;
            box.Height = borderSurface.Height;
            box.Draw(editor);

            base.Renderer.Render(borderSurface);
            // Assign a new handle
            Handle = Guid.NewGuid();

            Caption = caption;
        }

        public override void Draw(TimeSpan delta)
        {
            Global.DrawCalls.Add(new DrawCallSurface(borderSurface, this.CalculatedPosition, UsePixelPositioning));
            Print(1, 0, Caption, Color.White, Color.Black);
            base.Draw(delta);
        }

        public virtual void ReceiveMessage(Guid pipeline_handle, Guid sender_handle, Func<OpalConsoleWindow, string> action, bool is_broadcast) {
            string performed_action = action.Invoke(this);
            //Print(1, 1, performed_action);
        }

        public virtual void OnWindowManagerRegistration(WindowManager wm) { }
    }
}
