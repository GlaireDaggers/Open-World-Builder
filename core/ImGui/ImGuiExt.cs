using ImGuiNET;
using Microsoft.Xna.Framework;

namespace OpenWorldBuilder
{
    public class ImGuiExt
    {
        public static bool ColorEdit3(ReadOnlySpan<char> label, ref Color c)
        {
            var vec = new System.Numerics.Vector3(c.R / 255f, c.G / 255f, c.B / 255f);
            bool ret = ImGui.ColorEdit3(label, ref vec);
            c = new Color(vec.X, vec.Y, vec.Z);

            return ret;
        }

        public static bool ColorEdit4(ReadOnlySpan<char> label, ref Color c)
        {
            var vec = new System.Numerics.Vector4(c.R / 255f, c.G / 255f, c.B / 255f, c.A / 255f);
            bool ret = ImGui.ColorEdit4(label, ref vec);
            c = new Color(vec.X, vec.Y, vec.Z, vec.W);

            return ret;
        }

        public static bool ColorEdit3(ReadOnlySpan<char> label, ref Vector3 c)
        {
            var vec = new System.Numerics.Vector3(c.X, c.Y, c.Z);
            bool ret = ImGui.ColorEdit3(label, ref vec);
            c = new Vector3(vec.X, vec.Y, vec.Z);

            return ret;
        }

        public static bool ColorEdit4(ReadOnlySpan<char> label, ref Vector4 c)
        {
            var vec = new System.Numerics.Vector4(c.X, c.Y, c.Z, c.W);
            bool ret = ImGui.ColorEdit4(label, ref vec);
            c = new Vector4(vec.X, vec.Y, vec.Z, vec.W);

            return ret;
        }

        public static bool DragFloat2(ReadOnlySpan<char> label, ref Vector2 v)
        {
            var vec = new System.Numerics.Vector2(v.X, v.Y);
            bool ret = ImGui.DragFloat2(label, ref vec);
            v = new Vector2(vec.X, vec.Y);

            return ret;
        }

        public static bool DragFloat3(ReadOnlySpan<char> label, ref Vector3 v)
        {
            var vec = new System.Numerics.Vector3(v.X, v.Y, v.Z);
            bool ret = ImGui.DragFloat3(label, ref vec);
            v = new Vector3(vec.X, vec.Y, vec.Z);

            return ret;
        }

        public static bool DragFloat4(ReadOnlySpan<char> label, ref Vector4 v)
        {
            var vec = new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
            bool ret = ImGui.DragFloat4(label, ref vec);
            v = new Vector4(vec.X, vec.Y, vec.Z, vec.W);

            return ret;
        }

        public static bool InputFloat2(ReadOnlySpan<char> label, ref Vector2 v)
        {
            var vec = new System.Numerics.Vector2(v.X, v.Y);
            bool ret = ImGui.InputFloat2(label, ref vec);
            v = new Vector2(vec.X, vec.Y);

            return ret;
        }

        public static bool InputFloat3(ReadOnlySpan<char> label, ref Vector3 v)
        {
            var vec = new System.Numerics.Vector3(v.X, v.Y, v.Z);
            bool ret = ImGui.InputFloat3(label, ref vec);
            v = new Vector3(vec.X, vec.Y, vec.Z);

            return ret;
        }

        public static bool InputFloat4(ReadOnlySpan<char> label, ref Vector4 v)
        {
            var vec = new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
            bool ret = ImGui.InputFloat4(label, ref vec);
            v = new Vector4(vec.X, vec.Y, vec.Z, vec.W);

            return ret;
        }
    }
}