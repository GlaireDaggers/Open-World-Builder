using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    public class SceneWindow : ViewportWindow
    {
        private bool _localSpace = false;

        public SceneWindow() : base()
        {
            title = "Scene";
        }

        protected override void DrawViewport(Matrix view, Matrix proj)
        {
            base.DrawViewport(view, proj);
            DrawNode(App.Instance!.ActiveLevel.root, view, proj);
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            ImGui.Checkbox("Local Space", ref _localSpace);
        }

        protected override void DrawHandles(Matrix view, Matrix projection)
        {
            base.DrawHandles(view, projection);
            
            if (App.Instance!.activeNode != null)
            {
                App.Instance!.activeNode.DrawHandles(view, projection, this, _localSpace);
            }
        }

        private void DrawNode(Node node, Matrix view, Matrix proj)
        {
            node.Draw(view, proj);

            foreach (var child in node.Children)
            {
                DrawNode(child, view, proj);
            }
        }
    }
}