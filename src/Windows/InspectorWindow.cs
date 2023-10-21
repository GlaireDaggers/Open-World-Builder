using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    public class InspectorWindow : EditorWindow
    {
        public InspectorWindow() : base()
        {
            title = "Inspector";
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            if (App.Instance!.activeNode != null)
            {
                App.Instance!.activeNode.DrawInspector();
            }
            else
            {
                ImGui.Text("Nothing selected");
            }
        }
    }
}