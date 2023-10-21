using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    public class SceneHierarchyWindow : EditorWindow
    {
        public SceneHierarchyWindow() : base()
        {
            title = "Hierarchy";
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            Node? selectedNode = null;
            DrawNode(App.Instance!.ActiveLevel.root, ref selectedNode);

            if (selectedNode != null)
            {
                App.Instance!.activeNode = selectedNode;
            }
            else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && ImGui.IsWindowHovered())
            {
                App.Instance!.activeNode = null;
            }
        }

        private void DrawNode(Node node, ref Node? selectedNode)
        {
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.DefaultOpen;

            if (node == App.Instance!.activeNode)
            {
                flags |= ImGuiTreeNodeFlags.Selected;
            }

            if (node.Children.Count == 0)
            {
                flags |= ImGuiTreeNodeFlags.Leaf;
            }

            bool isOpen = ImGui.TreeNodeEx(node.name, flags);

            if (ImGui.IsItemClicked())
            {
                selectedNode = node;
            }

            if (isOpen)
            {
                foreach (var child in node.Children)
                {
                    DrawNode(child, ref selectedNode);
                }
                ImGui.TreePop();
            }
        }
    }
}