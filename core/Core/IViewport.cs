using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OpenWorldBuilder
{
    public interface IViewport
    {
        void GlobalTransformHandle(ref Vector3 position, ref Quaternion rotation,
                ref Vector3 scale, Matrix parentMatrix, bool localSpace);
        void PositionHandle(ref Vector3 position, Quaternion rotation, Matrix parentMatrix, bool localSpace);
        void RotationHandle(ref Quaternion rotation, Vector3 position, Matrix parentMatrix, bool localSpace);
        void ScaleHandle(ref Vector3 scale, Quaternion rotation, Vector3 position, Matrix parentMatrix, bool localSpace);
        void DrawWireframeMeshGizmo(Matrix transform, IndexBuffer ib, VertexBuffer vb, Color color);
        void DrawLineGizmo(Vector3 start, Vector3 end, Color color1, Color color2);
        void DrawCircleGizmo(Vector3 center, float radius, Quaternion rotation, Color color);
        void DrawSphereGizmo(Vector3 center, float radius, Color color);
    }
}