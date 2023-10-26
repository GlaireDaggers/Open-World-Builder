using System.Collections.ObjectModel;
using System.Diagnostics;
using ImGuiNET;
using ImGuizmoNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{
    /// <summary>
    /// Base class for a node in a scene hierarchy
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("Node")]
    public class Node : IDisposable
    {
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

        public virtual void Draw(Matrix view, Matrix projection, ViewportWindow viewport)
        {
        }

        public virtual void DrawHandles(Matrix view, Matrix projection, ViewportWindow viewport, bool localSpace)
        {
            Matrix parentMatrix = Parent?.World ?? Matrix.Identity;

            viewport.GlobalTransformHandle(ref position, ref rotation,
                ref scale, parentMatrix, localSpace);
        }

        public virtual void DrawInspector()
        {
            ImGui.InputText("Name", ref name, 1024);
            ImGui.Spacing();
            ImGuiExt.DragFloat3("Position", ref position);

            Vector3 euler = MathUtils.ToEulerAngles(rotation);
            euler = MathUtils.ToDegrees(euler);
            if (ImGuiExt.DragFloat3("Rotation", ref euler))
            {
                euler = MathUtils.ToRadians(euler);
                rotation = MathUtils.ToQuaternion(euler);
            }

            ImGuiExt.DragFloat3("Scale", ref scale);
        }
    }

    /// <summary>
    /// Base class for the root node containing the entire level
    /// </summary>
    [SerializedNode("SceneRootNode")]
    public class SceneRootNode : Node
    {
        public override void DrawHandles(Matrix view, Matrix projection, ViewportWindow viewport, bool localSpace)
        {
        }

        public override void DrawInspector()
        {
            ImGui.InputText("Name", ref name, 1024);
        }
    }
}