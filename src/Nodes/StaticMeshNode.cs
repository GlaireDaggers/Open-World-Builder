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

        public override void Draw(Matrix view, Matrix projection, ViewportWindow viewport)
        {
            /*base.Draw(view, projection, viewport);

            var vp = view * projection;

            _directionalLightFwd[0] = new Vector3(1f, -1f, 1f);
            _directionalLightFwd[0].Normalize();

            _directionalLightColor[0] = new Vector3(1f, 1f, 1f);

            if (_model != null)
            {
                var transform = World;

                foreach (var mat in _model.materials)
                {
                    mat.Parameters["ViewProjection"].SetValue(vp);
                    mat.Parameters["DirectionalLightCount"].SetValue(1);

                    mat.Parameters["DirectionalLightFwd"].SetValue(_directionalLightFwd);
                    mat.Parameters["DirectionalLightCol"].SetValue(_directionalLightColor);
                }
                
                foreach (var node in _model.nodes)
                {
                    var mesh = _model.meshes[node.meshIdx];
                    var nodeTransform = node.transform * transform;

                    foreach (var mat in _model.materials)
                    {
                        mat.Parameters["World"].SetValue(nodeTransform);
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
            }*/
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