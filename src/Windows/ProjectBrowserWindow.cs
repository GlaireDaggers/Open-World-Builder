using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    public class ProjectBrowserWindow : EditorWindow
    {
        public ProjectBrowserWindow() : base()
        {
            title = "Project Browser";
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            if (ImGui.TreeNodeEx("Levels"))
            {
                foreach (var level in App.Instance!.ActiveProject.levels)
                {
                    ImGui.TreeNodeEx(level, ImGuiTreeNodeFlags.Leaf);
                }
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Content"))
            {
                ImGui.TreePop();
            }
        }
    }
}