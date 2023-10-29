using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{
    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("BrushNode")]
    public class BrushNode : Node
    {
        [JsonObject(MemberSerialization.OptIn)]
        public struct ClipPlane
        {
            [JsonProperty]
            public Vector3 position;

            [JsonProperty]
            public Quaternion rotation;

            [JsonProperty]
            public bool visible;

            public Vector3 Normal
            {
                get
                {
                    Matrix rot = Matrix.CreateFromQuaternion(rotation);
                    return Vector3.TransformNormal(Vector3.UnitZ, rot);
                }
            }

            public Vector3 BasisX
            {
                get
                {
                    Matrix rot = Matrix.CreateFromQuaternion(rotation);
                    return Vector3.TransformNormal(Vector3.UnitX, rot);
                }
            }

            public Vector3 BasisY
            {
                get
                {
                    Matrix rot = Matrix.CreateFromQuaternion(rotation);
                    return Vector3.TransformNormal(Vector3.UnitY, rot);
                }
            }

            public ClipPlane(Vector3 position, Vector3 normal)
            {
                this.position = position;
                normal.Z *= -1f;
                visible = true;

                if (normal == Vector3.Up)
                    this.rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(-90f));
                else if (normal == Vector3.Down)
                    this.rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90f));
                else
                    this.rotation = Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.Zero, normal, Vector3.Up));
            }
        }

        [JsonProperty]
        public List<ClipPlane> planes = new List<ClipPlane>();

        private List<Vector3> tmpPolyA = new List<Vector3>();
        private List<Vector3> tmpPolyB = new List<Vector3>();

        private int _editPlane = 0;
        private bool _meshDirty = false;
        private List<GltfMeshPart> _meshParts = new List<GltfMeshPart>();

        public BrushNode()
        {
            planes.Add(new ClipPlane(Vector3.UnitX, Vector3.UnitX));
            planes.Add(new ClipPlane(-Vector3.UnitX, -Vector3.UnitX));
            planes.Add(new ClipPlane(Vector3.UnitY, Vector3.UnitY));
            planes.Add(new ClipPlane(-Vector3.UnitY, -Vector3.UnitY));
            planes.Add(new ClipPlane(Vector3.UnitZ, Vector3.UnitZ));
            planes.Add(new ClipPlane(-Vector3.UnitZ, -Vector3.UnitZ));
            _meshDirty = true;
        }

        public override void DrawInspector()
        {
            base.DrawInspector();

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Clip Planes"))
            {
                ImGui.Text($"{planes.Count} clip plane(s)");

                for (int i = 0; i < planes.Count; i++)
                {
                    int idx = i;
                    var clipPlane = planes[i];
                    ImGui.Text($"Clip Plane {i}");
                    ImGui.SameLine();
                    if (ImGui.Button($"Edit##cp_{i}"))
                    {
                        _editPlane = i;
                    }

                    bool prevVisible = clipPlane.visible;
                    bool newVisible = clipPlane.visible;
                    if (ImGui.Checkbox($"Visible##cp_{i}", ref newVisible))
                    {
                        App.Instance!.BeginRecordUndo("Change Clip Plane Visible", () => {
                            clipPlane.visible = prevVisible;
                            planes[idx] = clipPlane;
                            _meshDirty = true;
                        });

                        App.Instance!.EndRecordUndo(() => {
                            clipPlane.visible = newVisible;
                            planes[idx] = clipPlane;
                            _meshDirty = true;
                        });
                    }

                    _meshDirty |= ImGuiExt.DragFloat3($"Position##cp_{i}", ref clipPlane.position);
                    if (ImGui.IsItemActivated())
                    {
                        App.Instance!.BeginRecordUndo("Change Clip Plane Position", () => {
                            planes[idx] = clipPlane;
                            _meshDirty = true;
                        });
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        App.Instance!.EndRecordUndo(() => {
                            planes[idx] = clipPlane;
                            _meshDirty = true;
                        });
                    }

                    Vector3 euler = MathUtils.ToEulerAngles(clipPlane.rotation);
                    euler = MathUtils.ToDegrees(euler);
                    if (ImGuiExt.DragFloat3($"Rotation##cp_{i}", ref euler))
                    {
                        _meshDirty = true;
                        euler = MathUtils.ToRadians(euler);
                        clipPlane.rotation = MathUtils.ToQuaternion(euler);
                    }

                    if (ImGui.IsItemActivated())
                    {
                        App.Instance!.BeginRecordUndo("Change Clip Plane Rotation", () => {
                            planes[idx] = clipPlane;
                            _meshDirty = true;
                        });
                    }
                    if (ImGui.IsItemDeactivatedAfterEdit())
                    {
                        App.Instance!.EndRecordUndo(() => {
                            planes[idx] = clipPlane;
                            _meshDirty = true;
                        });
                    }

                    if (ImGui.Button($"Delete##cp_{i}"))
                    {
                        App.Instance!.BeginRecordUndo("Delete Clip Plane", () => {
                            planes.Insert(idx, clipPlane);
                            _meshDirty = true;
                        });
                        App.Instance!.EndRecordUndo(() => {
                            planes.RemoveAt(idx);
                            _meshDirty = true;
                        });
                    }
                    planes[i] = clipPlane;
                }

                if (ImGui.Button("Add Clip Plane"))
                {
                    ClipPlane newPlane;

                    if (planes.Count == 0)
                    {
                        newPlane = new ClipPlane
                        {
                            position = Vector3.Zero,
                            rotation = Quaternion.Identity
                        };
                    }
                    else
                    {
                        newPlane = planes[planes.Count - 1];
                    }

                    App.Instance!.BeginRecordUndo("Add Clip Plane", () => {
                        planes.RemoveAt(planes.Count - 1);
                        _meshDirty = true;
                    });
                    App.Instance!.EndRecordUndo(() => {
                        planes.Add(newPlane);
                        _meshDirty = true;
                    });
                }
            }
        }

        public override void DrawScene(RenderSystem renderSystem)
        {
            base.DrawScene(renderSystem);

            if (_meshDirty)
            {
                // regenerate mesh
                _meshDirty = false;
                GenerateMesh();
            }

            foreach (var meshpart in _meshParts)
            {
                RenderSystem.RenderMesh renderMesh = new RenderSystem.RenderMesh
                {
                    transform = World,
                    meshPart = meshpart,
                    material = new RenderMaterial
                    {
                        alphaMode = RenderMaterialAlphaMode.Opaque,
                        albedo = Color.White,
                        roughness = 1f,
                        metallic = 0f,
                    }
                };
                renderSystem.SubmitMesh(renderMesh);
            }
        }

        public override void DrawHandles(Matrix view, Matrix projection, ViewportWindow viewport, bool localSpace)
        {
            base.DrawHandles(view, projection, viewport, localSpace);

            if (_editPlane >= 0 && _editPlane <= planes.Count)
            {
                int idx = _editPlane;
                var clipPlane = planes[idx];

                Vector3 scale = Vector3.One;

                _meshDirty |= viewport.GlobalTransformHandle(ref clipPlane.position, ref clipPlane.rotation, ref scale, World, localSpace, () => {
                    App.Instance!.BeginRecordUndo("Transform Clip Plane", () => {
                        planes[idx] = clipPlane;
                        _meshDirty = true;
                    });
                }, () => {
                    App.Instance!.EndRecordUndo(() => {
                        planes[idx] = clipPlane;
                        _meshDirty = true;
                    });
                });

                planes[idx] = clipPlane;
            }
        }

        public override void DrawGizmos(Matrix view, Matrix projection, ViewportWindow viewport, bool selected)
        {
            base.DrawGizmos(view, projection, viewport, selected);

            if (selected && _editPlane >= 0 && _editPlane < planes.Count)
            {
                Vector3 n = planes[_editPlane].Normal;
                Vector3 bx = planes[_editPlane].BasisX;
                Vector3 by = planes[_editPlane].BasisY;
                var pos = planes[_editPlane].position;

                Quaternion r = planes[_editPlane].rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90f));

                Matrix planeTrs = Matrix.CreateFromQuaternion(r) * Matrix.CreateTranslation(pos) * World;
                pos = planeTrs.Translation;
                r = Quaternion.CreateFromRotationMatrix(planeTrs);
                n = Vector3.TransformNormal(n, World);
                bx = Vector3.TransformNormal(bx, World);
                by = Vector3.TransformNormal(by, World);
                
                viewport.DrawCircleGizmo(pos, 2f, r, Color.Yellow);
                viewport.DrawLineGizmo(pos, pos + n, Color.Blue, Color.Blue);
                viewport.DrawLineGizmo(pos, pos + bx, Color.Red, Color.Red);
                viewport.DrawLineGizmo(pos, pos + by, Color.Green, Color.Green);
            }

            if (selected)
            {
                for (int i = 0; i < planes.Count; i++)
                {
                    if (planes[i].visible) continue;

                    Vector3 bx = planes[i].BasisX;
                    Vector3 by = planes[i].BasisY;

                    // construct initial triangle
                    tmpPolyA.Clear();
                    tmpPolyB.Clear();
                    tmpPolyA.Add(planes[i].position + (by * 1000.0f));
                    tmpPolyA.Add(planes[i].position - (by * 1000.0f) - (bx * 1000.0f));
                    tmpPolyA.Add(planes[i].position - (by * 1000.0f) + (bx * 1000.0f));

                    for (int j = 0; j < planes.Count; j++)
                    {
                        if (j == i) continue;
                        ClipPolygon(tmpPolyA, tmpPolyB, planes[j]);
                        tmpPolyA.Clear();
                        tmpPolyA.AddRange(tmpPolyB);
                        tmpPolyB.Clear();
                    }

                    // draw clipped polygon
                    for (int v = 0; v < tmpPolyA.Count; v++)
                    {
                        int cur = v;
                        int next = (v + 1) % tmpPolyA.Count;

                        Vector3 a = tmpPolyA[cur];
                        Vector3 b = tmpPolyA[next];

                        a = Vector3.Transform(a, World);
                        b = Vector3.Transform(b, World);

                        viewport.DrawLineGizmo(a, b, Color.White, Color.White);
                    }
                }
            }
        }

        private void GenerateMesh()
        {
            foreach (var part in _meshParts)
            {
                part.Dispose();
            }

            _meshParts.Clear();

            for (int i = 0; i < planes.Count; i++)
            {
                var face = planes[i];
                if (!face.visible) continue;

                List<GltfUtil.MeshVert> vertices = new List<GltfUtil.MeshVert>();
                List<ushort> indices = new List<ushort>();

                Vector3 bx = face.BasisX;
                Vector3 by = face.BasisY;

                // construct initial triangle
                tmpPolyA.Clear();
                tmpPolyB.Clear();
                tmpPolyA.Add(face.position + (by * 1000.0f));
                tmpPolyA.Add(face.position - (by * 1000.0f) - (bx * 1000.0f));
                tmpPolyA.Add(face.position - (by * 1000.0f) + (bx * 1000.0f));

                // clip triangle to each plane
                for (int j = 0; j < planes.Count; j++)
                {
                    if (j == i) continue;
                    ClipPolygon(tmpPolyA, tmpPolyB, planes[j]);
                    tmpPolyA.Clear();
                    tmpPolyA.AddRange(tmpPolyB);
                    tmpPolyB.Clear();
                }

                // face clipped away, skip
                if (tmpPolyA.Count < 2) continue;

                // convert polygon into triangle fan
                foreach (var v in tmpPolyA)
                {
                    vertices.Add(new GltfUtil.MeshVert
                    {
                        pos = v,
                        normal = face.Normal,
                        tangent = new Vector4(bx, 1f),
                        col = Color.White,
                        uv = Vector2.Zero,
                    });
                }

                for (int j = 1; j < tmpPolyA.Count - 1; j++)
                {
                    indices.Add(0);
                    indices.Add((ushort)j);
                    indices.Add((ushort)(j + 1));
                }

                // construct vertex+index buffers
                VertexBuffer vb = new VertexBuffer(App.Instance!.GraphicsDevice, GltfUtil.MeshVertDeclaration, vertices.Count, BufferUsage.WriteOnly);
                vb.SetData(vertices.ToArray());

                IndexBuffer ib = new IndexBuffer(App.Instance!.GraphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);
                ib.SetData(indices.ToArray());

                GltfMeshPart meshPart = new GltfMeshPart
                {
                    materialIdx = i,
                    vb = vb,
                    ib = ib
                };

                _meshParts.Add(meshPart);
            }
        }

        private void ClipPolygon(List<Vector3> inPolygon, List<Vector3> outPolygon, ClipPlane plane)
        {
            Vector3 planeN = -plane.Normal;

            // clip each edge against the polygon
            for (int i = 0; i < inPolygon.Count; i++)
            {
                int cur = i;
                int next = (i + 1) % inPolygon.Count;

                Vector3 a = inPolygon[cur];
                Vector3 b = inPolygon[next];

                float ad = Vector3.Dot(a - plane.position, planeN);
                float bd = Vector3.Dot(b - plane.position, planeN);

                // both edges on outside of plane, skip
                if (ad < 0f && bd < 0f)
                {
                    continue;
                }

                // one vertex on outside, one vertex on inside
                if (ad < 0f || bd < 0f)
                {
                    // construct ray pointing from a -> b
                    Vector3 rayN = b - a;
                    rayN.Normalize();

                    float d = Vector3.Dot(rayN, planeN);
                    if (MathF.Abs(d) > float.Epsilon)
                    {
                        float t = Vector3.Dot(plane.position - a, planeN) / d;
                        Vector3 intersect = a + (rayN * t);
                        if (ad < 0f)
                        {
                            outPolygon.Add(intersect);
                        }
                        else if (bd < 0f)
                        {
                            outPolygon.Add(a);
                            outPolygon.Add(intersect);
                        }
                    }
                }
                // both vertices on inside
                else
                {
                    outPolygon.Add(a);
                }
            }
        }
    }
}