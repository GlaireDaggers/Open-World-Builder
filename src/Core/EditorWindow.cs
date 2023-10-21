using ImGuiNET;

using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    /// <summary>
    /// Base class for an editor window
    /// </summary>
    public class EditorWindow : IDisposable
    {
        private static int _nextId = 0;

        public bool IsOpen => _open;

        public string title;

        private int _id;
        private bool _open;

        private bool _queueFocus = false;

        public EditorWindow()
        {
            _id = _nextId++;
            _open = true;
            title = "Editor Window";
        }

        public virtual void PreDraw()
        {
        }

        public virtual void PostDraw()
        {
        }

        public void Draw(GameTime time)
        {
            PreDraw();

            if (_queueFocus)
            {
                ImGui.SetNextWindowFocus();
                _queueFocus = false;
            }

            if (ImGui.Begin(title + "##" + _id, ref _open))
            {
                OnDraw(time);
            }
            ImGui.End();
            PostDraw();
        }

        protected virtual void OnDraw(GameTime time)
        {
        }

        public virtual void Dispose()
        {
        }

        public void Focus()
        {
            _queueFocus = true;
        }
    }
}
