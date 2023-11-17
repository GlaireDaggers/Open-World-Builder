using System.Net.NetworkInformation;
using ImGuiNET;
using ImGuizmoNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NativeFileDialogSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenWorldBuilder
{
    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("TerrainNode")]
    public class TerrainNode : Node
    {
        private const int PATCH_RES = 64;
        private const float SKIRT_HEIGHT = 1.0f;

        private static VertexDeclaration TerrainVertDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
        );

        private struct TerrainVert
        {
            public Vector3 pos;
            public Vector2 uv;
        }

        public struct TextureLayer
        {
            public float scale;
            public string diffusePath;
            public string normalPath;
            public string ormPath;
        }

        [JsonProperty]
        public int detail = 6;

        [JsonProperty]
        public float lodDistanceMultiplier = 1f;

        [JsonProperty]
        public float heightScale = 1f;

        [JsonProperty]
        public float terrainScale = 100f;

        [JsonProperty]
        public int heightmapRes = 512;

        [JsonProperty]
        public List<TextureLayer> layers = new List<TextureLayer>();

        private GltfMeshPart _patchMesh;
        private Texture2D _heightmap;
        private Texture2D[] _splatmaps = new Texture2D[0];
        private Color[][] _splatmapData = new Color[0][];
        private RenderTerrainLayer[] _renderLayers = new RenderTerrainLayer[0];
        private float[] _heightmapData;
        private float[] _heightmapDataTemp;

        private int _importFormat = 0;

        private int _brushRadius = 32;
        private float _brushStrength = 1f;
        private int _brushType = 0;
        private int _paintLayer = 0;

        private Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private Texture2D _blankTexture;

        public TerrainNode()
        {
            _blankTexture = new Texture2D(App.Instance!.GraphicsDevice, 2, 2, true, SurfaceFormat.Color);
            _blankTexture.SetData(new Color[]
            {
                Color.White, Color.White, Color.White, Color.White
            });

            // dummy heightmap
            _heightmap = new Texture2D(App.Instance!.GraphicsDevice, 512, 512, false, SurfaceFormat.Single);
            _heightmapData = new float[512 * 512];
            _heightmapDataTemp = new float[512 * 512];

            // create terrain patch mesh
            List<ushort> indices = new List<ushort>();
            List<TerrainVert> vertices = new List<TerrainVert>();

            // vertex grid
            for (int j = 0; j <= PATCH_RES; j++)
            {
                for (int i = 0; i <= PATCH_RES; i++)
                {
                    Vector3 vpos = new Vector3(i, 0f, j) / PATCH_RES;
                    Vector2 uv = new Vector2(i, j) / PATCH_RES;

                    vertices.Add(new TerrainVert
                    {
                        pos = vpos,
                        uv = uv
                    });
                }
            }

            // skirt vertices
            int vbSkirtOffset0 = vertices.Count;
            for (int i = 0; i <= PATCH_RES; i++)
            {
                Vector3 vpos = new Vector3(i, 0f, 0) / PATCH_RES;
                Vector2 uv = new Vector2(i, 0) / PATCH_RES;

                vertices.Add(new TerrainVert
                {
                    pos = vpos + (Vector3.Down * SKIRT_HEIGHT),
                    uv = uv
                });
            }

            int vbSkirtOffset1 = vertices.Count;
            for (int i = 0; i <= PATCH_RES; i++)
            {
                Vector3 vpos = new Vector3(i, 0f, PATCH_RES) / PATCH_RES;
                Vector2 uv = new Vector2(i, PATCH_RES) / PATCH_RES;

                vertices.Add(new TerrainVert
                {
                    pos = vpos + (Vector3.Down * SKIRT_HEIGHT),
                    uv = uv
                });
            }

            int vbSkirtOffset2 = vertices.Count;
            for (int j = 0; j <= PATCH_RES; j++)
            {
                Vector3 vpos = new Vector3(0, 0f, j) / PATCH_RES;
                Vector2 uv = new Vector2(0, j) / PATCH_RES;

                vertices.Add(new TerrainVert
                {
                    pos = vpos + (Vector3.Down * SKIRT_HEIGHT),
                    uv = uv
                });
            }

            int vbSkirtOffset3 = vertices.Count;
            for (int j = 0; j <= PATCH_RES; j++)
            {
                Vector3 vpos = new Vector3(PATCH_RES, 0f, j) / PATCH_RES;
                Vector2 uv = new Vector2(PATCH_RES, j) / PATCH_RES;

                vertices.Add(new TerrainVert
                {
                    pos = vpos + (Vector3.Down * SKIRT_HEIGHT),
                    uv = uv
                });
            }

            // grid triangles
            for (int j = 0; j < PATCH_RES; j++)
            {
                for (int i = 0; i < PATCH_RES; i++)
                {
                    ushort vtx0 = (ushort)(i + (j * (PATCH_RES + 1)));
                    ushort vtx1 = (ushort)(vtx0 + 1);
                    ushort vtx2 = (ushort)(vtx0 + (PATCH_RES + 1));
                    ushort vtx3 = (ushort)(vtx2 + 1);

                    indices.Add(vtx2);
                    indices.Add(vtx1);
                    indices.Add(vtx0);

                    indices.Add(vtx1);
                    indices.Add(vtx2);
                    indices.Add(vtx3);
                }
            }

            // skirt triangles
            for (int i = 0; i < PATCH_RES; i++)
            {
                ushort vtx0 = (ushort)i;
                ushort vtx1 = (ushort)(vtx0 + 1);
                ushort vtx2 = (ushort)(vbSkirtOffset0 + i);
                ushort vtx3 = (ushort)(vbSkirtOffset0 + i + 1);

                indices.Add(vtx0);
                indices.Add(vtx1);
                indices.Add(vtx2);

                indices.Add(vtx3);
                indices.Add(vtx2);
                indices.Add(vtx1);
            }

            for (int i = 0; i < PATCH_RES; i++)
            {
                ushort vtx0 = (ushort)(i + (PATCH_RES * (PATCH_RES + 1)));
                ushort vtx1 = (ushort)(vtx0 + 1);
                ushort vtx2 = (ushort)(vbSkirtOffset1 + i);
                ushort vtx3 = (ushort)(vbSkirtOffset1 + i + 1);

                indices.Add(vtx2);
                indices.Add(vtx1);
                indices.Add(vtx0);

                indices.Add(vtx1);
                indices.Add(vtx2);
                indices.Add(vtx3);
            }

            for (int j = 0; j < PATCH_RES; j++)
            {
                ushort vtx0 = (ushort)(j * (PATCH_RES + 1));
                ushort vtx1 = (ushort)(vtx0 + (PATCH_RES + 1));
                ushort vtx2 = (ushort)(vbSkirtOffset2 + j);
                ushort vtx3 = (ushort)(vbSkirtOffset2 + j + 1);

                indices.Add(vtx2);
                indices.Add(vtx1);
                indices.Add(vtx0);

                indices.Add(vtx1);
                indices.Add(vtx2);
                indices.Add(vtx3);
            }

            for (int j = 0; j < PATCH_RES; j++)
            {
                ushort vtx0 = (ushort)(PATCH_RES + (j * (PATCH_RES + 1)));
                ushort vtx1 = (ushort)(vtx0 + (PATCH_RES + 1));
                ushort vtx2 = (ushort)(vbSkirtOffset3 + j);
                ushort vtx3 = (ushort)(vbSkirtOffset3 + j + 1);

                indices.Add(vtx0);
                indices.Add(vtx1);
                indices.Add(vtx2);

                indices.Add(vtx3);
                indices.Add(vtx2);
                indices.Add(vtx1);
            }

            VertexBuffer vb = new VertexBuffer(App.Instance!.GraphicsDevice, TerrainVertDeclaration, vertices.Count, BufferUsage.WriteOnly);
            IndexBuffer ib = new IndexBuffer(App.Instance!.GraphicsDevice, IndexElementSize.SixteenBits, indices.Count, BufferUsage.WriteOnly);

            vb.SetData(vertices.ToArray());
            ib.SetData(indices.ToArray());

            _patchMesh = new GltfMeshPart
            {
                ib = ib,
                vb = vb
            };
        }

        public override void Dispose()
        {
            base.Dispose();
            _patchMesh.Dispose();
            _heightmap.Dispose();

            _blankTexture.Dispose();

            foreach (var tex in _textures)
            {
                tex.Value.Dispose();
            }

            _textures.Clear();

            foreach (var splatmap in _splatmaps)
            {
                splatmap.Dispose();
            }
        }

        public override void OnSerialize(JObject jObject)
        {
            base.OnSerialize(jObject);

            // save heightmap in "levels/{level name}_{node ID}_heightmap.raw"
            string heightmapPath = Path.Combine(App.Instance!.ProjectFolder!, $"levels/{Scene!.name}_{guid}_heightmap.raw");
            using var heightmapFile = File.OpenWrite(heightmapPath);
            using var heightmapWriter = new BinaryWriter(heightmapFile);

            float[] data = new float[heightmapRes * heightmapRes];
            _heightmap.GetData(data);

            for (int i = 0; i < data.Length; i++)
            {
                heightmapWriter.Write(data[i]);
            }

            // save splatmaps in "levels/{level name}_{node ID}_splatmap{idx}.raw"
            for (int i = 0; i < _splatmaps.Length; i++)
            {
                string splatmapPath = Path.Combine(App.Instance!.ProjectFolder!, $"levels/{Scene!.name}_{guid}_splatmap{i}.raw");
                using var splatmapFile = File.OpenWrite(splatmapPath);
                ExportSplatmap(splatmapFile, _splatmapData[i]);
            }
        }

        public override void PostLoad()
        {
            base.PostLoad();

            _heightmap.Dispose();
            _heightmap = new Texture2D(App.Instance!.GraphicsDevice, heightmapRes, heightmapRes, false, SurfaceFormat.Single);

            _heightmapData = new float[heightmapRes * heightmapRes];
            _heightmapDataTemp = new float[heightmapRes * heightmapRes];

            // load heightmap saved as "levels/{level name}_{node ID}_heightmap.raw"
            string heightmapPath = Path.Combine(App.Instance!.ProjectFolder!, $"levels/{Scene!.name}_{guid}_heightmap.raw");
            using var heightmapFile = File.OpenRead(heightmapPath);
            ImportRawFP32(heightmapFile, false);

            UpdateSplatmapLayers();

            // load splatmaps saved as "levels/{level name}_{node ID}_splatmap{idx}.raw"
            for (int i = 0; i < _splatmaps.Length; i++)
            {
                string splatmapPath = Path.Combine(App.Instance!.ProjectFolder!, $"levels/{Scene!.name}_{guid}_splatmap{i}.raw");
                using var splatmapFile = File.OpenRead(splatmapPath);

                ImportSplatmap(splatmapFile, _splatmapData[i]);
                _splatmaps[i].SetData(_splatmapData[i]);
            }
        }

        public override void DrawGizmos(Matrix view, Matrix projection, ViewportWindow viewport, bool selected)
        {
            base.DrawGizmos(view, projection, viewport, selected);

            int mouseX = App.Instance!.curMouseState.X;
            int mouseY = App.Instance!.curMouseState.Y;

            if (selected && ImGui.IsWindowHovered() && !ImGuizmo.IsUsing())
            {
                // calc ray from mouse
                Viewport vp = viewport.viewport;
                Vector3 nearPoint = vp.Unproject(new Vector3(mouseX, mouseY, 0f), projection, view, Matrix.Identity);
                Vector3 farPoint = vp.Unproject(new Vector3(mouseX, mouseY, 1f), projection, view, Matrix.Identity);
                
                Vector3 direction = farPoint - nearPoint;
                direction.Normalize();

                if (Raymarch(nearPoint, direction, out var px, out var py) is Vector3 pt)
                {
                    float radius = (float)_brushRadius / heightmapRes * terrainScale;
                    viewport.DrawSphereGizmo(pt, radius, Color.Red);

                    if (App.Instance!.curMouseState.LeftButton == ButtonState.Pressed)
                    {
                        if (App.Instance!.prevMouseState.LeftButton != ButtonState.Pressed)
                        {
                            switch (_brushType)
                            {
                                case 0:
                                case 1:
                                case 2:
                                case 3:
                                    // save copy of heightmap
                                    float[] oldHeightmap = new float[_heightmapData.Length];
                                    Array.Copy(_heightmapData, oldHeightmap, _heightmapData.Length);

                                    App.Instance!.BeginRecordUndo("Paint Terrain", () => {
                                        Array.Copy(oldHeightmap, _heightmapData, _heightmapData.Length);
                                        _heightmap.SetData(_heightmapData);
                                    });
                                    break;
                                case 4:
                                    // save copy of splatmaps
                                    Color[][] oldSplatmapData = new Color[_splatmapData.Length][];
                                    for (int i = 0; i < _splatmapData.Length; i++)
                                    {
                                        oldSplatmapData[i] = new Color[_splatmapData[i].Length];
                                        Array.Copy(_splatmapData[i], oldSplatmapData[i], _splatmapData[i].Length);
                                    }

                                    App.Instance!.BeginRecordUndo("Paint Terrain", () => {
                                        for (int i = 0; i < _splatmaps.Length; i++)
                                        {
                                            Array.Copy(oldSplatmapData[i], _splatmapData[i], _splatmapData[i].Length);
                                            _splatmaps[i].SetData(_splatmapData[i]);
                                        }
                                    });
                                    break;
                            }
                        }

                        switch (_brushType)
                        {
                            // raise
                            case 0:
                                BrushRaise(px, py, _brushRadius, _brushStrength / heightScale);
                                break;
                            // lower
                            case 1:
                                BrushRaise(px, py, _brushRadius, -_brushStrength / heightScale);
                                break;
                            // smooth
                            case 2:
                                BrushSmooth(px, py, _brushRadius, _brushStrength);
                                break;
                            // flatten
                            case 3:
                                BrushFlatten(px, py, _brushRadius, _brushStrength);
                                break;
                            // texture paint
                            case 4:
                                BrushSplat(_paintLayer, px, py, _brushRadius, _brushStrength * 0.1f);
                                break;
                        }
                    }
                    else if (App.Instance!.prevMouseState.LeftButton == ButtonState.Pressed)
                    {
                        switch (_brushType)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                // save copy of heightmap
                                float[] newHeightmap = new float[_heightmapData.Length];
                                Array.Copy(_heightmapData, newHeightmap, _heightmapData.Length);

                                App.Instance!.EndRecordUndo(() => {
                                    Array.Copy(newHeightmap, _heightmapData, _heightmapData.Length);
                                    _heightmap.SetData(_heightmapData);
                                });
                                break;
                            case 4:
                                // save copy of splatmaps
                                Color[][] newSplatmapData = new Color[_splatmapData.Length][];
                                for (int i = 0; i < _splatmapData.Length; i++)
                                {
                                    newSplatmapData[i] = new Color[_splatmapData[i].Length];
                                    Array.Copy(_splatmapData[i], newSplatmapData[i], _splatmapData[i].Length);
                                }

                                App.Instance!.EndRecordUndo(() => {
                                    for (int i = 0; i < _splatmaps.Length; i++)
                                    {
                                        Array.Copy(newSplatmapData[i], _splatmapData[i], _splatmapData[i].Length);
                                        _splatmaps[i].SetData(_splatmapData[i]);
                                    }
                                });
                                break;
                        }
                    }
                }
            }
        }

        public override void DrawInspector()
        {
            base.DrawInspector();

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Heightmap Settings"))
            {
                ImGui.PushTextWrapPos();
                ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 0f, 1f), "WARNING: Changing heightmap resolution will ERASE current heightmap/splatmap data");
                ImGui.PopTextWrapPos();

                int oldHeightmapRes = heightmapRes;
                if (ImGui.InputInt("Resolution", ref heightmapRes, 1, 1, ImGuiInputTextFlags.EnterReturnsTrue))
                {
                    if (heightmapRes < 32) {
                        heightmapRes = 32;
                    }

                    var oldHeightmapData = _heightmapData;
                    var oldSplatmapData = new Color[_splatmapData.Length][];

                    Array.Copy(_splatmapData, oldSplatmapData, _splatmapData.Length);

                    App.Instance!.BeginRecordUndo("Change terrain resolution", () => {
                        heightmapRes = oldHeightmapRes;

                        _heightmap.Dispose();
                        _heightmap = new Texture2D(App.Instance!.GraphicsDevice, heightmapRes, heightmapRes, false, SurfaceFormat.Single);
                        _heightmapData = oldHeightmapData;
                        _heightmap.SetData(_heightmapData);

                        _splatmapData = oldSplatmapData;

                        for (int i = 0; i < _splatmaps.Length; i++)
                        {
                            _splatmaps[i].Dispose();
                            _splatmaps[i] = new Texture2D(App.Instance!.GraphicsDevice, heightmapRes, heightmapRes, false, SurfaceFormat.Color);
                            _splatmaps[i].SetData(_splatmapData[i]);
                        }
                    });

                    App.Instance!.EndRecordUndo(() => {
                        _heightmap.Dispose();
                        _heightmap = new Texture2D(App.Instance!.GraphicsDevice, heightmapRes, heightmapRes, false, SurfaceFormat.Single);
                        _heightmapData = new float[heightmapRes * heightmapRes];
                        _heightmapDataTemp = new float[heightmapRes * heightmapRes];

                        for (int i = 0; i < _splatmaps.Length; i++)
                        {
                            _splatmaps[i].Dispose();
                            _splatmaps[i] = new Texture2D(App.Instance!.GraphicsDevice, heightmapRes, heightmapRes, false, SurfaceFormat.Color);
                            _splatmapData[i] = new Color[heightmapRes * heightmapRes];

                            if (i == 0)
                            {
                                ClearSplatmap(_splatmaps[i], _splatmapData[i], new Color(1f, 0f, 0f, 0f));
                            }
                            else
                            {
                                ClearSplatmap(_splatmaps[i], _splatmapData[i], new Color(0f, 0f, 0f, 0f));
                            }
                        }
                    });
                }

                ImGui.Combo("Import Format", ref _importFormat, "16-bit unsigned\032-bit unsigned\0FP16\0FP32");

                if (ImGui.Button("Import Heightmap"))
                {
                    var result = Dialog.FileOpen("raw", App.Instance!.ContentPath);
                    if (result.IsOk)
                    {
                        using Stream instream = File.OpenRead(result.Path);

                        var oldHeightmapData = new float[_heightmapData.Length];
                        Array.Copy(_heightmapData, oldHeightmapData, _heightmapData.Length);

                        App.Instance!.BeginRecordUndo("Import Heightmap", () => {
                            Array.Copy(oldHeightmapData, _heightmapData, _heightmapData.Length);
                            _heightmap.SetData(_heightmapData);
                        });

                        App.Instance!.EndRecordUndo(() => {
                            switch (_importFormat)
                            {
                                case 0:
                                    ImportRaw16(instream);
                                    break;
                                case 1:
                                    ImportRaw32(instream);
                                    break;
                                case 2:
                                    ImportRawFP16(instream);
                                    break;
                                case 3:
                                    ImportRawFP32(instream);
                                    break;
                            }
                        });
                    }
                }
            }

            if (ImGui.CollapsingHeader("Terrain Settings"))
            {
                var oldDetail = detail;
                var oldLodMultiplier = lodDistanceMultiplier;
                var oldHeightScale = heightScale;
                var oldTerrainScale = terrainScale;

                ImGui.DragInt("Detail", ref detail, 1f, 0, 10);

                if (ImGui.IsItemActivated())
                {
                    App.Instance!.BeginRecordUndo("Change Terrain Detail", () => {
                        detail = oldDetail;
                    });
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    var newDetail = detail;
                    App.Instance!.EndRecordUndo(() => {
                        detail = newDetail;
                    });
                }

                ImGui.DragFloat("LOD Distance Scale", ref lodDistanceMultiplier, 1f, 0f, 4f);

                if (ImGui.IsItemActivated())
                {
                    App.Instance!.BeginRecordUndo("Change Terrain LOD Distance Scale", () => {
                        lodDistanceMultiplier = oldLodMultiplier;
                    });
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    var newLodMultiplier = lodDistanceMultiplier;
                    App.Instance!.EndRecordUndo(() => {
                        lodDistanceMultiplier = newLodMultiplier;
                    });
                }

                ImGui.InputFloat("Height Scale", ref heightScale);

                if (ImGui.IsItemActivated())
                {
                    App.Instance!.BeginRecordUndo("Change Terrain Height Scale", () => {
                        heightScale = oldHeightScale;
                    });
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    var newHeightScale = heightScale;
                    App.Instance!.EndRecordUndo(() => {
                        heightScale = newHeightScale;
                    });
                }

                ImGui.InputFloat("Terrain Scale", ref terrainScale);

                if (ImGui.IsItemActivated())
                {
                    App.Instance!.BeginRecordUndo("Change Terrain Scale", () => {
                        terrainScale = oldTerrainScale;
                    });
                }

                if (ImGui.IsItemDeactivatedAfterEdit())
                {
                    var newTerrainScale = terrainScale;
                    App.Instance!.EndRecordUndo(() => {
                        terrainScale = newTerrainScale;
                    });
                }
            }

            if (ImGui.CollapsingHeader("Layers"))
            {
                ImGui.Text($"{layers.Count} layer(s)");

                for (int i = 0; i < layers.Count; i++)
                {
                    var layer = layers[i];
                    var idx = i;

                    var oldScale = layer.scale;
                    var oldDiffusePath = layer.diffusePath;
                    var oldNormalPath = layer.normalPath;
                    var oldORMPath = layer.ormPath;

                    ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow;

                    if (_paintLayer == i)
                    {
                        flags |= ImGuiTreeNodeFlags.Selected;
                    }

                    bool open = ImGui.TreeNodeEx($"Layer {i}", flags);

                    if (ImGui.IsItemClicked())
                    {
                        _paintLayer = i;
                    }

                    if (open)
                    {
                        ImGui.DragFloat($"Scale##{i}", ref layer.scale, 1f, 0f, 100f);

                        if (ImGui.IsItemActivated())
                        {
                            App.Instance!.BeginRecordUndo("Modify Terrain Layer Scale", () => {
                                layer.scale = oldScale;
                                layers[idx] = layer;
                            });
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            App.Instance!.EndRecordUndo(() => {
                                layers[idx] = layer;
                            });
                        }
                        
                        ImGui.InputText($"Diffuse##{i}", ref layer.diffusePath, 1024);

                        if (ImGui.IsItemActivated())
                        {
                            App.Instance!.BeginRecordUndo("Modify Terrain Layer Diffuse", () => {
                                layer.diffusePath = oldDiffusePath;
                                layers[idx] = layer;
                            });
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            App.Instance!.EndRecordUndo(() => {
                                layers[idx] = layer;
                            });
                        }

                        ImGui.SameLine();
                        if (ImGui.Button($"Browse##diffuse{i}"))
                        {
                            if (BrowseTexture() is string path)
                            {
                                App.Instance!.BeginRecordUndo("Modify Terrain Layer Diffuse", () => {
                                    layer.diffusePath = oldDiffusePath;
                                    layers[idx] = layer;
                                });
                                
                                App.Instance!.EndRecordUndo(() => {
                                    layer.diffusePath = path;
                                    layers[idx] = layer;
                                });
                            }
                        }

                        ImGui.InputText($"Normal##{i}", ref layer.normalPath, 1024);

                        if (ImGui.IsItemActivated())
                        {
                            App.Instance!.BeginRecordUndo("Modify Terrain Layer Normal", () => {
                                layer.normalPath = oldNormalPath;
                                layers[idx] = layer;
                            });
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            App.Instance!.EndRecordUndo(() => {
                                layers[idx] = layer;
                            });
                        }

                        ImGui.SameLine();
                        if (ImGui.Button($"Browse##normal{i}"))
                        {
                            if (BrowseTexture() is string path)
                            {
                                App.Instance!.BeginRecordUndo("Modify Terrain Layer Normal", () => {
                                    layer.normalPath = oldNormalPath;
                                    layers[idx] = layer;
                                });
                                
                                App.Instance!.EndRecordUndo(() => {
                                    layer.normalPath = path;
                                    layers[idx] = layer;
                                });
                            }
                        }

                        ImGui.InputText($"ORM##{i}", ref layer.ormPath, 1024);

                        if (ImGui.IsItemActivated())
                        {
                            App.Instance!.BeginRecordUndo("Modify Terrain Layer ORM", () => {
                                layer.ormPath = oldORMPath;
                                layers[idx] = layer;
                            });
                        }

                        if (ImGui.IsItemDeactivatedAfterEdit())
                        {
                            App.Instance!.EndRecordUndo(() => {
                                layers[idx] = layer;
                            });
                        }

                        ImGui.SameLine();
                        if (ImGui.Button($"Browse##orm{i}"))
                        {
                            if (BrowseTexture() is string path)
                            {
                                App.Instance!.BeginRecordUndo("Modify Terrain Layer ORM", () => {
                                    layer.ormPath = oldORMPath;
                                    layers[idx] = layer;
                                });
                                
                                App.Instance!.EndRecordUndo(() => {
                                    layer.ormPath = path;
                                    layers[idx] = layer;
                                });
                            }
                        }

                        if (ImGui.Button($"Delete##{i}"))
                        {
                            if (_paintLayer == i)
                            {
                                _paintLayer = 0;
                            }

                            App.Instance!.BeginRecordUndo("Delete Terrain Layer", () => {
                                layers.Insert(idx, layer);
                                UpdateSplatmapLayers();
                            });

                            App.Instance!.EndRecordUndo(() => {
                                layers.RemoveAt(idx);
                                UpdateSplatmapLayers();
                            });

                            i--;
                        }

                        ImGui.TreePop();
                    }
                }

                if (ImGui.Button("Add Layer"))
                {
                    int idx = layers.Count;

                    App.Instance!.BeginRecordUndo("Add Terrain Layer", () => {
                        layers.RemoveAt(idx);
                        UpdateSplatmapLayers();
                    });

                    App.Instance!.EndRecordUndo(() => {
                        layers.Add(new TextureLayer
                        {
                            scale = 1f,
                            diffusePath = "",
                            normalPath = "",
                            ormPath = ""
                        });
                        UpdateSplatmapLayers();
                    });
                }
            }

            if (ImGui.CollapsingHeader("Paint Tools"))
            {
                ImGui.Combo("Brush Tool", ref _brushType, "Raise\0Lower\0Smooth\0Flatten\0Texture Paint");
                ImGui.SliderInt("Brush Size", ref _brushRadius, 1, 200);
                ImGui.SliderFloat("Brush Strength", ref _brushStrength, 0f, 1f);
            }
        }

        public override void DrawScene(RenderSystem renderSystem, Matrix view)
        {
            base.DrawScene(renderSystem, view);

            if (_renderLayers.Length != layers.Count)
            {
                Array.Resize(ref _renderLayers, layers.Count);
            }

            for (int i = 0; i < layers.Count; i++)
            {
                _renderLayers[i].scale = layers[i].scale;
                _renderLayers[i].albedoTexture = GetTexture(layers[i].diffusePath);
                _renderLayers[i].normalTexture = GetTexture(layers[i].normalPath);
                _renderLayers[i].ormTexture = GetTexture(layers[i].ormPath);
            }

            var patch = new RenderSystem.RenderTerrainPatch
            {
                transform = World,
                meshPart = _patchMesh,
                material = new RenderTerrainMaterial
                {
                    terrainSize = terrainScale,
                    terrainHeight = heightScale,
                    uvOffset = Vector2.Zero,
                    uvScale = Vector2.One,
                    heightmap = _heightmap,
                    splatmaps = _splatmaps,
                    layers = _renderLayers
                }
            };
            DrawPatch(renderSystem, view, patch);
        }

        private void UpdateSplatmapLayers()
        {
            // calculate number of splatmaps needed for these texture layers, adjust splatmaps accordingly
            int numSplatmaps = (int)MathF.Ceiling(layers.Count / 4f);

            if (_splatmaps.Length < numSplatmaps)
            {
                int oldLen = _splatmaps.Length;
                Array.Resize(ref _splatmaps, numSplatmaps);
                Array.Resize(ref _splatmapData, numSplatmaps);
                for (int i = oldLen; i < numSplatmaps; i++)
                {
                    _splatmaps[i] = new Texture2D(App.Instance!.GraphicsDevice, heightmapRes, heightmapRes, false, SurfaceFormat.Color);
                    _splatmapData[i] = new Color[heightmapRes * heightmapRes];

                    if (i == 0)
                    {
                        ClearSplatmap(_splatmaps[i], _splatmapData[i], new Color(1f, 0f, 0f, 0f));
                    }
                    else
                    {
                        ClearSplatmap(_splatmaps[i], _splatmapData[i], new Color(0f, 0f, 0f, 0f));
                    }
                }
            }
            else if (_splatmaps.Length > numSplatmaps)
            {
                for (int i = numSplatmaps; i < _splatmaps.Length; i++)
                {
                    _splatmaps[i].Dispose();
                }
                Array.Resize(ref _splatmaps, numSplatmaps);
                Array.Resize(ref _splatmapData, numSplatmaps);
            }
        }

        private void ClearSplatmap(Texture2D splatmap, Color[] data, Color c)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = c;
            }

            splatmap.SetData(data);
        }

        private Texture2D GetTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return _blankTexture;
            }

            if (!_textures.ContainsKey(path))
            {
                string fullpath = Path.Combine(App.Instance!.ContentPath, path);
                try
                {
                    using var texStream = File.OpenRead(fullpath);
                    Texture2D tex = Texture2D.FromStream(App.Instance!.GraphicsDevice, texStream);
                    _textures.Add(path, tex);
                }
                catch {}
            }

            return _textures[path];
        }

        private string? BrowseTexture()
        {
            var result = Dialog.FileOpen("png,jpg,jpeg,dds", App.Instance!.ContentPath);
            if (result.IsOk)
            {
                return Path.GetRelativePath(App.Instance!.ContentPath!, result.Path);
            }

            return null;
        }

        private void DrawPatch(RenderSystem renderSystem, Matrix view, RenderSystem.RenderTerrainPatch patch, int curSubdiv = 0)
        {
            // calculate patch AABB
            Vector3 basisX = Vector3.TransformNormal(Vector3.UnitX, patch.transform) * patch.material.terrainSize;
            Vector3 basisY = Vector3.TransformNormal(Vector3.UnitY, patch.transform) * patch.material.terrainHeight;
            Vector3 basisZ = Vector3.TransformNormal(Vector3.UnitZ, patch.transform) * patch.material.terrainSize;

            Vector3 p00 = patch.transform.Translation;
            Vector3 p01 = p00 + basisX;
            Vector3 p02 = p00 + basisZ;
            Vector3 p03 = p00 + basisX + basisZ;

            Vector3 p10 = p00 + basisY;
            Vector3 p11 = p01 + basisY;
            Vector3 p12 = p02 + basisY;
            Vector3 p13 = p03 + basisY;

            Vector3 min0 = Vector3.Min(p00, Vector3.Min(p01, Vector3.Min(p02, p03)));
            Vector3 min1 = Vector3.Min(p10, Vector3.Min(p11, Vector3.Min(p12, p13)));

            Vector3 max0 = Vector3.Max(p00, Vector3.Max(p01, Vector3.Max(p02, p03)));
            Vector3 max1 = Vector3.Max(p10, Vector3.Max(p11, Vector3.Max(p12, p13)));

            Vector3 min = Vector3.Min(min0, min1);
            Vector3 max = Vector3.Max(max0, max1);

            // get closest point on (or in) AABB
            Vector3 cameraPos = Matrix.Invert(view).Translation;
            Vector3 closestPt = cameraPos;
            closestPt = Vector3.Max(closestPt, min);
            closestPt = Vector3.Min(closestPt, max);

            float dist = Vector3.Distance(cameraPos, closestPt);

            // map distance to subdiv
            dist /= terrainScale * lodDistanceMultiplier;
            dist = 1f - MathHelper.Clamp(dist, 0f, 1f);
            int subdiv = (int)(dist * dist * detail);

            if (subdiv <= curSubdiv)
            {
                renderSystem.SubmitTerrainPatch(patch);
            }
            else
            {
                // split into four patches
                var patch0 = patch;
                var patch1 = patch;
                var patch2 = patch;
                var patch3 = patch;

                Vector2 newUvScale = patch.material.uvScale * 0.5f;
                float newTerrainSize = patch.material.terrainSize * 0.5f;

                patch0.material.uvScale = newUvScale;
                patch1.material.uvScale = newUvScale;
                patch2.material.uvScale = newUvScale;
                patch3.material.uvScale = newUvScale;

                patch0.material.terrainSize = newTerrainSize;
                patch1.material.terrainSize = newTerrainSize;
                patch2.material.terrainSize = newTerrainSize;
                patch3.material.terrainSize = newTerrainSize;

                patch1.material.uvOffset.X += newUvScale.X;
                patch2.material.uvOffset.Y += newUvScale.Y;
                patch3.material.uvOffset += newUvScale;

                patch1.transform = Matrix.CreateTranslation(terrainScale * newUvScale.X, 0f, 0f) * patch0.transform;
                patch2.transform = Matrix.CreateTranslation(0f, 0f, terrainScale * newUvScale.Y) * patch0.transform;
                patch3.transform = Matrix.CreateTranslation(terrainScale * newUvScale.X, 0f, terrainScale * newUvScale.Y) * patch0.transform;

                DrawPatch(renderSystem, view, patch0, curSubdiv + 1);
                DrawPatch(renderSystem, view, patch1, curSubdiv + 1);
                DrawPatch(renderSystem, view, patch2, curSubdiv + 1);
                DrawPatch(renderSystem, view, patch3, curSubdiv + 1);
            }
        }

        private Vector3? Raymarch(Vector3 origin, Vector3 direction, out int px, out int py)
        {
            // calculate terrain AABB
            Vector3 basisX = Vector3.TransformNormal(Vector3.UnitX, World) * terrainScale;
            Vector3 basisY = Vector3.TransformNormal(Vector3.UnitY, World) * heightScale;
            Vector3 basisZ = Vector3.TransformNormal(Vector3.UnitZ, World) * terrainScale;

            Vector3 p00 = World.Translation;
            Vector3 p01 = p00 + basisX;
            Vector3 p02 = p00 + basisZ;
            Vector3 p03 = p00 + basisX + basisZ;

            Vector3 p10 = p00 + basisY;
            Vector3 p11 = p01 + basisY;
            Vector3 p12 = p02 + basisY;
            Vector3 p13 = p03 + basisY;

            Vector3 min0 = Vector3.Min(p00, Vector3.Min(p01, Vector3.Min(p02, p03)));
            Vector3 min1 = Vector3.Min(p10, Vector3.Min(p11, Vector3.Min(p12, p13)));

            Vector3 max0 = Vector3.Max(p00, Vector3.Max(p01, Vector3.Max(p02, p03)));
            Vector3 max1 = Vector3.Max(p10, Vector3.Max(p11, Vector3.Max(p12, p13)));

            Vector3 min = Vector3.Min(min0, min1);
            Vector3 max = Vector3.Max(max0, max1);

            BoundingBox aabb = new BoundingBox(min, max);
            Ray ray = new Ray(origin, direction);

            if (ray.Intersects(aabb) is float enter)
            {
                float step = terrainScale / heightmapRes;
                Vector3 curPos = ray.Position + (ray.Direction * enter);

                // inflate AABB a bit
                aabb.Min -= Vector3.One * step;
                aabb.Max += Vector3.One * step;

                while (aabb.Contains(curPos) != ContainmentType.Disjoint)
                {
                    Vector3 curPosLocal = Vector3.Transform(curPos, Matrix.Invert(World));
                    float u = curPosLocal.X / terrainScale;
                    float v = curPosLocal.Z / terrainScale;
                    px = (int)(u * heightmapRes);
                    py = (int)(v * heightmapRes);

                    if (px >= 0 && px < heightmapRes && py >= 0 && py < heightmapRes)
                    {
                        float height = _heightmapData[px + (py * heightmapRes)] * heightScale;
                        if (height >= curPosLocal.Y)
                        {
                            curPosLocal.X = (float)px / heightmapRes * terrainScale;
                            curPosLocal.Z = (float)py / heightmapRes * terrainScale;
                            curPosLocal.Y = height;
                            return Vector3.Transform(curPosLocal, World);
                        }
                    }

                    curPos += ray.Direction * step;
                }
            }

            px = 0;
            py = 0;
            return null;
        }

        private void BrushSplat(int index, int brushPosX, int brushPosY, int brushRadius, float strength)
        {
            int minX = brushPosX - brushRadius;
            int maxX = brushPosX + brushRadius;

            int minY = brushPosY - brushRadius;
            int maxY = brushPosY + brushRadius;

            if (minX < 0) minX = 0;
            if (maxX >= heightmapRes) maxX = heightmapRes - 1;

            if (minY < 0) minY = 0;
            if (maxY >= heightmapRes) maxY = heightmapRes - 1;

            Vector2 center = new Vector2(brushPosX, brushPosY);

            int targetSplat = index / 4;
            int targetChannel = index % 4;

            for (int j = minY; j <= maxY; j++)
            {
                for (int i = minX; i <= maxX; i++)
                {
                    float falloff = MathHelper.Clamp(Vector2.Distance(new Vector2(i, j), center) / brushRadius, 0f, 1f);
                    falloff = 1f - (falloff * falloff);

                    Color src = _splatmapData[targetSplat][i + (j * heightmapRes)];
                    float ch = 0f;

                    switch (targetChannel)
                    {
                        case 0:
                            ch = src.R / 255.0f;
                            break;
                        case 1:
                            ch = src.G / 255.0f;
                            break;
                        case 2:
                            ch = src.B / 255.0f;
                            break;
                        case 3:
                            ch = src.A / 255.0f;
                            break;
                    }

                    ch += strength * falloff;
                    if (ch > 1f) ch = 1f;

                    switch (targetChannel)
                    {
                        case 0:
                            src.R = (byte)(ch * 255.0f);
                            break;
                        case 1:
                            src.G = (byte)(ch * 255.0f);
                            break;
                        case 2:
                            src.B = (byte)(ch * 255.0f);
                            break;
                        case 3:
                            src.A = (byte)(ch * 255.0f);
                            break;
                    }

                    _splatmapData[targetSplat][i + (j * heightmapRes)] = src;

                    float rem = 1.0f - ch;
                    float sum = 0.0f;

                    // normalize other channels
                    for (int s = 0; s < _splatmapData.Length; s++)
                    {
                        Color c = _splatmapData[s][i + (j * heightmapRes)];

                        if (s == targetSplat)
                        {
                            switch (targetChannel)
                            {
                                case 0:
                                    sum += c.G / 255.0f;
                                    sum += c.B / 255.0f;
                                    sum += c.A / 255.0f;
                                    break;
                                case 1:
                                    sum += c.R / 255.0f;
                                    sum += c.B / 255.0f;
                                    sum += c.A / 255.0f;
                                    break;
                                case 2:
                                    sum += c.R / 255.0f;
                                    sum += c.G / 255.0f;
                                    sum += c.A / 255.0f;
                                    break;
                                case 3:
                                    sum += c.R / 255.0f;
                                    sum += c.G / 255.0f;
                                    sum += c.B / 255.0f;
                                    break;
                            }
                        }
                        else
                        {
                            sum += c.R / 255.0f;
                            sum += c.G / 255.0f;
                            sum += c.B / 255.0f;
                            sum += c.A / 255.0f;
                        }
                    }

                    for (int s = 0; s < _splatmapData.Length; s++)
                    {
                        Color c = _splatmapData[s][i + (j * heightmapRes)];
                        Vector4 v = c.ToVector4();

                        if (s == targetSplat)
                        {
                            switch (targetChannel)
                            {
                                case 0:
                                    v.Y = (v.Y / sum) * rem;
                                    v.Z = (v.Z / sum) * rem;
                                    v.W = (v.W / sum) * rem;
                                    break;
                                case 1:
                                    v.X = (v.X / sum) * rem;
                                    v.Z = (v.Z / sum) * rem;
                                    v.W = (v.W / sum) * rem;
                                    break;
                                case 2:
                                    v.X = (v.X / sum) * rem;
                                    v.Y = (v.Y / sum) * rem;
                                    v.W = (v.W / sum) * rem;
                                    break;
                                case 3:
                                    v.X = (v.X / sum) * rem;
                                    v.Y = (v.Y / sum) * rem;
                                    v.Z = (v.Z / sum) * rem;
                                    break;
                            }
                        }
                        else
                        {
                            v.X = (v.X / sum) * rem;
                            v.Y = (v.Y / sum) * rem;
                            v.Z = (v.Z / sum) * rem;
                            v.W = (v.W / sum) * rem;
                        }

                        _splatmapData[s][i + (j * heightmapRes)] = new Color(v.X, v.Y, v.Z, v.W);
                    }
                }
            }

            for (int s = 0; s < _splatmapData.Length; s++)
            {
                _splatmaps[s].SetData(_splatmapData[s]);
            }
        }

        private void BrushRaise(int brushPosX, int brushPosY, int brushRadius, float strength)
        {
            int minX = brushPosX - brushRadius;
            int maxX = brushPosX + brushRadius;

            int minY = brushPosY - brushRadius;
            int maxY = brushPosY + brushRadius;

            if (minX < 0) minX = 0;
            if (maxX >= heightmapRes) maxX = heightmapRes - 1;

            if (minY < 0) minY = 0;
            if (maxY >= heightmapRes) maxY = heightmapRes - 1;

            Vector2 center = new Vector2(brushPosX, brushPosY);

            for (int j = minY; j <= maxY; j++)
            {
                for (int i = minX; i <= maxX; i++)
                {
                    float falloff = MathHelper.Clamp(Vector2.Distance(new Vector2(i, j), center) / brushRadius, 0f, 1f);
                    falloff = 1f - (falloff * falloff);

                    float h = _heightmapData[i + (j * heightmapRes)];
                    h += falloff * strength;
                    h = MathHelper.Clamp(h, 0f, 1f);
                    _heightmapData[i + (j * heightmapRes)] = h;
                }
            }

            _heightmap.SetData(_heightmapData);
        }

        private void BrushFlatten(int brushPosX, int brushPosY, int brushRadius, float strength)
        {
            int minX = brushPosX - brushRadius;
            int maxX = brushPosX + brushRadius;

            int minY = brushPosY - brushRadius;
            int maxY = brushPosY + brushRadius;

            if (minX < 0) minX = 0;
            if (maxX >= heightmapRes) maxX = heightmapRes - 1;

            if (minY < 0) minY = 0;
            if (maxY >= heightmapRes) maxY = heightmapRes - 1;

            Vector2 center = new Vector2(brushPosX, brushPosY);

            float hCenter = _heightmapData[brushPosX + (brushPosY * heightmapRes)];

            for (int j = minY; j <= maxY; j++)
            {
                for (int i = minX; i <= maxX; i++)
                {
                    float falloff = MathHelper.Clamp(Vector2.Distance(new Vector2(i, j), center) / brushRadius, 0f, 1f);
                    falloff = 1f - (falloff * falloff);

                    float h = _heightmapData[i + (j * heightmapRes)];
                    h = MathHelper.Lerp(h, hCenter, falloff * strength);

                    _heightmapData[i + (j * heightmapRes)] = h;
                }
            }

            _heightmap.SetData(_heightmapData);
        }

        private void BrushSmooth(int brushPosX, int brushPosY, int brushRadius, float strength)
        {
            int minX = brushPosX - brushRadius;
            int maxX = brushPosX + brushRadius;

            int minY = brushPosY - brushRadius;
            int maxY = brushPosY + brushRadius;

            if (minX < 0) minX = 0;
            if (maxX >= heightmapRes) maxX = heightmapRes - 1;

            if (minY < 0) minY = 0;
            if (maxY >= heightmapRes) maxY = heightmapRes - 1;

            Vector2 center = new Vector2(brushPosX, brushPosY);

            for (int j = minY; j <= maxY; j++)
            {
                for (int i = minX; i <= maxX; i++)
                {
                    float falloff = MathHelper.Clamp(Vector2.Distance(new Vector2(i, j), center) / brushRadius, 0f, 1f);
                    falloff = 1f - (falloff * falloff);

                    float h = (float)_heightmapData[i + (j * heightmapRes)];
                    
                    float avgH = 0.0f;
                    float sum = 0.0f;

                    for (int ky = -2; ky <= 2; ky++)
                    {
                        int offsY = j + ky;
                        if (offsY < 0) continue;
                        if (offsY >= heightmapRes) continue;

                        for (int kx = -2; kx <= 2; kx++)
                        {
                            int offsX = i + kx;
                            if (offsX < 0) continue;
                            if (offsX >= heightmapRes) continue;

                            avgH += _heightmapData[offsX + (offsY * heightmapRes)];
                            sum += 1.0f;
                        }
                    }

                    h = MathHelper.Lerp(h, avgH / sum, falloff * strength);

                    _heightmapDataTemp[i + (j * heightmapRes)] = h;
                }
            }

            for (int j = minY; j <= maxY; j++)
            {
                for (int i = minX; i <= maxX; i++)
                {
                    _heightmapData[i + (j * heightmapRes)] = _heightmapDataTemp[i + (j * heightmapRes)];
                }
            }

            _heightmap.SetData(_heightmapData);
        }

        private void ImportSplatmap(Stream inSteam, Color[] dst)
        {
            using var reader = new BinaryReader(inSteam);

            for (int i = 0; i < dst.Length; i++)
            {
                byte r = reader.ReadByte();
                byte g = reader.ReadByte();
                byte b = reader.ReadByte();
                byte a = reader.ReadByte();

                dst[i] = new Color(r, g, b, a);
            }
        }

        private void ExportSplatmap(Stream outSteam, Color[] src)
        {
            using var writer = new BinaryWriter(outSteam);

            for (int i = 0; i < src.Length; i++)
            {
                writer.Write(src[i].R);
                writer.Write(src[i].G);
                writer.Write(src[i].B);
                writer.Write(src[i].A);
            }
        }

        private void ImportRaw16(Stream inStream)
        {
            using var reader = new BinaryReader(inStream);

            for (int j = 0; j < heightmapRes; j++)
            {
                for (int i = 0; i < heightmapRes; i++)
                {
                    ushort h = reader.ReadUInt16();
                    _heightmapData[i + (j * heightmapRes)] = h / 65535.0f;
                }
            }

            _heightmap.SetData(_heightmapData);
        }

        private void ImportRaw32(Stream inStream)
        {
            using var reader = new BinaryReader(inStream);

            for (int j = 0; j < heightmapRes; j++)
            {
                for (int i = 0; i < heightmapRes; i++)
                {
                    uint h = reader.ReadUInt32();
                    _heightmapData[i + (j * heightmapRes)] = h / 4294967295.0f;
                }
            }

            _heightmap.SetData(_heightmapData);
        }

        private void ImportRawFP16(Stream inStream, bool normalize = true)
        {
            using var reader = new BinaryReader(inStream);

            float min = 0f;
            float max = 1f;

            for (int j = 0; j < heightmapRes; j++)
            {
                for (int i = 0; i < heightmapRes; i++)
                {
                    float h = (float)reader.ReadHalf();
                    min = MathHelper.Min(h, min);
                    max = MathHelper.Max(h, max);
                    _heightmapData[i + (j * heightmapRes)] = h;
                }
            }

            // normalize height data
            if (normalize && (min < 0f || max > 1f))
            {
                for (int i = 0; i < _heightmapData.Length; i++)
                {
                    _heightmapData[i] = (_heightmapData[i] - min) / (max - min);
                }
            }

            _heightmap.SetData(_heightmapData);
        }

        private void ImportRawFP32(Stream inStream, bool normalize = true)
        {
            using var reader = new BinaryReader(inStream);

            float min = 0f;
            float max = 1f;

            for (int j = 0; j < heightmapRes; j++)
            {
                for (int i = 0; i < heightmapRes; i++)
                {
                    float h = reader.ReadSingle();
                    min = MathHelper.Min(h, min);
                    max = MathHelper.Max(h, max);
                    _heightmapData[i + (j * heightmapRes)] = h;
                }
            }

            // normalize height data
            if (normalize && (min < 0f || max > 1f))
            {
                for (int i = 0; i < _heightmapData.Length; i++)
                {
                    _heightmapData[i] = (_heightmapData[i] - min) / (max - min);
                }
            }

            _heightmap.SetData(_heightmapData);
        }
    }
}