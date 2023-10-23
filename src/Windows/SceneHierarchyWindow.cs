using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    public class SceneHierarchyWindow : EditorWindow
    {
        private struct QueuedReparent
        {
            public Node node;
            public Node newParent;
        }

        private Queue<QueuedReparent> _reparent = new Queue<QueuedReparent>();

        public SceneHierarchyWindow() : base()
        {
            title = "Hierarchy";
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            Node? selectedNode = null;
            DrawNode(App.Instance!.ActiveLevel.root, ref selectedNode);

            while (_reparent.Count > 0)
            {
                var op = _reparent.Dequeue();
                op.node.Parent?.RemoveChild(op.node);
                op.newParent.AddChild(op.node);
            }

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

            if (ImGui.BeginDragDropSource())
            {
                App.dragPayload = node;
                ImGui.SetDragDropPayload("NODE", 0, 0);
                ImGui.EndDragDropSource();
            }

            if (ImGui.IsItemClicked())
            {
                selectedNode = node;
            }

            if (isOpen)
            {
                if (ImGui.BeginDragDropTarget())
                {
                    var payload = ImGui.AcceptDragDropPayload("ASSET");
                    unsafe
                    {
                        if (payload.NativePtr != null)
                        {
                            Console.WriteLine("ACCEPT: " + App.dragPayload);

                            // try to construct a node as child
                            if (App.Instance!.TryCreateNode((string)App.dragPayload!) is Node newNode)
                            {
                                node.AddChild(newNode);
                            }
                            
                            App.dragPayload = null;
                        }
                    }

                    payload = ImGui.AcceptDragDropPayload("NODE");
                    unsafe
                    {
                        if (payload.NativePtr != null)
                        {
                            Node payloadNode = (Node)App.dragPayload!;
                            Console.WriteLine("ACCEPT NODE: " + payloadNode.name);

                            if (payloadNode.Parent != node)
                            {
                                _reparent.Enqueue(new QueuedReparent
                                {
                                    node = payloadNode,
                                    newParent = node
                                });
                            }
                        }
                    }
                    ImGui.EndDragDropTarget();
                }

                foreach (var child in node.Children)
                {
                    DrawNode(child, ref selectedNode);
                }
                ImGui.TreePop();
            }
        }
    }
}