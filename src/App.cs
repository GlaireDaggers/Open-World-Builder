using System.Security.Cryptography.X509Certificates;
using ImGuiNET;

using ImGuizmoNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using Newtonsoft.Json;
using ObjLoader.Loader.Loaders;
using SDL2;
using SharpGLTF.Transforms;

namespace OpenWorldBuilder
{
    public delegate void ContentFolderChangedHandler(DirectoryInfo directory);
    public delegate void LevelFolderChangedHandler(DirectoryInfo directory);

    public class App : Game
    {
        public static object? dragPayload = null;
        public static App? Instance { get; private set; }

        public bool consumeMouseCursor = false;

        public ImGuiRenderer? ImGuiRenderer => _imGuiRenderer;

        public Project ActiveProject => _project;
        public Level ActiveLevel => _level;

        public string? ProjectPath => _projectPath;
        public string? ProjectFolder => _projectPath == null ? null : Path.GetDirectoryName(_projectPath);
        public string ContentPath => ProjectFolder == null ? _project.contentPath : Path.Combine(ProjectFolder, _project.contentPath);

        public event ContentFolderChangedHandler OnContentFolderChanged;
        public event LevelFolderChangedHandler OnLevelFolderChanged;
        
        public Node? activeNode;

        public MouseState curMouseState;
        public MouseState prevMouseState;

        public KeyboardState curKeyboardState;
        public KeyboardState prevKeyboardState;

        public IObjLoader objLoader;

        private ImGuiRenderer? _imGuiRenderer;

        private List<AssetNodeFactory> _nodeFactories = new List<AssetNodeFactory>();

        private List<EditorWindow> _windows = new List<EditorWindow>();
        private MenuContainer _rootMenu = new MenuContainer();

        private UserConfig _config = new UserConfig();
        private string _prefPath;
        private string _configPath;

        private Level _level = new Level();
        private Project _project = new Project();
        private string? _projectPath = null;

        private FileSystemWatcher? _contentWatcher;
        private FileSystemWatcher? _levelWatcher;
        private bool _queueUpdateContent = false;
        private bool _queueUpdateLevels = false;

        private Command? _activeCmd = null;
        private Stack<Command> _undoStack = new Stack<Command>();
        private Stack<Command> _redoStack = new Stack<Command>();

        private MenuItem _undoMenuItem;
        private MenuItem _redoMenuItem;

        public App()
        {
            Instance = this;

            GraphicsDeviceManager gdm = new GraphicsDeviceManager(this);
            gdm.PreferredBackBufferWidth = 1024;
            gdm.PreferredBackBufferHeight = 768;
            gdm.SynchronizeWithVerticalRetrace = true;

            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            IsFixedTimeStep = false;

            Window.Title = "Open World Builder";

            _prefPath = SDL.SDL_GetPrefPath("CritChanceStudios", "OpenWorldBuilder");
            _configPath = Path.Combine(_prefPath, "userconfig.json");
        }

