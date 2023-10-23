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

            if (App.Instance!.ProjectPath == null)
            {
                ImGui.BeginDisabled();
            }

            if (ImGui.InputText("Content Path", ref App.Instance!.ActiveProject.contentPath, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                App.Instance!.SetContentPath(App.Instance!.ActiveProject.contentPath);
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Browse"))
            {
                var result = Dialog.FolderPicker(App.Instance!.ActiveProject.contentPath);
                if (result.IsOk)
                {
                    string relPath = Path.GetRelativePath(App.Instance!.ProjectFolder!, result.Path);
                    App.Instance!.SetContentPath(relPath);
                }
            }

            if (App.Instance!.ProjectPath == null)
            {
                ImGui.EndDisabled();
            }
        }
    }
}