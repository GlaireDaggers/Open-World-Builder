using System.Collections.ObjectModel;
using System.Diagnostics;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    /// <summary>
    /// Base class for a node in a scene hierarchy
    /// </summary>
    public class Node
    {
        public string name = "Node";
        public Vector3 position = Vector3.Zero;
        public Quaternion rotation = Quaternion.Identity;
        public Vector3 scale = Vector3.One;

        /// <summary>
        /// Node's parent, if any
        /// </summary>
        public Node? Parent { get; private set; }

        /// <summary>
        /// Node's children
        /// </summary>
        public ReadOnlyCollection<Node> Children => _children.AsReadOnly();

        private List<Node> _children = new List<Node>();

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

        public virtual void Draw(Matrix view, Matrix projection)
        {
        }

        public virtual void DrawInspector()
        {
            ImGui.InputText("Name", ref name, 1024);
            ImGui.Spacing();
            ImGuiExt.DragFloat3("Position", ref position);

            Vector3 euler = ToEulerAngles(rotation);
            euler = ToDegrees(euler);
            if (ImGuiExt.DragFloat3("Rotation", ref euler))
            {
                euler = ToRadians(euler);
                rotation = ToQuaternion(euler);
            }

            ImGuiExt.DragFloat3("Scale", ref scale);
        }

        Quaternion ToQuaternion(Vector3 v)
        {
            float cy = (float)Math.Cos(v.Z * 0.5);
            float sy = (float)Math.Sin(v.Z * 0.5);
            float cp = (float)Math.Cos(v.Y * 0.5);
            float sp = (float)Math.Sin(v.Y * 0.5);
            float cr = (float)Math.Cos(v.X * 0.5);
            float sr = (float)Math.Sin(v.X * 0.5);

            return new Quaternion
            {
                W = (cr * cp * cy + sr * sp * sy),
                X = (sr * cp * cy - cr * sp * sy),
                Y = (cr * sp * cy + sr * cp * sy),
                Z = (cr * cp * sy - sr * sp * cy)
            };
        }

        Vector3 ToRadians(Vector3 v)
        {
            return new Vector3(
                MathHelper.ToRadians(v.X),
                MathHelper.ToRadians(v.Y),
                MathHelper.ToRadians(v.Z)
            );
        }

        Vector3 ToDegrees(Vector3 v)
        {
            return new Vector3(
                MathHelper.ToDegrees(v.X),
                MathHelper.ToDegrees(v.Y),
                MathHelper.ToDegrees(v.Z)
            );
        }

        Vector3 ToEulerAngles(Quaternion q)
        {
            Vector3 angles = new();

            // roll / x
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.Y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }
    }

    /// <summary>
    /// Base class for the root node containing the entire level
    /// </summary>
    public class SceneRootNode : Node
    {
        public override void DrawInspector()
        {
            ImGui.InputText("Name", ref name, 1024);
        }
    }
}