        protected override void LoadContent()
        {
            SDL.SDL_MaximizeWindow(Window.Handle);

            var objLoaderFactory = new ObjLoaderFactory();
            objLoader = objLoaderFactory.Create();

            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer!.RebuildFontAtlas();

            if (File.Exists(_configPath))
            {
                var configJson = File.ReadAllText(_configPath);
                _config = JsonConvert.DeserializeObject<UserConfig>(configJson) ?? new UserConfig();

                Console.WriteLine("User config loaded");
            }
            else
            {
                _config = new UserConfig();
            }

            AddNodeFactory(new StaticMeshNodeFactory());

            AddMenuItem("File/Open Project", () => {
                // todo: prompt to save project if changes have been made

                var result = Dialog.FileOpen(".owbproj");
                if (result.IsOk)
                {
                    try
                    {
                        string projData = File.ReadAllText(result.Path);
                        Project proj = JsonConvert.DeserializeObject<Project>(projData)!;
                        _project = proj;
                        _level.Dispose();
                        _level = new Level();
                        _projectPath = result.Path;
                        if (!_config.recentProjects.Contains(result.Path))
                        {
                            _config.recentProjects.Add(result.Path);
                        }
                        UpdateContent();
                        UpdateLevelFolder();
                        ClearUndoRedo();
                    }
                    catch {}
                }
            });

            foreach (var proj in _config.recentProjects)
            {
                string projpath = proj;
                AddMenuItem($"File/Recent Projects/{Path.GetFileName(projpath)}", () => {
                    try
                    {
                        string projData = File.ReadAllText(projpath);
                        Project proj = JsonConvert.DeserializeObject<Project>(projData)!;
                        _project = proj;
                        _level.Dispose();
                        _level = new Level();
                        _projectPath = projpath;
                        UpdateContent();
                        UpdateLevelFolder();
                        ClearUndoRedo();
                    }
                    catch {}
                });
            }

            AddMenuItem("File/Save Project", () => {
                // todo: save currently open level if necessary

                string projData = JsonConvert.SerializeObject(_project);

                if (_projectPath is string path)
                {
                    File.WriteAllText(path, projData);
                }
                else
                {
                    var result = Dialog.FileSave(".owbproj");
                    if (result.IsOk)
                    {
                        File.WriteAllText(result.Path, projData);
                        _projectPath = result.Path;

                        if (!_config.recentProjects.Contains(result.Path))
                        {
                            _config.recentProjects.Add(result.Path);
                        }

                        UpdateLevelFolder();
                    }
                }
            });

            AddMenuItem("File/Save Project As", () => {
                // todo: save currently open level if necessary

                string projData = JsonConvert.SerializeObject(_project);

                var result = Dialog.FileSave(".owbproj");
                if (result.IsOk)
                {
                    File.WriteAllText(result.Path, projData);
                    _projectPath = result.Path;

                    if (!_config.recentProjects.Contains(result.Path))
                    {
                        _config.recentProjects.Add(result.Path);
                    }

                    UpdateLevelFolder();
                }
            });

            AddMenuItem("File/Save Level", () => {
                JsonSerializerSettings settings = new JsonSerializerSettings();
                settings.Converters.Add(new JsonNodeConverter());

                string levelData = JsonConvert.SerializeObject(_level, settings);
                Directory.CreateDirectory(Path.Combine(ProjectFolder!, "levels"));
                File.WriteAllText(Path.Combine(ProjectFolder!, $"levels/{_level.root.name}.owblevel"), levelData);
            });

            _undoMenuItem = AddMenuItem("Edit/Undo", () => {
                Undo();
            });

            _redoMenuItem = AddMenuItem("Edit/Redo", () => {
                Redo();
            });

            AddMenuItem("Nodes/New Node", () => {
                Node node = new Node();
                AddNodeWithUndo("Create Node", node);
            });

            AddMenuItem("Nodes/New Spline", () => {
                SplineNode node = new SplineNode
                {
                    name = "Spline"
                };
                AddNodeWithUndo("Create Spline", node);
            });

            AddMenuItem("Nodes/Lights/New Point Light", () => {
                LightNode node = new LightNode
                {
                    name = "Point Light"
                };
                AddNodeWithUndo("Create Point Light", node);
            });

            AddMenuItem("Nodes/Lights/New Directional Light", () => {
                LightNode node = new LightNode
                {
                    name = "Directional Light",
                    lightType = LightType.Directional
                };
                AddNodeWithUndo("Create Directional Light", node);
            });

            AddMenuItem("Nodes/Lights/New Spot Light", () => {
                LightNode node = new LightNode
                {
                    name = "Spot Light",
                    lightType = LightType.Spot
                };
                AddNodeWithUndo("Create Spot Light", node);
            });

            AddMenuItem("Window/Project Settings", () =>
            {
                GetWindow<ProjectSettingsWindow>();
            });

            AddMenuItem("Window/Project Browser", () =>
            {
                GetWindow<ProjectBrowserWindow>();
            });

            AddMenuItem("Window/Inspector", () =>
            {
                GetWindow<InspectorWindow>();
            });

            AddMenuItem("Window/Hierarchy", () =>
            {
                GetWindow<SceneHierarchyWindow>();
            });

            AddMenuItem("Window/Scene", () =>
            {
                GetWindow<SceneWindow>();
            });

            UpdateUndoRedoMenu();

            // try to restore editor layout
            foreach (var win in _config.openWindows)
            {
                try
                {
                    GetWindow(Type.GetType(win)!);
                }
                catch {}
            }

            base.LoadContent();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            base.OnExiting(sender, args);

            // save project if possible
            if (_projectPath is string path)
            {
                string projData = JsonConvert.SerializeObject(_project);
                File.WriteAllText(path, projData);
            }

            _config.openWindows.Clear();

            // serialize user config
            foreach (var win in _windows)
            {
                _config.openWindows.Add(win.GetType().AssemblyQualifiedName!);
            }

            string configJson = JsonConvert.SerializeObject(_config);
            File.WriteAllText(_configPath, configJson);
        }

