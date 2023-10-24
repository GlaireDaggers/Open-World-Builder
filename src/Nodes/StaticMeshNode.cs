using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NativeFileDialogSharp;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{
    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("StaticMeshNode")]
    public class StaticMeshNode : Node
    {
        [JsonProperty]
        public string meshPath = "";

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

        public override void Draw(Matrix view, Matrix projection, ViewportWindow viewport)
        {
            base.Draw(view, projection, viewport);

            if (_model != null)
            {
                var transform = World;

                foreach (var mat in _model.materials)
                {
                    mat.View = view;
                    mat.Projection = projection;

                    mat.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
                    mat.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
                    mat.DirectionalLight0.Direction = new Vector3(-1f, -1f, -1f);
                    mat.DirectionalLight0.SpecularColor = new Vector3(0f, 0f, 0f);
                    mat.DirectionalLight0.Enabled = true;
                }
                
                foreach (var node in _model.nodes)
                {
                    var mesh = _model.meshes[node.meshIdx];
                    var nodeTransform = node.transform * transform;

                    foreach (var mat in _model.materials)
                    {
                        mat.World = nodeTransform;
                    }

                    App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                    App.Instance!.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;

                    foreach (var meshpart in mesh.meshParts)
                    {
                        App.Instance!.GraphicsDevice.SetVertexBuffer(meshpart.vb);
                        App.Instance!.GraphicsDevice.Indices = meshpart.ib;
                        _model.materials[meshpart.materialIdx].CurrentTechnique.Passes[0].Apply();
                        App.Instance!.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, meshpart.vb.VertexCount, 0, meshpart.ib.IndexCount / 3);
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
                    materials = new List<BasicEffect>()
                    {
                        new BasicEffect(App.Instance!.GraphicsDevice)
                        {
                            TextureEnabled = false,
                            VertexColorEnabled = true,
                            LightingEnabled = true,
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

            if (ImGui.InputText("Mesh", ref meshPath, 1024, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                try
                {
                    LoadMesh(meshPath);
                }
                catch {}
            }
            ImGui.SameLine();
            if (ImGui.Button("Browse"))
            {
                var result = Dialog.FileOpen("obj", App.Instance!.ActiveProject.contentPath);
                if (result.IsOk)
                {
                    var assetPath = Path.GetRelativePath(App.Instance!.ActiveProject.contentPath, result.Path);

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