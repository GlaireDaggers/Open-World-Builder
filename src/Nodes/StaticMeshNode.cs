using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NativeFileDialogSharp;

namespace OpenWorldBuilder
{
    public class StaticMeshNode : Node
    {
        public string meshPath = "";

        private IndexBuffer? _ib;
        private VertexBuffer? _vb;
        private BasicEffect _fx;

        public StaticMeshNode()
        {
            _fx = new BasicEffect(App.Instance!.GraphicsDevice)
            {
                TextureEnabled = false,
                VertexColorEnabled = true,
                LightingEnabled = true,
                AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f)
            };

            _fx.DirectionalLight0.DiffuseColor = new Vector3(1f, 1f, 1f);
            _fx.DirectionalLight0.Direction = new Vector3(-1f, -1f, -1f);
            _fx.DirectionalLight0.SpecularColor = new Vector3(0f, 0f, 0f);
            _fx.DirectionalLight0.Enabled = true;
        }

        public override void Dispose()
        {
            base.Dispose();

            _ib?.Dispose();
            _vb?.Dispose();
            _fx.Dispose();
        }

        public override void Draw(Matrix view, Matrix projection, ViewportWindow viewport)
        {
            base.Draw(view, projection, viewport);

            if (_ib != null && _vb != null)
            {
                _fx.World = World;
                _fx.View = view;
                _fx.Projection = projection;

                App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                App.Instance!.GraphicsDevice.RasterizerState = RasterizerState.CullClockwise;
                App.Instance!.GraphicsDevice.SetVertexBuffer(_vb);
                App.Instance!.GraphicsDevice.Indices = _ib;
                _fx.CurrentTechnique.Passes[0].Apply();
                App.Instance!.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, _vb.VertexCount, 0, _ib.IndexCount / 3);
            }
        }

        public void LoadMesh(string path)
        {
            _ib?.Dispose();
            _vb?.Dispose();

            _ib = null;
            _vb = null;

            if (path.EndsWith(".obj"))
            {
                var fullPath = Path.Combine(App.Instance!.ContentPath, path);

                using var file = File.Open(fullPath, FileMode.Open);
                ObjUtil.ConvertObj(file, App.Instance!.GraphicsDevice, out _vb, out _ib);
            }

            meshPath = path;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();

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