        protected override void Draw(GameTime gameTime)
        {
            lock (this)
            {
                if (_queueUpdateContent)
                {
                    _queueUpdateContent = false;
                    
                    try
                    {
                        OnContentFolderChanged?.Invoke(new DirectoryInfo(ContentPath));
                    }
                    catch {}
                }

                if (_queueUpdateLevels)
                {
                    _queueUpdateLevels = false;

                    try
                    {
                        OnLevelFolderChanged?.Invoke(new DirectoryInfo(Path.Combine(ProjectFolder!, "levels")));
                    }
                    catch {}
                }
            }

            prevKeyboardState = curKeyboardState;
            prevMouseState = curMouseState;

            curKeyboardState = Keyboard.GetState();
            curMouseState = Mouse.GetState();

            if (gameTime.ElapsedGameTime.TotalSeconds > 0f)
            {
                base.Draw(gameTime);
                GraphicsDevice.Clear(Color.CornflowerBlue);

                _imGuiRenderer!.BeforeLayout(gameTime);
                DrawUI(gameTime);
                _imGuiRenderer!.AfterLayout();
            }
        }

        private void UpdateUndoRedoMenu()
        {
            _undoMenuItem.enabled = _undoStack.Count > 0;
            _redoMenuItem.enabled = _redoStack.Count > 0;

            if (_undoStack.Count > 0)
            {
                _undoMenuItem.name = "Undo " + _undoStack.Peek().title;
            }
            else
            {
                _undoMenuItem.name = "Undo";
            }

            if (_redoStack.Count > 0)
            {
                _redoMenuItem.name = "Redo " + _redoStack.Peek().title;
            }
            else
            {
                _redoMenuItem.name = "Redo";
            }
        }

        public void AddNodeWithUndo(string title, Node node, Node? parent = null)
        {
            parent ??= activeNode ?? _level.root;

            BeginRecordUndo(title, () => {
                if (node == activeNode)
                {
                    activeNode = null;
                }
                parent.RemoveChild(node);
            });

            EndRecordUndo(() => {
                parent.AddChild(node);
            });

            parent.AddChild(node);
        }

        public void ReparentNodeWithUndo(string title, Node node, Node newParent)
        {
            var prevParent = node.Parent!;

            BeginRecordUndo(title, () => {
                newParent.RemoveChild(node);
                prevParent.AddChild(node);
            });

            EndRecordUndo(() => {
                prevParent.RemoveChild(node);
                newParent.AddChild(node);
            });

            prevParent.RemoveChild(node);
            newParent.AddChild(node);
        }

        public void DeleteNodeWithUndo(string title, Node node)
        {
            var prevParent = node.Parent!;

            BeginRecordUndo(title, () => {
                prevParent.AddChild(node);
            });

            EndRecordUndo(() => {
                prevParent.RemoveChild(node);
            });

            prevParent.RemoveChild(node);
        }

        public void BeginRecordUndo(string title, Action callback)
        {
            _activeCmd = new Command
            {
                title = title,
                undo = callback
            };
        }

        public void EndRecordUndo(Action callback)
        {
            _activeCmd!.execute = callback;
            _undoStack.Push(_activeCmd);
            _redoStack.Clear();
            _activeCmd = null;

            UpdateUndoRedoMenu();
        }

        public void ClearUndoRedo()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            UpdateUndoRedoMenu();
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var cmd = _undoStack.Pop();
                cmd.undo();
                _redoStack.Push(cmd);

                UpdateUndoRedoMenu();
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var cmd = _redoStack.Pop();
                cmd.execute();
                _undoStack.Push(cmd);

