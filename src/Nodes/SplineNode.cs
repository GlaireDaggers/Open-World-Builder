using System.Security.Principal;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{
    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("SplineNode")]
    public class SplineNode : Node
    {
        public struct ControlPoint
        {
            public Vector3 position;
            public Quaternion rotation;
            public float scale;
        }

        [JsonProperty]
        public List<ControlPoint> points = new List<ControlPoint>();

        [JsonProperty]
        public bool closed = false;

        public SplineNode()
        {
            points.Add(new ControlPoint
            {
                position = -Vector3.UnitZ,
                rotation = Quaternion.Identity,
                scale = 1f
            });

            points.Add(new ControlPoint
            {
                position = Vector3.UnitZ,
                rotation = Quaternion.Identity,
                scale = 1f
            });
        }

        public override void DrawInspector()
        {
            base.DrawInspector();

            ImGui.Spacing();

            ImGui.Checkbox("Closed", ref closed);

            ImGui.Text("Control Points: " + points.Count);

            for (int i = 0; i < points.Count; i++)
            {
                var cp = points[i];
                ImGui.Text("CP " + i);

                if (points.Count > 1)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Delete##cp" + i))
                    {
                        points.RemoveAt(i--);
                        continue;
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Duplicate##cp" + i))
                {
                    Matrix mat = Matrix.CreateFromQuaternion(cp.rotation);
                    var fw = Vector3.TransformNormal(Vector3.UnitZ, mat);
                    ControlPoint newCp = cp;
                    newCp.position += fw;
                    points.Insert(i + 1, newCp);
                }

                ImGuiExt.DragFloat3("Position##cp" + i, ref cp.position);
                
                Vector3 euler = MathUtils.ToEulerAngles(cp.rotation);
                euler = MathUtils.ToDegrees(euler);
                if (ImGuiExt.DragFloat3("Rotation##cp" + i, ref euler))
                {
                    euler = MathUtils.ToRadians(euler);
                    cp.rotation = MathUtils.ToQuaternion(euler);
                }

                ImGui.DragFloat("Scale##cp" + i, ref cp.scale);

                if (cp.scale < 0f)
                {
                    cp.scale = 0f;
                }
                
                points[i] = cp;
            }
        }

        public override void Draw(Matrix view, Matrix projection, ViewportWindow viewport, bool selected)
        {
            base.Draw(view, projection, viewport, selected);

            var trs = World;

            if (points.Count > 0)
            {
                // draw each segment of the curve
                for (int i = 0; i < points.Count - 1; i++)
                {
                    var cp0 = points[i];
                    var cp1 = points[i + 1];

                    DrawCurveSegment(cp0, cp1, trs, viewport);
                }

                if (closed)
                {
                    DrawCurveSegment(points[points.Count - 1], points[0], trs, viewport);
                }
            }
        }

        private void DrawCurveSegment(ControlPoint cp0, ControlPoint cp1, Matrix trs, ViewportWindow viewport)
        {
            Matrix mat0 = Matrix.CreateFromQuaternion(cp0.rotation) * Matrix.CreateTranslation(cp0.position) * trs;
            Matrix mat1 = Matrix.CreateFromQuaternion(cp1.rotation) * Matrix.CreateTranslation(cp1.position) * trs;

            var fw0 = Vector3.TransformNormal(Vector3.UnitZ, mat0);
            var fw1 = Vector3.TransformNormal(Vector3.UnitZ, mat1);

            var pt0 = mat0.Translation;
            var pt1 = pt0 + (fw0 * cp0.scale);
            var pt3 = mat1.Translation;
            var pt2 = pt3 - (fw1 * cp1.scale);
            
            viewport.DrawLineGizmo(pt0, pt1, Color.Gray, Color.Gray);
            viewport.DrawLineGizmo(pt2, pt3, Color.Gray, Color.Gray);

            var prevPt = pt0;
            for (int j = 1; j <= 32; j++)
            {
                float t = j / 32f;
                var q0 = Vector3.Lerp(pt0, pt1, t);
                var q1 = Vector3.Lerp(pt1, pt2, t);
                var q2 = Vector3.Lerp(pt2, pt3, t);
                var r0 = Vector3.Lerp(q0, q1, t);
                var r1 = Vector3.Lerp(q1, q2, t);
                var pt = Vector3.Lerp(r0, r1, t);
                viewport.DrawLineGizmo(prevPt, pt, Color.Yellow, Color.Yellow);
                prevPt = pt;
            }
        }

        public override void DrawHandles(Matrix view, Matrix projection, ViewportWindow viewport, bool localSpace)
        {
            base.DrawHandles(view, projection, viewport, localSpace);

            for (int i = 0; i < points.Count; i++)
            {
                var pt = points[i];
                Vector3 sc = Vector3.One * pt.scale;
                viewport.GlobalTransformHandle(ref pt.position, ref pt.rotation, ref sc, World, localSpace);
                pt.scale = MathF.Max(MathF.Max(sc.X, sc.Y), sc.Z);
                points[i] = pt;
            }
        }
    }
}