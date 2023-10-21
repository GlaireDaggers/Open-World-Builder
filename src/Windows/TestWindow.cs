using ImGuiNET;

using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenWorldBuilder
{
    public class TestWindow : EditorWindow
    {
        public TestWindow() : base()
        {
            title = "Test Window";
        }

        protected override void OnDraw(GameTime time)
        {
            ImGui.Text("Hello, world!");
            base.OnDraw(time);
        }
    }
}
