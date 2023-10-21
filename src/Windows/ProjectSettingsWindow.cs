using ImGuiNET;
using Microsoft.Xna.Framework;
using NativeFileDialogSharp;

namespace OpenWorldBuilder
{
    public class ProjectSettingsWindow : EditorWindow
    {
        public ProjectSettingsWindow() : base()
        {
            title = "Project Settings";
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            ImGui.InputText("Content Path", ref App.Instance!.ActiveProject.contentPath, 1024);
            ImGui.SameLine();
            if (ImGui.Button("Browse"))
            {
                var result = Dialog.FolderPicker(App.Instance!.ActiveProject.contentPath);
                if (result.IsOk)
                {
                    App.Instance!.SetContentPath(result.Path);
                }
            }
        }
    }
}