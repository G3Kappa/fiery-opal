using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SadConsole;

namespace FieryOpal.Src.Ui
{
    public static class ShaderManager
    {
        public static Effect LightingShader;

        public static void LoadContent(ContentManager cm)
        {
            LightingShader = cm.Load<Effect>(@"shaders\lighting");
        }
    }
}
