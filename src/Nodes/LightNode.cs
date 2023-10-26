﻿using ImGuiNET;

using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{
    public enum LightType
    {
        Directional,
        Point,
        Spot
    }

    [JsonObject(MemberSerialization.OptIn)]
    [SerializedNode("LightNode")]
    public class LightNode : Node
    {
        private static string[] lightTypeNames = new string[]
        {
            "Directional",
            "Point",
            "Spot"
        };

        [JsonProperty]
        public LightType lightType = LightType.Point;

        [JsonProperty]
        public Color color = Color.White;

        [JsonProperty]
        public float intensity = 1f;

        [JsonProperty]
        public float radius = 10f;

        [JsonProperty]
        public float innerConeAngle = 0f;

        [JsonProperty]
        public float outerConeAngle = 45f;

        public override void DrawInspector()
        {
            base.DrawInspector();

            ImGui.Spacing();

            int lightTypeIdx = (int)lightType;
            if (ImGui.Combo("Light Type", ref lightTypeIdx, lightTypeNames, 3))
            {
                lightType = (LightType)lightTypeIdx;
            }

            ImGuiExt.ColorEdit3("Color", ref color);
            ImGui.DragFloat("Intensity", ref intensity);
            ImGui.DragFloat("Radius", ref radius);
            ImGui.DragFloat("Inner Cone Angle", ref innerConeAngle);
            ImGui.DragFloat("Outer Cone Angle", ref outerConeAngle);

            if (intensity < 0f)
            {
                intensity = 0f;
            }

            if (radius < 0f)
            {
                radius = 0f;
            }

            if (innerConeAngle < 0f)
            {
                innerConeAngle = 0f;
            }

            if (outerConeAngle < innerConeAngle)
            {
                outerConeAngle = innerConeAngle;
            }
        }

        public override void Draw(Matrix view, Matrix projection, ViewportWindow viewport, bool selected)
        {
            base.Draw(view, projection, viewport, selected);

            Matrix trs = World;
            trs.Decompose(out _, out var worldRot, out var worldPos);

            Vector3 fwd = Vector3.TransformNormal(Vector3.UnitZ, Matrix.CreateFromQuaternion(worldRot));

            switch (lightType)
            {
                case LightType.Directional:
                    viewport.DrawLineGizmo(worldPos, worldPos + (fwd * 10f), color, color);
                    break;
                case LightType.Point:
                    viewport.DrawSphereGizmo(worldPos, radius, color);
                    break;
                case LightType.Spot:
                    float coneRadAtDistInner = radius * MathF.Tan(MathHelper.ToRadians(innerConeAngle));
                    float coneRadAtDistOuter = radius * MathF.Tan(MathHelper.ToRadians(outerConeAngle));

                    viewport.DrawLineGizmo(worldPos, worldPos + (fwd * radius), color, color);
                    
                    viewport.DrawCircleGizmo(worldPos + (fwd * radius), coneRadAtDistInner,
                        worldRot * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90f)),
                        color);
                    viewport.DrawCircleGizmo(worldPos + (fwd * radius), coneRadAtDistOuter,
                        worldRot * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(90f)),
                        color);
                    break;
            }
        }
    }
}
