using ImGuiNET;

using ImGuizmoNET;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace OpenWorldBuilder
{
    public struct CameraParams
    {
        public Vector3 pos;
        public Quaternion rot;
        public float near;
        public float far;
        public float fov;
        public float orthoSize;
        public bool isOrtho;

        public Matrix CalcView()
        {
            return Matrix.CreateTranslation(-pos) * Matrix.CreateFromQuaternion(Quaternion.Inverse(rot));
        }

        public Matrix CalcProjection(float aspect)
        {
            if (isOrtho)
            {
                return Matrix.CreateOrthographic(orthoSize * aspect, orthoSize, near, far);
            }
            else
            {
                return Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(fov), aspect, near, far);
            }
        }
    }

    public enum TransformOperation
    {
        Translate,
        Rotate,
        Scale
    }

    /// <summary>
    /// Base class for a window containing a 3D viewport with navigation
    /// </summary>
    public class ViewportWindow : EditorWindow
    {
        private VertexDeclaration GridVertDeclaration;

        private struct GridVert
        {
            public Vector4 pos;
            public Color col;
        }

        public CameraParams camera;

        private RenderTarget2D? _viewportRT;
        private IntPtr _viewportRtHandle;

        private bool _isDraggingView;

        private Matrix _cachedView;
        private Matrix _cachedProj;

        private TransformOperation _transformOp = TransformOperation.Translate;

        private RasterizerState _gridRS;
        private BasicEffect _gridEffect;
        private VertexBuffer _gridVB;
        private IndexBuffer _gridIB;

        public ViewportWindow() : base()
        {
            title = "Viewport Window";

            _gridEffect = new BasicEffect(App.Instance!.GraphicsDevice);
            _gridEffect.LightingEnabled = false;
            _gridEffect.TextureEnabled = false;
            _gridEffect.VertexColorEnabled = true;

            _gridRS = new RasterizerState();
            _gridRS.CullMode = CullMode.None;
            _gridRS.FillMode = FillMode.WireFrame;

            // build grid mesh
            unsafe
            {
                GridVertDeclaration = new VertexDeclaration(
                    sizeof(GridVert),

                    // Position
                    new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.Position, 0),

                    // Color
                    new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
                );
            }
            BuildGrid(out _gridVB, out _gridIB);

            camera = new CameraParams()
            {
                pos = new Vector3(0f, 5f, 10f),
                rot = Quaternion.Identity,
                near = 0.01f,
                far = 1000f,
                fov = 60f,
            };
        }

        protected override void OnDraw(GameTime time)
        {
            base.OnDraw(time);

            // allocate viewport to fit window
            var winPos = ImGui.GetWindowPos() + ImGui.GetWindowContentRegionMin();
            var winSize = ImGui.GetContentRegionAvail();
            var aspect = winSize.X / winSize.Y;
            
            if (_viewportRT == null || _viewportRT.Width != (int)winSize.X || _viewportRT.Height != (int)winSize.Y)
            {
                if (_viewportRT != null)
                {
                    App.Instance!.ImGuiRenderer!.UnbindTexture(_viewportRtHandle);
                }

                _viewportRT?.Dispose();
                _viewportRT = new RenderTarget2D(App.Instance!.GraphicsDevice, (int)winSize.X, (int)winSize.Y, false, SurfaceFormat.Color,
                    DepthFormat.Depth24Stencil8, 1, RenderTargetUsage.PlatformContents);

                _viewportRtHandle = App.Instance!.ImGuiRenderer!.BindTexture(_viewportRT);
            }

            var curMs = App.Instance!.curMouseState;
            var curKb = App.Instance!.curKeyboardState;

            if (_isDraggingView)
            {
                if (curMs.RightButton == ButtonState.Released)
                {
                    App.Instance!.consumeMouseCursor = false;
                    Mouse.IsRelativeMouseModeEXT = false;
                    _isDraggingView = false;
                }
                else
                {
                    int dx = curMs.X;
                    int dy = curMs.Y;

                    Matrix cr = Matrix.CreateFromQuaternion(camera.rot);

                    Vector3 up = Vector3.TransformNormal(Vector3.UnitY, cr);
                    Vector3 forward = Vector3.TransformNormal(-Vector3.UnitZ, cr);
                    Vector3 right = Vector3.TransformNormal(Vector3.UnitX, cr);
                    
                    camera.rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(dx * -0.25f)) *
                        Quaternion.CreateFromAxisAngle(right, MathHelper.ToRadians(dy * -0.25f)) *
                        camera.rot;

                    if (curKb.IsKeyDown(Keys.W))
                    {
                        camera.pos += forward * 10f * (float)time.ElapsedGameTime.TotalSeconds;
                    }
                    else if (curKb.IsKeyDown(Keys.S))
                    {
                        camera.pos -= forward * 10f * (float)time.ElapsedGameTime.TotalSeconds;
                    }

                    if (curKb.IsKeyDown(Keys.D))
                    {
                        camera.pos += right * 10f * (float)time.ElapsedGameTime.TotalSeconds;
                    }
                    else if (curKb.IsKeyDown(Keys.A))
                    {
                        camera.pos -= right * 10f * (float)time.ElapsedGameTime.TotalSeconds;
                    }

                    if (curKb.IsKeyDown(Keys.E))
                    {
                        camera.pos += up * 10f * (float)time.ElapsedGameTime.TotalSeconds;
                    }
                    else if (curKb.IsKeyDown(Keys.Q))
                    {
                        camera.pos -= up * 10f * (float)time.ElapsedGameTime.TotalSeconds;
                    }
                }
            }
            else
            {
                if (curMs.RightButton == ButtonState.Pressed)
                {
                    if (!App.Instance!.consumeMouseCursor && ImGui.IsWindowHovered() && !ImGuizmo.IsUsing())
                    {
                        App.Instance!.consumeMouseCursor = true;
                        Mouse.IsRelativeMouseModeEXT = true;
                        _isDraggingView = true;
                    }
                }

                if (curKb.IsKeyDown(Keys.W))
                {
                    _transformOp = TransformOperation.Translate;
                }
                else if (curKb.IsKeyDown(Keys.E))
                {
                    _transformOp = TransformOperation.Rotate;
                }
                else if (curKb.IsKeyDown(Keys.R))
                {
                    _transformOp = TransformOperation.Scale;
                }
            }

            _cachedView = camera.CalcView();
            _cachedProj = camera.CalcProjection(aspect);

            // draw to viewport render target
            App.Instance!.GraphicsDevice.SetRenderTarget(_viewportRT);
            DrawViewport();
            App.Instance!.GraphicsDevice.SetRenderTarget(null);

            // draw viewport texture to window, reset cursor
            ImGui.Image(_viewportRtHandle, winSize);
            ImGui.SetCursorPos(winPos);

            // set up ImGuizmo
            ImGuizmo.SetDrawlist();
            ImGuizmo.SetRect(winPos.X, winPos.Y, winSize.X, winSize.Y);

            // draw other gizmos
            DrawGizmos();
        }

        public override void PreDraw()
        {
            base.PreDraw();
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }

        public void GlobalGizmo(ref Vector3 position, ref Quaternion rotation, ref Vector3 scale, bool localSpace = false)
        {
            switch (_transformOp)
            {
                case TransformOperation.Translate:
                    PositionGizmo(ref position, rotation, localSpace);
                    break;
                case TransformOperation.Rotate:
                    RotationGizmo(ref rotation, position, localSpace);
                    break;
                case TransformOperation.Scale:
                    ScaleGizmo(ref scale, rotation, position, localSpace);
                    break;
            }
        }

        public void PositionGizmo(ref Vector3 position, Quaternion rotation, bool localSpace = false)
        {
            Matrix transform = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);
            ImGuizmo.Manipulate(ref _cachedView.M11, ref _cachedProj.M11, OPERATION.TRANSLATE, localSpace ? MODE.LOCAL : MODE.WORLD, ref transform.M11);

            transform.Decompose(out _, out _, out position);
        }

        public void RotationGizmo(ref Quaternion rotation, Vector3 position, bool localSpace = false)
        {
            Matrix transform = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);
            ImGuizmo.Manipulate(ref _cachedView.M11, ref _cachedProj.M11, OPERATION.ROTATE, localSpace ? MODE.LOCAL : MODE.WORLD, ref transform.M11);

            transform.Decompose(out _, out rotation, out _);
        }

        public void ScaleGizmo(ref Vector3 scale, Quaternion rotation, Vector3 position, bool localSpace = false)
        {
            Matrix transform = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);
            ImGuizmo.Manipulate(ref _cachedView.M11, ref _cachedProj.M11, OPERATION.SCALE, localSpace ? MODE.LOCAL : MODE.WORLD, ref transform.M11);

            transform.Decompose(out scale, out _, out _);
        }

        protected virtual void DrawViewport()
        {
            App.Instance!.GraphicsDevice.Clear(new Color(0.25f, 0.25f, 0.25f));

            // draw grid
            _gridEffect.View = _cachedView;
            _gridEffect.Projection = _cachedProj;
            _gridEffect.World = Matrix.Identity;

            _gridEffect.CurrentTechnique.Passes[0].Apply();
            App.Instance!.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            App.Instance!.GraphicsDevice.SetVertexBuffer(_gridVB);
            App.Instance!.GraphicsDevice.Indices = _gridIB;
            App.Instance!.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _gridVB.VertexCount, 0, _gridIB.IndexCount / 2);
        }

        protected virtual void DrawGizmos()
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            _viewportRT?.Dispose();
            _viewportRT = null;
        }

        private void BuildGrid(out VertexBuffer vb, out IndexBuffer ib)
        {
            List<ushort> indices = new List<ushort>();
            List<GridVert> verts = new List<GridVert>();

            for (int i = -100; i <= 100; i++)
            {
                if (i == 0) continue;
                AddLine(new Vector3(-100f, 0f, i), new Vector3(100f, 0f, i), new Color(1f, 1f, 1f, 0.5f), indices, verts);
            }

            for (int j = -100; j <= 100; j++)
            {
                if (j == 0) continue;
                AddLine(new Vector3(j, 0f, -100f), new Vector3(j, 0f, 100f), new Color(1f, 1f, 1f, 0.5f), indices, verts);
            }

            AddLine(new Vector3(-100f, 0f, 0f), new Vector3(100f, 0f, 0f), Color.Red, indices, verts);
            AddLine(new Vector3(0f, 0f, -100f), new Vector3(0f, 0f, 100f), Color.Blue, indices, verts);

            vb = new VertexBuffer(App.Instance!.GraphicsDevice, GridVertDeclaration, verts.Count, BufferUsage.WriteOnly);
            ib = new IndexBuffer(App.Instance!.GraphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);

            vb.SetData(verts.ToArray());
            ib.SetData(indices.ToArray());
        }

        private void AddLine(Vector3 start, Vector3 end, Color col, List<ushort> indices, List<GridVert> verts)
        {
            ushort idx = (ushort)verts.Count;

            indices.Add(idx++);
            indices.Add(idx);

            verts.Add(new GridVert()
            {
                pos = new Vector4(start, 1f),
                col = col,
            });

            verts.Add(new GridVert()
            {
                pos = new Vector4(end, 1f),
                col = col,
            });
        }
    }
}
