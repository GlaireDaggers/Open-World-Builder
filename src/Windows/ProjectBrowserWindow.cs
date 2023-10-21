using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{

    public class ProjectBrowserWindow : EditorWindow
    {
        private class ProjectFolder
        {
            public string name = "";
            public List<ProjectFolder> folders = new List<ProjectFolder>();
            public List<string> files = new List<string>();
        }

        private ProjectFolder _contentFolder = new ProjectFolder();

        public ProjectBrowserWindow() : base()
        {
            title = "Project Browser";
            App.Instance!.OnContentFolderChanged += (newDir) => {
                Console.WriteLine("Content change detected, refreshing browser...");
                RebuildFolder(_contentFolder, newDir);
            };
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            if (ImGui.TreeNodeEx("Levels"))
            {
                foreach (var level in App.Instance!.ActiveProject.levels)
                {
                    if (ImGui.TreeNodeEx(level, ImGuiTreeNodeFlags.Leaf))
                    {
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Content"))
            {
                DrawContentBrowser(_contentFolder);
                ImGui.TreePop();
            }
        }

        private void DrawContentBrowser(ProjectFolder folder)
        {
            foreach (var subdir in folder.folders)
            {
                if (ImGui.TreeNodeEx(subdir.name))
                {
                    DrawContentBrowser(subdir);
                    ImGui.TreePop();
                }
            }

            foreach (var file in folder.files)
            {
                if (ImGui.TreeNodeEx(file, ImGuiTreeNodeFlags.Leaf))
                {
                    if (ImGui.BeginDragDropSource())
                    {
                        App.dragPayload = file;
                        ImGui.SetDragDropPayload("ASSET", 0, 0);
                        ImGui.EndDragDropSource();
                    }
                    ImGui.TreePop();
                }
            }
        }

        private void RebuildFolder(ProjectFolder target, DirectoryInfo dir)
        {
            target.name = dir.Name;
            target.folders.Clear();
            target.files.Clear();

            foreach (var subdir in dir.GetDirectories())
            {
                ProjectFolder f = new ProjectFolder();
                RebuildFolder(f, subdir);
                target.folders.Add(f);
            }

            foreach (var file in dir.GetFiles())
            {
                target.files.Add(file.Name);
            }
        }
    }
}