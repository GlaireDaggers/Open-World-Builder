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
        private const int GIZMO_BUFFER_SIZE = 4096;

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

        private RasterizerState _gizmoRS;
        private BasicEffect _gizmoEffect;
        private VertexBuffer _gizmoVB;
        private IndexBuffer _gizmoIB;

        private ushort[] _gizmoIndices = new ushort[GIZMO_BUFFER_SIZE];
        private GridVert[] _gizmoVerts = new GridVert[GIZMO_BUFFER_SIZE];
        private int _gizmoVertsCount = 0;
        private int _gizmoIndicesCount = 0;

        private float _moveSpeed = 10f;

        private int _gizmoId = 0;

        public ViewportWindow() : base()
        {
            title = "Viewport Window";

            _gizmoEffect = new BasicEffect(App.Instance!.GraphicsDevice)
            {
                LightingEnabled = false,
                TextureEnabled = false,
                VertexColorEnabled = true
            };

            _gizmoRS = new RasterizerState
            {
                CullMode = CullMode.None,
                FillMode = FillMode.WireFrame
            };

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

            _gizmoIB = new IndexBuffer(App.Instance!.GraphicsDevice, IndexElementSize.SixteenBits, GIZMO_BUFFER_SIZE, BufferUsage.WriteOnly);
            _gizmoVB = new VertexBuffer(App.Instance!.GraphicsDevice, GridVertDeclaration, GIZMO_BUFFER_SIZE, BufferUsage.WriteOnly);

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

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ImGui.SetWindowFocus();
            }

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
            var prevMs = App.Instance!.prevMouseState;
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
                    
                    camera.rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(dx * -0.1f)) *
                        Quaternion.CreateFromAxisAngle(right, MathHelper.ToRadians(dy * -0.1f)) *
                        camera.rot;

                    if (curKb.IsKeyDown(Keys.W))
                    {
                        camera.pos += forward * _moveSpeed * (float)time.ElapsedGameTime.TotalSeconds;
                    }
                    else if (curKb.IsKeyDown(Keys.S))
                    {
                        camera.pos -= forward * _moveSpeed * (float)time.ElapsedGameTime.TotalSeconds;
                    }

                    if (curKb.IsKeyDown(Keys.D))
                    {
                        camera.pos += right * _moveSpeed * (float)time.ElapsedGameTime.TotalSeconds;
                    }
                    else if (curKb.IsKeyDown(Keys.A))
                    {
                        camera.pos -= right * _moveSpeed * (float)time.ElapsedGameTime.TotalSeconds;
                    }

                    if (curKb.IsKeyDown(Keys.E))
                    {
                        camera.pos += up * _moveSpeed * (float)time.ElapsedGameTime.TotalSeconds;
                    }
                    else if (curKb.IsKeyDown(Keys.Q))
                    {
                        camera.pos -= up * _moveSpeed * (float)time.ElapsedGameTime.TotalSeconds;
                    }

                    _moveSpeed += (curMs.ScrollWheelValue - prevMs.ScrollWheelValue) * 0.01f;
                    _moveSpeed = MathHelper.Clamp(_moveSpeed, 1f, 100f);
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

                if (ImGui.IsWindowFocused())
                {
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
            }

            _cachedView = camera.CalcView();
            _cachedProj = camera.CalcProjection(aspect);

            // draw to viewport render target
            App.Instance!.GraphicsDevice.SetRenderTarget(_viewportRT);
            DrawViewport(_cachedView, _cachedProj);
            DrawGridGizmo();
            FlushGizmoBuffer();
            App.Instance!.GraphicsDevice.SetRenderTarget(null);

            // draw viewport texture to window, reset cursor
            ImGui.Image(_viewportRtHandle, winSize);
            ImGui.SetCursorPos(ImGui.GetWindowContentRegionMin());

            // set up ImGuizmo
            ImGuizmo.SetDrawlist();
            ImGuizmo.SetRect(winPos.X, winPos.Y, winSize.X, winSize.Y);

            // draw handles
            _gizmoId = 0;
            DrawHandles(_cachedView, _cachedProj);
        }

        public override void PreDraw()
        {
            base.PreDraw();
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0f, 0f));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f);
            ImGui.SetNextWindowSize(new System.Numerics.Vector2(800f, 600f), ImGuiCond.FirstUseEver);
        }

        public override void PostDraw()
        {
            base.PostDraw();
            ImGui.PopStyleVar();
            ImGui.PopStyleVar();
        }

        public void GlobalTransformHandle(ref Vector3 position, ref Quaternion rotation, ref Vector3 scale, Matrix parentMatrix, bool localSpace = false)
        {
            switch (_transformOp)
            {
                case TransformOperation.Translate:
                    PositionHandle(ref position, rotation, parentMatrix, localSpace);
                    break;
                case TransformOperation.Rotate:
                    RotationHandle(ref rotation, position, parentMatrix, localSpace);
                    break;
                case TransformOperation.Scale:
                    ScaleHandle(ref scale, rotation, position, parentMatrix, localSpace);
                    break;
            }
        }

        public void PositionHandle(ref Vector3 position, Quaternion rotation, Matrix parentMatrix, bool localSpace = false)
        {
            ImGuizmo.SetID(_gizmoId++);

            Matrix transform = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position) * parentMatrix;
            bool mod = ImGuizmo.Manipulate(ref _cachedView.M11, ref _cachedProj.M11, OPERATION.TRANSLATE, localSpace ? MODE.LOCAL : MODE.WORLD, ref transform.M11);

            transform *= Matrix.Invert(parentMatrix);

            if (mod)
            {
                transform.Decompose(out _, out _, out position);
            }
        }

        public void RotationHandle(ref Quaternion rotation, Vector3 position, Matrix parentMatrix, bool localSpace = false)
        {
            ImGuizmo.SetID(_gizmoId++);

            Matrix transform = Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position) * parentMatrix;
            bool mod = ImGuizmo.Manipulate(ref _cachedView.M11, ref _cachedProj.M11, OPERATION.ROTATE, localSpace ? MODE.LOCAL : MODE.WORLD, ref transform.M11);

            transform *= Matrix.Invert(parentMatrix);

            if (mod)
            {
                transform.Decompose(out _, out rotation, out _);
            }
        }

        public void ScaleHandle(ref Vector3 scale, Quaternion rotation, Vector3 position, Matrix parentMatrix, bool localSpace = false)
        {
            ImGuizmo.SetID(_gizmoId++);
            
            Matrix transform = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position) * parentMatrix;
            bool mod = ImGuizmo.Manipulate(ref _cachedView.M11, ref _cachedProj.M11, OPERATION.SCALE, localSpace ? MODE.LOCAL : MODE.WORLD, ref transform.M11);

            transform *= Matrix.Invert(parentMatrix);

            if (mod)
            {
                transform.Decompose(out scale, out _, out _);
            }
        }

        protected virtual void DrawViewport(Matrix view, Matrix projection)
        {
            App.Instance!.GraphicsDevice.Clear(new Color(0.25f, 0.25f, 0.25f));
        }

        protected virtual void DrawHandles(Matrix view, Matrix projection)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            _viewportRT?.Dispose();
            _viewportRT = null;
        }

        public void DrawLineGizmo(Vector3 start, Vector3 end, Color color1, Color color2)
        {
            ushort idx = (ushort)_gizmoVertsCount;

            _gizmoIndices[_gizmoIndicesCount++] = idx++;
            _gizmoIndices[_gizmoIndicesCount++] = idx;

            _gizmoVerts[_gizmoVertsCount++] = new GridVert()
            {
                pos = new Vector4(start, 1f),
                col = color1,
            };

            _gizmoVerts[_gizmoVertsCount++] = new GridVert()
            {
                pos = new Vector4(end, 1f),
                col = color2,
            };

            if (_gizmoIndicesCount == _gizmoIB.IndexCount)
            {
                FlushGizmoBuffer();
            }
        }

        public void DrawCircleGizmo(Vector3 center, float radius, Quaternion rotation, Color color)
        {
            for (int i = 0; i < 64; i++)
            {
                int i2 = (i + 1) % 64;
                float angle1 = (i / 64f) * 360f;
                float angle2 = (i2 / 64f) * 360f;

                Quaternion r1 = rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(angle1));
                Quaternion r2 = rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.ToRadians(angle2));

                Matrix m1 = Matrix.CreateFromQuaternion(r1);
                Matrix m2 = Matrix.CreateFromQuaternion(r2);

                Vector3 v1 = Vector3.TransformNormal(Vector3.UnitX, m1) * radius;
                Vector3 v2 = Vector3.TransformNormal(Vector3.UnitX, m2) * radius;

                DrawLineGizmo(v1 + center, v2 + center, color, color);
            }
        }

        public void DrawSphereGizmo(Vector3 center, float radius, Color color)
        {
            DrawCircleGizmo(center, radius, Quaternion.Identity, color);
            DrawCircleGizmo(center, radius, Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90f)), color);
            DrawCircleGizmo(center, radius, Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(90f)), color);
        }

        public void DrawGridGizmo()
        {
            Vector3 quantizedCameraPos = camera.pos;
            quantizedCameraPos.X = MathF.Floor(quantizedCameraPos.X);
            quantizedCameraPos.Z = MathF.Floor(quantizedCameraPos.Z);
            quantizedCameraPos.Y = 0f;

            for (int i = -100; i <= 100; i++)
            {
                float alpha = 1f - (MathF.Abs(i) / 100f);
                DrawLineGizmo(new Vector3(-100f, 0f, i) + quantizedCameraPos, new Vector3(0f, 0f, i) + quantizedCameraPos,
                    new Color(1f, 1f, 1f, 0.0f),
                    new Color(1f, 1f, 1f, 0.5f * alpha * alpha));
                DrawLineGizmo(new Vector3(0f, 0f, i) + quantizedCameraPos, new Vector3(100f, 0f, i) + quantizedCameraPos,
                    new Color(1f, 1f, 1f, 0.5f * alpha * alpha),
                    new Color(1f, 1f, 1f, 0.0f));
            }

            for (int j = -100; j <= 100; j++)
            {
                float alpha = 1f - (MathF.Abs(j) / 100f);
                DrawLineGizmo(new Vector3(j, 0f, -100f) + quantizedCameraPos, new Vector3(j, 0f, 0f) + quantizedCameraPos,
                    new Color(1f, 1f, 1f, 0.0f),
                    new Color(1f, 1f, 1f, 0.5f * alpha * alpha));
                DrawLineGizmo(new Vector3(j, 0f, 0f) + quantizedCameraPos, new Vector3(j, 0f, 100f) + quantizedCameraPos,
                    new Color(1f, 1f, 1f, 0.5f * alpha * alpha),
                    new Color(1f, 1f, 1f, 0.0f));
            }

            DrawLineGizmo(new Vector3(-100f + quantizedCameraPos.X, 0f, 0f), new Vector3(quantizedCameraPos.X, 0f, 0f), new Color(1f, 0f, 0f, 0f), Color.Red);
            DrawLineGizmo(new Vector3(quantizedCameraPos.X, 0f, 0f), new Vector3(100f + quantizedCameraPos.X, 0f, 0f), Color.Red, new Color(1f, 0f, 0f, 0f));

            DrawLineGizmo(new Vector3(0f, 0f, -100f + quantizedCameraPos.Z), new Vector3(0f, 0f, quantizedCameraPos.Z), new Color(0f, 0f, 1f, 0f), Color.Blue);
            DrawLineGizmo(new Vector3(0f, 0f, quantizedCameraPos.Z), new Vector3(0f, 0f, 100f + quantizedCameraPos.Z), Color.Blue, new Color(0f, 0f, 1f, 0f));
        }

        private void FlushGizmoBuffer()
        {
            if (_gizmoIndicesCount > 0)
            {
                _gizmoVB.SetData(_gizmoVerts, 0, _gizmoVertsCount);
                _gizmoIB.SetData(_gizmoIndices, 0, _gizmoIndicesCount);

                // draw gizmo buffer
                _gizmoEffect.View = _cachedView;
                _gizmoEffect.Projection = _cachedProj;
                _gizmoEffect.World = Matrix.Identity;

                _gizmoEffect.CurrentTechnique.Passes[0].Apply();
                App.Instance!.GraphicsDevice.RasterizerState = _gizmoRS;
                App.Instance!.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

                App.Instance!.GraphicsDevice.SetVertexBuffer(_gizmoVB);
                App.Instance!.GraphicsDevice.Indices = _gizmoIB;
                App.Instance!.GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, _gizmoVertsCount, 0, _gizmoIndicesCount / 2);
                
                _gizmoVertsCount = 0;
                _gizmoIndicesCount = 0;
            }
        }
    }
}
