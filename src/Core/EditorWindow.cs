using ImGuiNET;

using Microsoft.Xna.Framework;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (ImGui.Begin(title + "##" + _id, ref _open))
            {
                OnDraw(time);
                ImGui.End();
            }
            PostDraw();
        }

        protected virtual void OnDraw(GameTime time)
        {
        }

        public virtual void Dispose()
        {
        }
    }
}
