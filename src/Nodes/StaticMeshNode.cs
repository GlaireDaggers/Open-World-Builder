using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NativeFileDialogSharp;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{
    public enum CollisionType
    {
        None,
        Collide,
        Trigger,
    }

    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("StaticMeshNode")]
    public class StaticMeshNode : Node
    {
        [JsonProperty]
        public string meshPath = "";

        [JsonProperty]
        public bool visible = true;

        [JsonProperty]
        public CollisionType collision = CollisionType.None;
        
        public GltfModel? Model => _model;

        private GltfModel? _model;

        public override void OnLoad()
        {
            base.OnLoad();

            try
            {
                LoadMesh(meshPath);
            }
            catch {}
        }

        public override void Dispose()
        {
            base.Dispose();

            _model?.Dispose();
            _model = null;
        }

        public override void Draw(Matrix view, Matrix projection, ViewportWindow viewport, bool selected)
        {
            base.Draw(view, projection, viewport, selected);

            if (collision != CollisionType.None && selected)
            {
                if (_model != null)
                {
                    var transform = World;

                    foreach (var node in _model.nodes)
                    {
                        var mesh = _model.meshes[node.meshIdx];
                        var nodeTransform = node.transform * transform;

                        foreach (var meshpart in mesh.meshParts)
                        {
                            viewport.DrawWireframeMeshGizmo(nodeTransform, meshpart.ib, meshpart.vb, Color.DarkCyan);
                        }
                    }
                }
            }
        }

        public void LoadMesh(string path)
        {
            _model?.Dispose();
            _model = null;
            
            var fullPath = Path.Combine(App.Instance!.ContentPath, path);

            if (path.EndsWith(".obj"))
            {
                using var file = File.Open(fullPath, FileMode.Open);

                ObjUtil.ConvertObj(file, App.Instance!.GraphicsDevice, out var vb, out var ib);

                _model = new GltfModel
                {
                    materials = new List<RenderMaterial>()
                    {
                        new RenderMaterial
                        {
                            albedo = new Color(1f, 1f, 1f),
                            roughness = 1f,
                            metallic = 0f,
                        }
                    },
                    meshes = new List<GltfMesh>()
                    {
                        new GltfMesh
                        {
                            meshParts = new List<GltfMeshPart>()
                            {
                                new GltfMeshPart
                                {
                                    materialIdx = 0,
                                    ib = ib,
                                    vb = vb,
                                }
                            }
                        }
                    },
                    nodes = new List<GltfModel.ModelNode>()
                    {
                        new GltfModel.ModelNode
                        {
                            meshIdx = 0,
                            transform = Matrix.Identity
                        }
                    },
                };
            }
            else if (path.EndsWith(".gltf") || path.EndsWith(".glb"))
            {
                _model = GltfUtil.ConvertGltf(fullPath, App.Instance!.GraphicsDevice);
            }

            meshPath = path;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();

            ImGui.Spacing();

            bool prevVisible = visible;
            if (ImGui.Checkbox("Visible", ref visible))
            {
                var newVisible = visible;
                App.Instance!.BeginRecordUndo("Change Mesh Visible", () => {
                    visible = prevVisible;
                });
                App.Instance!.EndRecordUndo(() => {
                    visible = newVisible;
                });
            }

            int col = (int)collision;
            var prevCollision = collision;
            if (ImGui.Combo("Collision Type", ref col, "None\0Collide\0Trigger"))
            {
                App.Instance!.BeginRecordUndo("Change Mesh Collision Type", () => {
                    collision = prevCollision;
                });
                App.Instance!.EndRecordUndo(() => {
                    collision = (CollisionType)col;
                });
            }
            collision = (CollisionType)col;

            ImGui.Spacing();

            var prevMeshPath = meshPath;
            ImGui.InputText("Mesh", ref meshPath, 1024);

            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Change Mesh Path", () => {
                    meshPath = prevMeshPath;
                    try
                    {
                        LoadMesh(meshPath);
                    }
                    catch {}
                });
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                var newMeshPath = meshPath;
                App.Instance!.EndRecordUndo(() => {
                    meshPath = newMeshPath;
                    try
                    {
                        LoadMesh(meshPath);
                    }
                    catch {}
                });

                try
                {
                    LoadMesh(meshPath);
                }
                catch {}
            }

            ImGui.SameLine();
            if (ImGui.Button("Browse"))
            {
                var result = Dialog.FileOpen("obj,gltf,glb", App.Instance!.ContentPath);
                if (result.IsOk)
                {
                    var assetPath = Path.GetRelativePath(App.Instance!.ContentPath!, result.Path);

                    App.Instance!.BeginRecordUndo("Change Mesh Path", () => {
                        meshPath = prevMeshPath;
                        try
                        {
                            LoadMesh(prevMeshPath);
                        }
                        catch {}
                    });
                    App.Instance!.EndRecordUndo(() => {
                        meshPath = assetPath;
                        try
                        {
                            LoadMesh(assetPath);
                        }
                        catch {}
                    });

                    try
                    {
                        LoadMesh(assetPath);
                    }
                    catch {}
                }
            }
        }
    }
}