using ImGuiNET;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace OpenWorldBuilder
{
    /// <summary>
    /// Base class for the root node containing the entire level
    /// </summary>
    [SerializedNode("SceneRootNode")]
    public class SceneRootNode : Node
    {
        [JsonProperty]
        public Color ambientColor = Color.Black;
        
        [JsonProperty]
        public float ambientIntensity = 1f;

        public override void DrawHandles(Matrix view, Matrix projection, ViewportWindow viewport, bool localSpace)
        {
        }

        public override void DrawInspector()
        {
            string prevName = name;
            ImGui.InputText("Name", ref name, 1024);

            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Change Scene Name", () =>
                {
                    name = prevName;
                });
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                App.Instance!.EndRecordUndo(() =>
                {
                    name = prevName;
                });
            }

            var prevAmbientColor = ambientColor;
            var prevAmbientIntensity = ambientIntensity;

            ImGuiExt.ColorEdit3("Ambient Color", ref ambientColor);

            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Change Scene Ambient Color", () =>
                {
                    ambientColor = prevAmbientColor;
                });
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                App.Instance!.EndRecordUndo(() =>
                {
                    ambientColor = prevAmbientColor;
                });
            }

            ImGui.DragFloat("Ambient Intensity", ref ambientIntensity);

            if (ImGui.IsItemActivated())
            {
                App.Instance!.BeginRecordUndo("Change Scene Ambient Intensity", () =>
                {
                    ambientIntensity = prevAmbientIntensity;
                });
            }

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                App.Instance!.EndRecordUndo(() =>
                {
                    ambientIntensity = prevAmbientIntensity;
                });
            }
        }
    }
}