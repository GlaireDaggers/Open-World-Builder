using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{

    public class ProjectBrowserWindow : EditorWindow
    {
        private struct ProjectItem
        {
            public string name;
            public string path;
        }

        private class ProjectFolder
        {
            public string name = "";
            public List<ProjectFolder> folders = new List<ProjectFolder>();
            public List<ProjectItem> files = new List<ProjectItem>();
        }

        private ProjectFolder _contentFolder = new ProjectFolder();
        private ProjectFolder _levelFolder = new ProjectFolder();

        public ProjectBrowserWindow() : base()
        {
            title = "Project Browser";
            App.Instance!.OnContentFolderChanged += (newDir) => {
                Console.WriteLine("Content change detected, refreshing browser...");
                if (newDir != null)
                {
                    RebuildFolder(_contentFolder, newDir, newDir);
                }
                else
                {
                    _contentFolder = new ProjectFolder();
                }
            };
            App.Instance!.OnLevelFolderChanged += (newDir) => {
                Console.WriteLine("Level folder change detected, refreshing browser...");
                if (newDir != null)
                {
                    RebuildFolder(_levelFolder, newDir, newDir);
                }
                else
                {
                    _levelFolder = new ProjectFolder();
                }
            };
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            if (ImGui.TreeNodeEx("Levels"))
            {
                DrawLevelBrowser(_levelFolder);
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Entities"))
            {
                DrawEntityBrowser();
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Content"))
            {
                DrawContentBrowser(_contentFolder);
                ImGui.TreePop();
            }
        }

        private void DrawLevelBrowser(ProjectFolder folder)
        {
            foreach (var subdir in folder.folders)
            {
                if (ImGui.TreeNodeEx(subdir.name))
                {
                    DrawLevelBrowser(subdir);
                    ImGui.TreePop();
                }
            }

            foreach (var file in folder.files)
            {
                bool isLevel = file.name.EndsWith(".owblevel");

                if (!isLevel)
                {
                    ImGui.BeginDisabled();
                }

                if (ImGui.TreeNodeEx(file.name, ImGuiTreeNodeFlags.Leaf))
                {
                    if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                    {
                        // load level
                        try
                        {
                            JsonSerializerSettings settings = new JsonSerializerSettings();
                            settings.Converters.Add(new JsonNodeConverter());

                            string levelPath = Path.Combine(App.Instance!.ProjectFolder!, "levels", file.path);
                            string levelJson = File.ReadAllText(levelPath);
                            Level level = JsonConvert.DeserializeObject<Level>(levelJson, settings)!;
                            App.Instance!.ChangeLevel(levelPath, level);
                            level.root.PostLoad();
                        }
                        catch {}
                    }
                    ImGui.TreePop();
                }

                if (!isLevel)
                {
                    ImGui.EndDisabled();
                }
            }
        }

        private void DrawEntityBrowser()
        {
            foreach (var ent in App.Instance!.ActiveProject.entityDefinitions)
            {
                if (ImGui.TreeNodeEx(ent.name, ImGuiTreeNodeFlags.Leaf))
                {
                    if (ImGui.BeginDragDropSource())
                    {
                        App.dragPayload = ent;
                        ImGui.SetDragDropPayload("ENTITYDEF", 0, 0);
                        ImGui.EndDragDropSource();
                    }
                    ImGui.TreePop();
                }
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
                bool hasNodeFactory = App.Instance!.HasNodeFactory(file.path);

                if (!hasNodeFactory)
                {
                    ImGui.BeginDisabled();
                }

                if (ImGui.TreeNodeEx(file.name, ImGuiTreeNodeFlags.Leaf))
                {
                    if (hasNodeFactory)
                    {
                        if (ImGui.BeginDragDropSource())
                        {
                            App.dragPayload = file.path;
                            ImGui.SetDragDropPayload("ASSET", 0, 0);
                            ImGui.EndDragDropSource();
                        }
                    }
                    ImGui.TreePop();
                }

                if (!hasNodeFactory)
                {
                    ImGui.EndDisabled();
                }
            }
        }

        private void RebuildFolder(ProjectFolder target, DirectoryInfo dir, DirectoryInfo parentDir)
        {
            target.name = dir.Name;
            target.folders.Clear();
            target.files.Clear();

            foreach (var subdir in dir.GetDirectories())
            {
                ProjectFolder f = new ProjectFolder();
                RebuildFolder(f, subdir, parentDir);
                target.folders.Add(f);
            }

            foreach (var file in dir.GetFiles())
            {
                target.files.Add(new ProjectItem
                {
                    name = file.Name,
                    path = Path.GetRelativePath(parentDir.FullName, file.FullName)
                });
            }
        }
    }
}