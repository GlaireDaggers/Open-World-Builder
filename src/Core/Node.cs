using System.Collections.ObjectModel;
using System.Diagnostics;
using ImGuiNET;
using ImGuizmoNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace OpenWorldBuilder
{
    /// <summary>
    /// Base class for a node in a scene hierarchy
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("Node")]
    public class Node : IDisposable
    {
        public SceneRootNode? Scene
        {
            get
            {
                if (this is SceneRootNode scene) return scene;
                return Parent?.Scene;
            }
        }

        [JsonProperty]
        public Guid guid = Guid.NewGuid();

        [JsonProperty]
        public string name = "Node";

        [JsonProperty]
        public Vector3 position = Vector3.Zero;

        [JsonProperty]
        public Quaternion rotation = Quaternion.Identity;

        [JsonProperty]
        public Vector3 scale = Vector3.One;

        public Matrix World
        {
            get
            {
                // kinda stupid but: BasicEffect will *crash* in debug mode if scale on any axis is 0
                Vector3 sc = scale;
                if (sc.X == 0.0f)
                {
                    sc.X = 0.001f;
                }
                if (sc.Y == 0.0f)
                {
                    sc.Y = 0.001f;
                }
                if (sc.Z == 0.0f)
                {
                    sc.Z = 0.001f;
                }

                Matrix trs = Matrix.CreateScale(sc) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(position);
                if (Parent != null)
                {
                    trs *= Parent.World;
                }

                return trs;
            }
        }

        /// <summary>
        /// Node's parent, if any
        /// </summary>
        public Node? Parent { get; private set; }

        /// <summary>
        /// Node's children
        /// </summary>
        public ReadOnlyCollection<Node> Children => _children.AsReadOnly();

        private List<Node> _children = new List<Node>();

        public Node? FindChildByGuid(Guid guid)
        {
            foreach (var child in _children)
            {
                if (child.guid == guid)
                {
                    return child; 
                }
                else if (child.FindChildByGuid(guid) is Node match)
                {
                    return match;
                }
            }

            return null;
        }

        public virtual void OnLoad()
        {
            foreach (var child in _children)
            {
                child.OnLoad();
            }
        }

        public virtual void Dispose()
        {
            foreach (var child in _children)
            {
                child.Dispose();
            }
        }

        public virtual void OnSerialize(JObject jObject)
        {
        }

        public virtual void OnDeserialize(JObject jObject)
        {
        }

        public void AddChild(Node child)
        {
            Debug.Assert(child != this);
            Debug.Assert(child.Parent == null);

            _children.Add(child);
            child.Parent = this;
        }

        public void RemoveChild(Node child)
        {
            Debug.Assert(child.Parent == this);
            _children.Remove(child);
            child.Parent = null;
        }

        public virtual void Draw(Matrix view, Matrix projection, ViewportWindow viewport, bool selected)
        {
        }

        public virtual void DrawHandles(Matrix view, Matrix projection, ViewportWindow viewport, bool localSpace)
        {
            Matrix parentMatrix = Parent?.World ?? Matrix.Identity;

            Vector3 prevPos = position;
            Quaternion prevRot = rotation;
            Vector3 prevScale = scale;

            viewport.GlobalTransformHandle(ref position, ref rotation,
                ref scale, parentMatrix, localSpace, () => {
                    App.Instance!.BeginRecordUndo("Transform Node", () => {
                        position = prevPos;
                        rotation = prevRot;
                        scale = prevScale;
                    });
                }, () => {
                    App.Instance!.EndRecordUndo(() => {
                        position = prevPos;
                        rotation = prevRot;
                        scale = prevScale;
                    });
                });
        }

        public virtual void DrawInspector()
        {
            ImGui.TextDisabled($"{guid}");

            string prevName = name;
            Vector3 prevPos = position;
            Quaternion prevRot = rotation;
            Vector3 prevScale = scale;

            ImGui.InputText("Name", ref name, 1024);
            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Rename Node", () => {
                    name = prevName;
                });
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                App.Instance!.EndRecordUndo(() => {
                    name = prevName;
                });
            }

            ImGui.Spacing();
            ImGuiExt.DragFloat3("Position", ref position);
            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Change Node Position", () => {
                    position = prevPos;
                });
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                App.Instance!.EndRecordUndo(() => {
                    position = prevPos;
                });
            }

            Vector3 euler = MathUtils.ToEulerAngles(rotation);
            euler = MathUtils.ToDegrees(euler);
            if (ImGuiExt.DragFloat3("Rotation", ref euler))
            {
                euler = MathUtils.ToRadians(euler);
                rotation = MathUtils.ToQuaternion(euler);
            }
            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Change Node Rotation", () => {
                    rotation = prevRot;
                });
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                App.Instance!.EndRecordUndo(() => {
                    rotation = prevRot;
                });
            }

            ImGuiExt.DragFloat3("Scale", ref scale);
            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Change Node Scale", () => {
                    scale = prevScale;
                });
            }
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                App.Instance!.EndRecordUndo(() => {
                    scale = prevScale;
                });
            }
        }
    }
}