                UpdateUndoRedoMenu();
            }
        }

        public T GetWindow<T>() where T : EditorWindow, new()
        {
            foreach (var w in _windows)
            {
                if (w is T)
                {
                    w.Focus();
                    return (T)w;
                }
            }

            T win = new();
            win.Focus();
            _windows.Add(win);

            return win;
        }

        public EditorWindow GetWindow(Type t)
        {
            foreach (var w in _windows)
            {
                if (t.IsAssignableFrom(w.GetType()))
                {
                    w.Focus();
                    return w;
                }
            }

            EditorWindow win = (EditorWindow)Activator.CreateInstance(t)!;
            win.Focus();
            _windows.Add(win);

            return win;
        }

        public MenuItem AddMenuItem(string path, Action callback)
        {
            string[] folders = path.Split('/');

            MenuContainer c = _rootMenu;
            for (int i = 0; i < folders.Length - 1; i++)
            {
                c = c.GetOrCreateSubMenu(folders[i]);
            }

            MenuItem item = new MenuItem(folders[folders.Length - 1], callback);
            c.subItems.Add(item);

            return item;
        }

        public void AddNodeFactory(AssetNodeFactory nodeFactory)
        {
            _nodeFactories.Add(nodeFactory);
        }

        public bool HasNodeFactory(string assetPath)
        {
            foreach (var factory in _nodeFactories)
            {
                if (factory.CanHandle(assetPath))
                {
                    return true;
                }
            }

            return false;
        }

        public Node? TryCreateNode(string assetPath)
        {
            foreach (var factory in _nodeFactories)
            {
                if (factory.CanHandle(assetPath))
                {
                    //try
                    {
                        return factory.Process(assetPath);
                    }
                    //catch {}
                }
            }

            Console.WriteLine("No node factories for asset: " + assetPath);
            return null;
        }

        public void ChangeLevel(Level level)
        {
            _level.Dispose();
            _level = level;
            level.root.OnLoad();
            ClearUndoRedo();
        }

        public void UpdateLevelFolder()
        {
            string levelFolder = Path.Combine(ProjectFolder!, "levels");
            Directory.CreateDirectory(levelFolder);

            try
            {
                if (_levelWatcher != null)
                {
                    _levelWatcher.EnableRaisingEvents = false;
                    _levelWatcher.Dispose();
                }

                _levelWatcher = new FileSystemWatcher(levelFolder)
                {
                    NotifyFilter = NotifyFilters.FileName
                };

                _levelWatcher.Created += (sender, e) => {
                    lock (this)
                    {
                        _queueUpdateLevels = true;
                    }
                };

                _levelWatcher.Deleted += (sender, e) => {
                    lock (this)
                    {
                        _queueUpdateLevels = true;
                    }
                };

                _levelWatcher.Renamed += (sender, e) => {
                    lock (this)
                    {
                        _queueUpdateLevels = true;
                    }
                };

                _levelWatcher.EnableRaisingEvents = true;

                OnLevelFolderChanged?.Invoke(new DirectoryInfo(levelFolder));
            }
            catch {}
        }

        public void UpdateContent()
        {
            try
            {
                if (_contentWatcher != null)
                {
                    _contentWatcher.EnableRaisingEvents = false;
                    _contentWatcher.Dispose();
                }

                _contentWatcher = new FileSystemWatcher(ContentPath)
                {
                    NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName,
                    IncludeSubdirectories = true
                };

                _contentWatcher.Created += (sender, e) => {
                    lock (this)
                    {
                        _queueUpdateContent = true;
                    }
                };

                _contentWatcher.Deleted += (sender, e) => {
                    lock (this)
                    {
                        _queueUpdateContent = true;
                    }
                };

                _contentWatcher.Renamed += (sender, e) => {
                    lock (this)
                    {
                        _queueUpdateContent = true;
                    }
                };

                _contentWatcher.EnableRaisingEvents = true;

                OnContentFolderChanged?.Invoke(new DirectoryInfo(ContentPath));
            }
            catch {}
        }

        public void SetContentPath(string newPath)
        {
            _project.contentPath = newPath;
            UpdateContent();
        }

        protected void DrawUI(GameTime time)
        {
            ImGui.DockSpaceOverViewport();

            if (ImGui.BeginMainMenuBar())
            {
                DrawMenu(_rootMenu);
                ImGui.EndMainMenuBar();
            }

            foreach (var w in _windows)
            {
                w.Draw(time);
            }

            _windows.RemoveAll(x => x.IsOpen == false);
        }

        private void DrawMenu(MenuContainer c)
        {
            foreach (var m in c.subMenus)
            {
                if (ImGui.BeginMenu(m.name))
                {
                    DrawMenu(m);
                    ImGui.EndMenu();
                }
            }

            foreach (var m in c.subItems)
            {
                if (ImGui.MenuItem(m.name, m.enabled))
                {
                    m.callback();
                }
            }
        }
    }
}
